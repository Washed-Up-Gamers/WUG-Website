using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using WUG.Database.Models.Users;
using System.Diagnostics;
using WUG.Models.Manage;
using System.Text.Json;
using WUG.Helpers;
using WUG.NonDBO;
using ChartJSCore.Helpers;
using ChartJSCore.Models;
using Microsoft.EntityFrameworkCore;
using WUG.Database.Models.Stats;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TargetType = WUG.Database.Models.Stats.TargetType;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class StatsController : SVController 
{
    private readonly ILogger<StatsController> _logger;
    private readonly WashedUpDB _dbctx;
    public StatsController(ILogger<StatsController> logger, WashedUpDB dbctx)
    {
        _logger = logger;
        _dbctx = dbctx;
    }

    private void GenerateGraphWithMoreThanOneDataSet(ViewDataDictionary ViewData, string title, List<string> Colors, List<string> DataTitles, string id, List<string> labels, List<List<double?>> datalists)
    {
        Chart chart = new Chart();

        chart.Type = Enums.ChartType.Line;

        ChartJSCore.Models.Data data = new ChartJSCore.Models.Data();
        data.Labels = labels;
        chart.Options = new Options()
        {
            Plugins = new()
            {
                Legend = new()
                {
                    Labels = new()
                    {
                        Color = ChartColor.FromRgba(255, 255, 255, 0.8)
                    },
                },
                Title = new()
                {
                    Color = ChartColor.FromRgba(255, 255, 255, 0.8),
                    Display = true,
                    Text = new List<string>() { title }
                }
            },
            Scales = new()
            {
                { "yAxes",
                    new CartesianScale() {
                        Display = true,
                        Ticks = new()
                        {
                            Color = ChartColor.FromRgba(255, 255, 255, 0.8)
                        }
                    }
                },
                { "xAxes",
                    new CartesianScale() {
                        Display = true,
                        Ticks = new()
                        {
                            Color = ChartColor.FromRgba(255, 255, 255, 0.8)
                        }
                    }
                }
            },
        };

        data.Datasets = new List<Dataset>();

        int i = 0;
        foreach (var datalist in datalists)
        {
            LineDataset dataset = new LineDataset()
            {
                Label = DataTitles[i],
                Data = datalist,
                Fill = "false",
                Tension = 0.1,
                BackgroundColor = new List<ChartColor> { ChartColor.FromHexString(Colors[i]) },
                BorderColor = new List<ChartColor> { ChartColor.FromHexString(Colors[i]) },
                PointBorderColor = new List<ChartColor> { ChartColor.FromHexString(Colors[i]) },
                PointBackgroundColor = new List<ChartColor> { ChartColor.FromHexString("#ffffff") },
                PointHoverBackgroundColor = new List<ChartColor> { ChartColor.FromHexString(Colors[i]) },
                PointHoverBorderColor = new List<ChartColor> { ChartColor.FromRgb(220, 220, 220) },
                PointHoverBorderWidth = new List<int> { 2 },
                PointRadius = new List<int> { 2 },
                PointHitRadius = new List<int> { 15 },
            };

            data.Datasets.Add(dataset);
            i += 1;
        }

        chart.Data = data;

        ViewData[id] = chart;
    }

    private void GenerateGraph(ViewDataDictionary ViewData, string title, string yaxistitle, string id, List<string> labels, List<double?> datalist)
    {
        Chart chart = new Chart();

        chart.Type = Enums.ChartType.Line;

        ChartJSCore.Models.Data data = new ChartJSCore.Models.Data();
        data.Labels = labels;
        chart.Options = new Options()
        {
            Plugins = new()
            {
                Legend = new()
                {
                    Labels = new()
                    {
                        Color = ChartColor.FromRgba(255, 255, 255, 0.8)
                    },
                },
                Title = new()
                {
                    Color = ChartColor.FromRgba(255, 255, 255, 0.8),
                    Display = true,
                    Text = new List<string>() { title }
                }
            },
            Scales = new()
            {
                { "yAxes", 
                    new CartesianScale() {
                        Display = true,
                        Ticks = new()
                        {
                            Color = ChartColor.FromRgba(255, 255, 255, 0.8)
                        }
                    }
                },
                { "xAxes",
                    new CartesianScale() {
                        Display = true,
                        Ticks = new()
                        {
                            Color = ChartColor.FromRgba(255, 255, 255, 0.8)
                        }
                    }
                }
            },
        };

        LineDataset dataset = new LineDataset()
        {
            Label = yaxistitle,
            Data = datalist,
            Fill = "false",
            Tension = 0.1,
            BackgroundColor = new List<ChartColor> { ChartColor.FromRgba(75, 192, 192, 0.4) },
            BorderColor = new List<ChartColor> { ChartColor.FromRgb(75, 192, 192) },
            PointBorderColor = new List<ChartColor> { ChartColor.FromRgb(75, 192, 192) },
            PointBackgroundColor = new List<ChartColor> { ChartColor.FromHexString("#ffffff") },
            PointHoverBackgroundColor = new List<ChartColor> { ChartColor.FromRgb(75, 192, 192) },
            PointHoverBorderColor = new List<ChartColor> { ChartColor.FromRgb(220, 220, 220) },
            PointHoverBorderWidth = new List<int> { 2 },
            PointRadius = new List<int> { 2 },
            PointHitRadius = new List<int> { 15 },
        };

        data.Datasets = new List<Dataset>();
        data.Datasets.Add(dataset);

        chart.Data = data;

        ViewData[id] = chart;
    }

    public async Task<IActionResult> GlobalGraphs()
    {
        var statsobjects = await _dbctx.Stats
            .Where(x => x.TargetType == TargetType.Global && x.StatType == StatType.Population)
            .OrderByDescending(x => x.Date)
            .Take(24*30)
            .ToListAsync();

        statsobjects.Reverse();

        GenerateGraph(ViewData, "Global Population (30 days)", "Global Population", "graph1", statsobjects.Select(x => String.Format("{0:M/d/yyyy} {0:t}", x.Date)).ToList(), statsobjects.Select(x => (double?)x.Value).ToList());

        statsobjects = await _dbctx.Stats
            .Where(x => x.TargetType == TargetType.Global && x.StatType == StatType.TotalBuildingSlots)
            .OrderByDescending(x => x.Date)
            .Take(24 * 30)
            .ToListAsync();

        statsobjects.Reverse();
        GenerateGraph(ViewData, "Global Total Building Slots (30 days)", "Total Building Slots", "graph2", statsobjects.Select(x => String.Format("{0:M/d/yyyy} {0:t}", x.Date)).ToList(), statsobjects.Select(x => (double?)x.Value).ToList());

        statsobjects = await _dbctx.Stats
            .Where(x => x.TargetType == TargetType.Global && x.StatType == StatType.UsedBuildingSlots)
            .OrderByDescending(x => x.Date)
            .Take(24 * 30)
            .ToListAsync();

        statsobjects.Reverse();
        GenerateGraph(ViewData, "Global Used Building Slots (30 days)", "Used Building", "graph3", statsobjects.Select(x => String.Format("{0:M/d/yyyy} {0:t}", x.Date)).ToList(), statsobjects.Select(x => (double?)x.Value).ToList());

        return View();
    }

    public async Task<IActionResult> AllNationsGraphs()
    {
        var statsobjects = await _dbctx.Stats
            .Where(x => x.TargetType == TargetType.Nation)
            .GroupBy(x => x.TargetId)
            .Select(x => new {
                Key = (long)x.Key,
                Items = x
                    .Where(x => x.TargetType == TargetType.Nation && x.StatType == StatType.Population)
                    .OrderByDescending(x => x.Date)
                    .Take(24 * 30)
                    .ToList()
            })
            .ToListAsync();

        List<List<double?>> Data = new();
        List<string> DataTitles = new();
        List<string> NationColors = new();

        foreach (var Nationdata in statsobjects)
        {
            var Nationid = (long)Nationdata.Key;
            var Nation = DBCache.Get<Nation>(Nationid);
            DataTitles.Add($"{Nation.Name} Population");
            NationColors.Add($"#{Nation.Color}");

            List<double?> data = new();
            var objects = Nationdata.Items;
            objects.Reverse();
            Data.Add(objects.Select(x => (double?)x.Value).ToList());
        }

        GenerateGraphWithMoreThanOneDataSet(ViewData, "Nations Population (30 days)", NationColors, DataTitles, "graph1", statsobjects.First().Items.Select(x => String.Format("{0:M/d/yyyy} {0:t}", x.Date)).ToList(), Data);

        statsobjects = await _dbctx.Stats
            .Where(x => x.TargetType == TargetType.Nation)
            .GroupBy(x => x.TargetId)
            .Select(x => new {
                Key = (long)x.Key,
                Items = x
                    .Where(x => x.TargetType == TargetType.Nation && x.StatType == StatType.TotalBuildingSlots)
                    .OrderByDescending(x => x.Date)
                    .Take(24 * 30)
                    .ToList()
            })
            .ToListAsync();

        Data = new();
        DataTitles = new();

        foreach (var Nationdata in statsobjects)
        {
            var Nationid = (long)Nationdata.Key;
            var Nation = DBCache.Get<Nation>(Nationid);
            DataTitles.Add($"{Nation.Name} Total Building Slots");

            List<double?> data = new();
            var objects = Nationdata.Items;
            objects.Reverse();
            Data.Add(objects.Select(x => (double?)x.Value).ToList());
        }
        GenerateGraphWithMoreThanOneDataSet(ViewData, "Nations Total Building Slots (30 days)", NationColors, DataTitles, "graph2", statsobjects.First().Items.Select(x => String.Format("{0:M/d/yyyy} {0:t}", x.Date)).ToList(), Data);

        statsobjects = await _dbctx.Stats
            .Where(x => x.TargetType == TargetType.Nation)
            .GroupBy(x => x.TargetId)
            .Select(x => new {
                Key = (long)x.Key,
                Items = x
                    .Where(x => x.TargetType == TargetType.Nation && x.StatType == StatType.UsedBuildingSlots)
                    .OrderByDescending(x => x.Date)
                    .Take(24 * 30)
                    .ToList()
            })
            .ToListAsync();

        Data = new();
        DataTitles = new();

        foreach (var Nationdata in statsobjects)
        {
            var Nationid = (long)Nationdata.Key;
            var Nation = DBCache.Get<Nation>(Nationid);
            DataTitles.Add($"{Nation.Name} Used Building Slots");

            List<double?> data = new();
            var objects = Nationdata.Items;
            objects.Reverse();
            Data.Add(objects.Select(x => (double?)x.Value).ToList());
        }
        GenerateGraphWithMoreThanOneDataSet(ViewData, "Nations Used Building Slots (30 days)", NationColors, DataTitles, "graph3", statsobjects.First().Items.Select(x => String.Format("{0:M/d/yyyy} {0:t}", x.Date)).ToList(), Data);

        return View();
    }
}