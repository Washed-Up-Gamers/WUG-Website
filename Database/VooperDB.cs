using Microsoft.EntityFrameworkCore;
using SV2.Database.Models.Users;
using SV2.Database.Models.Groups;
using SV2.Database.Models.Economy;
using SV2.Database.Models.Items;
using SV2.Database.Models.Factories;
using SV2.Database.Models.Forums;
using SV2.Database.Models.Districts;
using System;

/*  Valour - A free and secure chat client
 *  Copyright (C) 2021 Vooper Media LLC
 *  This program is subject to the GNU Affero General Public license
 *  A copy of the license should be included - if not, see <http://www.gnu.org/licenses/>
 */

namespace SV2.Database;

public class VooperDB : DbContext
{

    public static VooperDB Instance = new VooperDB(DBOptions);

    public static string ConnectionString = $"Host={DBConfig.instance.Host};Database={DBConfig.instance.Database};Username={DBConfig.instance.Username};Pwd={DBConfig.instance.Password}";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(ConnectionString, options => { 
            options.EnableRetryOnFailure(); 
        });
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
    public static DbContextOptions DBOptions = new DbContextOptionsBuilder().UseNpgsql(ConnectionString, options => {
        options.EnableRetryOnFailure();
    }).Options;


    /// <summary>
    /// Table for SV2 users
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Table for SV2 groups
    /// </summary>
    public DbSet<Group> Groups { get; set; }

    public DbSet<TaxPolicy> TaxPolicies { get; set; }
    public DbSet<ForumPost> ForumPosts { get; set; }
    public DbSet<TradeItemDefinition> TradeItemDefinitions {get; set; }
    public DbSet<TradeItem> TradeItems { get; set; }
    public DbSet<Factory> Factories { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<UBIPolicy> UBIPolicies { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<GroupRole> GroupRoles { get; set; }

    public static string GenerateSQL()
    {
        string sql = VooperDB.Instance.Database.GenerateCreateScript();
        sql = sql.Replace("numeric ", "DECIMAL(20,10) ");
        return sql;
    }

    public VooperDB(DbContextOptions options)
    {
            
    }

    public static async Task Startup() 
    {
        await DBCache.LoadAsync();
        if (DBCache.FindEntity("g-vooperia") is null) {
            Group Vooperia = new Group("Vooperia", "g-t");
            Vooperia.Id = "g-vooperia";
            Vooperia.GroupType = GroupType.NonProfit;
            Vooperia.Credits = 500_000_000.0m;
            await DBCache.Put<Group>(Vooperia.Id, Vooperia);
            await VooperDB.Instance.Groups.AddAsync(Vooperia);
        }

        string[] districtids = new []{
            "ardenti-terra",
            "avalon",
            "kogi",
            "elysian-katonia",
            "lanatia",
            "landing-cove",
            "los-vooperis",
            "new-avalon",
            "new-spudland",
            "new-vooperis",
            "novastella",
            "old-king",
            "san-vooperisco",
            "thesonica",
            "voopmont"
        };

        foreach(string id in districtids) {
            if (DBCache.FindEntity("g-"+id) is null) {
                string name = id.Replace("-", " ");
                string[] namesplit = name.Split(" ");
                if (namesplit.Length == 2) {
                    // first part
                    name = $"{Char.ToUpper(namesplit[0][0])}{namesplit[0].Substring(1, namesplit[0].Length-1)} ";

                    // next part
                    name += $"{Char.ToUpper(namesplit[1][0])}{namesplit[1].Substring(1, namesplit[1].Length-1)}";
                }
                else {
                    name = $"{Char.ToUpper(namesplit[0][0])}{namesplit[0].Substring(1, namesplit[0].Length-1)}";
                }
                Group district = new Group(name, "g-vooperia");
                district.Id = "g-"+id;
                district.Credits = 100_000.0m;


                District district_object = new()
                {
                    Id = id,
                    Name = name,
                    GroupId = district.Id
                };

                await DBCache.Put<Group>(district.Id, district);
                await VooperDB.Instance.Groups.AddAsync(district);
                await DBCache.Put<District>(district_object.Id, district_object);
                await VooperDB.Instance.Districts.AddAsync(district_object);
            }
        }
       
        await VooperDB.Instance.SaveChangesAsync();
    }
}
