using System;
using System.Collections.Generic;

namespace ScePSX.CdRom2
{
    [Serializable]
    public readonly struct TrackIndex
    {
        public int Number
        {
            get;
        }

        public TrackPosition Position
        {
            get;
        }

        public TrackIndex(int number, TrackPosition position)
        {
            Number = number;
            Position = position;
        }

        public override string ToString()
        {
            return $"{nameof(Number)}: {Number}, {nameof(Position)}: {Position}";
        }
    }

    [Serializable]
    public readonly struct TrackPosition
    {
        public int M
        {
            get;
        }

        public int S
        {
            get;
        }

        public int F
        {
            get;
        }

        public TrackPosition(int m, int s, int f)
        {
            //if (m is < 0 or > 99)
            //    throw new ArgumentOutOfRangeException(nameof(m), s, null);

            //if (s is < 0 or > 59)
            //    throw new ArgumentOutOfRangeException(nameof(s), s, null);

            //if (f is < 0 or > 74)
            //    throw new ArgumentOutOfRangeException(nameof(f), f, null);

            M = m;
            S = s;
            F = f;
        }

        public override string ToString()
        {
            return $"{M:D2}:{S:D2}:{F:D2}";
        }

        public int ToInt32()
        {
            return M * 60 * 75 + S * 75 + F;
        }
    }

    [Serializable]
    public class CDTrack
    {
        public CDTrack()
        {
        }

        public CDTrack(string file, long fileLength, byte index, int lbaStart, int lbaEnd)
        {
            File = file;
            FileLength = fileLength;
            Index = index;
            LbaStart = lbaStart;
            LbaEnd = lbaEnd;
        }

        public string File { get; init; } = null!;

        public long FilePosition
        {
            get; set;
        }

        public long FileLength
        {
            get; set;
        }

        public byte Index
        {
            get; set;
        }

        public int LbaStart
        {
            get; set;
        }

        public int LbaEnd
        {
            get; set;
        }

        public int LbaLength
        {
            get; set;
        }

        public IList<TrackIndex> Indices { get; } = new List<TrackIndex>();

        public override string ToString()
        {
            return
                $"{nameof(Index)}: {Index}, {nameof(LbaStart)}: {LbaStart}, {nameof(LbaEnd)}: {LbaEnd}, {nameof(LbaLength)}: {LbaLength}, {nameof(Indices)}: {Indices.Count}, {nameof(FilePosition)}: {FilePosition}";
        }
    }
}
