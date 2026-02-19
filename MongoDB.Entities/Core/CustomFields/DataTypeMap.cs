using System.Collections.Generic;
using Ardalis.SmartEnum;
using MongoDB.Bson;

namespace MongoDB.Entities;

public record CompatibleTypes(TypeCode[] TypeCodes, BsonType[] BsonTypes);

public static class DataTypeMap {
    /*public static Dictionary<DataType, CompatibleTypes> Map { get; } = new() {
        { DataType.STRING, new(new[] { TypeCode.String }, new[] { BsonType.String }) }, 
        { DataType.NUMBER, new([TypeCode.Decimal, TypeCode.Double, TypeCode.Single, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64], [BsonType.Double, BsonType.Int32, BsonType.Int64, BsonType.Decimal128]) },
        { DataType.BOOLEAN, new(new[] { TypeCode.Boolean }, new[] { BsonType.Boolean }) },
        { DataType.DATE, new(new[] { TypeCode.DateTime }, new[] { BsonType.DateTime }) },
        { DataType.LIST_DATE, new(new[] { TypeCode.DateTime }, new[] { BsonType.Array }) }, 
        { DataType.LIST_NUMBER, new([TypeCode.Decimal, TypeCode.Double, TypeCode.Single, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64], [BsonType.Array]) },
        { DataType.LIST_STRING, new(new[] { TypeCode.String }, new[] { BsonType.Array }) },
        { DataType.LIST_BOOLEAN, new(new[] { TypeCode.Boolean }, new[] { BsonType.Array }) }
        
    };*/
    public static Dictionary<DataType,BsonType> BsonTypeLookup { get; } = new() {
        { DataType.STRING, BsonType.String }, 
        { DataType.NUMBER, BsonType.Double },
        { DataType.BOOLEAN, BsonType.Boolean},
        { DataType.DATE, BsonType.DateTime },
        { DataType.LIST_DATE, BsonType.String }, 
        { DataType.LIST_NUMBER, BsonType.Double },
        { DataType.LIST_STRING, BsonType.String},
        { DataType.LIST_BOOLEAN, BsonType.Boolean}
    };
    
    public static Dictionary<DataType,TypeCode> TypeCodeLookup { get; } = new() {
        { DataType.STRING, TypeCode.String }, 
        { DataType.NUMBER, TypeCode.Double },
        { DataType.BOOLEAN, TypeCode.Boolean},
        { DataType.DATE, TypeCode.DateTime },
        { DataType.LIST_DATE, TypeCode.DateTime }, 
        { DataType.LIST_NUMBER, TypeCode.Double },
        { DataType.LIST_STRING, TypeCode.String},
        { DataType.LIST_BOOLEAN, TypeCode.Boolean}
    };

    public static object Fetch() {
        return new();
    }
}