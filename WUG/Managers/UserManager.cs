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
    static Dictionary<string, long> SessionIdsToWugid = new();

    public static User? GetUser(HttpContext ctx)
    {
        string? d = null;
        ctx.Request.Cookies.TryGetValue("wugid", out d);
        if (d is null) {
            return null;
        }
        return DBCache.Get<User>(long.Parse(d!));
    }
    
    public static void AddLogin(string code, long id)
    {
        SessionIdsToWugid.Add(code, id);
    }

    public static long GetWugidFromSession(HttpContext ctx)
    {
        long svid = 0;
        SessionIdsToWugid.Remove(ctx.Session.GetString("code"), out svid);
        return svid;
    }

    public static string GetCode(HttpContext ctx)
    {
        string code = Guid.NewGuid().ToString();
        ctx.Session.SetString("code", code);
        return code;
    }
}