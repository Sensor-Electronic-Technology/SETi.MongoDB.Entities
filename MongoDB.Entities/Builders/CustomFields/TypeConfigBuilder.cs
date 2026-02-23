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
