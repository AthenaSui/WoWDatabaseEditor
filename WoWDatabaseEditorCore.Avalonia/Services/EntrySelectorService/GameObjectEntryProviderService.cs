using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using WDE.Common;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.DBC;
using WDE.Common.TableData;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.EntrySelectorService;

[AutoRegister]
[SingleInstance]
public class GameObjectEntryProviderService : IGameobjectEntryOrGuidProviderService
{
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IFactionTemplateStore factionTemplateStore;
    private readonly IDatabaseRowsCountProvider databaseRowsCountProvider;
    private readonly IDatabaseProvider databaseProvider;

    public GameObjectEntryProviderService(ITabularDataPicker tabularDataPicker,
        IFactionTemplateStore factionTemplateStore,
        IDatabaseRowsCountProvider databaseRowsCountProvider,
        IDatabaseProvider databaseProvider)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.factionTemplateStore = factionTemplateStore;
        this.databaseRowsCountProvider = databaseRowsCountProvider;
        this.databaseProvider = databaseProvider;
    }
    
    private ITabularDataArgs<IGameObjectTemplate> BuildTable(string? customCounterTable, uint? entry, out int index)
    {
        index = -1;
        var gameObjects = databaseProvider.GetGameObjectTemplates();

        if (entry.HasValue)
        {
            for (int i = 0, count = gameObjects.Count; i < count; ++i)
                if (gameObjects[i].Entry == entry)
                {
                    index = i;
                    break;
                }
        }

        var columns = new List<ITabularDataColumn>()
        {
            new TabularDataColumn(nameof(IGameObjectTemplate.Entry), "Entry", 80),
            new TabularDataColumn(nameof(IGameObjectTemplate.Name), "Name", 210),
            new TabularDataColumn(nameof(IGameObjectTemplate.Type), "Type", 120)
        };
        if (customCounterTable != null)
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(IGameObjectTemplate.Entry), "Count", async (entry, token) =>
            {
                var count = await databaseRowsCountProvider.GetRowsCountByPrimaryKey(customCounterTable, entry, token);
                return count.ToString();
            }, 50));
        }
        else
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(IGameObjectTemplate.Entry), "Spawns",
                async (entry, token) =>
                {
                    var count = await databaseRowsCountProvider.GetGameObjectCountByEntry(entry, token);
                    return count.ToString();
                }, 50));
        }

        var table = new TabularDataBuilder<IGameObjectTemplate>()
            .SetTitle("Pick a gameobject")
            .SetData(gameObjects.AsIndexedCollection())
            .SetColumns(columns)
            .SetFilter((template, text) => template.Entry.Contains(text) ||
                                           template.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .SetExactMatchPredicate((template, search) => template.Entry.Is(search))
            .SetExactMatchCreator(search =>
            {
                if (!uint.TryParse(search, out var entry))
                    return null;
                return new AbstractGameObjectTemplate()
                {
                    Entry = entry,
                    Name = "Pick non existing"
                };
            })
            .Build();
        return table;
    }

    public async Task<int?> GetEntryFromService(uint? entry, string? customCounterTable = null)
    {
        var table = BuildTable(customCounterTable, entry, out var index);

        var result = await tabularDataPicker.PickRow(table, index);
        
        return (int?)result?.Entry;
    }

    public async Task<IReadOnlyCollection<int>> GetEntriesFromService(string? customCounterTable = null)
    {
        var table = BuildTable(customCounterTable, null, out var index);

        var result = await tabularDataPicker.PickRows(table);
        
        return result.Select(x => (int)x.Entry).ToList();
    }
}