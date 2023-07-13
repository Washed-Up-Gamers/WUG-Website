global using WUG.Database;
global using WUG.Database.Models.Districts;
global using WUG.Database.Models.Economy;
global using WUG.Database.Models.Entities;
global using WUG.Database.Models.Factories;
global using WUG.Database.Models.Government;
global using WUG.Database.Models.Groups;
global using WUG.Database.Models.Items;
global using WUG.Database.Models.Military;
global using Shared.Models.Permissions;
global using WUG.Database.Models.Buildings;
global using WUG.Database.Models.Users;
global using WUG.Database.Models.OAuth2;
global using WUG.Models.Districts;
global using WUG.Managers;
global using WUG.WUGVAI;
global using System.Net.Http.Json;
global using WUG.Http;
global using Shared.Models.TradeDeals;
global using ProvinceModifierType = Shared.Models.Districts.ProvinceModifierType;
global using NationModifierType = Shared.Models.Districts.Modifiers.NationModifierType;
global using DivisionModifierType = Shared.Models.Military.DivisionModifierType;
global using ProvinceMetadata = Shared.Models.Districts.ProvinceMetadata;
global using DisCatSharp.Enums;
global using DisCatSharp;
global using DisCatSharp.Entities;
global using DisCatSharp.CommandsNext;
global using DisCatSharp.CommandsNext.Attributes;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WUG.API;
using WUG.Workers;
using WUG.Managers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using WUG.Database.Managers;
using System.Net;
using WUG.Helpers;
using WUG.Scripting.Parser;
using Microsoft.OpenApi.Models;
using WUG.Web;
using SV2.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

//LuaParser parser = new();

//parser.LoadTokenizer();
//parser.Parse(File.ReadAllText("Managers/Data/BuildingUpgrades/factoryupgrades.lua"), "factoryupgrades.lua");

//string jsonString = JsonSerializer.Serialize((LuaTable)parser.Objects.Items.First(), options: new() { WriteIndented = true});
//await File.WriteAllTextAsync("Managers/Data/ParserOutput.txt", jsonString);

Defines.Load();


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "ApiPolicy",
        policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(_ => true)
                .AllowAnyOrigin();
        });
});

var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

builder.Configuration.GetSection("Discord").Get<DiscordConfig>();
builder.Configuration.GetSection("Database").Get<DBConfig>();


builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(builder.Configuration.GetSection("Kestrel"));
#if DEBUG
    options.Listen(IPAddress.Any, 7186, listenOptions => {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });
#else
    options.Listen(IPAddress.Any, 5002, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
    });
#endif
});

//builder.Services.AddMvc(options =>
//{
//    options.Filters.Add<UserRequiredAttribute>();
//});

if (false)
{
    string CONF_LOC = "SV2Config/";
    string DBCONF_FILE = "DBConfig.json";

    // Add services to the container.
    builder.Services.AddMvc(options =>
    {
        options.Filters.Add<UserRequiredAttribute>();
    }
    ).AddRazorRuntimeCompilation();

    // Create directory if it doesn't exist
    if (!Directory.Exists(CONF_LOC))
    {
        Directory.CreateDirectory(CONF_LOC);
    }

    // Load database settings
    DBConfig dbconfig;
    if (File.Exists(CONF_LOC + DBCONF_FILE))
    {
        // If there is a config, read it
        dbconfig = await JsonSerializer.DeserializeAsync<DBConfig>(File.OpenRead(CONF_LOC + DBCONF_FILE));
    }
    else
    {
        // Otherwise create a config with default values and write it to the location
        dbconfig = new DBConfig()
        {
            Database = "database",
            Host = "host",
            Password = "password",
            Username = "user"
        };

        File.WriteAllText(CONF_LOC + DBCONF_FILE, JsonSerializer.Serialize(dbconfig));
        Console.WriteLine("Error: No DB config was found. Creating file...");
    }

}

WashedUpDB.DbFactory = WashedUpDB.GetDbFactory();

using var dbctx = WashedUpDB.DbFactory.CreateDbContext();

string sql = WashedUpDB.GenerateSQL();

try
{
    await File.WriteAllTextAsync("../Database/Definitions.sql", sql);
}
catch (Exception e)
{

}

WashedUpDB.RawSqlQuery<string>(sql, null, true);

await DBCache.LoadAsync();

await VoopAI.Main();

builder.Services.AddDbContextPool<WashedUpDB>(options =>
{
    options.UseNpgsql(WashedUpDB.ConnectionString, options => options.EnableRetryOnFailure());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SpookVooper API", Description = "The official SpookVooper API", Version = "v1.0" });
    c.AddSecurityDefinition("Apikey", new OpenApiSecurityScheme()
    {
        Description = "The apikey used for authorizing your account.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Apikey"
    });
});

// ensure districts & Vooperia are created
await WashedUpDB.Startup();
//await ResourceManager.Load();

await GameDataManager.Load();

ProvinceManager.LoadMap();

builder.Services.AddHostedService<EconomyWorker>();
builder.Services.AddHostedService<TransactionWorker>();
builder.Services.AddHostedService<ItemTradeWorker>();
builder.Services.AddHostedService<TimeWorker>();
builder.Services.AddHostedService<DistrictUpdateWorker>();
builder.Services.AddHostedService<VoopAIWorker>();
builder.Services.AddHostedService<StatWorker>();
builder.Services.AddHostedService<SecurityHistoryWorker>();

builder.Services.AddDataProtection().PersistKeysToDbContext<WashedUpDB>();

builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.MaxAge = TimeSpan.FromDays(90);
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Expiration = TimeSpan.FromDays(150);
    options.SlidingExpiration = true;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(90);
    options.Cookie.MaxAge = TimeSpan.FromDays(90);
    options.Cookie.IsEssential = true;
});

builder.Services.AddSignalR();

builder.Services.AddMvc().AddSessionStateTempDataProvider().AddRazorRuntimeCompilation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging();
}

app.UseSwagger();

app.UseBlazorFrameworkFiles();
app.MapFallbackToFile("index.html");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();

app.UseCors();

//BaseAPI       .AddRoutes(app);
ItemDefinitionAPI.AddRoutes(app);
ItemAPI.AddRoutes(app);
EcoAPI.AddRoutes(app);
EntityAPI.AddRoutes(app);
DevAPI.AddRoutes(app);
BuildingAPI.AddRoutes(app);
RecipeAPI.AddRoutes(app);
DistrictAPI.AddRoutes(app);
UserAPI.AddRoutes(app);
TaxAPI.AddRoutes(app);
GroupAPI.AddRoutes(app);
SecurityAPI.AddRoutes(app);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ExchangeHub>("/ExchangeHub");
});

ExchangeHub.Current = app.Services.GetRequiredService<IHubContext<ExchangeHub>>();

foreach (var onaction in GameDataManager.LuaOnActions[WUG.Scripting.LuaObjects.OnActionType.OnServerStart]) {
    // OnServerStart actions MUST change scope
    onaction.EffectBody.Execute(new(null, null));
}

List<BaseEntity> entities = new();
//entities.AddRange(DBCache.GetAll<SVUser>());
//entities.AddRange(DBCache.GetAll<Group>());

foreach (var state in DBCache.GetAll<State>())
{
    if (state.GovernorId is not null)
    {
        BaseEntity entity = BaseEntity.Find(state.GovernorId);
        if (!state.Group.MembersIds.Contains((long)state.GovernorId))
            state.Group.MembersIds.Add((long)state.GovernorId);

        state.Group.AddEntityToRole(DBCache.Get<Group>(100), entity, state.Group.Roles.First(x => x.Name == "Governor"), true);
    }
}

foreach (var recipe in DBCache.GetAll<Recipe>())
{
    recipe.UpdateInputs();
    recipe.UpdateOutputs();
    recipe.UpdateModifiers();
    if (recipe.CustomOutputItemDefinitionId is not null)
    {
        var itemdef = DBCache.Get<ItemDefinition>(recipe.CustomOutputItemDefinitionId);
        itemdef.Modifiers = recipe.Modifiers;
    }
}

app.Run();