﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SoundPlayer.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass
{
    using System;

    /// <summary>
    /// Plays <see cref="Sound"/>s stored in the file system.
    /// </summary>
    public class SoundPlayer : IDisposable
    {
        #region Private Members

        /// <summary>
        /// Plays <see cref="Sound"/>s stored in the assembly.
        /// </summary>
        private readonly ResourceSoundPlayer resourceSoundPlayer;

        /// <summary>
        /// Plays <see cref="Sound"/>s stored in the file system.
        /// </summary>
        private readonly FileSoundPlayer fileSoundPlayer;

        /// <summary>
        /// Indicates whether this object has been disposed.
        /// </summary>
        private bool disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundPlayer"/> class.
        /// </summary>
        public SoundPlayer()
        {
            this.resourceSoundPlayer = new ResourceSoundPlayer();
            this.resourceSoundPlayer.PlaybackStarted += (s, e) => this.OnPlaybackStarted();
            this.resourceSoundPlayer.PlaybackStopped += (s, e) => this.OnPlaybackStopped();
            this.resourceSoundPlayer.PlaybackCompleted += (s, e) => this.OnPlaybackCompleted();

            this.fileSoundPlayer = new FileSoundPlayer();
            this.fileSoundPlayer.PlaybackStarted += (s, e) => this.OnPlaybackStarted();
            this.fileSoundPlayer.PlaybackStopped += (s, e) => this.OnPlaybackStopped();
            this.fileSoundPlayer.PlaybackCompleted += (s, e) => this.OnPlaybackCompleted();
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when sound playback has started.
        /// </summary>
        public event EventHandler PlaybackStarted;

        /// <summary>
        /// Raised when sound playback has stopped.
        /// </summary>
        public event EventHandler PlaybackStopped;

        /// <summary>
        /// Raised when sound playback has completed.
        /// </summary>
        public event EventHandler PlaybackCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays a <see cref="Sound"/> asynchronously.
        /// </summary>
        /// <param name="sound">A <see cref="Sound"/>.</param>
        /// <param name="loop">A value indicating whether playback should be looped.</param>
        /// <returns><c>true</c> if the <see cref="Sound"/> plays successfully, or <c>false</c> otherwise.</returns>
        public bool Play(Sound sound, bool loop)
        {
            this.ThrowIfDisposed();

            // Stop all playback
            if (!this.Stop())
            {
                return false;
            }

            // Do not play nothing
            if (sound == null)
            {
                return true;
            }

            // Play the sound using the right sound player
            if (sound.IsBuiltIn)
            {
                return this.resourceSoundPlayer.Play(sound, loop);
            }
            else
            {
                return this.fileSoundPlayer.Play(sound, loop);
            }
        }

        /// <summary>
        /// Stops playback of a <see cref="Sound"/> if playback is occurring.
        /// </summary>
        /// <returns><c>true</c> if playback is stopped successfully or no playback was occurring, or <c>false</c>
        /// otherwise.</returns>
        public bool Stop()
        {
            this.ThrowIfDisposed();

            return this.resourceSoundPlayer.Stop() && this.fileSoundPlayer.Stop();
        }

        /// <summary>
        /// Disposes the timer.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true /* disposing */);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Disposes the timer.
        /// </summary>
        /// <param name="disposing">A value indicating whether this method was invoked by an explicit call to <see
        /// cref="Dispose"/>.</param>
        protected void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                this.resourceSoundPlayer.Dispose();
                this.fileSoundPlayer.Dispose();
            }
        }

        /// <summary>
        /// Throws a <see cref="ObjectDisposedException"/> if the object has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        /// <summary>
        /// Raises the <see cref="PlaybackStarted"/> event.
        /// </summary>
        protected virtual void OnPlaybackStarted()
        {
            EventHandler eventHandler = this.PlaybackStarted;

            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises the <see cref="PlaybackStopped"/> event.
        /// </summary>
        protected virtual void OnPlaybackStopped()
        {
            EventHandler eventHandler = this.PlaybackStopped;

            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises the <see cref="PlaybackCompleted"/> event.
        /// </summary>
        protected virtual void OnPlaybackCompleted()
        {
            EventHandler eventHandler = this.PlaybackCompleted;

            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}