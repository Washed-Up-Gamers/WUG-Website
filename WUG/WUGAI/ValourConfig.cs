using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WUG.WUGVAI;

public class DiscordConfig
{
    public static DiscordConfig instance;

    public long OAuthClientId { get; set; }
    public string OAuthClientSecret { get; set; }
    public string BotToken { get; set; }
    public DiscordConfig()
    {
        // Set main instance to the most recently created config
        instance = this;
    }
}