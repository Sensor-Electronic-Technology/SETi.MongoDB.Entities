using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public class ConfigureType<TEntity>(DB db) where TEntity : IDocumentEntity {
    private List<Embedded<TEntity>> _embeddedProperties = [];
    
    public ConfigureType<TEntity> RegisterEmbedded(Expression<Func<TEntity, object?>> embeddedProperty) {
        this._embeddedProperties.Add(new(embeddedProperty));
        return this;
    }

    public async Task RegisterAsync() {
        var typeConfig = await db.Find<DocumentTypeConfiguration>()
                                      .Match(e => e.CollectionName == db.CollectionName<TEntity>())
                                      .ExecuteSingleAsync();

        if (typeConfig == null) {
            var config=await db.CreateDocumentConfigOnline<TEntity>();
            Cache<TEntity>.TypeConfiguration = config;
        }

        if (this._embeddedProperties.Any()) {

            if (!(this._embeddedProperties.All(e => e.IsArray) || this._embeddedProperties.All(e => !e.IsArray))) {
                throw new InvalidOperationException("Cannot register embedded properties of mixed types (array and non-array).");
            }
            
            foreach (var embedded in _embeddedProperties) {
                await db.CreateEmbeddedConfigOnline<TEntity>(embedded.Type,
                    isArray:this._embeddedProperties.Any(e=>e.IsArray), 
                    propertyNames:embedded.PropertyName);
            }
            
        }
    }
}

public class Embedded<TDoc> where TDoc : IDocumentEntity {
    public string PropertyName { get; set; }
    //public string PropertyName { get; set; }
    /*public string? OwningPropertyName { get; set; } = null;
    public bool IsEmbeddedInProperty { get; set; } = false;*/
    public Type Type { get; set; }
    public bool IsArray { get; set; } = false;

    internal Embedded(Expression<Func<TDoc, object?>> expression) {
        this.PropertyName=Prop.Path(expression);
        if (this.PropertyName.Contains('.')) {
            throw new InvalidOperationException($"Cannot register embedded property {this.PropertyName}, EmbeddedEntities are only supported on top level properties.");
            /*var parts=this.FullPath.Split('.');
            PropertyName = parts[0];
            IsEmbeddedInProperty = true;
            this.OwningPropertyName = parts[1];
            var type = typeof(TDoc).GetProperty(this.FullPath)?.PropertyType;
            if(type==null)
                throw new InvalidOperationException($"Could not find property {this.FullPath} on type {typeof(TDoc).FullName}");

            if (!type.IsAssignableTo(typeof(IEnumerable<>))) {
                throw new InvalidOperationException($"Property {this.FullPath} on type {typeof(TDoc).FullName} does not implement IEmbedded");
            }
            Type = type;*/
        }
        
        var type = typeof(TDoc).GetProperty(this.PropertyName)?.PropertyType;

        if (type == null) {
            throw new InvalidOperationException($"Could not find property {this.PropertyName} on type {typeof(TDoc).FullName}");
        }
        
        if (type!=typeof(string) && (type.IsArray || type.IsAssignableTo(typeof(IEnumerable)))) {
            var elementType = type.GetElementType();

            this.IsArray = true;
            type = elementType ?? throw new InvalidOperationException($"Could not determine element type of array/IEnumerable property {this.PropertyName} on type {typeof(TDoc).FullName}");

            if (!elementType.IsAssignableTo(typeof(IEmbeddedEntity))) {
                throw new InvalidOperationException($"Property {this.PropertyName} on type {typeof(TDoc).FullName} does not implement IEmbedded");
            }
            this.IsArray = type.IsArray;
            this.Type = elementType;
        } else {
            
            this.IsArray = false;
            this.Type = type;
        }

    }
}
