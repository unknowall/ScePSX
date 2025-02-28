using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ScePSX.CdRom2
{
    [Serializable]
    public class CDSector
    {
        // Standard size for a raw sector / CDDA
        public const int RAW_BUFFER = 2352;

        // This sector data is already pre decoded and resampled so we need a bigger buffer (RAW_BUFFER * 4)
        // and on the case of mono even a bigger one, as samples are mirrored to L/R as our output is allways stereo
        public const int XA_BUFFER = RAW_BUFFER * 8;

        private byte[] sectorBuffer;

        private int pointer;
        private int size;

        public CDSector(int size)
        {
            sectorBuffer = new byte[size];
        }

        public void fillWith(Span<byte> data)
        {
            pointer = 0;
            size = data.Length;
            var dest = sectorBuffer.AsSpan();
            data.CopyTo(dest);
        }

        public ref byte ReadByte()
        {
            ref var data = ref MemoryMarshal.GetArrayDataReference(sectorBuffer);
            return ref Unsafe.Add(ref data, pointer++);
        }

        public ref short readShort()
        {
            ref var data = ref MemoryMarshal.GetArrayDataReference(sectorBuffer);
            ref var valueB = ref Unsafe.Add(ref data, pointer);
            ref var valueS = ref Unsafe.As<byte, short>(ref valueB);
            pointer += 2;
            return ref valueS;
        }

        public Span<uint> Read(int size)
        { //size from dma comes as u32
            var dma = sectorBuffer.AsSpan().Slice(pointer, size * 4);
            pointer += size * 4;
            return MemoryMarshal.Cast<byte, uint>(dma);
        }

        public Span<byte> Read() => sectorBuffer.AsSpan().Slice(0, size);

        public bool HasData() => pointer < size;

        public bool hasSamples() => size - pointer > 3;

        public void Clear()
        {
            pointer = 0;
            size = 0;
        }

    }
}
