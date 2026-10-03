namespace AetherRadio.Core;

// Independent of the game: every shuffle cycle visits each ID once.
public sealed class PlaybackQueue(Random? random = null)
{
    private readonly Random random = random ?? Random.Shared;
    private List<ushort> items = [];
    private readonly List<ushort> bag = [];
    private readonly Stack<ushort> history = [];
    private bool shuffle;
    public ushort? Current { get; private set; }
    public int Count => items.Count;
    public bool Shuffle
    {
        get => shuffle;
        set { if (shuffle != value) { shuffle = value; Refill(); } }
    }
    public RepeatMode Repeat { get; set; } = RepeatMode.All;

    public void Clear()
    {
        items.Clear();
        bag.Clear();
        history.Clear();
        Current = null;
    }

    public void Start(IEnumerable<ushort> source, ushort selected)
    {
        items = source.Distinct().ToList();
        if (!items.Contains(selected)) items.Add(selected);
        Current = selected;
        history.Clear();
        Refill();
    }

    private void Refill()
    {
        bag.Clear();
        bag.AddRange(items.Where(id => id != Current));
        for (var i = bag.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }
    }

    public ushort? Next(bool automatic = false)
    {
        if (Current is not { } current || items.Count == 0) return null;
        if (automatic && Repeat == RepeatMode.One) return current;
        ushort next;
        if (Shuffle)
        {
            if (bag.Count == 0)
            {
                if (Repeat == RepeatMode.Off && automatic) return null;
                Refill();
            }
            if (bag.Count == 0) next = current;
            else { next = bag[^1]; bag.RemoveAt(bag.Count - 1); }
        }
        else
        {
            var index = items.IndexOf(current) + 1;
            if (index >= items.Count && automatic && Repeat == RepeatMode.Off) return null;
            next = items[index % items.Count];
        }
        history.Push(current);
        if (history.Count > 1000) { var recent = history.Take(500).Reverse().ToArray(); history.Clear(); foreach (var id in recent) history.Push(id); }
        return Current = next;
    }

    public ushort? Previous()
    {
        if (history.TryPop(out var previous)) return Current = previous;
        if (Current is not { } current || items.Count == 0) return null;
        return Current = items[(items.IndexOf(current) + items.Count - 1) % items.Count];
    }
}
