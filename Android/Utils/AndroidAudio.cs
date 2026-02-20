using System;
using System.Threading;
using Android.Media;

namespace ScePSX;

public class AndroidAudioHandler : IDisposable
{
    private AudioTrack? audioTrack;
    public CircularBuffer<byte>? samplesBuffer;
    private Thread? audioThread;
    private bool running;
    private int bufferSize;

    public AndroidAudioHandler(int sampleRate = 44100, int channels = 2)
    {
        ChannelOut channelConfig = channels == 2 ? ChannelOut.Stereo : ChannelOut.Mono;

        int minBufferSize = AudioTrack.GetMinBufferSize(
            sampleRate,
            channelConfig,
            Android.Media.Encoding.Pcm16bit
        );

        int targetSize = sampleRate * channels * 2 * 500 / 1000; // 500ms
        bufferSize = Math.Max(minBufferSize, targetSize);

        audioTrack = new AudioTrack(
            global::Android.Media.Stream.Music,
            sampleRate,
            channelConfig,
            Android.Media.Encoding.Pcm16bit,
            bufferSize,
            AudioTrackMode.Stream
        );

        samplesBuffer = new CircularBuffer<byte>(bufferSize * 2);
    }

    private void PlayThread()
    {
        byte[] temp = new byte[2048];

        while (running && audioTrack != null && samplesBuffer != null)
        {
            int available = audioTrack.PlaybackHeadPosition * 4; // 估算
            int bytesToWrite = Math.Min(temp.Length, samplesBuffer.Count);

            if (bytesToWrite > 0)
            {
                int read = samplesBuffer.Read(temp, 0, bytesToWrite);
                if (read > 0)
                {
                    audioTrack.Write(temp, 0, read);
                }
            } else
            {
                Array.Clear(temp, 0, temp.Length);
                audioTrack.Write(temp, 0, temp.Length);
            }

            Thread.Sleep(5);
        }
    }

    public void Play()
    {
        if (running)
            return;
        running = true;
        audioThread = new Thread(PlayThread);
        audioThread.Start();

        audioTrack?.Play();
    }

    public void Pause()
    {
        audioTrack?.Pause();
    }

    public void Stop()
    {
        if (!running)
            return;
        running = false;
        audioThread?.Join(100);
        audioTrack?.Stop();
    }

    public void Dispose()
    {
        Stop();
        audioTrack?.Release();
        audioTrack?.Dispose();
    }
}
