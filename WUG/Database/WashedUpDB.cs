using Microsoft.EntityFrameworkCore;
using WUG.Database.Models.Users;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Items;
using WUG.Database.Models.Factories;
using WUG.Database.Models.Districts;
using WUG.Database.Models.Government;
using System;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using System.Data.Common;
using System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using System.Text;
using WUG.Database.Models.News;
using WUG.Database.Models.Corporations;
using WUG.Database.Models.Misc;
using WUG.Database.Models.Stats;
using SV2.Database.Models.Misc;
using WUG.Database.Models.Economy.Stocks;

/*  Valour - A free and secure chat client
 *  Copyright (C) 2021 Vooper Media LLC
 *  This program is subject to the GNU Affero General Public license
 *  A copy of the license should be included - if not, see <http://www.gnu.org/licenses/>
 */

namespace WUG.Database;

/// <summary>A replacement for <see cref="NpgsqlSqlGenerationHelper"/>
/// to convert PascalCaseCsharpyIdentifiers to alllowercasenames.
/// So table and column names with no embedded punctuation
/// get generated with no quotes or delimiters.</summary>
public class NpgsqlSqlGenerationLowercasingHelper : NpgsqlSqlGenerationHelper
{
    //Don't lowercase ef's migration table
    const string dontAlter = "__EFMigrationsHistory";
    static string Customize(string input) => input == dontAlter ? input : input.ToLower();
    public NpgsqlSqlGenerationLowercasingHelper(RelationalSqlGenerationHelperDependencies dependencies)
        : base(dependencies) { }
    public override string DelimitIdentifier(string identifier)
        => base.DelimitIdentifier(Customize(identifier));
    public override void DelimitIdentifier(StringBuilder builder, string identifier)
        => base.DelimitIdentifier(builder, Customize(identifier));
}

public class WashedUpDB : DbContext, IDataProtectionKeyContext
{

    public static WashedUpDB Instance = new WashedUpDB(DBOptions);

    public static string ConnectionString = $"Host={DBConfig.instance.Host};Database={DBConfig.instance.Database};Username={DBConfig.instance.Username};Pwd={DBConfig.instance.Password}";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(ConnectionString, options => { 
            options.EnableRetryOnFailure();
        });
        options.ReplaceService<ISqlGenerationHelper, NpgsqlSqlGenerationLowercasingHelper>();
        //options.EnableSensitiveDataLogging();
        options.UseLowerCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //modelBuilder.HasCharSet(CharSet.Utf8Mb4);
    }

    /// <summary>
    /// This is only here to fulfill the need of the constructor.
    /// It does literally nothing at all.
    /// </summary>
    public static DbContextOptions DBOptions;

    public static string GenerateSQL()
    {
        using var dbctx = DbFactory.CreateDbContext();
        string sql = dbctx.Database.GenerateCreateScript();
        sql = sql.Replace("numeric(20,0) ", "BIGINT ");
        sql = sql.Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        sql = sql.Replace("CREATE INDEX", "CREATE INDEX IF NOT EXISTS");
        sql = sql.Replace("CREATE INDEX IF NOT EXISTS ix_messages_hash ON messages (hash);", "CREATE UNIQUE INDEX IF NOT EXISTS ix_messages_hash ON messages (hash);");
        return sql;
    }

    public static PooledDbContextFactory<WashedUpDB> DbFactory;

    public static PooledDbContextFactory<WashedUpDB> GetDbFactory()
    {
        string ConnectionString = $"Host={DBConfig.instance.Host};Database={DBConfig.instance.Database};Username={DBConfig.instance.Username};Pwd={DBConfig.instance.Password}";
        var options = new DbContextOptionsBuilder<WashedUpDB>()
            .UseNpgsql(ConnectionString, options => {
                options.EnableRetryOnFailure();
            })
            .ReplaceService<ISqlGenerationHelper, NpgsqlSqlGenerationLowercasingHelper>()
            .Options;
        return new PooledDbContextFactory<WashedUpDB>(options);
    }

    public static List<T> RawSqlQuery<T>(string query, Func<DbDataReader, T>? map, bool noresult = false)
    {
        using var dbctx = DbFactory.CreateDbContext();
        using DbCommand command = dbctx.Database.GetDbConnection().CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

        //Console.WriteLine(ConfigManger.Config);

        dbctx.Database.OpenConnection();

        using var result = command.ExecuteReader();
        if (!noresult)
        {
            var entities = new List<T>();

            while (result.Read())
            {
                entities.Add(map(result));
            }

            return entities;
        }
        return new List<T>();
    }


    /// <summary>
    /// Table for SV2 users
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Table for SV2 groups
    /// </summary>
    public DbSet<Group> Groups { get; set; }

    public DbSet<Corporation> Corporations { get; set; }
    public DbSet<CorporationShare> CorporationShares { get; set; }
    public DbSet<CorporationShareClass> CorporationShareClasses { get; set; }

    public DbSet<Security> Securities { get; set; }
    public DbSet<SecurityOwnership> SecurityOwnerships { get; set; }
    public DbSet<SecurityHistory> SecurityHistories { get; set; }

    public DbSet<TaxPolicy> TaxPolicies { get; set; }
    public DbSet<ItemDefinition> ItemDefinitions {get; set; }
    public DbSet<SVItemOwnership> SVItemOwnerships { get; set; }
    public DbSet<Factory> Factories { get; set; }
    public DbSet<Mine> Mines { get; set; }
    public DbSet<UBIPolicy> UBIPolicies { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Nation> Nations { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<Infrastructure> Infrastructures { get; set; }
    public DbSet<Farm> Farms { get; set; }
    public DbSet<GroupRole> GroupRoles { get; set; }
    public DbSet<Election> Elections { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<ItemTrade> ItemTrades { get; set; }

    public DbSet<UpdateTimeStuff> UpdateTimeStuffs { get; set; }
    public DbSet<CurrentTime> CurrentTimes { get; set; }

    //public DbSet<DistrictStaticModifier> DistrictStaticModifiers { get; set; }
    public DbSet<CouncilMember> Senators { get; set; }
    public DbSet<NewsPost> NewsPosts { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<BuildingRequest> BuildingRequests { get; set; }
    public DbSet<EntityBalanceRecord> EntityBalanceRecords { get; set; }
    public DbSet<State> States { get; set; }

    public DbSet<OauthApp> OauthApps { get; set; }
    public DbSet<AuthToken> AuthTokens { get; set; }
    public DbSet<Stat> Stats { get; set; }

    public DbSet<JobApplication> JobApplications { get; set; }

    public WashedUpDB(DbContextOptions options)
    {
            
    }

    public static async Task Startup() 
    {
        //List<string> cands = new List<string>() {
        //    "u-3bfaf0da-05db-4b4d-b77e-78d2faca261a", 
        //    "u-42a0bc23-6a2b-428b-940c-1595f355c8d0",
        //    "u-c6a3c7e4-825a-46a6-b75b-a1e420cb31d4",
        //    "u-df8a7699-ec5e-486e-8f0c-a81dd2bb3427"
        //};

        //Election elec = new Election(DateTime.UtcNow, DateTime.UtcNow.AddDays(1), cands, "new-vooperis", ElectionType.Senate);
        //await DBCache.Put<Election>(elec.Id, elec);
        //await VooperDB.Instance.Elections.AddAsync(elec);
        

        if (DBCache.FindEntity(100) is null) {
            Group UnitedNations = new Group("United Nations", 100);
            UnitedNations.Id = 100;
            UnitedNations.GroupType = GroupTypes.NonProfit;
            UnitedNations.Money = 1_500_000.0m;
            DBCache.AddNew(UnitedNations.Id, UnitedNations);
        }

        if (DBCache.FindEntity(99) is null)
        {
            Group UNSE = new Group("United Nations Stock Exchange", 99);
            UNSE.Id = 99;
            UNSE.GroupType = GroupTypes.NonProfit;
            UNSE.Money = 0.0m;
            DBCache.AddNew(UNSE.Id, UNSE);
        }

        Dictionary<string, long> Nations = new()
        {
            { "United States of Qortos", 101 },
            { "Azurite Empire", 102 }
        };

        foreach(var pair in Nations) {
            if (DBCache.FindEntity(pair.Value) is null) {

                // owner is Vooperia
                Group nation = new(pair.Key, 100)
                {
                    Id = pair.Value
                };

                nation.Money = 500_000.0m;


                Nation nation_object = new()
                {
                    Id = pair.Value,
                    Name = pair.Key,
                    GroupId = nation.Id,
                    ProvinceSlotsLeft = 0
                };

                nation_object.Modifiers = new();
                DBCache.AddNew(nation.Id, nation);
                DBCache.AddNew(nation_object.Id, nation_object);
            }
        }

        foreach (var district in DBCache.GetAll<Nation>()) {
            if (district.Group.GroupType != GroupTypes.District)
                district.Group.GroupType = GroupTypes.District;;
            if (!district.Group.Roles.Any(x => x.Name == "Governor")) {
                var role = new GroupRole() {
                    Name = "Governor",
                    Color = "ffffff",
                    GroupId = district.GroupId,
                    PermissionValue = GroupPermissions.FullControl.Value,
                    Id = IdManagers.GeneralIdGenerator.Generate(),
                    Authority = 99999999,
                    Salary = 0.0m,
                    MembersIds = new()
                };
                DBCache.AddNew(role.Id, role);
            }
        }

        foreach (var state in DBCache.GetAll<State>()) {
            if (state.Group.GroupType != GroupTypes.State)
                state.Group.GroupType = GroupTypes.State;
        }
    }
}
