namespace ScePSX
{
    public interface ICoreHandler
    {
        void FrameReady(int[] pixels, int width, int height);
        void SamplesReady(byte[] samples);
    }

    public interface IAudioHandler
    {
        void PlaySamples(byte[] samples);
    }

    public interface IRenderHandler
    {
        void RenderFrame(int[] pixels, int width, int height);
    }

    public interface IRumbleHandler
    {
        void ControllerRumble(byte VibrationRight, byte VibrationLeft);
    }
}
