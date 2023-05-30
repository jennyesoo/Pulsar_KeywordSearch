namespace HP.Pulsar.Search.Keyword.Infrastructure;

public class KeywordSearchInfo
{
    public required string DatabaseConnectionString { get; init; }
    public required string SearchEngineUrl { get; init; }
    public required string SearchEngineIndexName { get; init; }
}
