using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WUG;

public static class Functions
{
    public static string ToJson(object? value) => JsonSerializer.Serialize(value);
    public static async Task<WorthGraphData> GetWorthGraphData(WashedUpDB dbctx)
    {
        var worth = new Dictionary<string, long>();
        var data = await dbctx.SecurityOwnerships
            .Include(x => x.Security)
            .GroupBy(x => x.OwnerId)
            .Select(x => new
            {
                Key = x.Key,
                Item = x.Sum(x => x.Amount * x.Security.Price)
            })
            .ToListAsync();
        decimal total = data.Sum(x => x.Item);
        data = data.OrderByDescending(x => x.Item).ToList();
        foreach (var item in data)
        {
            worth.Add(BaseEntity.Find(item.Key).Name, (long)Math.Ceiling(item.Item));
        }

        return new WorthGraphData()
        {
            Total = total,
            Worth = worth
        };
    }
}

public class WorthGraphData
{
    public Dictionary<string, long> Worth { get; set; }
    public decimal Total { get; set; }
}