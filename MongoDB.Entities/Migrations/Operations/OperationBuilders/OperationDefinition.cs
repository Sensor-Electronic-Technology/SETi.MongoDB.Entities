namespace MongoDB.Entities;

public class OperationDefinition<TOperation> where TOperation:FieldOperation {
    protected virtual TOperation Operation { get; }
    public OperationDefinition(TOperation operation) {
        Operation = operation;
    }
}