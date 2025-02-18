using System.Numerics;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using ImGuiNET;
using InventoryTools;
using InventoryTools.Logic;
using InventoryTools.Ui;
using OtterGui;
using OtterGui.Raii;

namespace InventoryToolsMock;

public class MockGameItemsWindow : Window
{
    public MockGameItemsWindow(string name = "Item Viewer", ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow)
    {
        _selectedCategory = new Dictionary<ulong, InventoryCategory>();
    }
    
    public MockGameItemsWindow() : base("Item Viewer", ImGuiWindowFlags.None, false)
    {
        _selectedCategory = new Dictionary<ulong, InventoryCategory>();
    }

    public Dictionary<ulong, InventoryCategory> _selectedCategory;
    public override void Draw()
    {
        using (var tabBar = ImRaii.TabBar("Bags", ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.TabListPopupButton))
        {
            if (tabBar.Success)
            {
                foreach (var inventory in PluginService.InventoryMonitor.Inventories)
                {
                    var character = PluginService.CharacterMonitor.GetCharacterById(inventory.Key);
                    var characterName = character?.Name ?? "Unknown"; 
                    using (var tabItem = ImRaii.TabItem(characterName + "##" + inventory.Key))
                    {
                        if (tabItem.Success)
                        {
                            using(var sideChild = ImRaii.Child("side", new Vector2(200, 0)))
                            {
                                if (sideChild.Success)
                                {
                                    foreach (var category in inventory.Value)
                                    {
                                        if (!_selectedCategory.ContainsKey(inventory.Key))
                                        {
                                            _selectedCategory[inventory.Key] = category.Key;
                                        }
                                        if (ImGui.Selectable(category.Key.FormattedName(), _selectedCategory.ContainsKey(inventory.Key) && _selectedCategory[inventory.Key] == category.Key))
                                        {
                                            _selectedCategory[inventory.Key] = category.Key;
                                        }
                                    }
                                }
                            }
                            ImGui.SameLine();
                            using(var mainChild = ImRaii.Child("main", new Vector2(0, 0)))
                            {
                                if (mainChild.Success)
                                {
                                    foreach (var category in inventory.Value)
                                    {
                                        if (_selectedCategory.ContainsKey(inventory.Key) &&
                                            _selectedCategory[inventory.Key] == category.Key)
                                        {
                                            var itemsByType = category.Value.GroupBy(c => c.SortedContainer);
                                            foreach (var type in itemsByType)
                                            {
                                                using (var typeChild = ImRaii.Child(type + "##" + type))
                                                {
                                                    ImGui.Text(type.Key.ToString());
                                                    ImGui.NewLine();
                                                    if (typeChild.Success)
                                                    {
                                                        var chunkedItems = type.OrderBy(c => c.Slot).Chunk(5);
                                                        foreach (var itemChunk in chunkedItems)
                                                        {
                                                            foreach (var item in itemChunk)
                                                            {
                                                                using (ImRaii.PushId(item.Slot))
                                                                {
                                                                    if(ImGui.ImageButton(item.ItemId == 0 ? PluginService.IconStorage[62574].ImGuiHandle :
                                                                        PluginService.IconStorage[item.Icon]
                                                                            .ImGuiHandle,
                                                                        new Vector2(32, 32)))
                                                                    {
                                                                        item.ItemId = 0;
                                                                        PluginService.FilterService.InvalidateFilters();
                                                                    }
                                                                    ImGuiUtil.HoverTooltip(item.FormattedName + " - " + item.Quantity + " in slot " + item.Slot);
                                                                    ImGui.SameLine();
                                                                }
                                                            }
                                                            ImGui.NewLine();
                                                        }
                                                    }

                                                }
                                                ImGui.SameLine();
                                            }
                                        }
                                    }
                                }
                            }
                            
                        }
                    }
                }
            }
        }
    }

    public override void Invalidate()
    {
        
    }

    public static string AsKey => "ItemViewer";
    public override FilterConfiguration? SelectedConfiguration { get; } = null;
    public override string Key { get; } = AsKey;
    public override bool DestroyOnClose { get; } = true;
    public override bool SaveState { get; } = false;
    public override Vector2 DefaultSize { get; } = new Vector2(1000, 1000);
    public override Vector2 MaxSize { get; } = new Vector2(2000, 2000);
    public override Vector2 MinSize { get; } = new Vector2(300, 300);
}