using System;
using System.Collections.Generic;

namespace ScePSX.CdRom2
{
    [Serializable]
    public struct TrackIndex
    {
        public int Number;
        public TrackPosition Position;

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
    public struct TrackPosition
    {
        public int M;
        public int S;
        public int F;

        public TrackPosition(int m, int s, int f)
        {
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

        public string File;
        public long FilePosition;
        public long FileLength;
        public byte Index;
        public int LbaStart;
        public int LbaEnd;
        public int LbaLength;

        public IList<TrackIndex> Indices { get; } = new List<TrackIndex>();

        public override string ToString()
        {
            return
                $"{nameof(Index)}: {Index}, {nameof(LbaStart)}: {LbaStart}, {nameof(LbaEnd)}: {LbaEnd}, {nameof(LbaLength)}: {LbaLength}, {nameof(Indices)}: {Indices.Count}, {nameof(FilePosition)}: {FilePosition}";
        }
    }
}
