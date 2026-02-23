using System.Collections.Generic;
using System.Linq.Expressions;

namespace MongoDB.Entities;

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
    
    public EmbeddedPropertyBuilder<TParent> ForProperty(Expression<Func<TParent, IEmbeddedEntity?>> propExpression) {
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