using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;
using Dalamud.Game.Text;
using Dalamud.Utility;
using ImGuiNET;
using InventoryTools.Extensions;
using InventoryTools.Logic;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Model;
using OtterGui;
using OtterGui.Raii;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace InventoryTools.Ui
{
    class ItemWindow : Window
    {
        public override bool SaveState => false;
        public static string AsKey(uint itemId)
        {
            return "item_" + itemId;
        }
        private uint _itemId;
        private ItemEx? Item => Service.ExcelCache.GetItemExSheet().GetRow(_itemId);
        private CraftItem? _craftItem = null;
        public ItemWindow(uint itemId, string name = "Allagan Tools - Invalid Item") : base(name)
        {
            Flags = ImGuiWindowFlags.NoSavedSettings;
            _itemId = itemId;
            if (Item != null)
            {
                WindowName = "Allagan Tools - " + Item.Name;
                RetainerTasks = Item.RetainerTasks.ToArray();
                RecipesResult = Item.RecipesAsResult.ToArray();
                RecipesAsRequirement = Item.RecipesAsRequirement.ToArray();
                Vendors = new List<(IShop shop, ENpc? npc, ILocation? location)>();
                foreach (var vendor in Item.Vendors)
                {
                    if (vendor.Name == "")
                    {
                        continue;
                    }
                    if (!vendor.ENpcs.Any())
                    {
                        Vendors.Add(new (vendor, null, null));
                    }
                    else
                    {
                        foreach (var npc in vendor.ENpcs)
                        {
                            if (!npc.Locations.Any())
                            {
                                Vendors.Add(new (vendor, npc, null));
                            }
                            else
                            {
                                foreach (var location in npc.Locations)
                                {
                                    Vendors.Add(new (vendor, npc, location));
                                }
                            }
                        }
                    }
                }

                GatheringSources = Item.GetGatheringSources().ToList();
                SharedModels = Item.GetSharedModels();
                MobDrops = Item.MobDrops.ToArray();
            }
            else
            {
                RetainerTasks = Array.Empty<RetainerTaskNormalEx>();
                RecipesResult = Array.Empty<RecipeEx>();
                RecipesAsRequirement = Array.Empty<RecipeEx>();
                GatheringSources = new();
                Vendors = new();
                SharedModels = new();
                MobDrops = Array.Empty<MobDropEx>();
            }
        }

        public List<ItemEx> SharedModels { get; }

        private List<GatheringSource> GatheringSources { get; }

        private List<(IShop shop, ENpc? npc, ILocation? location)> Vendors { get; }

        private RecipeEx[] RecipesAsRequirement { get;  }

        private RecipeEx[] RecipesResult { get; }

        private RetainerTaskNormalEx[] RetainerTasks { get; }
        
        private MobDropEx[] MobDrops { get; }

        public override string Key => AsKey(_itemId);
        public override bool DestroyOnClose => true;
        public override void Draw()
        {
            if (ImGui.GetWindowPos() != CurrentPosition)
            {
                CurrentPosition = ImGui.GetWindowPos();
            }

            if (Item == null)
            {
                ImGui.TextUnformatted("Item with the ID " + _itemId + " could not be found.");   
            }
            else
            {
                ImGui.TextUnformatted("Item Level " + Item.LevelItem.Row.ToString());
                if (Item.DescriptionString != "")
                {
                    ImGui.TextWrapped(Item.DescriptionString);
                }

                if (Item.CanBeAcquired)
                {
                    ImGui.TextUnformatted("Acquired:" + (PluginService.GameInterface.HasAcquired(Item) ? "Yes" : "No"));
                }

                if (Item.SellToVendorPrice != 0)
                {
                    ImGui.TextUnformatted("Sell to Vendor: " + Item.SellToVendorPrice + SeIconChar.Gil.ToIconString());
                }

                if (Item.BuyFromVendorPrice != 0)
                {
                    ImGui.TextUnformatted("Buy from Vendor: " + Item.BuyFromVendorPrice + SeIconChar.Gil.ToIconString());
                }
                var itemIcon = PluginService.IconStorage[Item.Icon];
                if (itemIcon != null)
                {
                    ImGui.Image(itemIcon.ImGuiHandle, new Vector2(100, 100) * ImGui.GetIO().FontGlobalScale);
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled & ImGuiHoveredFlags.AllowWhenOverlapped & ImGuiHoveredFlags.AllowWhenBlockedByPopup & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem & ImGuiHoveredFlags.AnyWindow) && ImGui.IsMouseReleased(ImGuiMouseButton.Right)) 
                    {
                        ImGui.OpenPopup("RightClick" + _itemId);
                    }
                    
                    if (ImGui.BeginPopup("RightClick" + _itemId))
                    {
                        Item.DrawRightClickPopup();
                        ImGui.EndPopup();
                    }
                }
                
                var garlandIcon = PluginService.IconStorage[65090];
                if (ImGui.ImageButton(garlandIcon.ImGuiHandle,
                        new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                {
                    $"https://www.garlandtools.org/db/#item/{_itemId}".OpenBrowser();
                }
                ImGuiUtil.HoverTooltip("Open in Garland Tools");
                ImGui.SameLine();
                var tcIcon = PluginService.IconStorage[60046];
                if (ImGui.ImageButton(tcIcon.ImGuiHandle,
                        new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                {
                    $"https://ffxivteamcraft.com/db/en/item/{_itemId}".OpenBrowser();
                }
                ImGuiUtil.HoverTooltip("Open in Teamcraft");
                if (Item.CanOpenCraftLog)
                {
                    ImGui.SameLine();
                    var craftableIcon = PluginService.IconStorage[66456];
                    if (ImGui.ImageButton(craftableIcon.ImGuiHandle,
                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                    {
                        PluginService.GameInterface.OpenCraftingLog(_itemId);
                    }

                    ImGuiUtil.HoverTooltip("Craftable - Open in Craft Log");
                }
                if (Item.CanBeCrafted)
                {
                    ImGui.SameLine();
                    var craftableIcon = PluginService.IconStorage[60858];
                    if (ImGui.ImageButton(craftableIcon.ImGuiHandle,
                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                    {
                        ImGui.OpenPopup("AddCraftList" + _itemId);
                    }
                    
                    if (ImGui.BeginPopup("AddCraftList" + _itemId))
                    {
                        var craftFilters =
                            PluginService.FilterService.FiltersList.Where(c =>
                                c.FilterType == Logic.FilterType.CraftFilter && !c.CraftListDefault);
                        foreach (var filter in craftFilters)
                        {
                            if (ImGui.Selectable("Add item to craft list - " + filter.Name))
                            {
                                PluginService.FrameworkService.RunOnFrameworkThread(() =>
                                {
                                    filter.CraftList.AddCraftItem(_itemId, 1, InventoryItem.ItemFlags.None);
                                    filter.NeedsRefresh = true;
                                    filter.StartRefresh();
                                    PluginService.WindowService.OpenCraftsWindow();
                                    PluginService.WindowService.GetCraftsWindow().FocusFilter(filter);
                                });
                            }
                        }
                        ImGui.EndPopup();
                    }

                    ImGuiUtil.HoverTooltip("Craftable - Add to Craft List");
                }
                if (Item.CanOpenGatheringLog)
                {
                    ImGui.SameLine();
                    var gatherableIcon = PluginService.IconStorage[66457];
                    if (ImGui.ImageButton(gatherableIcon.ImGuiHandle,
                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                    {
                        PluginService.GameInterface.OpenGatheringLog(_itemId);
                    }

                    ImGuiUtil.HoverTooltip("Gatherable - Open in Gathering Log");
                    
                    ImGui.SameLine();
                    var gbIcon = PluginService.IconStorage[63900];
                    if (ImGui.ImageButton(gbIcon.ImGuiHandle,
                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                    {
                        PluginService.CommandService.ProcessCommand("/gather " + Item.NameString);
                    }

                    ImGuiUtil.HoverTooltip("Gatherable - Gather with Gatherbuddy");
                }

                if (Item.ObtainedFishing)
                {
                    ImGui.SameLine();
                    var gatherableIcon = PluginService.IconStorage[66457];
                    if (ImGui.ImageButton(gatherableIcon.ImGuiHandle,
                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                    {
                        PluginService.GameInterface.OpenFishingLog(_itemId, Item.IsSpearfishingItem());
                    }

                    ImGuiUtil.HoverTooltip("Gatherable - Open in Fishing Log");
                    
                    ImGui.SameLine();
                    var gbIcon = PluginService.IconStorage[63900];
                    if (ImGui.ImageButton(gbIcon.ImGuiHandle,
                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                    {
                        PluginService.CommandService.ProcessCommand("/gatherfish " + Item.NameString);
                    }

                    ImGuiUtil.HoverTooltip("Gatherable - Gather with Gatherbuddy");
                }

                ImGui.Separator();
                if (ImGui.CollapsingHeader("Sources (" + Item.Sources.Count + ")", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                {
                    ImGuiStylePtr style = ImGui.GetStyle();
                    float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                    var sources = Item.Sources;
                    for (var index = 0; index < sources.Count; index++)
                    {
                        ImGui.PushID("Source"+index);
                        var source = sources[index];
                        var sourceIcon = PluginService.IconStorage[source.Icon];
                        if (sourceIcon != null)
                        {
                            if (source.CanOpen)
                            {
                                if (source is ItemSource itemSource && itemSource.ItemId != null)
                                {
                                    if (ImGui.ImageButton(sourceIcon.ImGuiHandle,
                                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1),
                                            0))
                                    {
                                        PluginService.FrameworkService.RunOnFrameworkThread(() =>
                                        {
                                            PluginService.WindowService.OpenItemWindow(itemSource.ItemId.Value);
                                        });
                                    }
                                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled &
                                                            ImGuiHoveredFlags.AllowWhenOverlapped &
                                                            ImGuiHoveredFlags.AllowWhenBlockedByPopup &
                                                            ImGuiHoveredFlags.AllowWhenBlockedByActiveItem &
                                                            ImGuiHoveredFlags.AnyWindow) &&
                                        ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                                    {
                                        ImGui.OpenPopup("RightClickSource" + itemSource.ItemId);
                                    }
                                    if (ImGui.BeginPopup("RightClickSource" + itemSource.ItemId))
                                    {
                                        ImGui.OpenPopup("RightClickSource" + itemSource.ItemId);
                                        var itemEx = Service.ExcelCache.GetItemExSheet()
                                            .GetRow(itemSource.ItemId.Value);
                                        if (itemEx != null)
                                        {
                                            itemEx.DrawRightClickPopup();
                                        }
                                        ImGui.EndPopup();
                                    }
                                }
                                else if (source is DutySource dutySource)
                                {
                                    if (ImGui.ImageButton(sourceIcon.ImGuiHandle,
                                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1),
                                            0))
                                    {

                                        PluginService.WindowService.OpenDutyWindow(dutySource.ContentFinderConditionId);

                                    }
                                }
                                else if (source is AirshipSource airshipSource)
                                {
                                    if (ImGui.ImageButton(sourceIcon.ImGuiHandle,
                                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1),
                                            0))
                                    {

                                        PluginService.WindowService.OpenAirshipWindow(airshipSource.AirshipExplorationPointExId);

                                    }
                                }
                                else if (source is SubmarineSource submarineSource)
                                {
                                    if (ImGui.ImageButton(sourceIcon.ImGuiHandle,
                                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1),
                                            0))
                                    {

                                        PluginService.WindowService.OpenSubmarineWindow(submarineSource.SubmarineExplorationExId);

                                    }
                                }
                                else
                                {
                                    ImGui.Image(sourceIcon.ImGuiHandle,
                                        new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale);
                                }
                            }
                            else
                            {
                                ImGui.Image(sourceIcon.ImGuiHandle,
                                    new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale);
                            }

                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                            ImGuiUtil.HoverTooltip(source.FormattedName);
                            if (index + 1 < sources.Count && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }
                        }

                        ImGui.PopID();
                    }
                }
                if (ImGui.CollapsingHeader("Uses/Rewards (" + Item.Uses.Count + ")", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                {
                    ImGuiStylePtr style = ImGui.GetStyle();
                    float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                    var uses = Item.Uses;
                    for (var index = 0; index < uses.Count; index++)
                    {
                        ImGui.PushID("Use"+index);
                        var use = uses[index];
                        var useIcon = PluginService.IconStorage[use.Icon];
                        if (useIcon != null)
                        {
                            if (use.CanOpen)
                            {
                                if (use is ItemSource itemSource && itemSource.ItemId != null)
                                {
                                    if (ImGui.ImageButton(useIcon.ImGuiHandle,
                                            new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1),
                                            0))
                                    {
                                        PluginService.WindowService.OpenItemWindow(itemSource.ItemId.Value);
                                    }

                                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled &
                                                            ImGuiHoveredFlags.AllowWhenOverlapped &
                                                            ImGuiHoveredFlags.AllowWhenBlockedByPopup &
                                                            ImGuiHoveredFlags.AllowWhenBlockedByActiveItem &
                                                            ImGuiHoveredFlags.AnyWindow) &&
                                        ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                                    {
                                        ImGui.OpenPopup("RightClickUse" + itemSource.ItemId);
                                    }

                                    if (ImGui.BeginPopup("RightClickUse" + itemSource.ItemId))
                                    {
                                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(itemSource.ItemId.Value);
                                        if (itemEx != null)
                                        {
                                            itemEx.DrawRightClickPopup();
                                        }

                                        ImGui.EndPopup();
                                    }
                                }
                                else
                                {
                                    ImGui.Image(useIcon.ImGuiHandle,
                                        new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale);
                                }
                            }
                            else
                            {
                                ImGui.Image(useIcon.ImGuiHandle,
                                    new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale);
                            }

                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                            ImGuiUtil.HoverTooltip(use.FormattedName);
                            if (index + 1 < uses.Count && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }
                        }

                        ImGui.PopID();
                    }
                }

                if (MobDrops.Length != 0)
                {
                    if (ImGui.CollapsingHeader("Mob Drops (" + MobDrops.Length + ")", ImGuiTreeNodeFlags.CollapsingHeader))
                    {
                        var mobDrops = MobDrops;
                        for (var index = 0; index < mobDrops.Length; index++)
                        {
                            var mobDrop = mobDrops[index];
                            if (mobDrop.BNpcNameEx.Value != null)
                            {
                                var mobSpawns = mobDrops[index].GroupedMobSpawns;
                                if (mobSpawns.Count != 0)
                                {
                                    ImGui.PushID("MobDrop" + index);
                                    if (ImGui.CollapsingHeader("  " +
                                            mobDrop.BNpcNameEx.Value.FormattedName + "(" + mobSpawns.Count + ")",ImGuiTreeNodeFlags.CollapsingHeader))
                                    {
                                        ImGuiTable.DrawTable("MobSpawns" + index, mobSpawns, DrawMobSpawn,
                                            ImGuiTableFlags.None,
                                            new[] { "Map", "Spawn Locations" });
                                    }

                                    ImGui.PopID();
                                }
                            }
                        }
                    }
                }

                void DrawSupplierRow((IShop shop, ENpc? npc, ILocation? location) tuple)
                {
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(tuple.shop.Name);
                    if (tuple.npc != null)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(tuple.npc?.Resident?.Singular ?? "");
                    }
                    if (tuple.location != null)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(tuple.location + " ( " + Math.Round(tuple.location.MapX, 2) + "/" +
                                          Math.Round(tuple.location.MapY, 2) + ")");
                        ImGui.TableNextColumn();
                        if (ImGui.Button("Open Map Link##" + tuple.shop.RowId + "_" + tuple.npc.Key + "_" +
                                         tuple.location.MapEx.Row))
                        {
                            PluginService.ChatUtilities.PrintFullMapLink(tuple.location, Item.NameString);
                        }
                    }
                    else
                    {
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                    }

                }

                bool hasInformation = false;
                if (Vendors.Count != 0)
                {
                    hasInformation = true;
                    if (ImGui.CollapsingHeader("Shops (" + Vendors.Count + ")"))
                    {
                        ImGui.TextUnformatted("Shops: ");
                        ImGuiTable.DrawTable("VendorsText", Vendors, DrawSupplierRow, ImGuiTableFlags.None,
                            new[] { "Shop Name","NPC", "Location", "" });
                    }
                }
                if (RetainerTasks.Length != 0)
                {
                    hasInformation = true;
                    if (ImGui.CollapsingHeader("Ventures (" + RetainerTasks.Count() + ")"))
                    {
                        ImGuiTable.DrawTable("Ventures", RetainerTasks, DrawRetainerRow, ImGuiTableFlags.None,
                            new[] { "Name", "Time", "Quantities" });
                    }
                }
                if (GatheringSources.Count != 0)
                {
                    hasInformation = true;
                    if (ImGui.CollapsingHeader("Gathering (" + GatheringSources.Count + ")"))
                    {
                        ImGuiTable.DrawTable("Gathering", GatheringSources, DrawGatheringRow,
                            ImGuiTableFlags.None, new[] { "", "Level", "Location", "" });
                    }
                }
                if (RecipesAsRequirement.Length != 0)
                {
                    hasInformation = true;
                    if (ImGui.CollapsingHeader("Recipes (" + RecipesAsRequirement.Length + ")"))
                    {
                        ImGuiStylePtr style = ImGui.GetStyle();
                        float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                        for (var index = 0; index < RecipesAsRequirement.Length; index++)
                        {
                            ImGui.PushID(index);
                            var recipe = RecipesAsRequirement[index];
                            if (recipe.ItemResultEx.Value != null)
                            {
                                var icon = PluginService.IconStorage.LoadIcon(recipe.ItemResultEx.Value.Icon);
                                if (ImGui.ImageButton(icon.ImGuiHandle,
                                        new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1), 0))
                                {
                                    PluginService.WindowService.OpenItemWindow(recipe.ItemResultEx.Row);
                                }
                                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled & ImGuiHoveredFlags.AllowWhenOverlapped & ImGuiHoveredFlags.AllowWhenBlockedByPopup & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem & ImGuiHoveredFlags.AnyWindow) && ImGui.IsMouseReleased(ImGuiMouseButton.Right)) 
                                {
                                    ImGui.OpenPopup("RightClick" + recipe.RowId);
                                }
                    
                                if (ImGui.BeginPopup("RightClick"+ recipe.RowId))
                                {
                                    if (recipe.ItemResultEx.Value != null)
                                    {
                                        recipe.ItemResultEx.Value.DrawRightClickPopup();
                                    }

                                    ImGui.EndPopup();
                                }

                                float lastButtonX2 = ImGui.GetItemRectMax().X;
                                float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                                ImGuiUtil.HoverTooltip(recipe.ItemResultEx.Value!.NameString + " - " +
                                                       (recipe.CraftType.Value?.Name ?? "Unknown"));
                                if (index + 1 < RecipesAsRequirement.Length && nextButtonX2 < windowVisibleX2)
                                {
                                    ImGui.SameLine();
                                }
                            }

                            ImGui.PopID();
                        }
                    }
                }

                if (SharedModels.Count != 0)
                {
                    hasInformation = true;
                    if (ImGui.CollapsingHeader("Shared Models (" + SharedModels.Count + ")"))
                    {
                        ImGuiStylePtr style = ImGui.GetStyle();
                        float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                        for (var index = 0; index < SharedModels.Count; index++)
                        {
                            ImGui.PushID(index);
                            var sharedModel = SharedModels[index];
                            var icon = PluginService.IconStorage.LoadIcon(sharedModel.Icon);
                            if (ImGui.ImageButton(icon.ImGuiHandle, new(32, 32)))
                            {
                                PluginService.WindowService.OpenItemWindow(sharedModel.RowId);
                            }
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled & ImGuiHoveredFlags.AllowWhenOverlapped & ImGuiHoveredFlags.AllowWhenBlockedByPopup & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem & ImGuiHoveredFlags.AnyWindow) && ImGui.IsMouseReleased(ImGuiMouseButton.Right)) 
                            {
                                ImGui.OpenPopup("RightClick" + sharedModel.RowId);
                            }
                
                            if (ImGui.BeginPopup("RightClick"+ sharedModel.RowId))
                            {
                                sharedModel.DrawRightClickPopup();
                                ImGui.EndPopup();
                            }

                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                            ImGuiUtil.HoverTooltip(sharedModel.NameString);
                            if (index + 1 < SharedModels.Count && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }

                            ImGui.PopID();
                        }
                    }
                }

                if (Item.IsCompanyCraft)
                {
                    hasInformation = true;
                    if (_craftItem == null)
                    {
                        _craftItem = new CraftItem(Item.RowId, InventoryItem.ItemFlags.None, 1, true);
                        _craftItem.GenerateRequiredMaterials();
                    }
                    if (ImGui.CollapsingHeader("Company Craft Recipe (" + _craftItem.ChildCrafts.Count + ")"))
                    {
                        ImGuiStylePtr style = ImGui.GetStyle();
                        float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                        var index = 0;
                        foreach(var craftItem in _craftItem.ChildCrafts)
                        {
                            var item = Service.ExcelCache.GetItemExSheet().GetRow(craftItem.ItemId);
                            ImGui.PushID(index);
                            var icon = PluginService.IconStorage.LoadIcon(item.Icon);
                            if (ImGui.ImageButton(icon.ImGuiHandle, new(32, 32)))
                            {
                                PluginService.WindowService.OpenItemWindow(item.RowId);
                            }
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled & ImGuiHoveredFlags.AllowWhenOverlapped & ImGuiHoveredFlags.AllowWhenBlockedByPopup & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem & ImGuiHoveredFlags.AnyWindow) && ImGui.IsMouseReleased(ImGuiMouseButton.Right)) 
                            {
                                ImGui.OpenPopup("RightClick" + item.RowId);
                            }
                
                            if (ImGui.BeginPopup("RightClick"+ item.RowId))
                            {
                                item.DrawRightClickPopup();
                                ImGui.EndPopup();
                            }

                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                            ImGuiUtil.HoverTooltip(item.NameString + " - " + craftItem.QuantityRequired);
                            if (index + 1 < _craftItem.ChildCrafts.Count && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }

                            ImGui.PopID();
                            index++;
                        }
                    }
                }
                if (!hasInformation)
                {
                    ImGui.TextUnformatted("No information available.");
                }
                
                #if DEBUG
                if (ImGui.CollapsingHeader("Debug"))
                {
                    ImGui.TextUnformatted("Item ID: " + _itemId);
                    if (ImGui.Button("Copy"))
                    {
                        ImGui.SetClipboardText(_itemId.ToString());
                    }

                    Utils.PrintOutObject(Item, 0, new List<string>());
                }
                #endif

            }
        }

        private void DrawMobSpawn(KeyValuePair<TerritoryType, List<MobSpawnPositionEx>> spawnGroup)
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(spawnGroup.Key.PlaceName.Value?.Name ?? "Unknown");
            
            ImGui.TableNextColumn();

            using (var locationScrollChild = ImRaii.Child(spawnGroup.Key.RowId + "LocationScroll",
                       new Vector2(ImGui.GetColumnWidth() * ImGui.GetIO().FontGlobalScale,
                           32 + ImGui.GetStyle().CellPadding.Y) * ImGui.GetIO().FontGlobalScale, false))
            {
                if (locationScrollChild.Success)
                {
                    var columnWidth = ImGui.GetColumnWidth() - ImGui.GetStyle().ItemSpacing.X;
                    var itemWidth = (32 + ImGui.GetStyle().ItemSpacing.X) * ImGui.GetIO().FontGlobalScale;
                    var maxItems = itemWidth != 0 ? (int)Math.Floor(columnWidth / itemWidth) : 0;
                    maxItems = maxItems == 0 ? 1 : maxItems;
                    maxItems--;
                    var count = 0;
                    foreach (var position in spawnGroup.Value)
                    {
                        var territory = position.TerritoryTypeEx;
                        if (territory.Value?.PlaceName.Value != null)
                        {
                            ImGui.PushID("" + position.FormattedId);
                            if (ImGui.ImageButton(PluginService.IconStorage[60561].ImGuiHandle,
                                    new Vector2(32 * ImGui.GetIO().FontGlobalScale, 32 * ImGui.GetIO().FontGlobalScale),
                                    new Vector2(0, 0), new Vector2(1, 1), 0))
                            {
                                PluginService.ChatUtilities.PrintFullMapLink(
                                    new GenericMapLocation(position.Position.X, position.Position.Y,
                                        territory.Value.MapEx,
                                        territory.Value.PlaceNameEx), position.FormattedPosition);
                            }

                            if (ImGui.IsItemHovered())
                            {
                                using var tt = ImRaii.Tooltip();
                                ImGui.TextUnformatted(position.FormattedPosition);
                            }

                            if ((count + 1) % maxItems != 0)
                            {
                                ImGui.SameLine();
                            }

                            ImGui.PopID();
                        }

                        count++;
                    }
                }
            }
        }

        private void DrawGatheringRow(GatheringSource obj)
        {
            ImGui.TableNextColumn();
            ImGui.PushID(obj.GetHashCode());
            var source = obj.Source;
            var icon = PluginService.IconStorage[source.Icon];
            if (ImGui.ImageButton(icon.ImGuiHandle, new(32, 32)))
            {
                PluginService.GameInterface.OpenGatheringLog(_itemId);
            }
            ImGuiUtil.HoverTooltip(source.Name + " - Open in Gathering Log");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(obj.Level.GatheringItemLevel.ToString());     
            ImGui.TableNextColumn();
            ImGui.TextWrapped(obj.PlaceName.Name + " - " + (obj.TerritoryType.PlaceName.Value?.Name ?? "Unknown"));
            ImGui.PopID();
        }

        private void DrawRecipeResultRow(RecipeEx obj)
        {

        }

        private void DrawRetainerRow(RetainerTaskNormalEx obj)
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted( obj.TaskName);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(obj.TaskTime + " minutes");     
            ImGui.TableNextColumn();
            ImGui.TextWrapped(obj.Quantities);
        }

        public override void Invalidate()
        {
            
        }
        
        public override FilterConfiguration? SelectedConfiguration => null;
        public override Vector2 DefaultSize { get; } = new Vector2(500, 800);
        public override Vector2 MaxSize => new (800, 1500);
        public override Vector2 MinSize => new (100, 100);

        public override bool SavePosition => true;

        public override string GenericKey => "item";
    }
}