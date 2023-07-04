namespace WUG.Models.Exchange;

public class ExchangeIndexModel
{
    public BaseEntity ChosenAccount { get; set; }
    public long AccountId { get; set; }
    public string Sort { get; set; } = "Balance";
    public int Page { get; set; } = 0;
}
