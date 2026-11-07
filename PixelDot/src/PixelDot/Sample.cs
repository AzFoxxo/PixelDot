using SFML.Audio;

namespace PixelDot;

/// <summary>
/// Represents an audio sample loaded into memory.
/// </summary>
public sealed class Sample : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the underlying SFML SoundBuffer.
    /// </summary>
    public SoundBuffer? SoundBuffer { get; private set; }

    /// <summary>
    /// Gets the file path or identifier for this sample.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the total duration of this sample as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan Duration => SoundBuffer is not null ? TimeSpan.FromSeconds(SoundBuffer.Duration.AsSeconds()) : TimeSpan.Zero;

    /// <summary>
    /// Gets the sample rate in Hertz (e.g. 44100 Hz).
    /// </summary>
    public uint SampleRate => SoundBuffer?.SampleRate ?? 0;

    /// <summary>
    /// Gets the number of audio channels (e.g. 1 for mono, 2 for stereo).
    /// </summary>
    public uint ChannelCount => SoundBuffer?.ChannelCount ?? 0;

    /// <summary>
    /// Initialises a new instance of <see cref="Sample"/> from the specified audio file path.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    public Sample(string path)
    {
        Path = path;
        SoundBuffer = new SoundBuffer(path);
    }

    /// <summary>
    /// Initialises a new instance of <see cref="Sample"/> wrapping an existing <see cref="SFML.Audio.SoundBuffer"/>.
    /// </summary>
    /// <param name="soundBuffer">The SFML sound buffer.</param>
    /// <param name="path">Optional path or identifier.</param>
    public Sample(SoundBuffer soundBuffer, string path = "")
    {
        SoundBuffer = soundBuffer ?? throw new ArgumentNullException(nameof(soundBuffer));
        Path = path;
    }

    /// <summary>
    /// Plays this sample on the specified audio channel (1-5).
    /// </summary>
    /// <param name="channel">Channel number (1-5).</param>
    /// <param name="loop">Whether playback should loop indefinitely.</param>
    public void Play(int channel, bool loop = false)
    {
        Audio.Play(channel, this, loop);
    }

    /// <summary>
    /// Disposes the underlying audio buffer resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            SoundBuffer?.Dispose();
            SoundBuffer = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
