using AetherRadio.Core;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace AetherRadio;

// Transient mixer levels only. No system config, master, mute or sound-effect APIs.
public sealed unsafe class NativeMusicVolume : IMusicVolume
{
    public NativeMusicVolume()
    {
        if (SoundManager.StaticAddressPointers.ppInstance == null || SoundManager.MemberFunctionPointers.SetVolume == null)
            throw new InvalidOperationException("BGM音量APIを解決できません。ゲームとDalamudの更新を確認してください。");
    }
    public nint Owner => (nint)SoundManager.Instance();
    public float Read(nint owner, MusicChannel channel)
    {
        var manager = SoundManager.Instance();
        return manager != null && (nint)manager == owner ? manager->Volume[(int)Bus(channel)] : float.NaN;
    }
    public void Write(nint owner, MusicChannel channel, float value)
    {
        var manager = SoundManager.Instance();
        if (manager != null && (nint)manager == owner && float.IsFinite(value))
            manager->SetVolume(Bus(channel), Math.Clamp(value, 0, 1));
    }
    private static SoundBus Bus(MusicChannel channel) => channel switch
    {
        MusicChannel.Normal => SoundBus.Music,
        MusicChannel.TimeStretched => SoundBus.TimeStretchBGM,
        _ => throw new ArgumentOutOfRangeException(nameof(channel)),
    };
}
