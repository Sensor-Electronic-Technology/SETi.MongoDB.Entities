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

    public virtual TFieldBuilder Type(DataType dataType) {
        this.Field.DataType = dataType;
        this.Field.TypeCode = dataType.ToTypeCode();
        this.Field.BsonType = dataType.ToBsonType();
        return this.Builder;
    }

    public virtual TFieldBuilder CanRound(int decimalPlaces) {
        this.Field.Round = true;
        this.Field.DecimalPlaces = decimalPlaces;
        return this.Builder;
    }

    public TField Build() {
        return this.Field;
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
    
    public ObjectFieldBuilder HasField(Field field) {
        Field.Fields.Add(field);
        return this.Builder;
    }

    protected override ObjectFieldBuilder Builder => this;
    protected override ObjectField Field { get; } = new();
}
public class ValueFieldBuilder : FieldBuilderBase<ValueField, ValueFieldBuilder> {
    protected override ValueFieldBuilder Builder => this;
    protected override ValueField Field { get; } = new();

    public override ValueFieldBuilder Type(DataType dataType) {
        this.Field.DefaultValue = dataType.GetDefault();
        return base.Type(dataType);
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

    public override ReferenceFieldBuilder Type(DataType dataType) {
        this.Field.DefaultValue = dataType.GetDefault();
        return base.Type(dataType);
    }

    public ReferenceFieldBuilder HasVariable(Action<ExternPropertyValBuilder> variableBuilder) {
        var varBuilder=new ExternPropertyValBuilder();
        variableBuilder(varBuilder);
        this.Field.ExternalPropertyVariable = varBuilder.Build();
        return this;
    }

    public ReferenceFieldBuilder HasVariable(ExternalPropertyVariable variable) {
        this.Field.ExternalPropertyVariable = variable;
        return this.Builder;
    }
}
public class SelectionFieldBuilder : FieldBuilderBase<SelectionField, SelectionFieldBuilder> {
    protected override SelectionFieldBuilder Builder => this;
    protected override SelectionField Field { get; } = new();

    public SelectionFieldBuilder WithSelectionOptions(string defaultKey,(string, object)[] selections) {
        this.Field.SelectionDictionary = selections.ToDictionary();
        this.Field.DefaultValue =this.Field.SelectionDictionary[defaultKey];
        return this.Builder;
    }
    
    public SelectionFieldBuilder HasSelectionOption(string key, object value) {
        this.Field.SelectionDictionary.Add(key, value);
        return this.Builder;
    }
    
    public SelectionFieldBuilder HasDefaultKey(string key) {
        if (!this.Field.SelectionDictionary.ContainsKey(key)) {
            throw new ArgumentException($"The key '{key}' does not exist in the selection dictionary");
        }
        this.Field.DefaultValue = this.Field.SelectionDictionary[key];
        return this.Builder;
    }
}
public class CalculatedFieldBuilder : FieldBuilderBase<CalculatedField, CalculatedFieldBuilder> {
    
    protected override CalculatedFieldBuilder Builder => this;
    protected override CalculatedField Field { get; } = new();

    public override CalculatedFieldBuilder Type(DataType dataType) {
        this.Field.DefaultValue = dataType.GetDefault();
        return base.Type(dataType);
    }
    
    public CalculatedFieldBuilder ValueInfo(object? defaultValue = null,
                                            string? unitName = null,
                                            string? quantityName = null) {
        this.Field.DefaultValue = defaultValue;
        this.Field.UnitName = unitName;
        this.Field.QuantityName = quantityName;
        return this.Builder;
    }

    public CalculatedFieldBuilder Expression(string expression) {
        this.Field.Expression = expression;
        return this.Builder;
    }
    
    public CalculatedFieldBuilder IsBooleanExpression(object trueValue, object falseValue) {
        this.Field.IsBooleanExpression = true;
        this.Field.TrueValue = trueValue;
        this.Field.FalseValue = falseValue;
        return this.Builder;
    }

    public CalculatedFieldBuilder HasVariable<TVar,TBuilder>(Action<TBuilder> variableBuilder)
        where TVar : Variable, new() where TBuilder : VariableBuilderBase<TVar,TBuilder>, new() {
        var varBuilder=new TBuilder();
        variableBuilder(varBuilder);
        this.Field.Variables.Add(varBuilder.Build());
        return this.Builder;
    }

    public CalculatedFieldBuilder HasVariable(Variable variable) {
        this.Field.Variables.Add(variable);
        return this.Builder;
    }

}