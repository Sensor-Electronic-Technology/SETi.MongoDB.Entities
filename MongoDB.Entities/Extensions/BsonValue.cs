using MongoDB.Bson;

namespace MongoDB.Entities;

public static partial class Extensions  {
    public static object As(this BsonValue value, BsonType type) {
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
    }
}