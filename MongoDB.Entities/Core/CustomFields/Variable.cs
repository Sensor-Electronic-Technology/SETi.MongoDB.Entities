using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(ValueVariable), 
     typeof(OwnedCollectionPropertyVariable), 
     typeof(PropertyVariable),
     typeof(ExternalPropertyVariable),
     typeof(ExternalCollectionPropertyVariable),
     typeof(OwnedEmbeddedPropertyVariable))]
public class Variable {
    public string VariableName { get; set; } = null!;
    public DataType DataType { get; set; }
    public TypeCode TypeCode { get; set; }
    public BsonType BsonType { get; set; }
}
/// <summary>
/// Property from the owning entity
/// Object.Property
/// </summary>
public class PropertyVariable : Variable {
    public string Property { get; set; } = null!;
}

/// <summary>
/// Static value
/// Could be false, pass, 1, 2.5, etc
/// </summary>
public class ValueVariable : Variable {
    public object Value { get; set; } = null!;
}

/// <summary>
/// Object.OwningProperty.Property
/// </summary>
public class OwnedEmbeddedPropertyVariable : PropertyVariable {
    /// <summary>
    /// Path to the embedded object property
    /// the last item in the list is the target object with the embedded property in it
    /// </summary>
    public IList<string> EmbeddedObjectPropertyPath { get; set; } = [];
    public string EmbeddedProperty { get; set; } = null!;
}

/// <summary>
/// Property from a reference collection
/// Database.Collection.Collection.Where(Filter).Select(Property)
/// If(FilterOnEntityId) Database.Collection.Where(EntityIdFilter && Filter).Select(Property)
/// </summary>
public class ExternalPropertyVariable:PropertyVariable {
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public bool FilterOnEntityId { get; set; } = false;
    public string EntityIdProperty { get; set; } = string.Empty;
    public string RefEntityIdProperty { get; set; } = string.Empty;
    public Filter? Filter { get; set; }
    
}

/// <summary>
/// Collection in the owning entity
/// Object.Collection.Where(Filter).Property
/// </summary>
public class OwnedCollectionPropertyVariable:PropertyVariable {
    public string CollectionProperty { get; set; } = null!;
    public Filter? Filter { get; set; }
}

/// <summary>
/// Property from a reference collection with output of list type
/// Database.Collection.Where(Filter).Collection(Filter).Property
/// </summary>
/// 
public class ExternalCollectionPropertyVariable:PropertyVariable{
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public bool FilterOnEntityId { get; set; } = false;
    public string EntityIdProperty { get; set; } = string.Empty;
    public string RefEntityIdProperty { get; set; } = string.Empty;
    public Filter? Filter { get; set; }
    public string CollectionProperty { get; set; } = null!;
    public Filter? SubFilter { get; set; }
}






