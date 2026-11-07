using SFML.Audio;

namespace PixelDot;

/// <summary>
/// Audio playback system for PixelDot, providing multi-channel sound management.
/// </summary>
public static class Audio
{
    public const int MinChannel = 1;
    public const int MaxChannel = 5;
    public const int ChannelCount = 5;

    private static readonly Sound?[] _channels = new Sound?[ChannelCount];
    private static readonly Sample?[] _currentSamples = new Sample?[ChannelCount];
    private static readonly float[] _channelVolumes = [100f, 100f, 100f, 100f, 100f];

    /// <summary>
    /// Loads an audio sample from the specified file path.
    /// </summary>
    /// <param name="path">Path to the audio file (e.g. .wav, .ogg, .flac).</param>
    /// <returns>A loaded <see cref="Sample"/> instance, or null if loading failed.</returns>
    public static Sample? LoadSample(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            SafeLogError("Cannot load audio sample: path is null or empty.");
            return null;
        }

        var resolvedPath = ResolveAudioPath(path);
        if (resolvedPath is null)
        {
            SafeLogError($"Audio file not found: `{path}` (checked direct path, working directory, and application directory with common audio extensions)");
            return null;
        }

        try
        {
            var soundBuffer = new SoundBuffer(resolvedPath);
            SafeLogInfo($"Loaded audio sample: {Path.GetFileName(resolvedPath)}");
            return new Sample(soundBuffer, resolvedPath);
        }
        catch (Exception ex)
        {
            SafeLogError($"Failed to load audio sample from '{resolvedPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Plays an audio sample on the specified channel (1-5).
    /// </summary>
    /// <param name="channel">The audio channel to play on (1-5).</param>
    /// <param name="sample">The audio sample to play.</param>
    /// <param name="loop">Whether the audio should loop continuously.</param>
    public static void Play(int channel, Sample? sample, bool loop = false)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            SafeLogError($"Invalid audio channel: {channel}. Channel must be between {MinChannel} and {MaxChannel}.");
            return;
        }

        if (sample is null)
        {
            SafeLogError("Cannot play audio: sample is null.");
            return;
        }

        if (sample.SoundBuffer is null)
        {
            SafeLogError("Cannot play audio: sample sound buffer is invalid or disposed.");
            return;
        }

        try
        {
            var index = channel - 1;

            // SFML 3: Sound requires a SoundBuffer at construction; recreate if the buffer changes.
            var existingSound = _channels[index];
            if (existingSound is not null && existingSound.SoundBuffer != sample.SoundBuffer)
            {
                existingSound.Stop();
                existingSound.Dispose();
                _channels[index] = null;
            }

            if (_channels[index] is null)
            {
                _channels[index] = new Sound(sample.SoundBuffer);
                _channels[index]!.Volume = _channelVolumes[index];
            }

            var sound = _channels[index]!;
            sound.Stop();
            sound.IsLooping = loop;
            sound.Volume = _channelVolumes[index];

            // Retain reference so garbage collector does not prematurely collect buffer
            _currentSamples[index] = sample;

            sound.Play();
        }
        catch (Exception ex)
        {
            SafeLogError($"Failed to play audio on channel {channel}: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops playback on all audio channels.
    /// </summary>
    public static void Stop()
    {
        for (var ch = MinChannel; ch <= MaxChannel; ch++)
        {
            Stop(ch);
        }
    }

    /// <summary>
    /// Stops playback on the specified channel (1-5).
    /// </summary>
    /// <param name="channel">The channel to stop (1-5).</param>
    public static void Stop(int channel)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            SafeLogError($"Invalid audio channel: {channel}. Channel must be between {MinChannel} and {MaxChannel}.");
            return;
        }

        var index = channel - 1;
        if (_channels[index] is { } sound)
        {
            try
            {
                sound.Stop();
            }
            catch (Exception ex)
            {
                SafeLogError($"Failed to stop audio channel {channel}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Stops playback on the specified channel, or all channels if channel is null.
    /// </summary>
    /// <param name="channel">The channel to stop (1-5), or null for all channels.</param>
    public static void Stop(int? channel)
    {
        if (channel is null)
        {
            Stop();
        }
        else
        {
            Stop(channel.Value);
        }
    }

    /// <summary>
    /// Stops playback on all audio channels.
    /// </summary>
    public static void StopAll() => Stop();

    /// <summary>
    /// Sets the volume of a specific audio channel (1-5).
    /// </summary>
    /// <param name="channel">The channel number (1-5).</param>
    /// <param name="volume">Volume from 0 (silent) to 100 (full volume).</param>
    public static void SetVolume(int channel, float volume)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            SafeLogError($"Invalid audio channel: {channel}. Channel must be between {MinChannel} and {MaxChannel}.");
            return;
        }

        var clamped = Math.Clamp(volume, 0f, 100f);
        var index = channel - 1;
        _channelVolumes[index] = clamped;

        if (_channels[index] is { } sound)
        {
            sound.Volume = clamped;
        }
    }

    /// <summary>
    /// Sets the volume of a specific audio channel (1-5).
    /// </summary>
    /// <param name="channel">The channel number (1-5).</param>
    /// <param name="volume">Volume from 0 (silent) to 100 (full volume).</param>
    public static void SetVolume(int channel, double volume) => SetVolume(channel, (float)volume);

    /// <summary>
    /// Sets the volume across all audio channels (1-5).
    /// </summary>
    /// <param name="volume">Volume from 0 (silent) to 100 (full volume).</param>
    public static void SetVolume(float volume)
    {
        var clamped = Math.Clamp(volume, 0f, 100f);
        for (var ch = MinChannel; ch <= MaxChannel; ch++)
        {
            SetVolume(ch, clamped);
        }
    }

    /// <summary>
    /// Sets the volume across all audio channels (1-5).
    /// </summary>
    /// <param name="volume">Volume from 0 (silent) to 100 (full volume).</param>
    public static void SetVolume(double volume) => SetVolume((float)volume);

    /// <summary>
    /// Gets the current volume for the specified channel (1-5).
    /// </summary>
    /// <param name="channel">The channel number (1-5).</param>
    /// <returns>The volume level (0-100).</returns>
    public static float GetVolume(int channel)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            SafeLogError($"Invalid audio channel: {channel}. Channel must be between {MinChannel} and {MaxChannel}.");
            return 0f;
        }

        return _channelVolumes[channel - 1];
    }

    /// <summary>
    /// Checks whether audio is currently playing on the specified channel.
    /// </summary>
    /// <param name="channel">The channel number (1-5).</param>
    /// <returns>True if playing; otherwise false.</returns>
    public static bool IsPlaying(int channel)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            return false;
        }

        return _channels[channel - 1]?.Status == SoundStatus.Playing;
    }

    /// <summary>
    /// Pauses playback on the specified channel (1-5).
    /// </summary>
    /// <param name="channel">The channel number (1-5).</param>
    public static void Pause(int channel)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            SafeLogError($"Invalid audio channel: {channel}. Channel must be between {MinChannel} and {MaxChannel}.");
            return;
        }

        var index = channel - 1;
        _channels[index]?.Pause();
    }

    /// <summary>
    /// Resumes playback on the specified channel (1-5) if paused.
    /// </summary>
    /// <param name="channel">The channel number (1-5).</param>
    public static void Resume(int channel)
    {
        if (channel < MinChannel || channel > MaxChannel)
        {
            SafeLogError($"Invalid audio channel: {channel}. Channel must be between {MinChannel} and {MaxChannel}.");
            return;
        }

        var index = channel - 1;
        if (_channels[index]?.Status == SoundStatus.Paused)
        {
            _channels[index]?.Play();
        }
    }

    /// <summary>
    /// Disposes and cleans up all audio channel resources.
    /// </summary>
    public static void Dispose()
    {
        for (var i = 0; i < ChannelCount; i++)
        {
            if (_channels[i] is { } sound)
            {
                try
                {
                    sound.Stop();
                    sound.Dispose();
                }
                catch
                {
                    // Ignore disposal errors during application shutdown
                }
                _channels[i] = null;
            }
            _currentSamples[i] = null;
        }
    }


    private static void SafeLogError(string message)
    {
        try
        {
            Logger.LogError(message);
        }
        catch (InvalidOperationException)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
        }
    }

    private static void SafeLogInfo(string message)
    {
        try
        {
            Logger.LogInfo(message);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"[INFO] {message}");
        }
    }

    private static string? ResolveAudioPath(string path)
    {
        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        var baseCombined = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(baseCombined))
        {
            return Path.GetFullPath(baseCombined);
        }

        var currentCombined = Path.Combine(Directory.GetCurrentDirectory(), path);
        if (File.Exists(currentCombined))
        {
            return Path.GetFullPath(currentCombined);
        }

        // Try common audio extensions if path has no extension
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            string[] extensions = [".wav", ".ogg", ".flac", ".mp3", ".aiff"];
            foreach (var ext in extensions)
            {
                var withExt = path + ext;
                if (File.Exists(withExt))
                {
                    return Path.GetFullPath(withExt);
                }

                var baseWithExt = Path.Combine(AppContext.BaseDirectory, withExt);
                if (File.Exists(baseWithExt))
                {
                    return Path.GetFullPath(baseWithExt);
                }

                var currentWithExt = Path.Combine(Directory.GetCurrentDirectory(), withExt);
                if (File.Exists(currentWithExt))
                {
                    return Path.GetFullPath(currentWithExt);
                }
            }
        }

        return null;
    }
}
