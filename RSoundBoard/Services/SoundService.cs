using NAudio.Wave;

namespace TestApp1.Services;

public class SoundService : IDisposable
{
    private IWavePlayer? _wavePlayer;
    private AudioFileReader? _audioFileReader;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int? _deviceNumber = null;

    public void SetOutputDevice(int? deviceNumber)
    {
        _deviceNumber = deviceNumber;
    }

    public async Task PlayAsync(string filePath)
    {
        await _lock.WaitAsync();
        try
        {
            Stop();

            if (!File.Exists(filePath))
                return;

            _audioFileReader = new AudioFileReader(filePath);

            if (_deviceNumber.HasValue)
            {
                _wavePlayer = new WaveOutEvent { DeviceNumber = _deviceNumber.Value };
            }
            else
            {
                _wavePlayer = new WaveOutEvent();
            }

            _wavePlayer.Init(_audioFileReader);
            _wavePlayer.Play();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Stop()
    {
        _wavePlayer?.Stop();
        _wavePlayer?.Dispose();
        _wavePlayer = null;

        _audioFileReader?.Dispose();
        _audioFileReader = null;
    }

    public void Dispose()
    {
        Stop();
        _lock.Dispose();
    }
}
