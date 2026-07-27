using System.Text.Json.Serialization;

namespace Kartly.Infrastructure.Currency;

/// <summary>
/// Wire shape of https://open.er-api.com/v6/latest/{base}. Deliberately minimal — only the
/// fields we validate or use. Unix timestamps are preferred over the RFC1123 string variants
/// the API also returns: integers have no locale or format ambiguity to get wrong.
/// </summary>
internal sealed class OpenErApiResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("base_code")]
    public string? BaseCode { get; set; }

    [JsonPropertyName("time_last_update_unix")]
    public long TimeLastUpdateUnix { get; set; }

    [JsonPropertyName("time_next_update_unix")]
    public long TimeNextUpdateUnix { get; set; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal>? Rates { get; set; }
}
