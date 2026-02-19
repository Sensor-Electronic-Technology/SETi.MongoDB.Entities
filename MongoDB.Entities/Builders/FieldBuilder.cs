using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;

namespace MongoDB.Entities;

public abstract class FieldBuilderBase<TField, TFieldBuilder>
    where TField : Field, new() where TFieldBuilder : FieldBuilderBase<TField, TFieldBuilder>, new() {
    protected abstract TFieldBuilder Builder { get; }
    protected abstract TField Field { get; }

    public virtual TFieldBuilder FieldName(string name) {
        this.Field.FieldName = name;
        return this.Builder;
    }

    public virtual TFieldBuilder Types(DataType dataType) {
        this.Field.DataType = dataType;
        this.Field.TypeCode = DataTypeMap.TypeCodeLookup[dataType];
        this.Field.BsonType = DataTypeMap.BsonTypeLookup[dataType];
        return this.Builder;
    }

    public virtual TFieldBuilder CanRound(int decimalPlaces) {
        this.Field.Round = true;
        this.Field.DecimalPlaces = decimalPlaces;
        return this.Builder;
    }

    public TField Build() {
        return Field;
    }

    public static TFieldBuilder Create()
        => new();
}

public class FieldBuilder : FieldBuilderBase<Field, FieldBuilder> {
    protected override FieldBuilder Builder => this;
    protected override Field Field { get; } = new();
}

public class ObjectFieldBuilder : FieldBuilderBase<ObjectField, ObjectFieldBuilder> {
    
    public ObjectFieldBuilder HasField<TField,TFieldBuilder>(Action<TFieldBuilder> fieldBuilder)
        where TField : Field, new() where TFieldBuilder : FieldBuilderBase<TField, TFieldBuilder>, new() {
        var builder = new TFieldBuilder();
        fieldBuilder(builder);
        Field.Fields.Add(builder.Build());
        return this;
    }

    protected override ObjectFieldBuilder Builder => this;
    protected override ObjectField Field { get; } = new();
}

public class SelectionFieldBuilder : FieldBuilderBase<SelectionField, SelectionFieldBuilder> {
    protected override SelectionFieldBuilder Builder => this;
    protected override SelectionField Field { get; } = new();

    public SelectionFieldBuilder WithSelectionOptions(params (string, object)[] selections) {
        this.Field.SelectionDictionary = selections.ToDictionary();
        return this.Builder;
    }

    public SelectionFieldBuilder DefaultValue(object defaultValue) {
        this.Field.DefaultValue = defaultValue;
        return this.Builder;
    }
}

public class ValueFieldBuilder : FieldBuilderBase<ValueField, ValueFieldBuilder> {
    protected override ValueFieldBuilder Builder => this;
    protected override ValueField Field { get; } = new();

    public ValueFieldBuilder DataType(DataType type) {
        this.Field.DataType = type;
        return this.Builder;
    }

    public ValueFieldBuilder ValueInfo(object? defaultValue = null,
                                          string? unitName = null,
                                          string? quantityName = null) {
        this.Field.DefaultValue = defaultValue;
        this.Field.UnitName = unitName;
        this.Field.QuantityName = quantityName;
        return this.Builder;
    }
}

public class ReferenceFieldBuilder : FieldBuilderBase<ReferenceField, ReferenceFieldBuilder> {
    protected override ReferenceFieldBuilder Builder => this;
    protected override ReferenceField Field { get; } = new();
    
    public ReferenceFieldBuilder FilterOnId<TSource, T>(Expression<Func<TSource, object?>> idExpression, Expression<Func<T, object?>> refIdExpression) {
        this.Field.FilterOnEntityId = true;
        this.Field.EntityIdProperty = Prop.Path(idExpression);
        this.Field.RefEntityIdProperty = Prop.Path(refIdExpression);
        return this.Builder;
    }
    
    public ReferenceFieldBuilder FilterOnId(string idProperty, string refIdProperty) {
        this.Field.FilterOnEntityId = true;
        this.Field.EntityIdProperty = idProperty;
        this.Field.RefEntityIdProperty = refIdProperty;
        return this.Builder;
    }
    
    /*public ReferenceFieldBuilder Database(string database) {
        this.Field.DatabaseName = database;
        return this.Builder;
    }
    
    public ReferenceFieldBuilder Collection(string collection) {
        this.Field.CollectionName = collection;
        return this.Builder;
    }
    
    public ReferenceFieldBuilder ReferenceProperty<TEntity>(
        Expression<Func<TEntity, object?>> propertyExpression) {
        /*if(Prop.Property(propertyExpression).GetType().IsArray)#1#
        this.Field.ReferenceProperty = Prop.Path(propertyExpression);
        return this.Builder;
    }
    
    public ReferenceFieldBuilder ReferenceProperty(string property) {
        this.Field.ReferenceProperty = property;
        return this.Builder;
    }*/
    
}

public class CalculatedFieldBuilder : FieldBuilderBase<CalculatedField, CalculatedFieldBuilder> {
    
    protected override CalculatedFieldBuilder Builder => this;
    protected override CalculatedField Field { get; } = new();

    public CalculatedFieldBuilder ValueInfo(object? defaultValue = null,
                                               string? unitName = null,
                                               string? quantityName = null) {
        this.Field.DefaultValue = defaultValue;
        this.Field.UnitName = unitName;
        this.Field.QuantityName = quantityName;
        return this;
    }

    public CalculatedFieldBuilder WithExpression(string expression) {
        this.Field.Expression = expression;
        return this;
    }

    public CalculatedFieldBuilder HasVariable<TVar,TBuilder>(Action<TBuilder> variableBuilder)
        where TVar : Variable, new() where TBuilder : VariableBuilderBase<TVar,TBuilder>, new() {
        var varBuilder=new TBuilder();
        variableBuilder(varBuilder);
        this.Field.Variables.Add(varBuilder.Build());
        return this;
    }

    public CalculatedFieldBuilder IsBooleanExpression(object trueValue, object falseValue) {
        this.Field.IsBooleanExpression = true;
        this.Field.TrueValue = trueValue;
        this.Field.FalseValue = falseValue;
        return this;
    }
}