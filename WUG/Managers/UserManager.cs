using System.Threading.Tasks;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Factories;
using WUG.Database.Models.Users;
using WUG.Database.Models.Items;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace WUG.Managers;

public static class UserManager
{
    static List<string> LoginCodes = new();
    static Dictionary<string, long> SessionIdsToSvids = new();

    public static SVUser? GetUser(HttpContext ctx)
    {
        string? d = null;
        ctx.Request.Cookies.TryGetValue("svid", out d);
        if (d is null) {
            return null;
        }
        return DBCache.Get<SVUser>(long.Parse(d!));
    }
    
    public static void AddLogin(string code, long id)
    {
        SessionIdsToSvids.Add(code, id);
    }

    public static long GetSvidFromSession(HttpContext ctx)
    {
        long svid = 0;
        SessionIdsToSvids.Remove(ctx.Session.GetString("code"), out svid);
        return svid;
    }

    public static string GetCode(HttpContext ctx)
    {
        string code = Guid.NewGuid().ToString();
        ctx.Session.SetString("code", code);
        return code;
    }
}