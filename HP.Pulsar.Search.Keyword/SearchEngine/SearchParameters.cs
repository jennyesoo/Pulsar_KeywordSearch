using System.Text.Json.Serialization;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

internal class SearchParameters
{
    [JsonPropertyName("matchingStrategy")]
    public string MatchingStrategy => "all";

    [JsonPropertyName("limit")]
    public int Limit => 700;

    [JsonPropertyName("showMatchesPosition")]
    public bool ShowMatchesPosition => true;

    [JsonPropertyName("q")]
    public string Q { get; set; }
}
