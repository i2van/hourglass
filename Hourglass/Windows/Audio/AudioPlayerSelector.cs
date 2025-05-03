using System;
using System.IO;
using System.Reflection;

using Hourglass.Extensions;

namespace Hourglass.Windows.Audio;

internal static class AudioPlayerSelector
{
    public static IAudioPlayer Create(EventHandler stoppedEventHandler)
    {
        const string nAudio = "Hourglass.NAudio";

        var nAudioAssemblyPath = Path.Combine(AssemblyExtensions.GetExecutableDirectoryName(), $"{nAudio}.dll");

        if (File.Exists(nAudioAssemblyPath))
        {
            try
            {
                return new HourglassAudioPlayer(Activator.CreateInstanceFrom(
                    nAudioAssemblyPath,
                    $"{nAudio}.AudioPlayer",
                    false,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance,
                    null,
                    [stoppedEventHandler],
                    null,
                    null).Unwrap());
            }
            catch
            {
                // Ignored.
            }
        }

        return new AudioPlayer(stoppedEventHandler);
    }

    private class HourglassAudioPlayer : IAudioPlayer
    {
        private readonly IDisposable _disposable;

        private readonly Action<string> _open;
        private readonly Action _play;
        private readonly Action _stop;

        public HourglassAudioPlayer(object obj)
        {
            _disposable = (IDisposable)obj;

            var type = obj.GetType();

            _open = CreateDelegate<Action<string>>(nameof(Open));
            _play = CreateDelegate<Action>(nameof(Play));
            _stop = CreateDelegate<Action>(nameof(Stop));

            T CreateDelegate<T>(string methodName) where T: Delegate =>
                (T)Delegate.CreateDelegate(typeof(T), type.GetMethod(methodName)!);
        }

        public void Open(string uri) =>
            _open(uri);

        public void Play() =>
            _play();

        public void Stop() =>
            _stop();

        public void Dispose() =>
            _disposable.Dispose();
    }
}