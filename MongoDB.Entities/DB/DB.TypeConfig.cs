using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB {
    public DocumentTypeConfiguration? CreateDocumentConfig<TEntity>(string collectionName, string databaseName)
        where TEntity : IDocumentEntity {
        var typeConfig = new DocumentTypeConfiguration {
            CollectionName = collectionName,
            DatabaseName = databaseName,
            Fields = [],
        };
        Type type = typeof(TEntity);
        /*var typeName = type.AssemblyQualifiedName;*/
        var typeName = type.Name;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }
        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            typeConfig.AvailableProperties.Add(prop.Name, new() { TypeCode = Type.GetTypeCode(prop.PropertyType) });
        }

        return typeConfig;
    }

    public async Task<DocumentTypeConfiguration?> CreateDocumentConfigOnline<TEntity>()
        where TEntity : IDocumentEntity {
        Type type = typeof(TEntity);

        /*var typeName = type.AssemblyQualifiedName;*/
        var typeName = type.Name;

        if (string.IsNullOrEmpty(typeName)) {
            return null;
        }

        var typeConfig = await Find<DocumentTypeConfiguration>()
                               .Match(e => e.TypeName == typeName)
                               .ExecuteSingleAsync();

        if (typeConfig != null)
            return typeConfig;

        typeConfig = new() {
            CollectionName = CollectionName<TEntity>(),
            DatabaseName = DatabaseName(),
            Fields = [],
        };

        typeConfig.TypeName = typeName;
        typeConfig.AvailableProperties = [];

        foreach (var prop in type.GetProperties()) {
            /*if (prop.Name == nameof(IDocumentEntity.AdditionalData))
                continue;*/

            typeConfig.AvailableProperties.Add(
                prop.Name,
                new() {
                    TypeCode = Type.GetTypeCode(prop.PropertyType)
                });
        }

        await InsertAsync(typeConfig);

        return typeConfig;
    }

    public async Task<EmbeddedTypeConfiguration?> CreateEmbeddedConfigOnline<TEntity>(Type embeddedType,
        bool isArray = false,
        params string[] propertyNames)
        where TEntity : IDocumentEntity {
        Type type = embeddedType;
        Type parent = typeof(TEntity);
        var typeName = type.Name;
        var parentTypeName = parent.Name;

        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(parentTypeName)) {
            return null;
        }

        var typeConfig = await Find<EmbeddedTypeConfiguration>()
                               .Match(e => e.TypeName == typeName)
                               .ExecuteSingleAsync();

        if (typeConfig == null) {
            typeConfig = new() {
                TypeName = typeName,
            };

            if (!typeConfig.EmbeddedPropertyConfigs.TryGetValue(parentTypeName, out var fieldDef)) {
                fieldDef = new() {
                    DatabaseName = DatabaseName(),
                    ParentCollection = CollectionName<TEntity>(),
                    PropertyNames = propertyNames.ToList(),
                    IsArray = isArray,
                    Fields = [],
                    AvailableProperties = []
                };
                typeConfig.EmbeddedPropertyConfigs[parentTypeName] = fieldDef;
            }

            foreach (var prop in type.GetProperties()) {
                if (prop.Name == nameof(IEmbeddedEntity.AdditionalData))
                    continue;

                fieldDef.AvailableProperties[prop.Name] = new() { TypeCode = Type.GetTypeCode(prop.PropertyType) };
            }
            await InsertAsync(typeConfig);

            return typeConfig;
        } else {
            if (!typeConfig.EmbeddedPropertyConfigs.TryGetValue(parentTypeName, out var fieldDef)) {
                fieldDef = new() {
                    DatabaseName = DatabaseName(),
                    ParentCollection = CollectionName<TEntity>(),
                    PropertyNames = propertyNames.ToList(),
                    IsArray = isArray,
                    Fields = [],
                    AvailableProperties = []
                };
                typeConfig.EmbeddedPropertyConfigs[parentTypeName] = fieldDef;
            }

            foreach (var prop in type.GetProperties()) {
                if (prop.Name == nameof(IEmbeddedEntity.AdditionalData))
                    continue;

                fieldDef.AvailableProperties[prop.Name] = new() { TypeCode = Type.GetTypeCode(prop.PropertyType) };
            }
            var result = await Update<EmbeddedTypeConfiguration>()
                               .Match(e => e.ID == typeConfig.ID)
                               .Modify(e => e.Set(p => p.EmbeddedPropertyConfigs, typeConfig.EmbeddedPropertyConfigs))
                               .Modify(e => e.Set(p => p.TypeName, typeConfig.TypeName))
                               .Modify(e => e.Set(p => p.DocumentVersion, typeConfig.DocumentVersion))
                               .ExecuteAsync();

            return result.IsAcknowledged ? typeConfig : null;
        }
    }
}