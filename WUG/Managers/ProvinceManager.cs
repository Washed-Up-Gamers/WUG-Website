using WUG.Models.Map;
using WUG.NonDBO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Text;
using WUG.Database.Managers;

namespace WUG.Managers;

public class Color
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }

    public Color() { }

    public Color(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
    }
}

public class DevelopmentMapColor
{
    public int MaxValue { get; set; }
    public Color color { get; set; }
    public DevelopmentMapColor() { }

    public DevelopmentMapColor(int maxValue, Color color)
    {
        MaxValue = maxValue;
        this.color = color;
    }
}

public class ProvinceManager
{
    public static Dictionary<long, ProvinceMetadata> ProvincesMetadata = new();
    public static List<DevelopmentMapColor> DevelopmentMapColors = new()
    {
        new(0, new(255, 0, 0)),
        new(40, new(238, 154, 0)),
        new(80, new(255, 240, 125)),
        new(150, new(116, 218, 81)),
        new(250, new(30, 255, 20)),
        new(500, new(0, 255, 0))
    };

    public static void LoadMap()
    {
        //using var dbctx = VooperDB.DbFactory.CreateDbContext();
        string data = System.IO.File.ReadAllText("Data/dystopia.json");
        var mapdata = JsonSerializer.Deserialize<MapDataJson>(data);

        data = System.IO.File.ReadAllText("Data/province_metadata.json");
        var items = JsonSerializer.Deserialize<Dictionary<string, ProvinceMetadata>>(data);
        foreach (string key in items.Keys)
        {
            var id = long.Parse(key);
            items[key].Id = id;
            ProvincesMetadata[id] = items[key];
        }

        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.LoadXml(System.IO.File.ReadAllText("Data/mapfromtool.svg"));
        Dictionary<long, MapState> mapStates = new();

        Dictionary<long, Nation?> ProvinceIdsToNation = new();
        var nations = DBCache.GetAll<Nation>().ToList();
        foreach (var pair in mapdata.Data)
        {
            var nation = nations.FirstOrDefault(x => x.Name == pair.Key);
            foreach (var provinceid in pair.Value)
                ProvinceIdsToNation.Add(provinceid, nation);
        }

        var n = doc.ChildNodes.Item(0);
        foreach (var node in doc.ChildNodes)
        {
            if (((XmlNode)node).Name == "svg")
            {
                foreach (var _child in ((XmlNode)node).ChildNodes)
                {
                    var child = (XmlNode)_child;
                    if (child.Name == "path")
                    {
                        if (!(child.Name == "path"))
                            continue;
                        long id = long.Parse(child.Attributes["id"].Value);
                        var Nation = ProvinceIdsToNation.ContainsKey(id) ? ProvinceIdsToNation[id] : null;
                        long disid = 100;
                        if (Nation is not null)
                            disid = Nation.Id;
                        var state = new MapState()
                        {
                            Id = id,
                            D = child.Attributes["d"].Value,
                            NationId = disid,
                            IsOcean = ProvincesMetadata[id].TerrianType == "ocean"
                        };
                        ProvincesMetadata[id].Path = child.Attributes["d"].Value;
                        mapStates.Add(state.Id, state);
                    }
                }
            }
        }

        Console.WriteLine("Loaded SVG Paths into memory");

        Random rnd = new Random();
        bool createdNewProvinces = false;
        var _mapStates = new Dictionary<long, MapState>();
        foreach (var state in mapStates.Values)
        {
            //if (state.NationId == 100 || state.IsOcean == true)
            if (state.IsOcean == true)
                continue;

            var Nationstate = _mapStates.ContainsKey(state.NationId) ? _mapStates[state.NationId] : null;
            var Nationmapdata = MapController.NationMaps.FirstOrDefault(x => x.NationId == state.NationId);
            if (Nationstate is not null)
            {
                Nationmapdata.Provinces.Add(state);
                Nationstate.DStringBuilder.Append($" {state.D}");
                var posinfo = state.D.Split(" ");
                int xpos = (int)double.Parse(posinfo[1]);
                int ypos = (int)double.Parse(posinfo[2]);
                //var posinfo = state.D.Split(" ");
                //int xpos = (int)double.Parse(posinfo[0].Split("m")[1]);
                //var firstpart = posinfo[1].Split("h")[0];
                //firstpart = firstpart.Split("v")[0];
                //int ypos = (int)double.Parse(firstpart);
                state.XPos = xpos;
                state.YPos = ypos;

                if (Nationmapdata.LowestXPos > xpos)
                    Nationmapdata.LowestXPos = xpos;
                if (Nationmapdata.LowestYPos > ypos)
                    Nationmapdata.LowestYPos = ypos;
                if (Nationmapdata.HighestXPos < xpos)
                    Nationmapdata.HighestXPos = xpos;
                if (Nationmapdata.HighestYPos < ypos)
                    Nationmapdata.HighestYPos = ypos;
            }
            else
            {
                Nationstate = new MapState()
                {
                    Id = state.NationId,
                    D = "",
                    DStringBuilder = new StringBuilder(1_000_000),
                    NationId = state.NationId,
                    IsOcean = false
                };
                Nationstate.DStringBuilder.Append(state.D);
                _mapStates.Add(Nationstate.Id, Nationstate);

                Nationmapdata = new()
                {
                    Provinces = new(),
                    NationId = state.NationId,
                    LowestXPos = 9999,
                    LowestYPos = 9999,
                    HighestYPos = 0,
                    HighestXPos = 0,
                };

                Nationmapdata.Provinces.Add(state);

                MapController.NationMaps.Add(Nationmapdata);
            }

            var dbprovince = DBCache.Get<Province>(state.Id);
            if (dbprovince is null)
            {
                var Nation = DBCache.Get<Nation>(state.NationId);
                dbprovince = new(rnd)
                {
                    NationId = state.NationId,
                    Id = state.Id,
                    Name = $"Province {state.Id}"
                };
                createdNewProvinces = true;
                DBCache.ProvincesBuildings[dbprovince.Id] = new();
                DBCache.AddNew(dbprovince.Id, dbprovince);
                //dbctx.Provinces.Add(dbprovince);
                //Nation.Provinces.Add(dbprovince);
            }
            else
            {
                dbprovince.NationId = Nationstate.NationId;
                dbprovince.Nation = Nationstate.Nation;
            }
        }
        //dbctx.SaveChanges();

        Console.WriteLine("Finished Loading Provinces");

        foreach (var value in _mapStates.Values)
        {
            value.D = value.DStringBuilder.ToString();
            value.DStringBuilder.Clear();
            value.DStringBuilder = null;
        }
        MapController.MapStates = _mapStates.Values.ToList();

        if (createdNewProvinces)
        {
            foreach (var nation in DBCache.GetAll<Nation>())
            {
                nation.Provinces = DBCache.GetAll<Province>().Where(x => x.NationId == nation.Id).ToList();
                var populationtarget = nation.Citizens.Count() * 2_500_000.0;
                populationtarget += 500_000.0;
                populationtarget += nation.Provinces.Count() * 10_000;
                var baseProvincePopulation = populationtarget / nation.Provinces.Count;
                var totalPrevProvincePopulation = nation.Provinces.Sum(x => x.Population);
                var ratio = totalPrevProvincePopulation / populationtarget;
                foreach (var province in nation.Provinces)
                {
                    province.Nation = nation;
                    province.PopulationMultiplier = province.Population / baseProvincePopulation / ratio;
                    province.DevelopmentValue = (int)(Math.Floor(Math.Pow(province.Population, Defines.NProvince[NProvince.DEVELOPMENT_POPULATION_EXPONENT])) * Defines.NProvince[NProvince.DEVELOPMENT_POPULATION_FACTOR]);
                    province.MigrationAttraction = 1;
                }
                nation.ProvincesByDevelopmnet = nation.Provinces.OrderByDescending(x => x.DevelopmentValue).ToList();
                nation.ProvincesByMigrationAttraction = nation.Provinces.OrderByDescending(x => x.MigrationAttraction).ToList();
                nation.HourlyTick();
            }
        }
    }
}
