using AetherRadio.Core;

static class OrchestrionTests
{
    public static void Run(Action<bool, string> check)
    {
        var mixer = new TestMusicVolume();
        var session = new BgmVolumeSession(mixer);
        session.Apply(100);
        check(mixer.Values.SequenceEqual(new[] { 0.8f, 0.6f, 0f }),
            "Orchestrion is suppressed even at 100 percent without muting selected BGM");
        var writes = mixer.Writes;
        for (var i = 0; i < 100; i++) session.Apply(100);
        check(mixer.Writes == writes, "Orchestrion suppression does not write unchanged levels every frame");
        session.Apply(0); session.Apply(50);
        check(mixer.Values.SequenceEqual(new[] { 0.4f, 0.3f, 0f }),
            "Volume changes and track transitions keep orchestrion suppressed");
        session.Restore();
        check(mixer.Values.SequenceEqual(new[] { 0.8f, 0.6f, 0.7f }),
            "Stopping restores all music buses to their captured levels");
        session.Apply(50); mixer.Values[2] = 0.25f; session.Apply(50);
        check(mixer.Values[2] == 0, "A later game-side orchestrion volume update is suppressed while playing");
        session.Restore();
        check(mixer.Values[2] == 0.25f, "Stop restores the newer game-side orchestrion volume");
        session.Apply(50); mixer.Values[2] = 0.9f; session.Restore();
        check(mixer.Values[2] == 0.9f, "Cleanup preserves an external change made after the last player update");
        mixer.Values[2] = 0; session.Apply(100); session.Restore();
        check(mixer.Values[2] == 0, "An initially silent orchestrion is never made audible on stop");
        mixer.Values[2] = 0.7f; session.Apply(50);
        mixer.Owner = 2; mixer.Values = [0.9f, 0.5f, 0.35f]; session.Apply(50); session.Restore();
        check(mixer.Values.SequenceEqual(new[] { 0.9f, 0.5f, 0.35f }),
            "A replacement sound manager restores its own levels, not the previous session's");
        session.Apply(50); mixer.FailChannel = MusicChannel.Normal;
        var reported = false;
        try { session.Restore(); } catch (AggregateException) { reported = true; }
        check(reported && mixer.Values[1] == 0.5f && mixer.Values[2] == 0.35f,
            "Failure restoring one BGM bus does not leave orchestrion muted");
        mixer.FailChannel = null; writes = mixer.Writes; session.Restore();
        check(mixer.Writes == writes, "Failed cleanup discards stale ownership before the next session");
    }
}
