using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace ScePSX.Core.GPU
{
    public static class PGXPVector
    {
        public struct LowPos
        {
            public short x;
            public short y;
            public short z;
            public short w;

            public override bool Equals(object obj)
            {
                if (!(obj is LowPos))
                    return false;

                var other = (LowPos)obj;
                return x == other.x && y == other.y;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + x.GetHashCode();
                    hash = hash * 23 + y.GetHashCode();
                    return hash;
                }
            }
        }

        public struct HighPos
        {
            public double x;
            public double y;
            public double z;

            public double worldX;
            public double worldY;
            public double worldZ;
        }

        private static Dictionary<LowPos, HighPos> lowToHighMap = new Dictionary<LowPos, HighPos>();
        private static HashSet<LowPos> addedKeys = new();

        public static void Add(LowPos low, HighPos high)
        {
            lowToHighMap[low] = high;
            addedKeys.Add(low);
        }

        public static bool Find(LowPos low, out HighPos high)
        {
            return lowToHighMap.TryGetValue(low, out high);
        }

        public static bool Find(short x, short y, out HighPos high)
        {
            LowPos low = new LowPos { x = x, y = y };
            return lowToHighMap.TryGetValue(low, out high);
        }

        public static bool Delete(LowPos low)
        {
            return lowToHighMap.Remove(low);
        }

        public static bool Delete(short x, short y)
        {
            LowPos low = new LowPos { x = x, y = y };
            return lowToHighMap.Remove(low);
        }

        public static void Deletes()
        {
            foreach (var key in addedKeys)
            {
                lowToHighMap.Remove(key);
            }
            addedKeys.Clear();
        }

        public static void Clear()
        {
            lowToHighMap.Clear();
            addedKeys.Clear();
        }

        public static int Count => lowToHighMap.Count;
    }

}
