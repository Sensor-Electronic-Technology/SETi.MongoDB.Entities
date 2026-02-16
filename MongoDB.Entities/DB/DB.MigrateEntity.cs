using System.Threading.Tasks;

namespace MongoDB.Entities;

public partial class DB {
    public async Task ApplyMigrations<TEntity>() where TEntity : IDocumentEntity,IEmbeddedEntity {
        
    }
}