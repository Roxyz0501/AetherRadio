namespace AetherRadio.Core;

public interface IBgmPlayback : IDisposable
{
    ushort? Current { get; }
    bool Locked { get; set; }
    int VolumePercent { get; set; }
    bool IsLoading => false;
    bool CanSeek => false;
    bool HasEnded => false;
    double PositionSeconds => 0;
    double DurationSeconds => 0;
    bool Loop { get => true; set { } }
    void Seek(double seconds) { }
    void Play(ushort id);
    void ValidateOwner();
    void Stop();
    // Release the override without replaying requests from a previous login.
    void ResetSession();
}
