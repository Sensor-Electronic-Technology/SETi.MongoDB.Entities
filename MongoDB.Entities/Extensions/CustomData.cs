using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Entities;

public static partial class Extensions {
    /*public static object As(this BsonValue value, BsonType type) {
        return type switch {
            BsonType.Null => BsonNull.Value,
            BsonType.String => value.AsString,
            BsonType.ObjectId => value.AsObjectId,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.Decimal128 => value.AsDecimal128,
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.AsUniversalTime,
            BsonType.Array => value.AsBsonArray,
            BsonType.Document => value.AsBsonDocument,
            _ => value
        };
    }*/

    public static object GetDefault(this DataType type)
        => type switch {
            DataType.NUMBER => 0.00,
            DataType.STRING => "",
            DataType.BOOLEAN => false,
            DataType.DATE => DateTime.MinValue,
            DataType.LIST_NUMBER => new List<double>(),
            DataType.LIST_STRING => new List<string>(),
            DataType.LIST_BOOLEAN => new List<bool>(),
            DataType.LIST_DATE => new List<DateTime>(),
            _ => throw new ArgumentException("Empty Value type not supported")
        };

    public static BsonType ToBsonType(this DataType type) {
        return type switch {
            DataType.NUMBER => BsonType.Double,
            DataType.STRING => BsonType.String,
            DataType.BOOLEAN => BsonType.Boolean,
            DataType.DATE => BsonType.DateTime,
            DataType.LIST_NUMBER => BsonType.Double,
            DataType.LIST_STRING => BsonType.String,
            DataType.LIST_BOOLEAN => BsonType.Boolean,
            DataType.LIST_DATE => BsonType.DateTime,
            _ => throw new ArgumentException("Empty Value type not supported")
        };
    }

    public static TypeCode ToTypeCode(this DataType type) {
        return type switch {
            DataType.NUMBER => TypeCode.Double,
            DataType.STRING => TypeCode.String,
            DataType.BOOLEAN => TypeCode.Boolean,
            DataType.DATE => TypeCode.DateTime,
            DataType.LIST_NUMBER => TypeCode.Double,
            DataType.LIST_STRING => TypeCode.String,
            DataType.LIST_BOOLEAN => TypeCode.Boolean,
            DataType.LIST_DATE => TypeCode.DateTime,
            _ => throw new ArgumentException("Empty Value type not supported")
        };
    }

    public static object ToDataType(this BsonDocument doc, DataType type, string property) {
        if (doc.Contains(property)) {
            return type switch {
                DataType.NUMBER => doc[property].AsDouble,
                DataType.STRING => doc[property].AsString,
                DataType.BOOLEAN => doc[property].AsBoolean,
                DataType.DATE => doc[property].AsString,
                DataType.LIST_NUMBER => doc[property].AsBsonArray.Select(e => e.AsDouble),
                DataType.LIST_STRING => doc[property].AsBsonArray.Select(e => e.AsString),
                DataType.LIST_BOOLEAN => doc[property].AsBsonArray.Select(e => e.AsBoolean),
                DataType.LIST_DATE => doc[property].AsBsonArray.Select(e => e.AsUniversalTime),
                _ => throw new ArgumentException("Empty Value type not supported")
            };
        }
        return type.GetDefault();
    }

    public static object ToDataType(this IEnumerable<BsonValue> query, DataType type, string Property) {
        var bsonValues = query as BsonValue[] ?? query.ToArray();

        if (bsonValues.Any()) {
            return type switch {
                DataType.NUMBER => bsonValues.Select(e => e[Property].AsDouble).FirstOrDefault(),
                DataType.STRING => bsonValues.Select(e => e[Property].AsString).FirstOrDefault() ?? "",
                DataType.BOOLEAN => bsonValues.Select(e => e[Property].AsBoolean).FirstOrDefault(),
                DataType.DATE => bsonValues.Select(e => e[Property].AsUniversalTime).FirstOrDefault(),
                DataType.LIST_NUMBER => bsonValues.Select(e => e[Property].AsDouble),
                DataType.LIST_STRING => bsonValues.Select(e => e[Property].AsString),
                DataType.LIST_BOOLEAN => bsonValues.Select(e => e[Property].AsBoolean),
                DataType.LIST_DATE => bsonValues.Select(e => e[Property].AsUniversalTime),
                _ => throw new ArgumentException("Empty Value type not supported")
            };
        }
        return type.GetDefault();
    }

    public static object ToDataType(this IQueryable<BsonValue> query, DataType type, string Property) {
        if (query.Any()) {
            return type switch {
                DataType.NUMBER => query.Select(e => e[Property].AsDouble).FirstOrDefault(),
                DataType.STRING => query.Select(e => e[Property].AsString).FirstOrDefault() ?? "",
                DataType.BOOLEAN => query.Select(e => e[Property].AsBoolean).FirstOrDefault(),
                DataType.DATE => query.Select(e => e[Property].AsUniversalTime).FirstOrDefault(),
                DataType.LIST_NUMBER => query.Select(e => e[Property].AsDouble),
                DataType.LIST_STRING => query.Select(e => e[Property].AsString),
                DataType.LIST_BOOLEAN => query.Select(e => e[Property].AsBoolean),
                DataType.LIST_DATE => query.Select(e => e[Property].AsUniversalTime),
                _ => throw new ArgumentException("Empty Value type not supported")
            };
        }
        return type.GetDefault();
    }

    public static object ToDataType(this IEnumerable<BsonDocument> query, DataType type, string Property) {
        var bsonValues = query as BsonValue[] ?? query.ToArray();

        if (bsonValues.Any()) {
            return type switch {
                DataType.NUMBER => bsonValues.Select(e => e[Property].AsDouble).FirstOrDefault(),
                DataType.STRING => bsonValues.Select(e => e[Property].AsString).FirstOrDefault() ?? "",
                DataType.BOOLEAN => bsonValues.Select(e => e[Property].AsBoolean).FirstOrDefault(),
                DataType.DATE => bsonValues.Select(e => e[Property].AsUniversalTime).FirstOrDefault(),
                DataType.LIST_NUMBER => bsonValues.Select(e => e[Property].AsDouble),
                DataType.LIST_STRING => bsonValues.Select(e => e[Property].AsString),
                DataType.LIST_BOOLEAN => bsonValues.Select(e => e[Property].AsBoolean),
                DataType.LIST_DATE => bsonValues.Select(e => e[Property].AsUniversalTime),
                _ => throw new ArgumentException("Empty Value type not supported")
            };
        }
        return type.GetDefault();
    }

    public static object ToDataType(this IQueryable<BsonDocument> query, DataType type, string Property) {
        if (query.Any()) {
            return type switch {
                DataType.NUMBER => query.Select(e => e[Property].AsDouble).FirstOrDefault(),
                DataType.STRING => query.Select(e => e[Property].AsString).FirstOrDefault() ?? "",
                DataType.BOOLEAN => query.Select(e => e[Property].AsBoolean).FirstOrDefault(),
                DataType.DATE => query.Select(e => e[Property].AsUniversalTime).FirstOrDefault(),
                DataType.LIST_NUMBER => query.Select(e => e[Property].AsDouble),
                DataType.LIST_STRING => query.Select(e => e[Property].AsString),
                DataType.LIST_BOOLEAN => query.Select(e => e[Property].AsBoolean),
                DataType.LIST_DATE => query.Select(e => e[Property].AsUniversalTime),
                _ => throw new ArgumentException("Empty Value type not supported")
            };
        }
        return type.GetDefault();
    }
}