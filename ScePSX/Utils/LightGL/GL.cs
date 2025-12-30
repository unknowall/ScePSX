/*
 * LightGL - a Lightweight OpenGL Library
 * 
 * github: http://github.com/unknowall/LightGL
 * 
 * for projects:
 * 
 * ScePSX a lightweight PS1 emulator: http://github.com/unknowall/ScePSX
 * 
 * ScePSP a lightweight PSP emulator: http://github.com/unknowall/ScePSP
 * 
 * ePceCD a lightweight PCEngineCD emulator: http://github.com/unknowall/emuPCE
 * 
 * unknowall - sgfree@hotmail.com
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

using LightGL.DynamicLibrary;

namespace LightGL
{
    public enum DepthFunction
    {
        Never = 0x0200,
        Less = 0x0201,
        Equal = 0x0202,
        Lequal = 0x0203,
        Greater = 0x0204,
        Notequal = 0x0205,
        Gequal = 0x0206,
        Always = 0x0207
    }

    public enum BufferTarget
    {
        ArrayBuffer = 0x8892,
        ElementArrayBuffer = 0x8893,
        ArrayBufferBinding = 0x8894,
        ElementArrayBufferBinding = 0x8895
    }

    public enum BufferUsage
    {
        StreamDraw = 0x88E0,
        StaticDraw = 0x88E4,
        DynamicDraw = 0x88E8
    }

    public enum VertexAttribPointerType
    {
        Byte = 0x1400,
        UnsignedByte = 0x1401,
        Short = 0x1402,
        UnsignedShort = 0x1403,
        Int = 0x1404,
        UnsignedInt = 0x1405,
        Float = 0x1406,
        Fixed = 0x140C
    }

    public enum VertexAttribIType
    {
        Byte = 0x1400,
        UnsignedByte = 0x1401,
        Short = 0x1402,
        UnsignedShort = 0x1403,
        Int = 0x1404,
        UnsignedInt = 0x1405
    }

    public enum FramebufferAttachment
    {
        ColorAttachment0 = 0x8CE0,
        DepthAttachment = 0x8D00,
        StencilAttachment = 0x8D20
    }

    public enum ClearBufferMask
    {
        DepthBufferBit = 0x00000100,
        StencilBufferBit = 0x00000400,
        ColorBufferBit = 0x00004000
    }

    public enum EnableCap
    {
        CullFace = 0x0B44,
        Blend = 0x0BE2,
        Dither = 0x0BD0,
        AlphaTest = 0x0BC0,
        StencilTest = 0x0B90,
        DepthTest = 0x0B71,
        ScissorTest = 0x0C11,
        PolygonOffsetFill = 0x8037,
        SampleAlphaToCoverage = 0x809E,
        SampleCoverage = 0x80A0,
        Lighting = 0x0B50,
        Fog = 0x0B60,
        LogicOp = 0x0BF1,
        ColorMaterial = 0x0B57,
        Texture2d = 0x0DE1,
        Multisample = 0x809D
    }

    public enum TextureMinFilter
    {
        Nearest = 0x2600,
        Linear = 0x2601,
        NearestMipmapNearest = 0x2700,
        LinearMipmapNearest = 0x2701,
        NearestMipmapLinear = 0x2702,
        LinearMipmapLinear = 0x2703
    }

    public enum TextureWrapMode
    {
        Repeat = 0x2901,
        ClampToEdge = 0x812F,
        MirroredRepeat = 0x8370
    }

    public enum TextureTarget
    {
        Texture2d = 0x0DE1,
        TextureCubeMap = 0x8513,
        TextureCubeMapPositiveX = 0x8515,
        TextureCubeMapNegativeX = 0x8516,
        TextureCubeMapPositiveY = 0x8517,
        TextureCubeMapNegativeY = 0x8518,
        TextureCubeMapPositiveZ = 0x8519,
        TextureCubeMapNegativeZ = 0x851A
    }

    public enum TextureParameterName
    {
        TextureMagFilter = 0x2800,
        TextureMinFilter = 0x2801,
        TextureWrapS = 0x2802,
        TextureWrapT = 0x2803
    }

    public enum FramebufferTarget
    {
        Framebuffer = 0x8D40,
        ReadFramebuffer = 0x8CA8,
        DrawFramebuffer = 0x8CA9
    }

    public enum InternalFormat
    {
        DepthComponent = 0x1902,
        Alpha = 0x1906,
        Rgb = 0x1907,
        Rgba = 0x1908,
        Rgba8 = 0x8058,
        Luminance = 0x1909,
        LuminanceAlpha = 0x190A,
        Rgba4 = 0x8056,
        Rgb5A1 = 0x8057,
        Rgb565 = 0x8D62,
        DepthComponent16 = 0x81A5,
        StencilIndex = 0x1901,
        StencilIndex8 = 0x8D48
    }

    public enum PixelFormat
    {
        DepthComponent = 0x1902,
        Alpha = 0x1906,
        Rgb = 0x1907,
        Rgba = 0x1908,
        Luminance = 0x1909,
        LuminanceAlpha = 0x190A,
        StencilIndex = 0x1901,
        Bgra = 0x80E1
    }

    public enum PixelType
    {
        Byte = 0x1400,
        UnsignedByte = 0x1401,
        Short = 0x1402,
        UnsignedShort = 0x1403,
        Int = 0x1404,
        UnsignedInt = 0x1405,
        Float = 0x1406,
        Fixed = 0x140C,
        UnsignedShort4444 = 0x8033,
        UnsignedShort5551 = 0x8034,
        UnsignedShort565 = 0x8363,
        UnsignedShort1555Rev = 0x8366
    }

    public enum FramebufferStatus
    {
        Complete = 0x8CD5,
        IncompleteAttachment = 0x8CD6,
        IncompleteMissingAttachment = 0x8CD7,
        IncompleteDimensions = 0x8CD9,
        Unsupported = 0x8CDD,
        InvalidFramebufferOperation = 0x0506
    }

    public enum BlitFramebufferFilter
    {
        Nearest = 0x2600,
        Linear = 0x2601
    }

    public enum PrimitiveType
    {
        Points = 0x0000,
        Lines = 0x0001,
        LineLoop = 0x0002,
        LineStrip = 0x0003,
        Triangles = 0x0004,
        TriangleStrip = 0x0005,
        TriangleFan = 0x0006,
        Quads = 0x0007
    }

    public enum BlendEquationMode
    {
        FuncAdd = 0x8006,
        FuncSubtract = 0x800A,
        FuncReverseSubtract = 0x800B
    }

    public enum BlendingFactor
    {
        Zero = 0,
        One = 1,
        SrcColor = 0x0300,
        OneMinusSrcColor = 0x0301,
        SrcAlpha = 0x0302,
        OneMinusSrcAlpha = 0x0303,
        DstAlpha = 0x0304,
        OneMinusDstAlpha = 0x0305,
        DstColor = 0x0306,
        OneMinusDstColor = 0x0307,
        SrcAlphaSaturate = 0x0308,
        ConstantColor = 0x8001,
        OneMinusConstantColor = 0x8002,
        ConstantAlpha = 0x8003,
        OneMinusConstantAlpha = 0x8004,

        Src1Color = 0x88F9,
        Src1Alpha = 0x8589,

        //ARB_blend_func_extended
        Src1AlphaSaturate = 0x88FB,
        OneMinusSrc1Color = 0x88FC,
        OneMinusSrc1Alpha = 0x88FD
    }

    public enum PixelStoreParameter
    {
        UnpackAlignment = 0x0CF5,
        PackAlignment = 0x0D05,
        PackRowLength = 0x0D02
    }

    public enum MatrixMode
    {
        Modelview = 0x1700,
        Projection = 0x1701,
        Texture = 0x1702
    }

    public unsafe class GL
    {
        internal static readonly object Lock = new object();

        internal const string DllWindows = "OpenGL32";
        internal const string DllLinux = "libGL.so.1";
        internal const string DllMac = "/System/Library/Frameworks/OpenGL.framework/OpenGL";
        internal const string DllAndroid = "libopengl.so.1";

        // "opengl32.dll",
        // "libGL.so.2,libGL.so.1,libGL.so",
        // "../Frameworks/OpenGL.framework/OpenGL, /Library/Frameworks/OpenGL.framework/OpenGL, /System/Library/Frameworks/OpenGL.framework/OpenGL"

        static GL()
        {
            LoadAll();
        }

        public static void LoadAll()
        {
            DynamicLibraryFactory.MapLibraryToType<GL>(new DynamicLibraryGl(), "gl");
        }

        private static Dictionary<int, string> Constants;

        public static string GetConstantString(int Value)
        {
            lock (Lock)
            {
                if (Constants == null)
                {
                    Constants = new Dictionary<int, string>();
                    foreach (var Field in typeof(GL).GetFields(BindingFlags.Static | BindingFlags.Public))
                    {
                        if (Field.FieldType == typeof(int))
                        {
#pragma warning disable CS8605
                            Constants[(int)Field.GetValue(null)] = Field.Name;
#pragma warning restore CS8605
                        }
                    }
                }
                return Constants[Value];
            }
        }

        public const int GL_ES_VERSION_2_0 = 1;
        public const int GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const int GL_STENCIL_BUFFER_BIT = 0x00000400;
        public const int GL_COLOR_BUFFER_BIT = 0x00004000;
        public const bool GL_FALSE = false;
        public const bool GL_TRUE = true;
        public const int GL_POINTS = 0x0000;
        public const int GL_LINES = 0x0001;
        public const int GL_LINE_LOOP = 0x0002;
        public const int GL_LINE_STRIP = 0x0003;
        public const int GL_TRIANGLES = 0x0004;
        public const int GL_TRIANGLE_STRIP = 0x0005;
        public const int GL_TRIANGLE_FAN = 0x0006;
        public const int GL_ZERO = 0;
        public const int GL_ONE = 1;
        public const int GL_SRC_COLOR = 0x0300;
        public const int GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const int GL_SRC_ALPHA = 0x0302;
        public const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const int GL_DST_ALPHA = 0x0304;
        public const int GL_ONE_MINUS_DST_ALPHA = 0x0305;
        public const int GL_DST_COLOR = 0x0306;
        public const int GL_ONE_MINUS_DST_COLOR = 0x0307;
        public const int GL_SRC_ALPHA_SATURATE = 0x0308;
        public const int GL_FUNC_ADD = 0x8006;
        public const int GL_BLEND_EQUATION = 0x8009;
        public const int GL_BLEND_EQUATION_RGB = 0x8009;
        public const int GL_BLEND_EQUATION_ALPHA = 0x883D;
        public const int GL_FUNC_SUBTRACT = 0x800A;
        public const int GL_FUNC_REVERSE_SUBTRACT = 0x800B;
        public const int GL_BLEND_DST_RGB = 0x80C8;
        public const int GL_BLEND_SRC_RGB = 0x80C9;
        public const int GL_BLEND_DST_ALPHA = 0x80CA;
        public const int GL_BLEND_SRC_ALPHA = 0x80CB;
        public const int GL_CONSTANT_COLOR = 0x8001;
        public const int GL_ONE_MINUS_CONSTANT_COLOR = 0x8002;
        public const int GL_CONSTANT_ALPHA = 0x8003;
        public const int GL_ONE_MINUS_CONSTANT_ALPHA = 0x8004;
        public const int GL_BLEND_COLOR = 0x8005;
        public const int GL_ARRAY_BUFFER = 0x8892;
        public const int GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const int GL_ARRAY_BUFFER_BINDING = 0x8894;
        public const int GL_ELEMENT_ARRAY_BUFFER_BINDING = 0x8895;
        public const int GL_STREAM_DRAW = 0x88E0;
        public const int GL_STATIC_DRAW = 0x88E4;
        public const int GL_DYNAMIC_DRAW = 0x88E8;
        public const int GL_BUFFER_SIZE = 0x8764;
        public const int GL_BUFFER_USAGE = 0x8765;
        public const int GL_CURRENT_VERTEX_ATTRIB = 0x8626;
        public const int GL_FRONT = 0x0404;
        public const int GL_BACK = 0x0405;
        public const int GL_FRONT_AND_BACK = 0x0408;
        public const int GL_TEXTURE_2D = 0x0DE1;
        public const int GL_CULL_FACE = 0x0B44;
        public const int GL_BLEND = 0x0BE2;
        public const int GL_DITHER = 0x0BD0;
        public const int GL_ALPHA_TEST = 0x0BC0;
        public const int GL_STENCIL_TEST = 0x0B90;
        public const int GL_DEPTH_TEST = 0x0B71;
        public const int GL_SCISSOR_TEST = 0x0C11;
        public const int GL_POLYGON_OFFSET_FILL = 0x8037;
        public const int GL_SAMPLE_ALPHA_TO_COVERAGE = 0x809E;
        public const int GL_SAMPLE_COVERAGE = 0x80A0;
        public const int GL_NO_ERROR = 0;
        public const int GL_INVALID_ENUM = 0x0500;
        public const int GL_INVALID_VALUE = 0x0501;
        public const int GL_INVALID_OPERATION = 0x0502;
        public const int GL_OUT_OF_MEMORY = 0x0505;
        public const int GL_CW = 0x0900;
        public const int GL_CCW = 0x0901;
        public const int GL_LINE_WIDTH = 0x0B21;
        public const int GL_ALIASED_POINT_SIZE_RANGE = 0x846D;
        public const int GL_ALIASED_LINE_WIDTH_RANGE = 0x846E;
        public const int GL_CULL_FACE_MODE = 0x0B45;
        public const int GL_FRONT_FACE = 0x0B46;
        public const int GL_DEPTH_RANGE = 0x0B70;
        public const int GL_DEPTH_WRITEMASK = 0x0B72;
        public const int GL_DEPTH_CLEAR_VALUE = 0x0B73;
        public const int GL_DEPTH_FUNC = 0x0B74;
        public const int GL_STENCIL_CLEAR_VALUE = 0x0B91;
        public const int GL_STENCIL_FUNC = 0x0B92;
        public const int GL_STENCIL_FAIL = 0x0B94;
        public const int GL_STENCIL_PASS_DEPTH_FAIL = 0x0B95;
        public const int GL_STENCIL_PASS_DEPTH_PASS = 0x0B96;
        public const int GL_STENCIL_REF = 0x0B97;
        public const int GL_STENCIL_VALUE_MASK = 0x0B93;
        public const int GL_STENCIL_WRITEMASK = 0x0B98;
        public const int GL_STENCIL_BACK_FUNC = 0x8800;
        public const int GL_STENCIL_BACK_FAIL = 0x8801;
        public const int GL_STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802;
        public const int GL_STENCIL_BACK_PASS_DEPTH_PASS = 0x8803;
        public const int GL_STENCIL_BACK_REF = 0x8CA3;
        public const int GL_STENCIL_BACK_VALUE_MASK = 0x8CA4;
        public const int GL_STENCIL_BACK_WRITEMASK = 0x8CA5;
        public const int GL_VIEWPORT = 0x0BA2;
        public const int GL_SCISSOR_BOX = 0x0C10;
        public const int GL_COLOR_CLEAR_VALUE = 0x0C22;
        public const int GL_COLOR_WRITEMASK = 0x0C23;
        public const int GL_UNPACK_ALIGNMENT = 0x0CF5;
        public const int GL_PACK_ALIGNMENT = 0x0D05;
        public const int GL_MAX_TEXTURE_SIZE = 0x0D33;
        public const int GL_MAX_VIEWPORT_DIMS = 0x0D3A;
        public const int GL_SUBPIXEL_BITS = 0x0D50;
        public const int GL_RED_BITS = 0x0D52;
        public const int GL_GREEN_BITS = 0x0D53;
        public const int GL_BLUE_BITS = 0x0D54;
        public const int GL_ALPHA_BITS = 0x0D55;
        public const int GL_DEPTH_BITS = 0x0D56;
        public const int GL_STENCIL_BITS = 0x0D57;
        public const int GL_POLYGON_OFFSET_UNITS = 0x2A00;
        public const int GL_POLYGON_OFFSET_FACTOR = 0x8038;
        public const int GL_TEXTURE_BINDING_2D = 0x8069;
        public const int GL_SAMPLE_BUFFERS = 0x80A8;
        public const int GL_SAMPLES = 0x80A9;
        public const int GL_SAMPLE_COVERAGE_VALUE = 0x80AA;
        public const int GL_SAMPLE_COVERAGE_INVERT = 0x80AB;
        public const int GL_NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;
        public const int GL_COMPRESSED_TEXTURE_FORMATS = 0x86A3;
        public const int GL_DONT_CARE = 0x1100;
        public const int GL_FASTEST = 0x1101;
        public const int GL_NICEST = 0x1102;
        public const int GL_GENERATE_MIPMAP_HINT = 0x8192;
        public const int GL_BYTE = 0x1400;
        public const int GL_UNSIGNED_BYTE = 0x1401;
        public const int GL_SHORT = 0x1402;
        public const int GL_UNSIGNED_SHORT = 0x1403;
        public const int GL_INT = 0x1404;
        public const int GL_UNSIGNED_INT = 0x1405;
        public const int GL_FLOAT = 0x1406;
        public const int GL_FIXED = 0x140C;
        public const int GL_DEPTH_COMPONENT = 0x1902;
        public const int GL_ALPHA = 0x1906;
        public const int GL_RGB = 0x1907;
        public const int GL_RGBA = 0x1908;
        public const int GL_BGRA = 0x80E1;
        public const int GL_RGBA8 = 0x8058;
        public const int GL_LUMINANCE = 0x1909;
        public const int GL_LUMINANCE_ALPHA = 0x190A;
        public const int GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033;
        public const int GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034;
        public const int GL_UNSIGNED_SHORT_5_6_5 = 0x8363;
        public const int GL_FRAGMENT_SHADER = 0x8B30;
        public const int GL_VERTEX_SHADER = 0x8B31;
        public const int GL_COMPUTE_SHADER = 0x91B9;
        public const int GL_MAX_VERTEX_ATTRIBS = 0x8869;
        public const int GL_MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB;
        public const int GL_MAX_VARYING_VECTORS = 0x8DFC;
        public const int GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;
        public const int GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C;
        public const int GL_MAX_TEXTURE_IMAGE_UNITS = 0x8872;
        public const int GL_MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD;
        public const int GL_SHADER_TYPE = 0x8B4F;
        public const int GL_DELETE_STATUS = 0x8B80;
        public const int GL_LINK_STATUS = 0x8B82;
        public const int GL_VALIDATE_STATUS = 0x8B83;
        public const int GL_ATTACHED_SHADERS = 0x8B85;
        public const int GL_ACTIVE_UNIFORMS = 0x8B86;
        public const int GL_ACTIVE_UNIFORM_MAX_LENGTH = 0x8B87;
        public const int GL_ACTIVE_ATTRIBUTES = 0x8B89;
        public const int GL_ACTIVE_ATTRIBUTE_MAX_LENGTH = 0x8B8A;
        public const int GL_SHADING_LANGUAGE_VERSION = 0x8B8C;
        public const int GL_CURRENT_PROGRAM = 0x8B8D;
        public const int GL_NEVER = 0x0200;
        public const int GL_LESS = 0x0201;
        public const int GL_EQUAL = 0x0202;
        public const int GL_LEQUAL = 0x0203;
        public const int GL_GREATER = 0x0204;
        public const int GL_NOTEQUAL = 0x0205;
        public const int GL_GEQUAL = 0x0206;
        public const int GL_ALWAYS = 0x0207;
        public const int GL_KEEP = 0x1E00;
        public const int GL_REPLACE = 0x1E01;
        public const int GL_INCR = 0x1E02;
        public const int GL_DECR = 0x1E03;
        public const int GL_INVERT = 0x150A;
        public const int GL_INCR_WRAP = 0x8507;
        public const int GL_DECR_WRAP = 0x8508;
        public const int GL_VENDOR = 0x1F00;
        public const int GL_RENDERER = 0x1F01;
        public const int GL_VERSION = 0x1F02;
        public const int GL_EXTENSIONS = 0x1F03;
        public const int GL_NEAREST = 0x2600;
        public const int GL_LINEAR = 0x2601;
        public const int GL_NEAREST_MIPMAP_NEAREST = 0x2700;
        public const int GL_LINEAR_MIPMAP_NEAREST = 0x2701;
        public const int GL_NEAREST_MIPMAP_LINEAR = 0x2702;
        public const int GL_LINEAR_MIPMAP_LINEAR = 0x2703;
        public const int GL_TEXTURE_MAG_FILTER = 0x2800;
        public const int GL_TEXTURE_MIN_FILTER = 0x2801;
        public const int GL_TEXTURE_WRAP_S = 0x2802;
        public const int GL_TEXTURE_WRAP_T = 0x2803;
        public const int GL_TEXTURE = 0x1702;
        public const int GL_TEXTURE_CUBE_MAP = 0x8513;
        public const int GL_TEXTURE_BINDING_CUBE_MAP = 0x8514;
        public const int GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
        public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
        public const int GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
        public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
        public const int GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
        public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
        public const int GL_MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;
        public const int GL_TEXTURE0 = 0x84C0;
        public const int GL_TEXTURE1 = 0x84C1;
        public const int GL_TEXTURE2 = 0x84C2;
        public const int GL_TEXTURE3 = 0x84C3;
        public const int GL_TEXTURE4 = 0x84C4;
        public const int GL_TEXTURE5 = 0x84C5;
        public const int GL_TEXTURE6 = 0x84C6;
        public const int GL_TEXTURE7 = 0x84C7;
        public const int GL_TEXTURE8 = 0x84C8;
        public const int GL_TEXTURE9 = 0x84C9;
        public const int GL_TEXTURE10 = 0x84CA;
        public const int GL_TEXTURE11 = 0x84CB;
        public const int GL_TEXTURE12 = 0x84CC;
        public const int GL_TEXTURE13 = 0x84CD;
        public const int GL_TEXTURE14 = 0x84CE;
        public const int GL_TEXTURE15 = 0x84CF;
        public const int GL_TEXTURE16 = 0x84D0;
        public const int GL_TEXTURE17 = 0x84D1;
        public const int GL_TEXTURE18 = 0x84D2;
        public const int GL_TEXTURE19 = 0x84D3;
        public const int GL_TEXTURE20 = 0x84D4;
        public const int GL_TEXTURE21 = 0x84D5;
        public const int GL_TEXTURE22 = 0x84D6;
        public const int GL_TEXTURE23 = 0x84D7;
        public const int GL_TEXTURE24 = 0x84D8;
        public const int GL_TEXTURE25 = 0x84D9;
        public const int GL_TEXTURE26 = 0x84DA;
        public const int GL_TEXTURE27 = 0x84DB;
        public const int GL_TEXTURE28 = 0x84DC;
        public const int GL_TEXTURE29 = 0x84DD;
        public const int GL_TEXTURE30 = 0x84DE;
        public const int GL_TEXTURE31 = 0x84DF;
        public const int GL_ACTIVE_TEXTURE = 0x84E0;
        public const int GL_REPEAT = 0x2901;
        public const int GL_CLAMP_TO_EDGE = 0x812F;
        public const int GL_MIRRORED_REPEAT = 0x8370;
        public const int GL_FLOAT_VEC2 = 0x8B50;
        public const int GL_FLOAT_VEC3 = 0x8B51;
        public const int GL_FLOAT_VEC4 = 0x8B52;
        public const int GL_INT_VEC2 = 0x8B53;
        public const int GL_INT_VEC3 = 0x8B54;
        public const int GL_INT_VEC4 = 0x8B55;
        public const int GL_BOOL = 0x8B56;
        public const int GL_BOOL_VEC2 = 0x8B57;
        public const int GL_BOOL_VEC3 = 0x8B58;
        public const int GL_BOOL_VEC4 = 0x8B59;
        public const int GL_FLOAT_MAT2 = 0x8B5A;
        public const int GL_FLOAT_MAT3 = 0x8B5B;
        public const int GL_FLOAT_MAT4 = 0x8B5C;
        public const int GL_SAMPLER_2D = 0x8B5E;
        public const int GL_SAMPLER_CUBE = 0x8B60;
        public const int GL_VERTEX_ATTRIB_ARRAY_ENABLED = 0x8622;
        public const int GL_VERTEX_ATTRIB_ARRAY_SIZE = 0x8623;
        public const int GL_VERTEX_ATTRIB_ARRAY_STRIDE = 0x8624;
        public const int GL_VERTEX_ATTRIB_ARRAY_TYPE = 0x8625;
        public const int GL_VERTEX_ATTRIB_ARRAY_NORMALIZED = 0x886A;
        public const int GL_VERTEX_ATTRIB_ARRAY_POINTER = 0x8645;
        public const int GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;
        public const int GL_IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A;
        public const int GL_IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B;
        public const int GL_COMPILE_STATUS = 0x8B81;
        public const int GL_INFO_LOG_LENGTH = 0x8B84;
        public const int GL_SHADER_SOURCE_LENGTH = 0x8B88;
        public const int GL_SHADER_COMPILER = 0x8DFA;
        public const int GL_SHADER_BINARY_FORMATS = 0x8DF8;
        public const int GL_NUM_SHADER_BINARY_FORMATS = 0x8DF9;
        public const int GL_LOW_FLOAT = 0x8DF0;
        public const int GL_MEDIUM_FLOAT = 0x8DF1;
        public const int GL_HIGH_FLOAT = 0x8DF2;
        public const int GL_LOW_INT = 0x8DF3;
        public const int GL_MEDIUM_INT = 0x8DF4;
        public const int GL_HIGH_INT = 0x8DF5;
        public const int GL_FRAMEBUFFER = 0x8D40;
        public const int GL_RENDERBUFFER = 0x8D41;
        public const int GL_RGBA4 = 0x8056;
        public const int GL_RGB5_A1 = 0x8057;
        public const int GL_RGB565 = 0x8D62;
        public const int GL_DEPTH_COMPONENT16 = 0x81A5;
        public const int GL_STENCIL_INDEX = 0x1901;
        public const int GL_STENCIL_INDEX8 = 0x8D48;
        public const int GL_RENDERBUFFER_WIDTH = 0x8D42;
        public const int GL_RENDERBUFFER_HEIGHT = 0x8D43;
        public const int GL_RENDERBUFFER_INTERNAL_FORMAT = 0x8D44;
        public const int GL_RENDERBUFFER_RED_SIZE = 0x8D50;
        public const int GL_RENDERBUFFER_GREEN_SIZE = 0x8D51;
        public const int GL_RENDERBUFFER_BLUE_SIZE = 0x8D52;
        public const int GL_RENDERBUFFER_ALPHA_SIZE = 0x8D53;
        public const int GL_RENDERBUFFER_DEPTH_SIZE = 0x8D54;
        public const int GL_RENDERBUFFER_STENCIL_SIZE = 0x8D55;
        public const int GL_FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE = 0x8CD0;
        public const int GL_FRAMEBUFFER_ATTACHMENT_OBJECT_NAME = 0x8CD1;
        public const int GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL = 0x8CD2;
        public const int GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;
        public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const int GL_DEPTH_ATTACHMENT = 0x8D00;
        public const int GL_STENCIL_ATTACHMENT = 0x8D20;
        public const int GL_NONE = 0;
        public const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
        public const int GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT = 0x8CD6;
        public const int GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;
        public const int GL_FRAMEBUFFER_INCOMPLETE_DIMENSIONS = 0x8CD9;
        public const int GL_FRAMEBUFFER_UNSUPPORTED = 0x8CDD;
        public const int GL_FRAMEBUFFER_BINDING = 0x8CA6;
        public const int GL_RENDERBUFFER_BINDING = 0x8CA7;
        public const int GL_MAX_RENDERBUFFER_SIZE = 0x84E8;
        public const int GL_INVALID_FRAMEBUFFER_OPERATION = 0x0506;
        public const int GL_COLOR_MATERIAL = 0x0B57;
        public const int GL_LIGHTING = 0x0B50;
        public const int GL_FOG = 0x0B60;
        public const int GL_LOGIC_OP = 0x0BF1;
        public const int GL_SHADER_COMPUTER_STATUS = 0x91B9;
        public const int GL_SHADER_INFOLENGTH = 0x8B84;
        public const int GL_SHADER_STORAGE_BARRIER_BIT = 0x2000;
        public const int GL_SRC1_COLOR = 0x88F9;
        public const int GL_SRC1_ALPHA = 0x8589;
        public const int GL_ONE_MINUS_SRC1_COLOR = 0x88FC;
        public const int GL_ONE_MINUS_SRC1_ALPHA = 0x88FD;

        public static readonly glActiveTexture ActiveTexture;
        public static readonly glAttachShader AttachShader;
        public static readonly glBindAttribLocation BindAttribLocation;
        public static readonly glBindBuffer BindBuffer;
        public static readonly glBindFramebuffer BindFramebuffer;
        public static readonly glBindRenderbuffer BindRenderbuffer;
        public static readonly glBindTexture BindTexture;
        public static readonly glBlendColor BlendColor;
        public static readonly glBlendEquation BlendEquation;
        public static readonly glBlendEquationSeparate BlendEquationSeparate;
        public static readonly glBlendFunc BlendFunc;
        public static readonly glBlendFuncSeparate BlendFuncSeparate;
        public static readonly glBufferData BufferData;
        public static readonly glBufferSubData BufferSubData;
        public static readonly glCheckFramebufferStatus CheckFramebufferStatus;
        public static readonly glClear Clear;
        public static readonly glClearColor ClearColor;
        public static readonly glClearDepthf ClearDepthf;
        public static readonly glClearStencil ClearStencil;
        public static readonly glColorMask ColorMask;
        public static readonly glCompileShader CompileShader;
        public static readonly glCompressedTexImage2D CompressedTexImage2D;
        public static readonly glCompressedTexSubImage2D CompressedTexSubImage2D;
        public static readonly glCopyTexImage2D CopyTexImage2D;
        public static readonly glCopyTexSubImage2D CopyTexSubImage2D;
        public static readonly glCreateProgram CreateProgram;
        public static readonly glCreateShader CreateShader;
        public static readonly glCullFace CullFace;
        public static readonly glDeleteBuffers DeleteBuffers;
        public static readonly glDeleteFramebuffers DeleteFramebuffers;
        public static readonly glDeleteProgram DeleteProgram;
        public static readonly glDeleteRenderbuffers DeleteRenderbuffers;
        public static readonly glDeleteShader DeleteShader;
        public static readonly glDeleteTextures DeleteTextures;
        public static readonly glDepthFunc DepthFunc;
        public static readonly glDepthMask DepthMask;
        public static readonly glDepthRangef DepthRangef;

        // IF NOT FOUND DepthRangef
        public static readonly glDepthRange DepthRange;

        public static void GetDepthRange(float near, float far)
        {
            if (DepthRangef != null)
                DepthRangef(near, far);
            else
                DepthRange(near, far);
        }

        public static readonly glDetachShader DetachShader;
        public static readonly glDisable Disable;
        public static readonly glDisableVertexAttribArray DisableVertexAttribArray;
        public static readonly glDrawArrays DrawArrays;
        public static readonly glDrawElements DrawElements;
        public static readonly glEnable Enable;
        public static readonly glEnableVertexAttribArray EnableVertexAttribArray;
        public static readonly glFinish Finish;
        public static readonly glFlush Flush;
        public static readonly glFramebufferRenderbuffer FramebufferRenderbuffer;
        public static readonly glFramebufferTexture2D FramebufferTexture2D;
        public static readonly glFrontFace FrontFace;
        public static readonly glGenBuffers GenBuffers;
        public static readonly glGenerateMipmap GenerateMipmap;
        public static readonly glGenFramebuffers GenFramebuffers;
        public static readonly glGenRenderbuffers GenRenderbuffers;

        //static public uint GenTexture()
        //{
        //	uint Texture;
        //	glGenTextures(1, &Texture);
        //	return Texture;
        //}

        public static int GetInteger(int Name)
        {
            int Out = 0;
            GetIntegerv(Name, &Out);
            return Out;
        }

        //public static string GetString(int Name) => Marshal.PtrToStringAnsi(new intptr(glGetString(Name)));
        public static string GetStringStr(int Name)
        {
            var str = Marshal.PtrToStringAnsi(GetString(Name));
            if (str == null) return "";
            return str;
        }

        public static readonly glGenTextures GenTextures;
        public static readonly glGetActiveAttrib GetActiveAttrib;
        public static readonly glGetActiveUniform GetActiveUniform;
        public static readonly glGetAttachedShaders GetAttachedShaders;
        public static readonly glGetAttribLocation GetAttribLocation;
        public static readonly glGetBooleanv GetBooleanv;
        public static readonly glGetBufferParameteriv GetBufferParameteriv;
        public static readonly glGetError GetError;
        public static readonly glGetFloatv GetFloatv;
        public static readonly glGetFramebufferAttachmentParameteriv GetFramebufferAttachmentParameteriv;
        public static readonly glGetIntegerv GetIntegerv;
        public static readonly glGetProgramiv GetProgramiv;
        public static readonly glGetProgramInfoLog GetProgramInfoLog;
        public static readonly glGetRenderbufferParameteriv GetRenderbufferParameteriv;
        public static readonly glGetShaderiv GetShaderiv;
        public static readonly glGetShaderInfoLog GetShaderInfoLog;
        public static readonly glGetShaderPrecisionFormat GetShaderPrecisionFormat;
        public static readonly glGetShaderSource GetShaderSource;
        public static readonly glGetString GetString;
        public static readonly glGetTexParameterfv GetTexParameterfv;
        public static readonly glGetTexParameteriv GetTexParameteriv;
        public static readonly glGetUniformfv GetUniformfv;
        public static readonly glGetUniformiv GetUniformiv;
        public static readonly glGetUniformLocation GetUniformLocation;
        public static readonly glGetVertexAttribfv GetVertexAttribfv;
        public static readonly glGetVertexAttribiv GetVertexAttribiv;
        public static readonly glGetVertexAttribPointerv GetVertexAttribPointerv;
        public static readonly glHint Hint;
        public static readonly glIsBuffer IsBuffer;
        public static readonly glIsEnabled IsEnabled;
        public static readonly glIsFramebuffer IsFramebuffer;
        public static readonly glIsProgram IsProgram;
        public static readonly glIsRenderbuffer IsRenderbuffer;
        public static readonly glIsShader IsShader;
        public static readonly glIsTexture IsTexture;
        public static readonly glLineWidth LineWidth;
        public static readonly glLinkProgram LinkProgram;
        public static readonly glPixelStorei PixelStorei;
        public static readonly glPolygonOffset PolygonOffset;
        public static readonly glReadPixels ReadPixels;
        public static readonly glReleaseShaderCompiler ReleaseShaderCompiler;
        public static readonly glRenderbufferStorage RenderbufferStorage;
        public static readonly glSampleCoverage SampleCoverage;
        public static readonly glScissor Scissor;
        public static readonly glShaderBinary ShaderBinary;
        public static readonly glShaderSource ShaderSource;
        public static readonly glStencilFunc StencilFunc;
        public static readonly glStencilFuncSeparate StencilFuncSeparate;
        public static readonly glStencilMask StencilMask;
        public static readonly glStencilMaskSeparate StencilMaskSeparate;
        public static readonly glStencilOp StencilOp;
        public static readonly glStencilOpSeparate StencilOpSeparate;
        public static readonly glTexImage2D TexImage2D;
        public static readonly glTexParameterf TexParameterf;
        public static readonly glTexParameterfv TexParameterfv;
        public static readonly glTexParameteri TexParameteri;
        public static readonly glTexParameteriv TexParameteriv;
        public static readonly glTexSubImage2D TexSubImage2D;
        public static readonly glUniform1f Uniform1f;
        public static readonly glUniform1fv Uniform1fv;
        public static readonly glUniform1i Uniform1i;
        public static readonly glUniform1iv Uniform1iv;
        public static readonly glUniform2f Uniform2f;
        public static readonly glUniform2fv Uniform2fv;
        public static readonly glUniform2i Uniform2i;
        public static readonly glUniform2iv Uniform2iv;
        public static readonly glUniform3f Uniform3f;
        public static readonly glUniform3fv Uniform3fv;
        public static readonly glUniform3i Uniform3i;
        public static readonly glUniform3iv Uniform3iv;
        public static readonly glUniform4f Uniform4f;
        public static readonly glUniform4fv Uniform4fv;
        public static readonly glUniform4i Uniform4i;
        public static readonly glUniform4iv Uniform4iv;
        public static readonly glUniformMatrix2fv UniformMatrix2fv;
        public static readonly glUniformMatrix3fv UniformMatrix3fv;
        public static readonly glUniformMatrix4fv UniformMatrix4fv;
        public static readonly glUseProgram UseProgram;
        public static readonly glValidateProgram ValidateProgram;
        public static readonly glVertexAttrib1f VertexAttrib1f;
        public static readonly glVertexAttrib1fv VertexAttrib1fv;
        public static readonly glVertexAttrib2f VertexAttrib2f;
        public static readonly glVertexAttrib2fv VertexAttrib2fv;
        public static readonly glVertexAttrib3f VertexAttrib3f;
        public static readonly glVertexAttrib3fv VertexAttrib3fv;
        public static readonly glVertexAttrib4f VertexAttrib4f;
        public static readonly glVertexAttrib4fv VertexAttrib4fv;
        public static readonly glVertexAttribPointer VertexAttribPointer;
        public static readonly glViewport Viewport;
        public static readonly glMaterialfv Materialfv;
        public static readonly glMaterialf Materialf;
        public static readonly glMateriali Materiali;
        public static readonly glAlphaFunc AlphaFunc;
        public static readonly glGetUniformBlockIndex GetUniformBlockIndex;
        public static readonly glUniformBlockBinding UniformBlockBinding;
        public static readonly glDispatchCompute DispatchCompute;
        public static readonly glMemoryBarrier MemoryBarrier;
        public static readonly glVertexAttribIPointer VertexAttribIPointer;
        public static readonly glGenVertexArrays GenVertexArrays;
        public static readonly glDeleteVertexArrays DeleteVertexArrays;
        public static readonly glBindVertexArray BindVertexArray;
        public static readonly glBlitFramebuffer BlitFramebuffer;
        public static readonly glLoadIdentity LoadIdentity;
        public static readonly glMatrixMode MatrixMode;
        public static readonly glOrtho Ortho;

        public static void ClearError()
        {
            while (GetError() != GL_NO_ERROR)
            {
            }
        }

        [DebuggerHidden]
        public static void CheckError(string prefix = "")
        {
            try
            {
                var Error = GetError();
                if (Error != GL_NO_ERROR)
                    throw new Exception($"{prefix} glError: 0x{Error:X4}");
            } finally
            {
                ClearError();
            }
        }

        public static bool EnableDisable(int EnableCap, bool EnableDisable)
        {
            if (EnableDisable)
            {
                Enable(EnableCap);
            } else
            {
                Disable(EnableCap);
            }
            return EnableDisable;
        }

        // REMOVE! Not available in OpenGL|ES
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void glGetTexImage_(int texture, int level, int format, int type, void* img);

        public static readonly glGetTexImage_ GetTexImage;

        public const int GL_MAJOR_VERSION = 0x821B;
        public const int GL_MINOR_VERSION = 0x821C;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glActiveTexture(int texture);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glAttachShader(uint program, uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBindAttribLocation(uint program, uint index, string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBindBuffer(int target, uint buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBindFramebuffer(int target, uint framebuffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBindRenderbuffer(int target, uint renderbuffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBindTexture(int target, uint texture);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBlendColor(float red, float green, float blue, float alpha);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBlendEquation(int mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBlendEquationSeparate(int modeRGB, int modeAlpha);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBlendFunc(int sfactor, int dfactor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glBlendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glBufferData(int target, uint size, void* data, int usage);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glBufferSubData(int target, int offset, uint size, void* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate int glCheckFramebufferStatus(int target);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glClear(uint mask);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glClearColor(float red, float green, float blue, float alpha);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glClearDepthf(float depth);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glClearStencil(int s);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glColorMask(bool red, bool green, bool blue, bool alpha);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glCompileShader(uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glCompressedTexImage2D(int target, int level, int internalformat, int width,
        int height, int border, int imageSize, void* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glCompressedTexSubImage2D(int target, int level, int xoffset, int yoffset,
        int width, int height, int format, int imageSize, void* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glCopyTexImage2D(int target, int level, int internalformat, int x, int y,
        int width, int height, int border);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glCopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x,
        int y, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glCreateProgram();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glCreateShader(int type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glCullFace(int mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glDeleteBuffers(int n, uint* buffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glDeleteFramebuffers(int n, uint* framebuffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDeleteProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glDeleteRenderbuffers(int n, uint* renderbuffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDeleteShader(uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glDeleteTextures(int n, uint* textures);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDepthFunc(int func);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDepthMask(bool flag);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDepthRangef(float zNear, float zFar);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDepthRange(double zNear, double zFar);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDetachShader(uint program, uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDisable(int cap);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDisableVertexAttribArray(uint index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glDrawArrays(int mode, int first, int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glDrawElements(int mode, int count, int type, void* indices);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glEnable(int cap);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glEnableVertexAttribArray(uint index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glFinish();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glFlush();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glFramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glFramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glFrontFace(int mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGenBuffers(int n, uint* buffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glGenerateMipmap(int target);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGenFramebuffers(int n, uint* framebuffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGenRenderbuffers(int n, uint* renderbuffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGenTextures(int n, uint* textures);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetActiveAttrib(uint program, uint index, int bufsize, int* length, int* size, int* type, byte* name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetActiveUniform(uint program, uint index, int bufsize, int* length, int* size, int* type, byte* name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetAttachedShaders(uint program, int maxcount, int* count, uint* shaders);

    //[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity] unsafe public delegate uint glGetAttribLocation(GLuint program, char* name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
    public delegate int glGetAttribLocation(uint program, string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetBooleanv(int pname, bool* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetBufferParameteriv(int target, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate int glGetError();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetFloatv(int pname, float* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetIntegerv(int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetProgramiv(uint program, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetProgramInfoLog(uint program, int bufsize, int* length, byte* infolog);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetRenderbufferParameteriv(int target, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetShaderiv(uint shader, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetShaderInfoLog(uint shader, int bufsize, int* length, byte* infolog);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetShaderPrecisionFormat(int shadertype, int precisiontype, int* range, int* precision);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetShaderSource(uint shader, int bufsize, int* length, byte* source);

    //[System.CLSCompliant(false)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    //public unsafe delegate string GetString(int name);
    //public unsafe delegate byte* GetString(int name);
    public unsafe delegate IntPtr glGetString(int name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetTexParameterfv(int target, int pname, float* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetTexParameteriv(int target, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetUniformfv(uint program, int location, float* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetUniformiv(uint program, int location, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate int glGetUniformLocation(uint program, string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetVertexAttribfv(uint index, int pname, float* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetVertexAttribiv(uint index, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glGetVertexAttribPointerv(uint index, int pname, void** pointer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glHint(int target, int mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsBuffer(uint buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsEnabled(int cap);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsFramebuffer(uint framebuffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsRenderbuffer(uint renderbuffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsShader(uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate bool glIsTexture(uint texture);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glLineWidth(float width);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glLinkProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glPixelStorei(int pname, int param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glPolygonOffset(float factor, float units);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glReadPixels(int x, int y, int width, int height, int format, int type, void* pixels);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glReleaseShaderCompiler();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glRenderbufferStorage(int target, int internalformat, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glSampleCoverage(float value, bool invert);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glScissor(int x, int y, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glShaderBinary(int n, uint* shaders, int binaryformat, void* binary, int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glShaderSource(uint shader, int count, byte** @string, int* length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glStencilFunc(int func, int @ref, uint mask);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glStencilFuncSeparate(int face, int func, int @ref, uint mask);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glStencilMask(uint mask);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glStencilMaskSeparate(int face, uint mask);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glStencilOp(int fail, int zfail, int zpass);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glStencilOpSeparate(int face, int fail, int zfail, int zpass);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glTexImage2D(int target, int level, int internalformat, int width,
        int height, int border, int format, int type, void* pixels);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glTexParameterf(int target, int pname, float param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glTexParameterfv(int target, int pname, float* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glTexParameteri(int target, int pname, int param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glTexParameteriv(int target, int pname, int* @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width,
        int height, int format, int type, void* pixels);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform1f(int location, float x);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform1fv(int location, int count, float* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform1i(int location, int x);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform1iv(int location, int count, int* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform2f(int location, float x, float y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform2fv(int location, int count, float* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform2i(int location, int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform2iv(int location, int count, int* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform3f(int location, float x, float y, float z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform3fv(int location, int count, float* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform3i(int location, int x, int y, int z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform3iv(int location, int count, int* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform4f(int location, float x, float y, float z, float w);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform4fv(int location, int count, float* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUniform4i(int location, int x, int y, int z, int w);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniform4iv(int location, int count, int* v);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniformMatrix2fv(int location, int count, bool transpose, float* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniformMatrix3fv(int location, int count, bool transpose, float* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glUniformMatrix4fv(int location, int count, bool transpose, float* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glUseProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glValidateProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glVertexAttrib1f(uint indx, float x);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glVertexAttrib1fv(uint indx, float* values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glVertexAttrib2f(uint indx, float x, float y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glVertexAttrib2fv(uint indx, float* values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glVertexAttrib3f(uint indx, float x, float y, float z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glVertexAttrib3fv(uint indx, float* values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glVertexAttrib4f(uint indx, float x, float y, float z, float w);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glVertexAttrib4fv(uint indx, float* values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glVertexAttribPointer(uint indx, int size, int type, bool normalized, int stride, void* ptr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate void glVertexAttribIPointer(uint indx, int size, int type, int stride, void* ptr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glViewport(int x, int y, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glMaterialfv(int face, int pname, float[] @params);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glMaterialf(int face, int pname, float param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glMateriali(int face, int pname, int param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate void glAlphaFunc(int func, float refValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glGetUniformBlockIndex(uint program, string uniformBlockName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glUniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glDispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glMemoryBarrier(uint barriers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate uint glGenVertexArrays(uint num_groups_x, uint* num_);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public unsafe delegate uint glDeleteVertexArrays(uint num_groups_x, uint* num_);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glBindVertexArray(uint array);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1,
        int dstX0, int dstY0, int dstX1, int dstY1, uint mask, int filter);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glLoadIdentity();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glMatrixMode(int mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate uint glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);

}
