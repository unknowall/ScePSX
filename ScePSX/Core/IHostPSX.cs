namespace ScePSX
{
    public interface IPSXHost
    {
        void HandlerError();

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
}
