using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities;

public abstract class MigrateBuilder<TConfig, TMigration, TBuilder>
    where TConfig : TypeConfiguration
    where TMigration : EntityMigration
    where TBuilder : MigrateBuilder<TConfig, TMigration, TBuilder>,new() {
    public List<FieldOperation> Operations { get; } = [];
    protected abstract TBuilder Builder { get; }
    protected abstract TConfig? TypeConfiguration { get; set; }
    protected abstract TMigration Migration { get; } 
    public abstract TBuilder WithTypeConfiguration(TConfig typeConfiguration);
    
    public static TBuilder CreateBuilder()=>new();
    
    public TBuilder WithMigrationNumber(int migrationNumber) {
        this.Migration.MigrationNumber = migrationNumber;
        return this.Builder;
    }

    public virtual TBuilder AddField<TField, TFieldBuilder>(Action<TFieldBuilder> fieldConfig)
        where TField : Field, new() where TFieldBuilder : FieldBuilderBase<TField, TFieldBuilder>, new() {
        var builder = new TFieldBuilder();
        fieldConfig(builder);
        var field = builder.Build();
        var operation = new AddFieldOperation {
            Field = field,
            IsDestructive = false
        };
        Operations.Add(operation);
        return this.Builder;
    }

    public virtual TBuilder AddField(Field field) {
        var operation = new AddFieldOperation {
            Field = field,
            IsDestructive = false
        };
        Operations.Add(operation);
        return this.Builder;
    }

    public virtual TBuilder DropField<TField, TFieldBuilder>(Action<TFieldBuilder> fieldConfig)
        where TField : Field, new() where TFieldBuilder : FieldBuilderBase<TField, TFieldBuilder>, new() {
        var builder = new TFieldBuilder();
        fieldConfig(builder);
        var field = builder.Build();
        var operation = new DropFieldOperation {
            Field = field,
            IsDestructive = true
        };
        Operations.Add(operation);
        return this.Builder;
    }

    public virtual TBuilder DropField(Field field) {
        var operation = new DropFieldOperation {
            Field = field,
            IsDestructive = true
        };
        Operations.Add(operation);
        return this.Builder;
    }

    public virtual TBuilder AlterField<TField, TFieldBuilder>(Action<TFieldBuilder> fieldConfig, Field oldField)
        where TField : Field, new() where TFieldBuilder : FieldBuilderBase<TField, TFieldBuilder>, new() {
        var builder = new TFieldBuilder();
        fieldConfig(builder);
        var field = builder.Build();
        var operation = new AlterFieldOperation {
            Field = field,
            OldField = oldField,
            IsDestructive = true
        };
        Operations.Add(operation);
        return this.Builder;
    }

    public virtual TBuilder AlterField(Field field, Field oldField) {
        var operation = new AlterFieldOperation {
            Field = field,
            OldField = oldField,
            IsDestructive = true
        };
        Operations.Add(operation);
        return this.Builder;
    }
    
    public abstract TMigration Build();
}

public class DocumentMigrationBuilder : MigrateBuilder<DocumentTypeConfiguration, DocumentMigration, DocumentMigrationBuilder> {
    protected override DocumentMigrationBuilder Builder => this;
    protected override DocumentTypeConfiguration? TypeConfiguration { get; set; }
    protected sealed override DocumentMigration Migration { get; } = new();

    public DocumentMigrationBuilder() {
        this.Migration.MigratedOn = DateTime.MinValue.ToUniversalTime();
        this.Migration.IsMigrated = false;
    }

    public override DocumentMigrationBuilder WithTypeConfiguration(DocumentTypeConfiguration typeConfiguration) {
        this.TypeConfiguration = typeConfiguration;
        this.Migration.TypeConfiguration = this.TypeConfiguration.ToReference();
        return this.Builder;
    }

    public override DocumentMigration Build() {
        if (this.TypeConfiguration == null)
            throw new InvalidOperationException("TypeConfiguration is required for building a DocumentMigration");
        
        bool major = this.Migration.UpOperations.OfType<AddFieldOperation>().Any();
        major = major || this.Migration.UpOperations.OfType<DropFieldOperation>().Any();

        if (major) {
            this.Migration.Version = this.TypeConfiguration.DocumentVersion.IncrementMajor();
            this.Migration.IsMajorVersion = true;
        } else {
            this.Migration.Version = this.TypeConfiguration.DocumentVersion.Increment();
            this.Migration.IsMajorVersion = this.Migration.Version.Major > this.TypeConfiguration.DocumentVersion.Major;
        }
        
        this.Operations.ForEach(op => {
            switch (op) {
                case AddFieldOperation addOp:
                    this.Migration.UpOperations.Add(addOp);
                    this.Migration.DownOperations.Add(new DropFieldOperation {
                        Field = addOp.Field,
                        IsDestructive = true
                    });
                    break;
                case DropFieldOperation dropOp:
                    this.Migration.UpOperations.Add(dropOp);
                    this.Migration.DownOperations.Add(new AddFieldOperation {
                        Field = dropOp.Field,
                        IsDestructive = true
                    });
                    break;
                case AlterFieldOperation alterOp:
                    this.Migration.UpOperations.Add(alterOp);
                    this.Migration.DownOperations.Add(new AlterFieldOperation {
                        Field = alterOp.OldField,
                        OldField = alterOp.Field,
                    });
                    break;
            }
        });
        return this.Migration;
    }
}

public class EmbeddedMigrationBuilder : MigrateBuilder<EmbeddedTypeConfiguration, EmbeddedMigration, EmbeddedMigrationBuilder> {

    protected override EmbeddedMigrationBuilder Builder => this;
    protected override EmbeddedTypeConfiguration? TypeConfiguration { get; set; }
    protected sealed override EmbeddedMigration Migration { get; } = new();
    
    private Type? _parentType;
    
    public EmbeddedMigrationBuilder() {
        this.Migration.MigratedOn = DateTime.MinValue.ToUniversalTime();
        this.Migration.IsMigrated = false;
    }
    
    public override EmbeddedMigrationBuilder WithTypeConfiguration(EmbeddedTypeConfiguration typeConfiguration) {
        this.TypeConfiguration = typeConfiguration;
        this.Migration.EmbeddedTypeConfiguration = this.TypeConfiguration.ToReference();
        return this.Builder;
    }

    public EmbeddedMigrationBuilder HasParent<TEntity>() where TEntity : IDocumentEntity {
        this._parentType = typeof(TEntity);
        this.Migration.ParentTypeName = this._parentType.Name;
        return this.Builder;
    }
    
    public override EmbeddedMigration Build() {
        if (this.TypeConfiguration == null)
            throw new InvalidOperationException("TypeConfiguration is required for building a EmbeddedMigration");
        
        if(this._parentType == null)
            throw new InvalidOperationException("ParentType is required for building a EmbeddedMigration");

        bool major = this.Migration.UpOperations.OfType<AddFieldOperation>().Any();
        major = major || this.Migration.UpOperations.OfType<DropFieldOperation>().Any();

        if (major) {
            this.Migration.Version = this.TypeConfiguration.DocumentVersion.IncrementMajor();
            this.Migration.IsMajorVersion = true;
        } else {
            this.Migration.Version = this.TypeConfiguration.DocumentVersion.Increment();
            this.Migration.IsMajorVersion = this.Migration.Version.Major > this.TypeConfiguration.DocumentVersion.Major;
        }
        this.Operations.ForEach(op => {
            switch (op) {
                case AddFieldOperation addOp:
                    this.Migration.UpOperations.Add(addOp);
                    this.Migration.DownOperations.Add(new DropFieldOperation {
                        Field = addOp.Field,
                        IsDestructive = true
                    });
                    break;
                case DropFieldOperation dropOp:
                    this.Migration.UpOperations.Add(dropOp);
                    this.Migration.DownOperations.Add(new AddFieldOperation {
                        Field = dropOp.Field,
                        IsDestructive = true
                    });
                    break;
                case AlterFieldOperation alterOp:
                    this.Migration.UpOperations.Add(alterOp);
                    this.Migration.DownOperations.Add(new AlterFieldOperation {
                        Field = alterOp.OldField,
                        OldField = alterOp.Field,
                    });
                    break;
            }
        });
        return this.Migration;
    }


}

/*public class MigrationBuilder {
    public virtual List<FieldOperation> Operations { get; } = [];

    public Dictionary<Type, List<FieldOperation>> FieldOperations { get; } = new();

    public virtual OperationDefinition<AddFieldOperation> AddField(Field field) {
        var operation = new AddFieldOperation {
            Field = field,
            IsDestructive = false
        };
        Operations.Add(operation);
        return new(operation);
    }

    /*public OperationBuilder<AddFieldOperation> AddField<TConfig>(Field field) where TConfig : TypeConfiguration {
        var operation = new AddFieldOperation() {
            Field = field,
            IsDestructive = false
        };
        this.FieldOperations[typeof(TConfig)] = [];
        this.FieldOperations[typeof(TConfig)].Add(operation);
        return new(operation);
    }#1#

    public virtual OperationDefinition<DropFieldOperation> DropField(Field field) {
        var operation = new DropFieldOperation {
            Field = field,
            IsDestructive = true
        };
        Operations.Add(operation);
        return new(operation);
    }

    public virtual OperationDefinition<AlterFieldOperation> AlterField(Field field, Field oldField) {
        var operation = new AlterFieldOperation {
            Field = field,
            OldField = oldField,
            IsDestructive = true
        };
        Operations.Add(operation);
        return new(operation);
    }

    public virtual DocumentMigration Build(DocumentTypeConfiguration documentTypeConfig, int migrationNumber) {
        DocumentMigration migration = new DocumentMigration {
            MigratedOn = DateTime.MinValue.ToUniversalTime(),
            IsMigrated = false,
            MigrationNumber = 0,
        };
        migration.Build(this);
        bool major = migration.UpOperations.OfType<AddFieldOperation>().Any();
        major = major || migration.UpOperations.OfType<DropFieldOperation>().Any();

        if (major) {
            migration.Version = documentTypeConfig.DocumentVersion.IncrementMajor();
            migration.IsMajorVersion = true;
        } else {
            migration.Version = documentTypeConfig.DocumentVersion.Increment();
            migration.IsMajorVersion = migration.Version.Major > documentTypeConfig.DocumentVersion.Major;
        }
        migration.TypeConfiguration = documentTypeConfig.ToReference();
        migration.MigrationNumber = ++migrationNumber;
        return migration;
    }

    public virtual EmbeddedMigration Build(EmbeddedTypeConfiguration typeConfig,
                                           int migrationNumber,
                                           string parentTypeName) {
        EmbeddedMigration migration = new EmbeddedMigration() {
            MigratedOn = DateTime.MinValue.ToUniversalTime(),
            IsMigrated = false,
            MigrationNumber = 0,
        };
        migration.Build(this);
        bool major = migration.UpOperations.OfType<AddFieldOperation>().Any();
        major = major || migration.UpOperations.OfType<DropFieldOperation>().Any();

        if (major) {
            migration.Version = typeConfig.DocumentVersion.IncrementMajor();
            migration.IsMajorVersion = true;
        } else {
            migration.Version = typeConfig.DocumentVersion.Increment();
            migration.IsMajorVersion = migration.Version.Major > typeConfig.DocumentVersion.Major;
        }
        migration.EmbeddedTypeConfiguration = typeConfig.ToReference();
        migration.MigrationNumber = ++migrationNumber;
        migration.ParentTypeName = parentTypeName;
        return migration;
    }
}*/