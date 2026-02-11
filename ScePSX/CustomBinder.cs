using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

public class FastBinder : SerializationBinder
{
    private static readonly Dictionary<string, Type> _typeMap = new()
    {
        // Queue<byte>
        ["System.Collections.Generic.Queue`1[[System.Byte, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"] = typeof(Queue<byte>),
        // Queue<short>
        ["System.Collections.Generic.Queue`1[[System.Int16, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"] = typeof(Queue<short>),

        ["System.Collections.Generic.Queue`1[[ScePSX.CdRom.Response, Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]"] =
            typeof(Queue<ScePSX.CdRom.Response>),
    };

    public override Type BindToType(string assemblyName, string typeName)
    {
        if (_typeMap.TryGetValue(typeName, out var mappedType))
        {
            //Console.WriteLine($"映射: {typeName} -> {mappedType.Name}");
            return mappedType;
        }

        return null;
    }
}
