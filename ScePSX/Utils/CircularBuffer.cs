using System;
using System.Threading;

namespace ScePSX
{

    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _readPos;
        private int _writePos;
        private int _count;
        private readonly object _syncRoot = new object();

        public int Capacity
        {
            get;
        }
        public int Count => _count;

        public CircularBuffer(int capacity)
        {
            Capacity = capacity;
            _buffer = new T[capacity];
        }

        public void Write(T[] data, int offset = 0, int? length = null)
        {
            int dataLength = length ?? data.Length - offset;
            if (dataLength <= 0)
                return;

            lock (_syncRoot)
            {
                // 计算需要覆盖的旧数据长度
                int overflow = Math.Max(0, _count + dataLength - Capacity);
                if (overflow > 0)
                {
                    _readPos = (_readPos + overflow) % Capacity;
                    _count -= overflow;
                }

                // 分两次写入（从writePos到末尾，然后从头开始）
                int firstSegment = Math.Min(dataLength, Capacity - _writePos);
                Array.Copy(data, offset, _buffer, _writePos, firstSegment);

                int secondSegment = dataLength - firstSegment;
                if (secondSegment > 0)
                {
                    Array.Copy(data, offset + firstSegment, _buffer, 0, secondSegment);
                }

                _writePos = (_writePos + dataLength) % Capacity;
                _count += dataLength;
            }
        }

        public int Read(T[] output, int offset = 0, int? length = null)
        {
            int requested = length ?? output.Length - offset;
            if (requested <= 0)
                return 0;

            lock (_syncRoot)
            {
                int actualRead = Math.Min(requested, _count);
                if (actualRead == 0)
                    return 0;

                // 分两次读取（从readPos到末尾，然后从头开始）
                int firstSegment = Math.Min(actualRead, Capacity - _readPos);
                Array.Copy(_buffer, _readPos, output, offset, firstSegment);

                int secondSegment = actualRead - firstSegment;
                if (secondSegment > 0)
                {
                    Array.Copy(_buffer, 0, output, offset + firstSegment, secondSegment);
                }

                _readPos = (_readPos + actualRead) % Capacity;
                _count -= actualRead;
                return actualRead;
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _readPos = 0;
                _writePos = 0;
                _count = 0;
                Array.Clear(_buffer, 0, Capacity);
            }
        }
    }
}
