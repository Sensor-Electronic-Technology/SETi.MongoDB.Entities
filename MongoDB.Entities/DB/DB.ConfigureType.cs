namespace MongoDB.Entities;

public partial class DB {
    public ConfigureType<TEntity> ConfigureType<TEntity>() where TEntity : DocumentEntity
        => new(this);
    
    /*public ConfigureType<TEntity> EmbeddedData<TEntity>() where TEntity : EmbeddedEntity
        => new(this);*/
}