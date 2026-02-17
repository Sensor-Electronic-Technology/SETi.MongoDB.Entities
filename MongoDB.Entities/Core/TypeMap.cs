using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class TypeMap {
    static readonly ConcurrentDictionary<Type, DocumentTypeConfiguration?> _typeToCollectionMap = new();
    static readonly ConcurrentDictionary<Type, EmbeddedTypeConfiguration?> _typeToEmbeddedCollectMap = new();

    internal static void Clear() {
        _typeToCollectionMap.Clear();
        _typeToEmbeddedCollectMap.Clear();
    }

    internal static DocumentTypeConfiguration? GetTypeConfiguration(Type entityType) {
        _typeToCollectionMap.TryGetValue(entityType, out var configuration);

        return configuration;
    }

    internal static EmbeddedTypeConfiguration? GetEmbeddedTypeConfiguration(Type entityType) {
        _typeToEmbeddedCollectMap.TryGetValue(entityType, out var configuration);
        return configuration;
    }

    internal static string DisplayEmbeddedTypes() {
        var types = GetEmbeddedTypeConfigurationKeys().ToList();
        return string.Join(", ", types);
    }
    
    internal static string DisplayTypes() {
        var types = GetTypeConfigurationKeys().ToList();
        return string.Join(", ", types);
    }

    internal static ICollection<Type> GetEmbeddedTypeConfigurationKeys()
        => _typeToEmbeddedCollectMap.Keys;

    internal static ICollection<Type> GetTypeConfigurationKeys()
        => _typeToCollectionMap.Keys;

    internal static void AddUpdateTypeConfiguration(Type entityType, DocumentTypeConfiguration? typeConfiguration)
        => _typeToCollectionMap[entityType] = typeConfiguration;

    internal static void AddUpdateEmbeddedTypeConfiguration(Type entityType,
                                                            EmbeddedTypeConfiguration? typeConfiguration)
        => _typeToEmbeddedCollectMap[entityType] = typeConfiguration;

    internal static void ClearTypeConfigurations()
        => _typeToCollectionMap.Clear();

    internal static void ClearEmbeddedTypeConfigurations()
        => _typeToEmbeddedCollectMap.Clear();
}