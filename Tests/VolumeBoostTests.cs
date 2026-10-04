using AetherRadio.Core;

static class VolumeBoostTests
{
    public static void Run(Action<bool, string> check)
    {
        var mixer = new TestMusicVolume { Values = [0.3f, 0.4f, 0.7f] };
        var session = new BgmVolumeSession(mixer);
        session.Apply(200);
        check(mixer.Values.SequenceEqual(new[] { 0.6f, 0.8f, 0f }), "200 percent doubles BGM mixer gain while orchestrion stays suppressed");
        var writes = mixer.Writes;
        for (var i = 0; i < 100; i++) session.Apply(200);
        check(mixer.Writes == writes && mixer.Values[0] == 0.6f, "Boost does not compound or write unchanged levels each frame");
        session.Apply(100);
        check(mixer.Values[0] == 0.3f && mixer.Values[1] == 0.4f, "100 percent returns to the original BGM level after boost");
        session.Apply(0); session.Apply(200); session.Restore();
        check(mixer.Values.SequenceEqual(new[] { 0.3f, 0.4f, 0.7f }), "Mute, boost and stop restore all original music levels");
        session.Apply(500);
        check(mixer.Values[0] == 0.6f && mixer.Values[1] == 0.8f, "Out-of-range boost clamps to 200 percent");
        mixer.Values[0] = 0.2f; session.Apply(200); session.Restore();
        check(mixer.Values[0] == 0.2f, "A game-side volume change while boosted becomes the restoration baseline");
        mixer.Values = [0, 0.8f, 0.7f]; session.Apply(200);
        check(mixer.Values[0] == 0 && mixer.Values[1] == 1, "Boost preserves muted BGM and clamps louder channels to the native ceiling");
        session.Restore();
        check(mixer.Values[1] == 0.8f, "Ceiling-limited boost still restores the original level");
    }
}
