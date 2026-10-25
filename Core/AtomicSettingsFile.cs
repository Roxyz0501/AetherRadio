namespace AetherRadio.Core;

/// <summary>One caller writes at a time. Backups are immutable previous primary files.</summary>
public sealed class AtomicSettingsFile<T>(string path, Func<byte[], T> decode, Func<T, byte[]> encode,
    Action<string>? beforeStage = null)
{
    public sealed record Loaded(T Value, byte[]? Baseline, bool Recovered);
    public long LogicalBytesWritten { get; private set; }
    public Exception? CleanupError { get; private set; }
    public int Commits { get; private set; }
    public string Path { get; } = System.IO.Path.GetFullPath(path);
    private string DirectoryPath => System.IO.Path.GetDirectoryName(Path)!;
    private string BackupPattern => System.IO.Path.GetFileName(Path) + ".backup-*.json";
    public byte[] Encode(T value) => encode(value);

    private byte[] Read(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 16 * 1024 * 1024) throw new InvalidDataException("Settings exceed the 16 MiB read limit.");
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes); return bytes;
    }
    private string[] Backups() => Directory.Exists(DirectoryPath)
        ? Directory.GetFiles(DirectoryPath, BackupPattern).OrderByDescending(x => x, StringComparer.Ordinal).ToArray() : [];
    public Loaded Load(Func<T> defaults)
    {
        if (File.Exists(Path))
        {
            // Invalid data includes oversized files; permission/sharing errors still propagate.
            try { var value = decode(Read(Path)); return new(value, encode(value), false); }
            catch (InvalidDataException) { }
        }
        foreach (var backup in Backups())
        {
            try { var value = decode(Read(backup)); return new(value, null, true); }
            catch (InvalidDataException) { } // Preserve invalid backups, never promote them.
        }
        if (File.Exists(Path) || Backups().Length != 0)
            throw new InvalidDataException("No valid BGMPlayer settings or backup. Original files were preserved.");
        return new(defaults(), null, false);
    }
    public void Write(byte[] bytes)
    {
        _ = decode(bytes);
        // Resolve a retry after an uncertain commit without writing or rotating again.
        if (bytes.Length > 16 * 1024 * 1024) throw new InvalidDataException("Settings exceed the 16 MiB read limit.");
        if (File.Exists(Path))
        {
            try { if (Read(Path).AsSpan().SequenceEqual(bytes)) return; }
            catch (InvalidDataException) { } // Preserve an oversized primary during repair.
        }
        Directory.CreateDirectory(DirectoryPath);
        var temp = Path + ".pending-" + Guid.NewGuid().ToString("N");
        try
        {
            beforeStage?.Invoke("write");
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes); LogicalBytesWritten += bytes.Length;
                beforeStage?.Invoke("flush"); file.Flush(true);
            }
            _ = decode(Read(temp));
            string? previous = null;
            if (File.Exists(Path))
            {
                var valid = true;
                try { _ = decode(Read(Path)); } catch (InvalidDataException) { valid = false; }
                // Sequence numbers preserve ordering if wall-clock time moves backwards.
                var backups = Backups();
                var sequence = backups.Select(x => System.IO.Path.GetFileName(x).Split(".backup-")[1].Split('-')[0])
                    .Select(x => long.TryParse(x, out var n) ? n : 0).DefaultIfEmpty().Max() + 1;
                previous = valid ? Path + $".backup-{sequence:D20}-{Guid.NewGuid():N}.json"
                    : Path + $".corrupt-{Guid.NewGuid():N}.json";
            }
            beforeStage?.Invoke("replace");
            if (previous != null) File.Replace(temp, Path, previous);
            else File.Move(temp, Path);
            Commits++;
            // Only prune after committing. A failed replacement cannot rotate generations.
            // Cleanup failure does not turn a committed save into a retry of the payload.
            CleanupBackups();
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    public void CleanupBackups()
    {
        CleanupError = null;
        try
        {
            var validCount = 0;
            foreach (var backup in Backups())
            {
                try
                {
                    _ = decode(Read(backup));
                    if (++validCount > 3) File.Delete(backup);
                }
                catch (InvalidDataException) { }
                catch (NotSupportedException) { } // Preserve backups from a newer schema.
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { CleanupError = e; }
    }
}
