using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace MongoDB.Entities;


public abstract class TypeConfigBuilderBase<TType, TTypeBuilder>
    where TType : TypeConfiguration, new() where TTypeBuilder : TypeConfigBuilderBase<TType, TTypeBuilder>, new() {
    protected abstract TTypeBuilder Builder { get; }
    protected abstract TType TypeConfiguration { get; set; }
    protected Type? EntityType { get; set; }
    
    public virtual TTypeBuilder HasType<TEntity>() where TEntity : DocumentEntity {
        this.EntityType = typeof(TEntity);
        this.TypeConfiguration.TypeName = this.EntityType.Name;
        return this.Builder;
    }

    public virtual TTypeBuilder DocVersion(int major, int minor, int rev) {
        this.TypeConfiguration.DocumentVersion=new(major, minor, rev);
        return this.Builder;
    }

    public virtual TType Build() {
        return this.TypeConfiguration;
    }
    public static TTypeBuilder Create()
        => new();
}

public class DocTypeConfigBuilder : TypeConfigBuilderBase<DocumentTypeConfiguration, DocTypeConfigBuilder> {

    
    protected override DocTypeConfigBuilder Builder => this;
    protected override DocumentTypeConfiguration TypeConfiguration { get; set; } = new();
    
    /// <summary>
    /// Load a document type configuration from an existing configuration.
    /// </summary>
    /// <param name="config">Existing document type configuration</param>
    /// <returns>DocTypeConfigBuilder</returns>
    public DocTypeConfigBuilder FromConfig(DocumentTypeConfiguration config) {
        this.TypeConfiguration = config;
        return this.Builder;
    }

    /// <summary>
    /// Configure a document type. Sets the type name, collection name, and database name for the document type.
    /// </summary>
    /// <param name="database">database name</param>
    /// <param name="collectionName">collection name for the entity</param>
    /// <typeparam name="TEntity">Field Type</typeparam>
    /// <returns></returns>
    public DocTypeConfigBuilder ConfigureDatabase(string database,string collectionName) {
        this.TypeConfiguration.CollectionName = collectionName;
        this.TypeConfiguration.DatabaseName = database;
        return this.Builder;
    }
    
    /// <summary>
    /// Set the database name for this document type.
    /// </summary>
    /// <param name="databaseName"></param>
    /// <returns>DocTypeConfigBuilder</returns>
    public DocTypeConfigBuilder DatabaseName(string databaseName) {
        this.TypeConfiguration.DatabaseName=databaseName;
        return this.Builder;
    }
    
    /// <summary>
    /// Set the collection name for this document type.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <returns>DocTypeConfigBuilder</returns>
    public DocTypeConfigBuilder CollectionName(string collectionName) {
        this.TypeConfiguration.CollectionName=collectionName;
        return this.Builder;
    }

    /// <summary>
    /// Add a field to this document type.
    /// </summary>
    /// <param name="fieldConfig">Field builder for field type</param>
    /// <typeparam name="TField">Type of field</typeparam>
    /// <typeparam name="TBuilder">Builder of same field type</typeparam>
    /// <returns></returns>
    public DocTypeConfigBuilder HasField<TField, TBuilder>(Action<TBuilder> fieldConfig)
        where TField : Field,new() where TBuilder : FieldBuilderBase<TField, TBuilder>,new() {
        var builder = new TBuilder();
        fieldConfig(builder);
        this.TypeConfiguration.Fields.Add(builder.Build());
        return this.Builder;
    }

    /// <summary>
    /// Add a field to this document type. Alternative to using the field builder.
    /// </summary>
    /// <param name="field">Field to add</param>
    /// <typeparam name="TField">Field Type</typeparam>
    /// <returns></returns>
    public DocTypeConfigBuilder HasField<TField>(TField field) where TField : Field {
        this.TypeConfiguration.Fields.Add(field);
        return this.Builder;
    }

    public override DocumentTypeConfiguration Build() {
        if(this.EntityType == null)
            throw new InvalidOperationException("You must specify the entity type before building the configuration");
        
        this.TypeConfiguration.AvailableProperties = [];

        foreach (var prop in this.EntityType.GetProperties()) {
            this.TypeConfiguration.AvailableProperties.Add(prop.Name, new() { TypeCode = Type.GetTypeCode(prop.PropertyType) });
        }
        return this.TypeConfiguration;
    }
}

public class EmbeddedTypeConfigBuilder : TypeConfigBuilderBase<EmbeddedTypeConfiguration, EmbeddedTypeConfigBuilder> {
    protected override EmbeddedTypeConfigBuilder Builder => this;
    protected override EmbeddedTypeConfiguration TypeConfiguration { get; set; } = new();
    
    private DocumentTypeConfiguration _parentEntity;
    private List<EmbeddedProperty> _embeddedProperties = [];
    
    public EmbeddedTypeConfigBuilder FromConfig(EmbeddedTypeConfiguration config) {
        this.TypeConfiguration = config;
        return this.Builder;
    }

    public EmbeddedTypeConfigBuilder HasEmbeddedPropertyConfig<TParent>(EmbeddedPropertyConfig config)
        where TParent : IDocumentEntity{
        this.TypeConfiguration.EmbeddedPropertyConfigs.Add(typeof(TParent).Name,config);
        return this.Builder;
    }

    public EmbeddedTypeConfigBuilder HasEmbeddedPropertyConfig<TParent>(
        Action<EmbeddedPropertyBuilder<TParent>> builder) {
        var builderInstance = new EmbeddedPropertyBuilder<TParent>();
        builder(builderInstance);
        this.TypeConfiguration.EmbeddedPropertyConfigs.Add(typeof(TParent).Name,builderInstance.Build());
        return this.Builder;
    }

    /*public EmbeddedTypeConfigBuilder Register<TParent>(Expression<Func<TParent, object?>> propExpression)
    where TParent : IDocumentEntity {
        EmbeddedProperty property = new();
        property.PropertyName=Prop.Path(propExpression);
        if (property.PropertyName.Contains('.')) {
            throw new InvalidOperationException($"Cannot register embedded property {property.PropertyName}, " +
                                                $"EmbeddedEntities are only supported on top level properties.");
        }
        
        var type = typeof(TParent).GetProperty(property.PropertyName)?.PropertyType;

        if (type == null) {
            throw new InvalidOperationException($"Could not find property {property.PropertyName} " +
                                                $"on type {typeof(TParent).FullName}");
        }
        
        if (type!=typeof(string) && (type.IsArray || type.IsAssignableTo(typeof(IEnumerable<>)))) {
            var elementType = type.GetElementType();

            property.IsArray = true;
            type = elementType ?? 
                   throw new InvalidOperationException($"Could not determine element type of " +
                                                       $"Array/IEnumerable property " +
                                                       $"{property.PropertyName} on type {typeof(TParent).FullName}");

            if (!elementType.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {property.PropertyName} on type " +
                                                    $"{typeof(TParent).FullName} does not implement IEmbedded");
            }
            property.IsArray = type.IsArray;
            property.Type = elementType;
        } else {
            if (!type.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {property.PropertyName} on type " +
                                                    $"{typeof(TParent).FullName} does not implement IEmbedded");
            }
            property.IsArray = false;
            property.Type = type;
        }
        this._embeddedProperties.Add(property);
        return this.Builder;
    }*/
    
}

public class EmbeddedPropertyBuilder<TParent> {
    protected EmbeddedPropertyBuilder<TParent> Builder => this;
    protected EmbeddedPropertyConfig EmbeddedPropertyConfig{ get; set; } = new();
    
    public EmbeddedPropertyBuilder<TParent> ParentCollection(string parentCollection) {
        this.EmbeddedPropertyConfig.ParentCollection = parentCollection;
        return this.Builder;
    }
    
    public EmbeddedPropertyBuilder<TParent> ParentDatabase(string parentDatabase) {
        this.EmbeddedPropertyConfig.DatabaseName = parentDatabase;
        return this.Builder;
    }

    public EmbeddedPropertyBuilder<TParent> IsArray() {
        this.EmbeddedPropertyConfig.IsArray = true;
        return this.Builder;
    }
    
    public EmbeddedPropertyBuilder<TParent> ForProperty(Expression<Func<TParent, object?>> propExpression) {
        var propertyName=Prop.Path(propExpression);
        if (propertyName.Contains('.')) {
            throw new InvalidOperationException($"Cannot register embedded property {propertyName}, " +
                                                $"EmbeddedEntities are only supported on top level properties.");
        }
        
        var type = typeof(TParent).GetProperty(propertyName)?.PropertyType;

        if (type == null) {
            throw new InvalidOperationException($"Could not find property {propertyName} " +
                                                $"on type {typeof(TParent).FullName}");
        }
        
        if (type!=typeof(string) && (type.IsArray || type.IsAssignableTo(typeof(IEnumerable<>)))) {
            var elementType = type.GetElementType();

            
            type = elementType ?? 
                   throw new InvalidOperationException($"Could not determine element type of " +
                                                       $"Array/IEnumerable property " +
                                                       $"{propertyName} on type {typeof(TParent).FullName}");
            
            if (!elementType.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {propertyName} on type " +
                                                    $"{typeof(TParent).FullName} does not implement IEmbedded");
            }
            this.EmbeddedPropertyConfig.IsArray = true;
            this.EmbeddedPropertyConfig.PropertyNames.Add(propertyName);

            foreach (var prop in type.GetProperties()) {
                this.EmbeddedPropertyConfig.AvailableProperties[prop.Name] = new() {
                    TypeCode = Type.GetTypeCode(prop.PropertyType)
                };
            }
        } else {
            if (!type.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {propertyName} on type " +
                                                    $"{typeof(TParent).Name} does not implement IEmbedded");
            }
            this.EmbeddedPropertyConfig.IsArray = false;
            this.EmbeddedPropertyConfig.PropertyNames.Add(propertyName);

            foreach (var prop in type.GetProperties()) {
                this.EmbeddedPropertyConfig.AvailableProperties[prop.Name] = new() {
                    TypeCode = Type.GetTypeCode(prop.PropertyType)
                };
            }
        }
        return this.Builder;
    }

    public EmbeddedPropertyBuilder<TParent> HasField<TField, TBuilder>(Action<TBuilder> fieldConfig)
        where TField : Field, new() where TBuilder : FieldBuilderBase<TField, TBuilder>, new() {
        var builder = new TBuilder();
        fieldConfig(builder);
        this.EmbeddedPropertyConfig.Fields.Add(builder.Build());
        return this.Builder;
    }
    
    public EmbeddedPropertyBuilder<TParent> HasField(Field field) {
        this.EmbeddedPropertyConfig.Fields.Add(field);
        return this.Builder;
    }
    
    public EmbeddedPropertyConfig Build() {
        return this.EmbeddedPropertyConfig;
    }
}


public class EmbeddedProperty{
    public string PropertyName { get; set; }
    public Type Type { get; set; }
    public bool IsArray { get; set; } = false;

    /*internal EmbeddedProperty(Expression<Func<TParent, object?>> expression) {
        this.PropertyName=Prop.Path(expression);
        if (this.PropertyName.Contains('.')) {
            throw new InvalidOperationException($"Cannot register embedded property {this.PropertyName}, EmbeddedEntities are only supported on top level properties.");
        }
        
        var type = typeof(TParent).GetProperty(this.PropertyName)?.PropertyType;

        if (type == null) {
            throw new InvalidOperationException($"Could not find property {this.PropertyName} " +
                                                $"on type {typeof(TParent).FullName}");
        }
        
        if (type!=typeof(string) && (type.IsArray || type.IsAssignableTo(typeof(IEnumerable<>)))) {
            var elementType = type.GetElementType();

            this.IsArray = true;
            type = elementType ?? 
                   throw new InvalidOperationException($"Could not determine element type of " +
                                                       $"Array/IEnumerable property " +
                                                       $"{this.PropertyName} on type {typeof(TParent).FullName}");

            if (!elementType.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {this.PropertyName} on type " +
                                                    $"{typeof(TParent).FullName} does not implement IEmbedded");
            }
            this.IsArray = type.IsArray;
            this.Type = elementType;
        } else {
            if (!type.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {this.PropertyName} on type " +
                                                    $"{typeof(TParent).FullName} does not implement IEmbedded");
            }
            this.IsArray = false;
            this.Type = type;
        }

    }*/
}