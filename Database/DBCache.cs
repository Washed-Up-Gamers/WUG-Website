using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SV2.Database;

public static class DBCache
{
    /// <summary>
    /// The high level cache object which contains the lower level caches
    /// </summary>
    public static Dictionary<Type, ConcurrentDictionary<long, object>> HCache = new();

    public static IEnumerable<T> GetAll<T>() where T : class
    {
        var type = typeof(T);

        if (!HCache.ContainsKey(type))
            yield break;

        foreach (T item in HCache[type].Values)
            yield return item;
    }

    /// <summary>
    /// Returns true if the cache contains the item
    /// </summary>
    public static bool Contains<T>(long Id) where T : class
    {
        var type = typeof(T);

        if (!HCache.ContainsKey(typeof(T)))
            return false;

        return HCache[type].ContainsKey(Id);
    }

    /// <summary>
    /// Places an item into the cache
    /// </summary>
    public static async Task Put<T>(long Id, T? obj) where T : class
    {
        // Empty object is ignored
        if (obj == null)
            return;

        // Get the type of the item
        var type = typeof(T);

        // If there isn't a cache for this type, create one
        if (!HCache.ContainsKey(type))
            HCache.Add(type, new ConcurrentDictionary<long, object>());

        if (!HCache[type].ContainsKey(Id)) {
            HCache[type][Id] = obj;
        }
    }

    /// <summary>
    /// Returns the item for the given id, or null if it does not exist
    /// </summary>
    public static T? Get<T>(long Id) where T : class
    {
        var type = typeof(T);

        if (HCache.ContainsKey(type))
            if (HCache[type].ContainsKey(Id)) 
                return HCache[type][Id] as T;

        return null;
    }

    public static T? Get<T>(long? Id) where T : class
    {
        if (Id is null)
            return null;
        var type = typeof(T);

        if (HCache.ContainsKey(type))
            if (HCache[type].ContainsKey((long)Id)) 
                return HCache[type][(long)Id] as T;

        return null;
    }

    public static BaseEntity? FindEntity(long Id)
    {
        var group = Get<Group>(Id);
        if (group is not null)
            return group;
        var user = Get<User>(Id);
        if (user is not null)
            return user;
        return null;
    }

    public static async Task LoadAsync()
    {
        //#if !DEBUG

        List<Task> tasks = new();
        foreach(Group group in VooperDB.Instance.Groups) {
            tasks.Add(DBCache.Put<Group>(group.Id, group));
        }
        foreach(User user in VooperDB.Instance.Users) {
            tasks.Add(DBCache.Put<User>(user.Id, user));
        }
        foreach(TaxPolicy policy in VooperDB.Instance.TaxPolicies) {
            tasks.Add(DBCache.Put<TaxPolicy>(policy.Id, policy));
        }
        foreach(TradeItem item in VooperDB.Instance.TradeItems) {
            tasks.Add(DBCache.Put<TradeItem>(item.Id, item));
        }
        foreach(TradeItemDefinition definition in VooperDB.Instance.TradeItemDefinitions) {
            tasks.Add(DBCache.Put<TradeItemDefinition>(definition.Id, definition));
        }
        foreach(Factory factory in VooperDB.Instance.Factories) {
            tasks.Add(DBCache.Put<Factory>(factory.Id, factory));
        }
        foreach(UBIPolicy policy in VooperDB.Instance.UBIPolicies) {
            tasks.Add(DBCache.Put<UBIPolicy>(policy.Id, policy));
        }
        foreach(District district in VooperDB.Instance.Districts) {
            tasks.Add(DBCache.Put<District>(district.Id, district));
        }
        foreach(GroupRole role in VooperDB.Instance.GroupRoles) {
            tasks.Add(DBCache.Put<GroupRole>(role.Id, role));
        }
        foreach(Election election in VooperDB.Instance.Elections) {
            tasks.Add(DBCache.Put<Election>(election.Id, election));
        }
        foreach(Vote vote in VooperDB.Instance.Votes) {
            tasks.Add(DBCache.Put<Vote>(vote.Id, vote));
        }
        foreach(Province province in VooperDB.Instance.Provinces) {
            tasks.Add(DBCache.Put<Province>(province.Id, province));
        }
        foreach(Recipe recipe in VooperDB.Instance.Recipes) {
            tasks.Add(DBCache.Put<Recipe>(recipe.Id, recipe));
        }
        foreach(Minister minister in VooperDB.Instance.Ministers) {
            tasks.Add(DBCache.Put<Minister>(minister.UserId, minister));
        }
        await Task.WhenAll(tasks);

        //#endif
    }

    public static async Task SaveAsync()
    {
        VooperDB.Instance.Groups.UpdateRange(GetAll<Group>());
        VooperDB.Instance.Users.UpdateRange(GetAll<User>());
        VooperDB.Instance.TaxPolicies.UpdateRange(GetAll<TaxPolicy>());
        VooperDB.Instance.TradeItems.UpdateRange(GetAll<TradeItem>());
        VooperDB.Instance.TradeItemDefinitions.UpdateRange(GetAll<TradeItemDefinition>());
        VooperDB.Instance.Factories.UpdateRange(GetAll<Factory>());
        VooperDB.Instance.TaxPolicies.UpdateRange(GetAll<TaxPolicy>());
        VooperDB.Instance.Districts.UpdateRange(GetAll<District>());
        VooperDB.Instance.Provinces.UpdateRange(GetAll<Province>());
        VooperDB.Instance.Recipes.UpdateRange(GetAll<Recipe>());
        VooperDB.Instance.Ministers.UpdateRange(GetAll<Minister>());
        await VooperDB.Instance.SaveChangesAsync();
    }
}