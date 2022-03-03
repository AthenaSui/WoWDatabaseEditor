using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using DynamicData;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels;

[AutoRegister]
[SingleInstance]
public class TablesListToolViewModel : ObservableBase, ITool
{
    private readonly ITableOpenService tableOpenService;
    private readonly IEventAggregator eventAggregator;
    private readonly Lazy<IDocumentManager> documentManager;
    private bool visibility;
    private bool isSelected;
    private List<TableItemViewModel> allTables = new();
    private string searchText = "";

    public string Title => "Tables";
    public string UniqueId => "tables";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Left;
    public bool OpenOnStart => true;
    public ObservableCollection<TableItemViewModel> FilteredTables { get; } = new();

    public TablesListToolViewModel(ITableDefinitionProvider definitionProvider,
        ITableOpenService tableOpenService,
        IEventAggregator eventAggregator,
        Lazy<IDocumentManager> documentManager) 
    {
        this.tableOpenService = tableOpenService;
        this.eventAggregator = eventAggregator;
        this.documentManager = documentManager;
        foreach (var defi in definitionProvider.Definitions)
        {
            allTables.Add(new TableItemViewModel(defi.TableName, defi));
            if (defi.ForeignTable != null)
            {
                foreach (var foreign in defi.ForeignTable)
                {
                    allTables.Add(new TableItemViewModel(foreign.TableName, defi));
                }   
            }
        }
        allTables.Sort((a, b) => String.Compare(a.TableName, b.TableName, StringComparison.Ordinal));
        
        On(() => SearchText, search =>
        {
            FilteredTables.Clear();
            if (string.IsNullOrEmpty(search))
            {
                FilteredTables.AddRange(allTables);
            }
            else
            {
                search = search.ToLower();
                foreach (var table in allTables)
                {
                    if (table.TableName.Contains(search, StringComparison.Ordinal))
                    {
                        FilteredTables.Add(table);
                    }
                }
            }
        });
    }

    public void OpenTable(TableItemViewModel item)
    {
        DoOpenTable(item).ListenErrors();
    }

    public void OpenOnly()
    {
        if (FilteredTables.Count == 1)
        {
            DoOpenTable(FilteredTables[0]).ListenErrors();
        }
    }
    
    private async Task DoOpenTable(TableItemViewModel item)
    {
        var solution = await tableOpenService.TryCreate(item.Definition);
        if (solution != null)
        {
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solution);
        }
    }
    
    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    public bool Visibility
    {
        get => visibility;
        set => SetProperty(ref visibility, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}

public class TableItemViewModel : ObservableBase
{
    public ImageUri Icon { get; }
    public string TableName { get; }
    public DatabaseTableDefinitionJson Definition { get; }

    public TableItemViewModel(string tableName, DatabaseTableDefinitionJson definition)
    {
        TableName = tableName;
        Definition = definition;
        Icon = new ImageUri(definition.IconPath ?? "Icons/document.png");
    }
}