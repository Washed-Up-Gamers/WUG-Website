using WUG.Models.Map;
using WUG.NonDBO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Text;

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
                        var district = ProvinceIdsToNation.ContainsKey(id) ? ProvinceIdsToNation[id] : null;
                        long disid = 100;
                        if (district is not null)
                            disid = district.Id;
                        var state = new MapState()
                        {
                            Id = id,
                            D = child.Attributes["d"].Value,
                            DistrictId = disid,
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
        var _mapStates = new Dictionary<long, MapState>();
        foreach (var state in mapStates.Values)
        {
            //if (state.DistrictId == 100 || state.IsOcean == true)
            if (state.IsOcean == true)
                continue;

            var districtstate = _mapStates.ContainsKey(state.DistrictId) ? _mapStates[state.DistrictId] : null;
            var districtmapdata = MapController.DistrictMaps.FirstOrDefault(x => x.DistrictId == state.DistrictId);
            if (districtstate is not null)
            {
                districtmapdata.Provinces.Add(state);
                districtstate.DStringBuilder.Append($" {state.D}");
                var posinfo = state.D.Split(" ");
                int xpos = (int)double.Parse(posinfo[1]);
                int ypos = (int)double.Parse(posinfo[2]);
                state.XPos = xpos;
                state.YPos = ypos;

                if (districtmapdata.LowestXPos > xpos)
                    districtmapdata.LowestXPos = xpos;
                if (districtmapdata.LowestYPos > ypos)
                    districtmapdata.LowestYPos = ypos;
                if (districtmapdata.HighestXPos < xpos)
                    districtmapdata.HighestXPos = xpos;
                if (districtmapdata.HighestYPos < ypos)
                    districtmapdata.HighestYPos = ypos;
            }
            else
            {
                districtstate = new MapState()
                {
                    Id = state.DistrictId,
                    D = "",
                    DStringBuilder = new StringBuilder(1_000_000),
                    DistrictId = state.DistrictId,
                    IsOcean = false
                };
                districtstate.DStringBuilder.Append(state.D);
                _mapStates.Add(districtstate.Id, districtstate);

                districtmapdata = new()
                {
                    Provinces = new(),
                    DistrictId = state.DistrictId,
                    LowestXPos = 9999,
                    LowestYPos = 9999,
                    HighestYPos = 0,
                    HighestXPos = 0,
                };

                districtmapdata.Provinces.Add(state);

                MapController.DistrictMaps.Add(districtmapdata);
            }

            var dbprovince = DBCache.Get<Province>(state.Id);
            if (dbprovince is null)
            {
                var district = DBCache.Get<Nation>(state.DistrictId);
                dbprovince = new(rnd)
                {
                    DistrictId = state.DistrictId,
                    Id = state.Id,
                    Name = $"Province {state.Id}"
                };
                DBCache.AddNew(dbprovince.Id, dbprovince);
                //dbctx.Provinces.Add(dbprovince);
                //district.Provinces.Add(dbprovince);
            }
            else
            {
                dbprovince.DistrictId = districtstate.DistrictId;
                dbprovince.District = districtstate.District;
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
    }
}
