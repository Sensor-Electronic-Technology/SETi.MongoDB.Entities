using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

public static partial class Extensions {
    /*public static async Task ApplyMigrations<TDoc>(this TDoc docEntity) where TDoc : IDocumentEntity {

    }*/

    extension<TDoc>(TDoc entity) where TDoc : IDocumentEntity {
        public async Task ApplyMigrations() {
            
        }

        /*internal async Task EntityApplyDocMigration(DocumentTypeConfiguration typeConfig,
                                                    Dictionary<string, object>? additionalData = null,
                                                    CancellationToken cancellation = default) {
            var migrations = await typeConfig.Migrations
                                             .ChildrenQueryable()
                                             .ToListAsync(cancellationToken: cancellation);
            if (migrations.Count <= 0) {
                return;
            }

            if (entity.AdditionalData == null) {
                entity.AdditionalData = [];
            }

            var doc = entity.AdditionalData;
            var entityDoc = entity.ToBsonDocument();

            foreach (var migration in migrations) {
                foreach (var op in migration.UpOperations) {
                    if (op is AddFieldOperation addField) {
                        await DB.Default.AddField(addField.Field, doc, entityDoc);
                    } else if (op is DropFieldOperation dropField) {
                        doc.Remove(dropField.Field.FieldName);
                    } else if (op is AlterFieldOperation alterField) {
                        doc.Remove(alterField.OldField.FieldName);
                        await DB.AddField(alterField.Field, doc, entityDoc);
                    }
                }
            }

            if (additionalData != null) {
                foreach (var fieldItem in additionalData) {
                    if (doc.Contains(fieldItem.Key)) {
                        doc[fieldItem.Key] = BsonValue.Create(fieldItem.Value);
                    }
                }
            }

            entity.AdditionalData = doc;
        }*/
    }
}