using System.Collections.Generic;

namespace MongoDB.Entities;

public interface IDocumentMigration {
    public DocumentVersion Version { get; set; }
    public List<FieldOperation> UpOperations { get; set; }
    public List<FieldOperation> DownOperations { get; set; }
    void Build(MigrationBuilder builder);
}