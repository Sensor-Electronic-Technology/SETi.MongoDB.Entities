using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;

namespace MongoDB.Entities;

public abstract class FieldBuilderBase<TField, TFieldBuilder>
    where TField : Field, new() where TFieldBuilder : FieldBuilderBase<TField, TFieldBuilder>, new() {
    protected abstract TFieldBuilder Builder { get; }
    protected abstract TField Field { get; }

    public virtual TFieldBuilder WithFieldName(string name) {
        this.Field.FieldName = name;
        return this.Builder;
    }

    public virtual TFieldBuilder WithTypes(BsonType type, TypeCode typeCode) {
        this.Field.BsonType = type;
        this.Field.TypeCode = typeCode;
        return this.Builder;
    }

    public TField Build() {
        return Field;
    }

    public static TFieldBuilder Create()
        => new TFieldBuilder();
}

public class FieldBuilder : FieldBuilderBase<Field, FieldBuilder> {
    protected override FieldBuilder Builder => this;
    protected override Field Field { get; } = new();
}

public class ObjectFieldBuilder : FieldBuilderBase<ObjectField, ObjectFieldBuilder> {
    public ObjectFieldBuilder WithField(Action<FieldBuilder> fieldBuilder) {
        FieldBuilder builder = new();
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

    public SelectionFieldBuilder WithDataType(DataType type) {
        this.Field.DataType = type;
        return this.Builder;
    }

    public SelectionFieldBuilder WithSelection(params (string, object)[] selections) {
        this.Field.SelectionDictionary = selections.ToDictionary();
        return this.Builder;
    }

    public SelectionFieldBuilder WithDefaultValue(object defaultValue) {
        this.Field.DefaultValue = defaultValue;
        return this.Builder;
    }
}

public class ValueFieldBuilder : FieldBuilderBase<ValueField, ValueFieldBuilder> {
    protected override ValueFieldBuilder Builder => this;
    protected override ValueField Field { get; } = new();

    public ValueFieldBuilder WithDataType(DataType type) {
        this.Field.DataType = type;
        return this.Builder;
    }

    public ValueFieldBuilder WithValueInfo(object? defaultValue = null,
                                          string? unitName = null,
                                          string? quantityName = null) {
        this.Field.DefaultValue = defaultValue;
        this.Field.UnitName = unitName;
        this.Field.QuantityName = quantityName;
        return this.Builder;
    }
}

public class CalculatedFieldBuilder : FieldBuilderBase<CalculatedField, CalculatedFieldBuilder> {
    
    protected override CalculatedFieldBuilder Builder => this;
    protected override CalculatedField Field { get; } = new();
    
    public CalculatedFieldBuilder WithDataType(DataType type) {
        this.Field.DataType = type;
        return this;
    }

    public CalculatedFieldBuilder WithValueInfo(object? defaultValue = null,
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

    public CalculatedFieldBuilder WithVariable<TVar>(VariableBuilder<TVar> variableBuilder)
        where TVar : Variable, new() {
        this.Field.Variables.Add(variableBuilder.Build());
        return this;
    }

    public CalculatedFieldBuilder IsBooleanExpression(object trueValue, object falseValue) {
        this.Field.IsBooleanExpression = true;
        this.Field.TrueValue = trueValue;
        this.Field.FalseValue = falseValue;
        return this;
    }
}

public class VariableBuilder<TVar> where TVar : Variable, new() {
    public TVar Variable { get; private set; } = new();

    public virtual VariableBuilder<TVar> SetName(string name) {
        this.Variable.VariableName = name;
        return this;
    }

    public virtual VariableBuilder<TVar> SetType(DataType type) {
        this.Variable.DataType = type;
        return this;
    }

    public virtual TVar Build() {
        return this.Variable;
    }
}

public class PropertyVarBuilder<TVal> : VariableBuilder<TVal> where TVal : PropertyVariable, new() {
    public virtual PropertyVariable SetProperty<TEntity>(Expression<Func<TEntity, object?>> propertyExpression) {
        this.Variable.Property = Prop.Path(propertyExpression);
        return this.Variable;
    }
}

public class RefPropertyValBuilder : VariableBuilder<RefPropertyVariable> {
    public RefPropertyValBuilder SetRefProperty<TEntity>(Expression<Func<TEntity, object?>> propertyExpression) {
        this.Variable.Property = Prop.Path(propertyExpression);
        return this;
    }
}

public class EmbeddedPropertyVarBuilder : VariableBuilder<EmbeddedPropertyVariable> {
    public EmbeddedPropertyVarBuilder SetProperty<TEntity>(Expression<Func<TEntity, object?>> propertyExpression) {
        this.Variable.Property = Prop.Path(propertyExpression);
        return this;
    }

    public EmbeddedPropertyVariable SetEmbeddedObjectPath<T>(Expression<Func<T, object?>> embeddedObjectPath) {
        string path = Prop.Path(embeddedObjectPath);

        if (path.Contains('.')) {
            foreach (var part in path.Split('.')) {
                this.Variable.EmbeddedObjectPropertyPath.Add(part);
            }
        } else {
            this.Variable.EmbeddedObjectPropertyPath = [path];
        }
        return this.Variable;
    }

    public EmbeddedPropertyVariable SetEmbeddedObjectPath(params string[] path) {
        this.Variable.EmbeddedObjectPropertyPath = path;

        return this.Variable;
    }

    public EmbeddedPropertyVariable SetEmbeddedProperty(string embeddedProperty) {
        this.Variable.EmbeddedProperty = embeddedProperty;
        return this.Variable;
    }

    public EmbeddedPropertyVariable SetEmbeddedProperty<TEntity>(Expression<Func<TEntity, object?>> embeddedProperty) {
        this.Variable.EmbeddedProperty = Prop.Path(embeddedProperty);
        return this.Variable;
    }
}

// public interface IFieldBuilder<TField> where TField : Field {
//     public TField Field { get; }
//     public TField Build();
// } 
//
//
// public class ObjectFieldBuilder : IFieldBuilder<ObjectField> {
//     public ObjectField Field { get; }
//     
//     public ObjectFieldBuilder()
//         => Field = new ObjectField();
//
//     public void AddField<TField>(IFieldBuilder<TField> fieldBuilder) where TField : Field {
//         Field.Fields.Add(fieldBuilder.Build());
//     }
//     
//     public ObjectField Build()
//         => throw new NotImplementedException();
// }
//
// public class PropertyFieldBuilder : FieldGuilder {
//     
// }
//
// public class ValueFieldBuilder : FieldGuilder {
//     
// }