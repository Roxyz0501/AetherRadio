namespace AetherRadio.Core;

public interface IBgmPlayback : IDisposable
{
    ushort? Current { get; }
    bool Locked { get; set; }
    int VolumePercent { get; set; }
    void Play(ushort id);
    void ValidateOwner();
    void Stop();
    // Release the override without replaying requests from a previous login.
    void ResetSession();
}
