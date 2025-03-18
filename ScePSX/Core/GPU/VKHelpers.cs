
using System;
using System.Runtime.InteropServices;
using System.Text;

using Vulkan;

namespace ScePSX
{
    public static class vkShaders
    {
        public readonly static byte[] fillvert = new byte[] { };
        public readonly static byte[] fillfrag = new byte[] { };

        public readonly static byte[] drawvert = new byte[] { };
        public readonly static byte[] drawfrag = new byte[] { };

        public readonly static byte[] out24vert = new byte[] { };
        public readonly static byte[] out24frag = new byte[] { };

        public readonly static byte[] out16vert = new byte[] { };
        public readonly static byte[] out16frag = new byte[] { };

        public readonly static byte[] displayvert = new byte[] { };
        public readonly static byte[] displayfrag = new byte[] { };

        public readonly static byte[] ramviewvert = new byte[] { };
        public readonly static byte[] ramviewfrag = new byte[] { };
    }

}
