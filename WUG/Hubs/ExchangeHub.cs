using Microsoft.AspNetCore.SignalR;

namespace SV2.Hubs;

public class ExchangeHub : Hub
{
    public static IHubContext<ExchangeHub> Current;
}