namespace MongoDB.Entities;

public class EmbeddedTypeConfigBuilder : TypeConfigBuilderBase<EmbeddedTypeConfiguration, EmbeddedTypeConfigBuilder> {
    protected override EmbeddedTypeConfigBuilder Builder => this;
    protected override EmbeddedTypeConfiguration TypeConfiguration { get; set; } = new();
    
    public EmbeddedTypeConfigBuilder FromConfig(EmbeddedTypeConfiguration config) {
        this.TypeConfiguration = config;
        return this.Builder;
    }

    public EmbeddedTypeConfigBuilder HasEmbeddedPropertyConfig<TParent>(EmbeddedPropertyConfig config)
        where TParent : IDocumentEntity{
        this.TypeConfiguration.EmbeddedPropertyConfigs.Add(typeof(TParent).Name,config);
        return this.Builder;
    }

    public EmbeddedTypeConfigBuilder HasEmbeddedPropertyConfig<TParent>(
        Action<EmbeddedPropertyBuilder<TParent>> builder) {
        var builderInstance = new EmbeddedPropertyBuilder<TParent>();
        builder(builderInstance);
        this.TypeConfiguration.EmbeddedPropertyConfigs.Add(typeof(TParent).Name,builderInstance.Build());
        return this.Builder;
    }
}