using System;

namespace Hourglass.Windows.Audio;

internal static class AudioPlayerSelector
{
    private static Func<EventHandler, IAudioPlayer>? _createFunc;

    public static IAudioPlayer Create(EventHandler stoppedEventHandler)
    {
        if (_createFunc is not null)
        {
            return _createFunc(stoppedEventHandler);
        }

        IAudioPlayer player;

        try
        {
            player = CreateNAudioPlayer(stoppedEventHandler);
            _createFunc = CreateNAudioPlayer;
        }
        catch
        {
            player = CreateAudioPlayer(stoppedEventHandler);
            _createFunc = CreateAudioPlayer;
        }

        return player;

        static IAudioPlayer CreateAudioPlayer(EventHandler stoppedEventHandler) =>
            new AudioPlayer(stoppedEventHandler);

        static IAudioPlayer CreateNAudioPlayer(EventHandler stoppedEventHandler) =>
            new NAudioPlayer(stoppedEventHandler);
    }
}