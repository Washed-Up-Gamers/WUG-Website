﻿using System.Text.Json.Serialization;

namespace WUG.Scripting.Parser;

public enum ObjType
{
    Decimal,
    Boolean,
    Long,
    String,
    LuaTable,
    LuaMainTable,
    StringForNumber,
    LuaArray
}

public abstract class ILuaObject : IConvertible
{
    public string Name { get; set; }
    public ObjType type { get; set; }
    public string Value { get; set; }

    public int IPosition { get; set; } = 0;

    [JsonIgnore]
    public LuaTable Parent { get; set; }

    [JsonIgnore]
    public int LineNumber { get; set; }

    [JsonIgnore]
    public string FileName { get; set; }

    public TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    public bool ToBoolean(IFormatProvider? provider)
    {
        return Convert.ToBoolean(Value, provider);
    }

    public byte ToByte(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public char ToChar(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public decimal ToDecimal(IFormatProvider? provider)
    {
        return Convert.ToDecimal(Value, provider);
    }

    public double ToDouble(IFormatProvider? provider)
    {
        return Convert.ToDouble(Value, provider);
    }

    public short ToInt16(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public int ToInt32(IFormatProvider? provider)
    {
        return Convert.ToInt32(Value, provider);
    }

    public long ToInt64(IFormatProvider? provider)
    {
        return Convert.ToInt64(Value, provider);
    }

    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public float ToSingle(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public string ToString(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public uint ToUInt32(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }
}

[JsonDerivedType(typeof(LuaObject), 0)]
[JsonDerivedType(typeof(LuaTable), 1)]
public class LuaObject : ILuaObject
{

}
