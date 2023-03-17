namespace HP.Pulsar.Search.Keyword.Infrastructure;

public class KeywordSearchInfo
{
    public string DatabaseConnectionString { get; init; }
    public string SearchEngineUrl { get; init; }
    public PulsarEnvironment Environment { get; init; }
    public string SearchEngineIndexName { get; init; }
}
