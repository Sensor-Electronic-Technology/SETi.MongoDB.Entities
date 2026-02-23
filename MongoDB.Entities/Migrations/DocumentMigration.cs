using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[Collection("_entity_migrations_"),
 BsonDiscriminator(RootClass = true), 
 BsonKnownTypes(typeof(DocumentMigration),typeof(EmbeddedMigration))]
public class EntityMigration : Entity, IDocumentMigration, ICreatedOn {
    public DateTime CreatedOn { get; set; }
    public DateTime MigratedOn { get; set; }
    public int MigrationNumber { get; set; }
    public bool IsMajorVersion { get; set; }
    public bool IsMigrated { get; set; }
    public DocumentVersion Version { get; set; }
    public List<FieldOperation> UpOperations { get; set; } = [];
    public List<FieldOperation> DownOperations { get; set; } = [];
}

[Collection("_entity_migrations_")]
public class DocumentMigration : EntityMigration {
    public One<DocumentTypeConfiguration>? TypeConfiguration { get; set; }
}

[Collection("_entity_migrations_")]
public class EmbeddedMigration : EntityMigration {
    public One<EmbeddedTypeConfiguration>? EmbeddedTypeConfiguration { get; set; }
    public string ParentTypeName { get; set; } = null!;
}


