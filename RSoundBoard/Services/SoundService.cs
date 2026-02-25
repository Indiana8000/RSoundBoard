using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TestApp1.Helpers;

namespace TestApp1.Services;

public class SoundService : IDisposable
{
    private IWavePlayer? _wavePlayer;
    private AudioFileReader? _audioFileReader;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int? _deviceNumber = null;
    private WaveInEvent? _waveIn;
    private BufferedWaveProvider? _microphoneBuffer;
    private MixingSampleProvider? _mixer;
    private int? _microphoneDeviceNumber = null;

    public void SetOutputDevice(int? deviceNumber)
    {
        _deviceNumber = deviceNumber;
    }

    public void SetMicrophoneDevice(int? deviceNumber)
    {
        _microphoneDeviceNumber = deviceNumber;
        RestartMicrophone();
    }

    private void RestartMicrophone()
    {
        StopMicrophone();

        if (_microphoneDeviceNumber.HasValue)
        {
            try
            {
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = _microphoneDeviceNumber.Value,
                    WaveFormat = new WaveFormat(44100, 16, 1)
                };

                _microphoneBuffer = new BufferedWaveProvider(_waveIn.WaveFormat)
                {
                    BufferLength = 44100 * 2 * 5,
                    DiscardOnBufferOverflow = true
                };

                _waveIn.DataAvailable += (sender, args) =>
                {
                    _microphoneBuffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
                };

                _waveIn.StartRecording();

                if (_mixer == null)
                {
                    _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
                    {
                        ReadFully = true
                    };
                }

                var microphoneSampleProvider = _microphoneBuffer.ToSampleProvider();
                if (microphoneSampleProvider.WaveFormat.Channels == 1)
                {
                    microphoneSampleProvider = new MonoToStereoSampleProvider(microphoneSampleProvider);
                }

                _mixer.AddMixerInput(microphoneSampleProvider);
            }
            catch
            {
                StopMicrophone();
            }
        }
    }

    private void StopMicrophone()
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }

        _microphoneBuffer = null;
    }

    public async Task PlayAsync(string filePath)
    {
        await _lock.WaitAsync();
        try
        {
            Stop();

            var fullPath = PathHelper.GetFullPath(filePath);
            if (!File.Exists(fullPath))
                return;

            _audioFileReader = new AudioFileReader(fullPath);

            if (_deviceNumber.HasValue)
            {
                _wavePlayer = new WaveOutEvent { DeviceNumber = _deviceNumber.Value };
            }
            else
            {
                _wavePlayer = new WaveOutEvent();
            }

            if (_mixer != null && _microphoneDeviceNumber.HasValue)
            {
                var fileSampleProvider = _audioFileReader.ToSampleProvider();

                // Ensure the file sample provider matches the mixer's format
                if (fileSampleProvider.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
                {
                    fileSampleProvider = new WdlResamplingSampleProvider(fileSampleProvider, _mixer.WaveFormat.SampleRate);
                }

                if (fileSampleProvider.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
                {
                    fileSampleProvider = new MonoToStereoSampleProvider(fileSampleProvider);
                }

                if (_mixer.MixerInputs.Count() > 1)
                {
                    _mixer.RemoveMixerInput(_mixer.MixerInputs.First());
                }

                _mixer.AddMixerInput(fileSampleProvider);

                _wavePlayer.Init(_mixer);
            }
            else
            {
                _wavePlayer.Init(_audioFileReader);
            }

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
        StopMicrophone();
        _mixer = null;
        _lock.Dispose();
    }
}
