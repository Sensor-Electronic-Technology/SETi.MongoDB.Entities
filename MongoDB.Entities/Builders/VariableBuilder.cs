using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Entities;

namespace MongoDB.Entities;

public abstract class VariableBuilderBase<TVar, TBuilder>
    where TVar : Variable, new() where TBuilder : VariableBuilderBase<TVar, TBuilder>, new() {
    protected TBuilder Builder => (TBuilder)this;
    protected TVar Variable => new TVar();

    public virtual TBuilder VariableName(string name) {
        this.Variable.VariableName = name;
        return this.Builder;
    }

    public virtual TBuilder DataType(DataType type) {
        this.Variable.DataType = type;
        this.Variable.BsonType=DataTypeMap.BsonTypeLookup[type];
        this.Variable.TypeCode=DataTypeMap.TypeCodeLookup[type];
        return this.Builder;
    }

    public virtual TVar Build()
        => this.Variable;

    public static TBuilder Create()
        => new();
}

public class ValueVarBuilder : VariableBuilderBase<ValueVariable, ValueVarBuilder> {
    public ValueVarBuilder Value(object value) {
        this.Variable.Value = value;
        return this.Builder;
    }
    
}

public abstract class PropertyVarBuilderBase<TVariable,TBuilder> : VariableBuilderBase<TVariable, TBuilder>
    where TBuilder : VariableBuilderBase<TVariable, TBuilder>, new() where TVariable : PropertyVariable, new(){
    
    public virtual TBuilder Property<TEntity>(Expression<Func<TEntity, object?>> propertyExpression) {
        this.Variable.Property = Prop.Path(propertyExpression);
        return this.Builder;
    }

    public virtual TBuilder Property(string property) {
        this.Variable.Property = property;
        return this.Builder;
    }
}

public class PropertyVarBuilder : PropertyVarBuilderBase<PropertyVariable, PropertyVarBuilder> {
}

public class ExternalPropertyValBuilder : PropertyVarBuilderBase<ExternalPropertyVariable, ExternalPropertyValBuilder> {
    
    public ExternalPropertyValBuilder Database(string database) {
        this.Variable.DatabaseName = database;
        return this.Builder;
    }
    
    public ExternalPropertyValBuilder Collection(string collection) {
        this.Variable.CollectionName = collection;
        return this.Builder;
    }
    
}

public class CollectionPropertyVarBuilder 
    : PropertyVarBuilderBase<OwnedCollectionPropertyVariable, CollectionPropertyVarBuilder> {
    
    /// <summary>
    /// Property selection on a specific class.
    /// </summary>
    /// <param name="propertyExpression">property selection expression, property must be of type IEnumerable</param>
    /// <typeparam name="TEntity">The class containing the property</typeparam>
    /// <returns>CollectionPropertyVarBuilder</returns>
    public CollectionPropertyVarBuilder CollectionProperty<TEntity>(
        Expression<Func<TEntity, object?>> propertyExpression) {
        /*if(Prop.Property(propertyExpression).GetType().IsArray)*/
        this.Variable.CollectionProperty = Prop.Path(propertyExpression);
        return this.Builder;
    }
    
    public CollectionPropertyVarBuilder CollectionProperty(string property) {
        this.Variable.CollectionProperty = property;
        return this.Builder;
    }
    
    public CollectionPropertyVarBuilder HasFilter(Func<IFilterBuilder,Filter> filterBuilder) {
        this.Variable.Filter = filterBuilder.Invoke(FilterBuilder.CreateBuilder());
        return this.Builder;
    }

    public CollectionPropertyVarBuilder HasFilter(Filter filter) {
        this.Variable.Filter = filter;
        return this.Builder;
    }
}

public class RefCollectionPropertyVarBuilder : PropertyVarBuilderBase<ExternalCollectionPropertyVariable, RefCollectionPropertyVarBuilder> {

    public RefCollectionPropertyVarBuilder FilterOnId<TSource, T>(Expression<Func<TSource, object?>> idExpression, Expression<Func<T, object?>> refIdExpression) {
        this.Variable.FilterOnEntityId = true;
        this.Variable.EntityIdProperty = Prop.Path(idExpression);
        this.Variable.RefEntityIdProperty = Prop.Path(refIdExpression);
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder FilterOnId(string idProperty, string refIdProperty) {
        this.Variable.FilterOnEntityId = true;
        this.Variable.EntityIdProperty = idProperty;
        this.Variable.RefEntityIdProperty = refIdProperty;
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder Database(string database) {
        this.Variable.DatabaseName = database;
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder Collection(string collection) {
        this.Variable.CollectionName = collection;
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder CollectionProperty<T>(Expression<Func<T, object?>> propertyExpression) {
        this.Variable.CollectionProperty = Prop.Path(propertyExpression);
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder CollectionProperty(string property) {
        this.Variable.CollectionProperty = property;
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder HasFilter(Func<IFilterBuilder,Filter> filterBuilder) {
        this.Variable.Filter = filterBuilder.Invoke(FilterBuilder.CreateBuilder());
        return this.Builder;
    }

    public RefCollectionPropertyVarBuilder HasFilter(Filter filter) {
        this.Variable.Filter = filter;
        return this.Builder;
    }
    
    public RefCollectionPropertyVarBuilder HasCollectionFilter(Func<IFilterBuilder,Filter> filterBuilder) {
        this.Variable.SubFilter = filterBuilder.Invoke(FilterBuilder.CreateBuilder());
        return this.Builder;
    }

    public RefCollectionPropertyVarBuilder HasCollectionFilter(Filter filter) {
        this.Variable.SubFilter = filter;
        return this.Builder;
    }
}

public class OwnedEmbeddedPropertyVarBuilder : PropertyVarBuilderBase<OwnedEmbeddedPropertyVariable, OwnedEmbeddedPropertyVarBuilder> {
    public OwnedEmbeddedPropertyVarBuilder EmbeddedProperty(string embeddedProperty) {
        this.Variable.EmbeddedProperty = embeddedProperty;
        return this.Builder;
    }
    
    public OwnedEmbeddedPropertyVarBuilder EmbeddedProperty<TEntity>(Expression<Func<TEntity, object?>> propertyExpression) {
        this.Variable.EmbeddedProperty = Prop.Path(propertyExpression);
        return this.Builder;
    }

    /// <summary>
    /// Selection method for an embedded property path. Will only work if the embedded property is in a defince type.
    /// If the embedded property is in a BsonDocument, please use the override with string[] path
    /// </summary>
    /// <param name="propertyExpression"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public OwnedEmbeddedPropertyVarBuilder HasPropertyPath<TEntity>(Expression<Func<TEntity, object?>> propertyExpression) {
        var path=Prop.Path(propertyExpression);

        if (path.Contains('.')) {
            var parts=path.Split('.');
            this.Variable.EmbeddedObjectPropertyPath=parts;
        } else {
            this.Variable.EmbeddedObjectPropertyPath=[path];
        }
        return this.Builder;
    }
    
    public OwnedEmbeddedPropertyVarBuilder EmbeddedObjectPath(params string[] path) {
        this.Variable.EmbeddedObjectPropertyPath=path;
        return this.Builder;
    }
    
}

/*public interface IVarBuilder {
    

}

public interface ICanSetVar{
    
}

//Variable
public interface ICanSetVariable {
    
}

//Property
public interface ICanSetProperty {
    
}

public interface ICanSetVariableValue {
    
}

//Embedded property
public interface ICanSetEmbeddedObjPath {
    
}

public interface ICanSetEmbeddedProp {
    
}


//Collection
public interface ICanSetDatabase {
    
}

//RefCollection
public interface ICanSetCollection {
    
}

//RefProperty






public interface ICanBuildValue {
    Variable Build();
}

public class VariableBuilder {
    
}*/