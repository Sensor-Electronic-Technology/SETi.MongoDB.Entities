namespace MongoDB.Entities;

public interface IFilterBuilder {
    ICanSetValue FieldName(string fieldName);
    bool Validate();
}

public interface ICanSetValue {
    ICanSetCompareOperator Value(object value);
}

public interface ICanSetCompareOperator {
    ICanSetLogicalOperator ComparisonOperator(ComparisonOperator op);
}

public interface ICanSetLogicalOperator {
    ICanBuildFilter LogicalOperator(LogicalOperator op);
}


public interface ICanAddFilters {
    ICanBuildFilter HasFilter(Func<IFilterBuilder,Filter> filterBuilder);
}

public interface ICanBuildFilter:ICanAddFilters {
    public Filter Build();
}

public class FilterBuilder : IFilterBuilder, 
                             ICanSetValue, 
                             ICanSetCompareOperator, 
                             ICanSetLogicalOperator,
                             ICanAddFilters,
                              ICanBuildFilter {

    public static IFilterBuilder CreateBuilder() {
        return new FilterBuilder();
    }

    private Filter _filter=new();

    public ICanSetValue FieldName(string fieldName) {
        this._filter.FieldName = fieldName;
        return this;
    }

    public bool Validate() {
        return true;
    }

    public ICanSetCompareOperator Value(object value) {
        this._filter.Value = value;
        return this;
    }

    public ICanSetLogicalOperator ComparisonOperator(ComparisonOperator op) {
        this._filter.CompareOperator = op;
        return this;
    }

    public ICanBuildFilter LogicalOperator(LogicalOperator op) {
        this._filter.FilterLogicalOperator = op;
        return this;
    }

    public Filter Build() {
        return this._filter;
    }

    public ICanBuildFilter HasFilter(Func<IFilterBuilder,Filter> filterBuilder) {
        this._filter.Filters.Add(filterBuilder.Invoke(CreateBuilder()));
        return this;
    }
}