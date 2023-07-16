using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using WUG.Database.Models.Users;
using System.Diagnostics;
using WUG.Models.Manage;
using System.Web;
using System.Text.Json;
using WUG.Helpers;
using WUG.NonDBO;
using WUG.Models.Oauth;
using System.Net;
using System.Net.Http;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class AccountController : SVController {
    private static List<string> OAuthStates = new();

#if DEBUG
    private static string Redirecturl = "https://localhost:7186/callback";
#else
    private static string Redirecturl = "https://wug.superjacobl.com/callback";
    //private static string Redirecturl = "https://spookvooper.com/callback";
#endif
	private readonly ILogger<AccountController> _logger;
    

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Manage()
    {
        User? user = UserManager.GetUser(HttpContext);
        UserManageModel userManageModel = new()
        {
            Id = user.Id,
            Name = user.Name,
            user = user
        };

        if (user is null) 
        {
            return Redirect("/account/login");
        }

        return View(userManageModel);
    }

    public async Task<IActionResult> ViewAPIKey()
    {
        User? user = UserManager.GetUser(HttpContext);

        if (user is null) 
        {
            return Redirect("/account/login");
        }

        return View((object)user.ApiKey);
    }

    public IActionResult Logout()
    {
        HttpContext.Response.Cookies.Delete("wugid");
        return Redirect("/");
    }

    public IActionResult Entered()
    {
        
        long wugid = UserManager.GetWugidFromSession(HttpContext);
        Console.WriteLine(HttpContext.Session.GetString("code"));
        HttpContext.Response.Cookies.Append("wugid", wugid.ToString());
        return Redirect("/");
    }

    [Route("/callback")]
    public async Task<IActionResult> Callback(string code, string state)
    {
        if (!OAuthStates.Contains(state))
            return Forbid();

        HttpClient client = new HttpClient();

        var url = $"https://discord.com/api/oauth2/token";

        string stringresult = "";
        using (var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
        {
            new("client_id", DiscordConfig.instance.OAuthClientId.ToString()),
            new("client_secret", DiscordConfig.instance.OAuthClientSecret),
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", Redirecturl)
        }))
        {
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync(url, content);

            stringresult = await response.Content.ReadAsStringAsync();
        }

        var result = JsonSerializer.Deserialize<DiscordOAuthResponse>(stringresult);

        HttpWebRequest webRequest1 = (HttpWebRequest)WebRequest.Create("https://discord.com/api/users/@me");
        webRequest1.Method = "Get";
        webRequest1.ContentLength = 0;
        webRequest1.Headers.Add("Authorization", "Bearer " + result.access_token);
        webRequest1.ContentType = "application/x-www-form-urlencoded";

        string apiResponse = "";
        using (HttpWebResponse response1 = webRequest1.GetResponse() as HttpWebResponse)
        {
            StreamReader reader1 = new StreamReader(response1.GetResponseStream());
            apiResponse = reader1.ReadToEnd();
        }

        var userinfo = JsonSerializer.Deserialize<DiscordUserInfo>(apiResponse);
        ulong userid = ulong.Parse(userinfo.id);

        var member = await VoopAI.Server.GetMemberAsync(userid);
        var user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == userid);
        if (user is null)
        {
            using var dbctx = WashedUpDB.DbFactory.CreateDbContext();
            user = new User(member.Nickname, userid);
            DBCache.AddNew(user.Id, user);

            //await dbctx.SaveChangesAsync();
        }

        user.ImageUrl = member.AvatarUrl;
        user.Name = member.Nickname;
        await user.Create();

        HttpContext.Response.Cookies.Append("wugid", user.Id.ToString());
        return Redirect("/");
    }

    public async Task<IActionResult> Login()
    {
        var oauthstate = Guid.NewGuid().ToString();

        OAuthStates.Add(oauthstate);
        var url = $"https://discord.com/api/oauth2/authorize?client_id=1124797654199717938&redirect_uri={HttpUtility.UrlEncode(Redirecturl)}&response_type=code&scope=identify&state={oauthstate}";
        return Redirect(url);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class DiscordOAuthDataModel
{
    public long client_id { get; set; }
    public string client_secret { get; set; }
    public string grant_type { get; set; }
    public string code { get; set; }
    public string redirect_uri { get; set; }
}

public class DiscordOAuthResponse
{
    public string access_token { get; set; }
}

public class DiscordUserInfo
{
    public string username { get; set; }
    public string id { get; set; }
}