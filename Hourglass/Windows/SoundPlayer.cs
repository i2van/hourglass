// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SoundPlayer.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows;

using System;
using System.Windows.Media;
using System.Windows.Threading;

using Audio;
using Extensions;
using Timing;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Plays <see cref="Sound"/>s stored in the file system.
/// </summary>
public sealed class SoundPlayer : IDisposable
{
    /// <summary>
    /// A <see cref="System.Media.SoundPlayer"/> that can play *.wav files.
    /// </summary>
    private readonly System.Media.SoundPlayer _soundPlayer;

    /// <summary>
    /// A <see cref="DispatcherTimer"/> used to raise events.
    /// </summary>
    private readonly DispatcherTimer _dispatcherTimer;

    /// <summary>
    /// A <see cref="IAudioPlayer"/> that can play most audio files.
    /// </summary>
    private readonly IAudioPlayer _audioPlayer;

    private bool _isLooping;

    /// <summary>
    /// Indicates whether this object has been disposed.
    /// </summary>
    private bool _disposed;

    public SoundPlayer()
    {
        // Resource sound player
        _soundPlayer = new();

        _dispatcherTimer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _dispatcherTimer.Tick += DispatcherTimerTick;

        // File audio player
        _audioPlayer = AudioPlayerSelector.Create(OnAudioStopped);
    }

    /// <summary>
    /// Raised when sound playback has completed.
    /// </summary>
    public event EventHandler? PlaybackCompleted;

    /// <summary>
    /// Gets a value indicating whether the player is playing a sound.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Plays a <see cref="Sound"/> asynchronously.
    /// </summary>
    /// <param name="sound">A <see cref="Sound"/>.</param>
    /// <param name="loop">A value indicating whether playback should be looped.</param>
    public void Play(Sound? sound, bool loop)
    {
        ThrowIfDisposed();

        // Stop all playback
        if (!Stop())
        {
            return;
        }

        // Do not play anything
        if (sound is null)
        {
            return;
        }

        try
        {
            IsPlaying = true;
            _isLooping = loop;

            if (sound.IsBuiltIn)
            {
                // Use the sound player
                _soundPlayer.Stream = sound.GetStream();

                if (loop)
                {
                    // Asynchronously play looping sound
                    _soundPlayer.PlayLooping();
                }
                else
                {
                    // Asynchronously play sound once
                    _soundPlayer.Play();

                    // Start a timer to notify the completion of playback if we know the duration
                    if (sound.Duration is not null)
                    {
                        _dispatcherTimer.Interval = sound.Duration.Value;
                        _dispatcherTimer.Start();
                    }
                }
            }
            else
            {
                // Use the audio player
                _audioPlayer.Open(sound.Path!);
                _audioPlayer.Play();
            }
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            // Nothing is being played
            IsPlaying = false;
            _isLooping = false;
            ReleaseSoundPlayerStream();
        }
    }

    /// <summary>
    /// Stops playback of a <see cref="Sound"/> if playback is occurring.
    /// </summary>
    /// <returns><c>true</c> if playback is stopped successfully or no playback was occurring, or <c>false</c> otherwise.</returns>
    public bool Stop()
    {
        ThrowIfDisposed();

        try
        {
            IsPlaying = false;
            _isLooping = false;

            _dispatcherTimer.Stop();

            // Stop the sound player
            StopSoundPlayer();

            // Stop the audio player
            _audioPlayer.Stop();
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Stops the sound player and releases its stream if a sound has been loaded into it.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Media.SoundPlayer.Stop"/> stops all the sounds being played by the process, including the ones
    /// started by the other <see cref="SoundPlayer"/> instances, so it is invoked only if this instance has a sound loaded.
    /// </remarks>
    private void StopSoundPlayer()
    {
        if (_soundPlayer.Stream is null)
        {
            return;
        }

        _soundPlayer.Stop();
        ReleaseSoundPlayerStream();
    }

    /// <summary>
    /// Releases the sound player stream.
    /// </summary>
    private void ReleaseSoundPlayerStream()
    {
        _soundPlayer.Stream?.Dispose();
        _soundPlayer.Stream = null;
    }

    /// <summary>
    /// Disposes the timer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// Disposes the timer.
    /// </summary>
    /// <param name="disposing">A value indicating whether this method was invoked by an explicit call to <see
    /// cref="Dispose"/>.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            IsPlaying = false;
            _isLooping = false;

            _dispatcherTimer.Stop();

            // Dispose the sound player
            StopSoundPlayer();
            _soundPlayer.Dispose();

            // Dispose the media player
            _audioPlayer.Dispose();
        }
    }

    /// <summary>
    /// Throws a <see cref="ObjectDisposedException"/> if the object has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    /// <summary>
    /// Invoked when the <see cref="DispatcherTimer"/> interval has elapsed.
    /// </summary>
    /// <param name="sender">The <see cref="DispatcherTimer"/>.</param>
    /// <param name="e">The event data.</param>
    private void DispatcherTimerTick(object sender, EventArgs e)
    {
        _dispatcherTimer.Stop();

        // The sound has been played, so the sound player should not be stopped anymore
        IsPlaying = false;
        ReleaseSoundPlayerStream();

        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Invoked when the media has finished playback in the <see cref="MediaPlayer"/>.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event data.</param>
    private void OnAudioStopped(object sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (!_isLooping)
        {
            IsPlaying = false;
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        try
        {
            _audioPlayer.Play();
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            // Stop looping if the sound cannot be replayed, e.g. when the audio device is not ready after resuming from sleep.
            IsPlaying = false;
            _isLooping = false;
        }
    }
}