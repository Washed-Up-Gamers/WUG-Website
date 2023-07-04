using Newtonsoft.Json;

namespace SV2.Models.Exchange;

public class StockTradeModel
{
    [JsonProperty("Ticker")]
    public string Ticker { get; set; }

    [JsonProperty("Amount")]
    public long Amount { get; set; }

    [JsonProperty("Price")]
    public decimal Price { get; set; }

    [JsonProperty("StockBalance")]
    public decimal StockBalance { get; set; }

    [JsonProperty("EntityId")]
    public string EntityId { get; set; }

    [JsonProperty("Type")]
    public string Type { get; set; }
}