using System.Text;
using AetherRadio;
using AetherRadio.Core;

static class PersistenceTests
{
    public static void Run(Action<bool, string> check)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../work/persistence-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        AtomicSettingsFile<Configuration> FileFor(string name, Action<string>? fault = null) => new(Path.Combine(root, name + ".json"), ConfigurationCodec.Decode, ConfigurationCodec.Encode, fault);
        var config = new Configuration { Language = "ja", Favorites = [4, 2], Playlists = [new() { Name = "合成", Tracks = [1, 2] }] };
        var file = FileFor("cadence"); file.Write(ConfigurationCodec.Encode(config));
        long now = 0; var errors = 0;
        using var settings = new DeferredSettings<Configuration>(file, () => config, file.Load(() => new()).Baseline, false, _ => errors++, () => now);
        var initialBytes = file.LogicalBytesWritten; var commits = file.Commits;
        for (var i = 0; i < 10; i++) { now += 60_000; settings.Tick(); }
        check(file.Commits == commits && settings.SerializationCount == 0, "Persistence: unchanged periods perform zero writes and zero serialization");
        for (var i = 0; i < 600; i++) { config.VolumePercent = i % 201; settings.MarkDirty(); now += 16; settings.Tick(); }
        check(file.Commits == commits, "Persistence: 600 drag changes do not write before the interval");
        now += 60_000; settings.Tick(); settings.Flush();
        check(file.Commits == commits + 1 && file.Load(() => new()).Value.VolumePercent == config.VolumePercent, "Persistence: a continuous edit saves once at the boundary");
        var savedVolume = config.VolumePercent;
        config.VolumePercent = 7; settings.MarkDirty(); config.VolumePercent = savedVolume; settings.MarkDirty();
        config.Favorites.Remove(4); config.Favorites.Add(4); settings.MarkDirty();
        now += 60_000; settings.Tick(); settings.Flush();
        check(file.Commits == commits + 1, "Persistence: reverted values and favorite-set order cause zero writes");
        config.MiniPosition = new(41, 52); settings.MarkDirty(); settings.Flush(); settings.Flush();
        check(file.Commits == commits + 2 && file.Load(() => new()).Value.MiniPosition == config.MiniPosition, "Persistence: logout flush keeps a held drag's latest position, repeated flush writes zero");
        config.Opacity = .7f; settings.MarkDirty(); settings.Dispose();
        var afterClose = file.Commits; config.Opacity = .8f; settings.MarkDirty(); now += 600_000; settings.Tick(); settings.Flush(); settings.Retry(); settings.Dispose();
        check(file.Commits == afterClose && !settings.HasPendingChanges, "Persistence: dispose flushes once and all later entry points perform zero writes");
        Console.WriteLine($"PERSISTENCE drag/flush: changed commits={file.Commits - commits}; logical payload bytes={file.LogicalBytesWritten - initialBytes}; idle commits=0; after-dispose commits=0");

        var generations = FileFor("generations");
        for (var n = 0; n < 6; n++) generations.Write(ConfigurationCodec.Encode(new Configuration { VolumePercent = n }));
        var backups = Directory.GetFiles(root, "generations.json.backup-*.json").OrderDescending().ToArray();
        check(backups.Length == 3 && backups.Select(p => ConfigurationCodec.Decode(File.ReadAllBytes(p)).VolumePercent).SequenceEqual([4, 3, 2]), "Persistence: exactly three previous valid generations remain");
        check(!generations.Load(() => new()).Recovered && generations.Load(() => new()).Value.VolumePercent == 5, "Persistence: valid primary wins over backups");
        File.WriteAllText(generations.Path, "{broken", Encoding.UTF8);
        File.WriteAllText(backups[0], "broken backup", Encoding.UTF8);
        var recovered = generations.Load(() => new());
        check(recovered.Recovered && recovered.Value.VolumePercent == 3, "Persistence: corrupt primary and newest backup recover from the next valid generation");
        using (var recovery = new DeferredSettings<Configuration>(generations, () => recovered.Value, recovered.Baseline, true, _ => { }, () => 0)) recovery.Flush();
        check(generations.Load(() => new()).Value.VolumePercent == 3 && Directory.GetFiles(root, "generations.json.corrupt-*.json").Length == 1 && File.ReadAllText(backups[0]).Contains("broken backup"), "Persistence: repair preserves damaged primary and backup without promoting corruption");
        var prune = FileFor("prune");
        for (var i = 0; i < 4; i++) prune.Write(ConfigurationCodec.Encode(new Configuration { VolumePercent = i }));
        var oldest = Directory.GetFiles(root, "prune.json.backup-*.json").Order().First();
        using (var held = new FileStream(oldest, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            prune.Write(ConfigurationCodec.Encode(new Configuration { VolumePercent = 7 }));
            check(prune.Commits == 5 && prune.CleanupError != null && prune.Load(() => new()).Value.VolumePercent == 7,
                "Persistence: backup cleanup failure reports separately without retrying a committed payload");
        }
        prune.CleanupBackups();
        check(prune.CleanupError == null && Directory.GetFiles(root, "prune.json.backup-*.json").Length == 3 && prune.Commits == 5,
            "Persistence: backup cleanup retry restores three generations without a settings rewrite");
        var oversized = FileFor("oversized");
        oversized.Write(ConfigurationCodec.Encode(new Configuration { VolumePercent = 42 }));
        oversized.Write(ConfigurationCodec.Encode(new Configuration { VolumePercent = 43 }));
        using (var huge = new FileStream(oversized.Path, FileMode.Open, FileAccess.Write)) huge.SetLength(16 * 1024 * 1024 + 1);
        var rescued = oversized.Load(() => new());
        check(rescued.Recovered && rescued.Value.VolumePercent == 42, "Persistence: oversized primary recovers from a valid backup");
        oversized.Write(ConfigurationCodec.Encode(rescued.Value));
        check(oversized.Load(() => new()).Value.VolumePercent == 42 && new FileInfo(Directory.GetFiles(root, "oversized.json.corrupt-*.json").Single()).Length == 16 * 1024 * 1024 + 1,
            "Persistence: oversized primary repair preserves the original bytes");
        var broken = FileFor("unrecoverable"); File.WriteAllText(broken.Path, "invalid");
        var rejected = false; try { broken.Load(() => new()); } catch (InvalidDataException) { rejected = true; }
        check(rejected && File.ReadAllText(broken.Path) == "invalid", "Persistence: no valid recovery candidate fails closed without replacing user data");
        var newer = FileFor("future"); File.WriteAllText(newer.Path, "{\"Version\":99}"); rejected = false;
        try { newer.Load(() => new()); } catch (NotSupportedException) { rejected = true; }
        check(rejected, "Persistence: newer schema cannot be downgraded through a backup");
        var collectionMetadata = ConfigurationCodec.Decode(Encoding.UTF8.GetBytes("{\"$type\":\"AetherRadio.Configuration, AetherRadio\",\"Version\":2,\"Favorites\":{\"$type\":\"ignored-type\",\"$values\":[8]},\"MiniPosition\":{\"X\":12.5,\"Y\":22.5}}"));
        check(collectionMetadata.Favorites.Contains(8) && collectionMetadata.MiniPosition?.X == 12.5f, "Persistence: legacy collection metadata and vector settings remain readable without type activation");
        check(ConfigurationCodec.Decode(Encoding.UTF8.GetPreamble().Concat(ConfigurationCodec.Encode(config)).ToArray()).Language == config.Language, "Persistence: UTF-8 BOM settings remain readable");
        var legacy = ConfigurationCodec.Decode(Encoding.UTF8.GetBytes("{\"$type\":\"AetherRadio.Configuration, AetherRadio\",\"Version\":1,\"Language\":\"fr\",\"Favorites\":[9]}"));
        legacy.Upgrade();
        check(legacy.Language == "fr" && legacy.Favorites.Contains(9) && legacy.Repeat == RepeatMode.One, "Persistence: legacy Dalamud type metadata and saved choices migrate without resolving types");

        foreach (var stage in new[] { "write", "flush", "replace" })
        {
            var fail = false; var attempts = 0;
            var faulty = FileFor("fault-" + stage, s => { if (s == stage && fail) { attempts++; throw new IOException("Injected " + stage + " failure (disk-full/flush/rename)"); } });
            var value = new Configuration(); faulty.Write(ConfigurationCodec.Encode(value));
            var before = File.ReadAllBytes(faulty.Path); fail = true; long clock = 0; var notices = 0;
            using var queue = new DeferredSettings<Configuration>(faulty, () => value, faulty.Load(() => new()).Baseline, false, _ => notices++, () => clock);
            value.VolumePercent = 12; queue.MarkDirty();
            for (var i = 0; i < 3; i++) { clock += 60_000; queue.Tick(); queue.Flush(); }
            for (var i = 0; i < 1000; i++) { clock += 60_000; queue.Tick(); queue.Flush(); }
            check(attempts == 3 && queue.Stopped && queue.HasPendingChanges && notices == 2, $"Persistence: {stage} failure stops after three bounded attempts and retains edits");
            check(File.ReadAllBytes(faulty.Path).SequenceEqual(before) && Directory.GetFiles(root, "fault-" + stage + ".json.backup-*.json").Length == 0, $"Persistence: {stage} failure leaves primary and backup generations unchanged");
            fail = false; queue.Retry();
            check(!queue.Stopped && queue.LastError == null && faulty.Load(() => new()).Value.VolumePercent == 12, $"Persistence: explicit retry recovers from {stage} failure");
        }
        var locked = FileFor("locked"); var lockConfig = new Configuration(); locked.Write(ConfigurationCodec.Encode(lockConfig));
        using (var queue = new DeferredSettings<Configuration>(locked, () => lockConfig, locked.Load(() => new()).Baseline, false, _ => { }))
        {
            using (var held = new FileStream(locked.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            { lockConfig.VolumePercent = 23; queue.MarkDirty(); queue.Flush(); check(queue.LastError is IOException && locked.Commits == 1, "Persistence: actual Windows sharing violation preserves the primary"); }
            queue.Retry(); check(locked.Load(() => new()).Value.VolumePercent == 23, "Persistence: releasing a file lock permits explicit retry");
        }

        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var block = false; var active = 0; var maxActive = 0;
        var overlap = FileFor("overlap", stage => {
            if (stage == "write") { var n = Interlocked.Increment(ref active); maxActive = Math.Max(maxActive, n); }
            if (stage == "replace") { if (block) { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException(); } Interlocked.Decrement(ref active); }
        });
        var concurrent = new Configuration(); overlap.Write(ConfigurationCodec.Encode(concurrent)); long time = 0;
        using (var queue = new DeferredSettings<Configuration>(overlap, () => concurrent, overlap.Load(() => new()).Baseline, false, _ => { }, () => time))
        {
            block = true; concurrent.VolumePercent = 31; queue.MarkDirty(); time = 60_000; queue.Tick();
            check(entered.Wait(TimeSpan.FromSeconds(5)), "Persistence: write barrier reached");
            concurrent.VolumePercent = 32; queue.MarkDirty(); time += 60_000; queue.Tick();
            release.Set(); queue.Flush();
            check(overlap.Load(() => new()).Value.VolumePercent == 32 && maxActive == 1 && !queue.HasPendingChanges, "Persistence: edits during saving survive and writers never overlap");
            concurrent.VolumePercent = 33; queue.MarkDirty();
            Parallel.Invoke(queue.Flush, queue.Flush, queue.Dispose);
            check(overlap.Load(() => new()).Value.VolumePercent == 33 && maxActive == 1, "Persistence: competing flush/dispose calls drain a single writer");
        }
        var midnight = FileFor("midnight"); var nightConfig = new Configuration(); midnight.Write(ConfigurationCodec.Encode(nightConfig)); long monotonic = 0;
        using (var queue = new DeferredSettings<Configuration>(midnight, () => nightConfig, midnight.Load(() => new()).Baseline, false, _ => { }, () => monotonic))
        {
            // Wall time can cross midnight or roll back; the scheduler uses elapsed time only.
            nightConfig.VolumePercent = 44; queue.MarkDirty(); monotonic = 59_999; queue.Tick();
            check(midnight.Commits == 1, "Persistence: midnight/clock rollback cannot cause an early save");
            monotonic = 60_000; queue.Tick(); queue.Flush();
            check(midnight.Commits == 2, "Persistence: monotonic minute still saves across a calendar boundary");
        }
        var cancelled = new Catalog(null!, (_, _) => throw new Exception("Cancellation was swallowed"));
        try { cancelled.Load(new CancellationToken(true)); check(false, "Catalog cancellation"); }
        catch (OperationCanceledException) { check(cancelled.Tracks.Count == 0, "Lifecycle: catalogue cancellation exits without warning/retry or reading data"); }
        check(Directory.GetFiles(root, "*.pending-*").Length == 0, "Persistence: successful and failed writes leave no pending temporary files");
        Console.WriteLine($"PERSISTENCE fixtures: {root}");
    }
}
