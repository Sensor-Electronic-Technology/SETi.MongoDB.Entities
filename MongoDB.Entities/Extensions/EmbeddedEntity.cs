using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

//ReSharper disable once InconsistentNaming
public static partial class Extensions {
    public static async Task ApplyEmbedded<TEmbedded>(this TEmbedded embedded,
                                                      Type parent,
                                                      DB db,
                                                      Dictionary<string, object>? additionalData = null,
                                                      CancellationToken cancellation = default)
        where TEmbedded : IEmbeddedEntity {
        var embeddedConfig = await db.Find<EmbeddedTypeConfiguration>()
                                     .Match(e => e.EmbeddedPropertyConfigs.ContainsKey(parent.Name))
                                     .ExecuteFirstAsync(cancellation);

        if (embeddedConfig != null && embeddedConfig.EmbeddedMigrations.Any()) {
            await EmbeddedMigration(embedded, embeddedConfig, db,additionalData, cancellation);
        }
    }

    public static async Task ApplyEmbedded<TEmbedded>(this IEnumerable<TEmbedded> embedded,
                                                      Type parent,
                                                      DB db,
                                                      List<Dictionary<string, object>>? additionalData = null,
                                                      CancellationToken cancellation = default)
        where TEmbedded : IEmbeddedEntity {
        var embeddedConfig = await db.Find<EmbeddedTypeConfiguration>()
                                     .Match(e => e.EmbeddedPropertyConfigs.ContainsKey(parent.Name))
                                     .ExecuteFirstAsync(cancellation);

        if (embeddedConfig != null && embeddedConfig.EmbeddedMigrations.Any()) {
            var entities = embedded as TEmbedded[] ?? embedded.ToArray();

            if (entities.Length == additionalData?.Count) {
                for (var i = 0; i < entities.Length; i++) {
                    await EmbeddedMigration(
                        entities[i],
                        embeddedConfig,
                        db,
                        additionalData[i],
                        cancellation: cancellation);
                }
            } else {
                foreach (var embed in entities) {
                    await EmbeddedMigration(embed, embeddedConfig, db, cancellation: cancellation);
                }
            }
        }
    }

    internal static async Task EmbeddedMigration<TEmbedded>(TEmbedded entity,
                                                            EmbeddedTypeConfiguration typeConfig,
                                                            DB db,
                                                            Dictionary<string, object>? additionalData = null,
                                                            CancellationToken cancellation = default)
        where TEmbedded : IEmbeddedEntity {
        var migrations = await typeConfig.EmbeddedMigrations
                                         .ChildrenQueryable()
                                         .ToListAsync(cancellationToken: cancellation);

        if (migrations.Count == 0) {
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
                    await db.AddField(addField.Field, doc, entityDoc);
                } else if (op is DropFieldOperation dropField) {
                    doc.Remove(dropField.Field.FieldName);
                } else if (op is AlterFieldOperation alterField) {
                    doc.Remove(alterField.OldField.FieldName);
                    await db.AddField(alterField.Field, doc, entityDoc);
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
    }
}