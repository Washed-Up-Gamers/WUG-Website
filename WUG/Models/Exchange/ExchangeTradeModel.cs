using WUG.Database.Models.Economy.Stocks;

namespace WUG.Models.Exchange;

public class ExchangeTradeModel
{
    public Security Security { get; set; }
    public BaseEntity? Chosen_Account { get; set; }
}
