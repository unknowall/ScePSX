using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LightGL.DynamicLibrary
{
    public static class DynamicLibraryFactory
    {
        static string name;

        public static IDynamicLibrary CreateForLibrary(string nameWindows, string nameLinux = "", string nameMac = "", string nameAndroid = "")
        {
            if (nameLinux == null)
                nameLinux = nameWindows;
            if (nameMac == null)
                nameMac = nameLinux;
            if (nameAndroid == null)
                nameAndroid = nameLinux;

            switch (Platform.OS)
            {
                case OS.Windows:
                    name = nameWindows;
                    break;
                case OS.Mac:
                    name = nameMac;
                    break;
                case OS.Android:
                    name = nameAndroid;
                    break;
                case OS.IOS:
                    name = nameWindows;
                    break;
                case OS.Linux:
                    name = nameLinux;
                    break;
                default:
                    name = nameLinux;
                    break;
            }

            switch (Platform.OS)
            {
                case OS.Windows:
                    return new DynamicLibraryWindows(name);
                case OS.Mac:
                    return new DynamicLibraryMac(name);
                default:
                    return new DynamicLibraryPosix(name);
            }
        }

        public static void MapLibraryToType<TType>(IDynamicLibrary dynamicLibrary, string prefix = "")
        {
            var type = typeof(TType);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.FieldType.IsSubclassOf(typeof(Delegate)))
                    continue;
                if (field.GetValue(null) != null)
                    continue;
                var method = dynamicLibrary.GetMethod(prefix + field.Name);
                if (method != nint.Zero)
                {
                    field.SetValue(null, Marshal.GetDelegateForFunctionPointer(method, field.FieldType));
                } else
                {
                    Console.WriteLine($"GetProcAddress {name} Not Found {field.Name} : {method}");
                }
            }
        }
    }
}
