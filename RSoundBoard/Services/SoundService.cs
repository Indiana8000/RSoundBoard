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
    private IWavePlayer? _microphonePlayer;

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
                    WaveFormat = new WaveFormat(44100, 16, 1),
                    BufferMilliseconds = 20
                };

                _microphoneBuffer = new BufferedWaveProvider(_waveIn.WaveFormat)
                {
                    BufferLength = (int)(44100 * 2 * 0.2),
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

                if (_deviceNumber.HasValue)
                {
                    _microphonePlayer = new WaveOutEvent {
                        DeviceNumber = _deviceNumber.Value,
                        DesiredLatency = 125
                    };
                }
                else
                {
                    _microphonePlayer = new WaveOutEvent();
                }

                _microphonePlayer.Init(_mixer);
                _microphonePlayer.Play();
            }
            catch
            {
                StopMicrophone();
            }
        }
    }

    private void StopMicrophone()
    {
        if (_microphonePlayer != null)
        {
            try
            {
                _microphonePlayer.Stop();
            }
            catch
            {
            }

            try
            {
                _microphonePlayer.Dispose();
            }
            catch
            {
            }

            _microphonePlayer = null;
        }

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
            var fullPath = PathHelper.GetFullPath(filePath);
            if (!File.Exists(fullPath))
                return;

            const int maxRetries = 3;
            Exception? lastException = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    Stop();
                    await Task.Delay(attempt == 0 ? 50 : 100 * attempt);

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

                        if (fileSampleProvider.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
                        {
                            fileSampleProvider = new WdlResamplingSampleProvider(fileSampleProvider, _mixer.WaveFormat.SampleRate);
                        }

                        if (fileSampleProvider.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
                        {
                            fileSampleProvider = new MonoToStereoSampleProvider(fileSampleProvider);
                        }

                        var existingFileInput = _mixer.MixerInputs.Skip(1).FirstOrDefault();
                        if (existingFileInput != null)
                        {
                            _mixer.RemoveMixerInput(existingFileInput);
                        }

                        _mixer.AddMixerInput(fileSampleProvider);

                        _wavePlayer.Init(_mixer);
                    }
                    else
                    {
                        _wavePlayer.Init(_audioFileReader);
                    }

                    _wavePlayer.Play();
                    return;
                }
                catch (Exception ex) when (ex.Message.Contains("Already Allocated") && attempt < maxRetries - 1)
                {
                    lastException = ex;
                    Stop();
                    await Task.Delay(150);
                }
            }

            Stop();
            if (lastException != null)
            {
                throw lastException;
            }
        }
        catch
        {
            Stop();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Stop()
    {
        if (_wavePlayer != null)
        {
            try
            {
                _wavePlayer.Stop();
            }
            catch
            {
            }

            try
            {
                _wavePlayer.Dispose();
            }
            catch
            {
            }

            _wavePlayer = null;
        }

        if (_audioFileReader != null)
        {
            try
            {
                _audioFileReader.Dispose();
            }
            catch
            {
            }

            _audioFileReader = null;
        }
    }

    public void Dispose()
    {
        Stop();
        StopMicrophone();
        _mixer = null;
        _lock.Dispose();
    }
}
