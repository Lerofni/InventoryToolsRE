using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib;
using Dalamud.Utility;
using ImGuiNET;
using InventoryTools.Extensions;
using InventoryTools.Logic;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Model;
using OtterGui;

namespace InventoryTools.Ui
{
    class DutyWindow : Window
    {
        public override bool SaveState => false;
        public static string AsKey(uint contentFinderConditionId)
        {
            return "cfcid_" + contentFinderConditionId;
        }
        private uint _contentFinderConditionId;
        private ContentFinderCondition? ContentFinderCondition => Service.ExcelCache.GetContentFinderConditionExSheet().GetRow(_contentFinderConditionId);

        private HashSet<uint> DungeonChestItems { get; }
        private HashSet<uint> DungeonRewards { get; }
        private List<DungeonBoss> DungeonBosses { get; }
        private Dictionary<uint, List<DungeonBossDrop>> DungeonBossDrops { get; }
        
        private Dictionary<uint, List<DungeonBossChest>> DungeonBossChests { get; }
        public DutyWindow(uint contentFinderConditionId, string name = "Allagan Tools - Invalid Duty") : base(name)
        {
            _contentFinderConditionId = contentFinderConditionId;
            if (ContentFinderCondition != null)
            {
                WindowName = "Allagan Tools - " + ContentFinderCondition.Name.ToDalamudString();
                DungeonChestItems = new HashSet<uint>();
                DungeonRewards = new HashSet<uint>();
                foreach (var dungeonChest in Service.ExcelCache.DungeonChests)
                {
                    if (dungeonChest.ContentFinderConditionId == _contentFinderConditionId)
                    {
                        var items = Service.ExcelCache.DungeonChestItems.Where(c => c.ChestId == dungeonChest.RowId);
                        foreach (var item in items)
                        {
                            DungeonChestItems.Add(item.ItemId);
                        }
                    }
                }
                foreach (var dungeonDrop in Service.ExcelCache.DungeonDrops)
                {
                    if (dungeonDrop.ContentFinderConditionId == _contentFinderConditionId)
                    {
                        DungeonRewards.Add(dungeonDrop.ItemId);
                    }
                }

                DungeonBossDrops = Service.ExcelCache.DungeonBossDrops.Where(c => c.ContentFinderConditionId == _contentFinderConditionId).GroupBy(c => c.FightNo).ToDictionary(c => c.Key, c => c.ToList());
                DungeonBosses = Service.ExcelCache.DungeonBosses.Where(c => c.ContentFinderConditionId == _contentFinderConditionId).ToList();
                DungeonBossChests =  Service.ExcelCache.DungeonBossChests.Where(c => c.ContentFinderConditionId == _contentFinderConditionId).GroupBy(c => c.FightNo).ToDictionary(c => c.Key, c => c.ToList());
            }
            else
            {
                DungeonChestItems = new HashSet<uint>();
            }
        }
        public override string Key => AsKey(_contentFinderConditionId);
        public override bool DestroyOnClose => true;
        public override void Draw()
        {
            if (ContentFinderCondition == null)
            {
                ImGui.TextUnformatted("Dungeon with the ID " + _contentFinderConditionId + " could not be found.");   
            }
            else
            {
                ImGui.TextUnformatted(ContentFinderCondition.Name.ToDalamudString().ToString());
                ImGui.TextUnformatted(ContentFinderCondition.ContentType?.Value?.Name.ToString() ?? "Unknown Content Type");
                ImGui.TextUnformatted("Level Required: " + ContentFinderCondition.ClassJobLevelRequired);
                ImGui.TextUnformatted("Item Level Required: " + ContentFinderCondition.ItemLevelRequired);
                ;
                var itemIcon = PluginService.IconStorage[(int)(ContentFinderCondition.ContentType?.Value?.IconDutyFinder ?? 61801)];
                ImGui.Image(itemIcon.ImGuiHandle, new Vector2(100, 100) * ImGui.GetIO().FontGlobalScale);
                
                var garlandIcon = PluginService.IconStorage[65090];
                if (ImGui.ImageButton(garlandIcon.ImGuiHandle,
                        new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale))
                {
                    $"https://www.garlandtools.org/db/#instance/{ContentFinderCondition.Content}".OpenBrowser();
                }
                foreach (var dungeonBoss in DungeonBosses)
                {
                    if (ImGui.CollapsingHeader(
                            Service.ExcelCache.GetBNpcNameExSheet().GetRow(dungeonBoss.BNpcNameId)?.FormattedName + " - Fight " + (dungeonBoss.FightNo + 1) ??
                            "Unknown Boss",
                            ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                    {
                        if (DungeonBossChests.ContainsKey(dungeonBoss.FightNo))
                        {
                            var chests = DungeonBossChests[dungeonBoss.FightNo];
                            foreach (var chest in chests.GroupBy(c => c.CofferNo))
                            {
                                if (ImGui.CollapsingHeader("Coffer " + (chest.Key + 1),
                                        ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    ImGuiStylePtr style = ImGui.GetStyle();
                                    float windowVisibleX2 =
                                        ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                                    var items = chest.Select(c =>
                                        (Service.ExcelCache.GetItemExSheet().GetRow(c.ItemId), c.Quantity)).ToList();

                                    for (var index = 0; index < items.Count; index++)
                                    {
                                        var item = items[index];
                                        ImGui.PushID("dbc" + dungeonBoss.RowId + "_" + chest.Key + "_" + index);
                                        var useIcon = PluginService.IconStorage[item.Item1.Icon];
                                        if (useIcon != null)
                                        {
                                            if (ImGui.ImageButton(useIcon.ImGuiHandle,
                                                    new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0),
                                                    new(1, 1), 0))
                                            {
                                                PluginService.WindowService.OpenItemWindow(item.Item1.RowId);
                                            }

                                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled &
                                                                    ImGuiHoveredFlags.AllowWhenOverlapped &
                                                                    ImGuiHoveredFlags.AllowWhenBlockedByPopup &
                                                                    ImGuiHoveredFlags.AllowWhenBlockedByActiveItem &
                                                                    ImGuiHoveredFlags.AnyWindow) &&
                                                ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                                            {
                                                ImGui.OpenPopup("RightClickUse" + item.Item1.RowId);
                                            }

                                            if (ImGui.BeginPopup("RightClickUse" + item.Item1.RowId))
                                            {
                                                var itemEx = Service.ExcelCache.GetItemExSheet()
                                                    .GetRow(item.Item1.RowId);
                                                if (itemEx != null)
                                                {
                                                    itemEx.DrawRightClickPopup();
                                                }

                                                ImGui.EndPopup();
                                            }

                                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                                            ImGuiUtil.HoverTooltip(item.Item1.NameString);
                                            if (index + 1 < items.Count && nextButtonX2 < windowVisibleX2)
                                            {
                                                ImGui.SameLine();
                                            }
                                        }
                                        ImGui.PopID();
                                    }
                                }
                            }
                        }
                        if (DungeonBossDrops.ContainsKey(dungeonBoss.FightNo))
                        {
                            var drops = DungeonBossDrops[dungeonBoss.FightNo].Select(c => Service.ExcelCache.GetItemExSheet().GetRow(c.ItemId)).Where(c => c != null).Select(c => c!).ToList();
                            if (ImGui.CollapsingHeader("Drops", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                            {
                                ImGuiStylePtr style = ImGui.GetStyle();
                                float windowVisibleX2 =
                                    ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                                for (var index = 0; index < drops.Count; index++)
                                {
                                    var item = drops[index];
                                    var useIcon = PluginService.IconStorage[item.Icon];
                                    if (useIcon != null)
                                    {
                                        ImGui.PushID("dbd" + dungeonBoss.RowId + "_" + index);
                                        if (ImGui.ImageButton(useIcon.ImGuiHandle,
                                                new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0),
                                                new(1, 1), 0))
                                        {
                                            PluginService.WindowService.OpenItemWindow(item.RowId);
                                        }

                                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled &
                                                                ImGuiHoveredFlags.AllowWhenOverlapped &
                                                                ImGuiHoveredFlags.AllowWhenBlockedByPopup &
                                                                ImGuiHoveredFlags.AllowWhenBlockedByActiveItem &
                                                                ImGuiHoveredFlags.AnyWindow) &&
                                            ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                                        {
                                            ImGui.OpenPopup("RightClickUse" + item.RowId);
                                        }

                                        if (ImGui.BeginPopup("RightClickUse" + item.RowId))
                                        {
                                            var itemEx = Service.ExcelCache.GetItemExSheet()
                                                .GetRow(item.RowId);
                                            if (itemEx != null)
                                            {
                                                itemEx.DrawRightClickPopup();
                                            }

                                            ImGui.EndPopup();
                                        }

                                        float lastButtonX2 = ImGui.GetItemRectMax().X;
                                        float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                                        ImGuiUtil.HoverTooltip(item.NameString);
                                        if (index + 1 < drops.Count && nextButtonX2 < windowVisibleX2)
                                        {
                                            ImGui.SameLine();
                                        }
                                        ImGui.PopID();
                                    }
                                }
                            }
                        }                        
                    }
                }

                if (ImGui.CollapsingHeader("Other Chests (" + DungeonChestItems.Count + ")", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                {
                    ImGuiStylePtr style = ImGui.GetStyle();
                    float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                    var uses = DungeonChestItems.Select(c => Service.ExcelCache.GetItemExSheet().GetRow(c)).Where(c => c != null).Select(c => c!).ToList();
                    for (var index = 0; index < uses.Count; index++)
                    {
                        ImGui.PushID("Use"+index);
                        var use = uses[index];
                        var useIcon = PluginService.IconStorage[use.Icon];
                        if (useIcon != null)
                        {
                            if (ImGui.ImageButton(useIcon.ImGuiHandle,
                                    new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1), 0))
                            {
                                PluginService.WindowService.OpenItemWindow(use.RowId);
                            }
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled & ImGuiHoveredFlags.AllowWhenOverlapped & ImGuiHoveredFlags.AllowWhenBlockedByPopup & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem & ImGuiHoveredFlags.AnyWindow) && ImGui.IsMouseReleased(ImGuiMouseButton.Right)) 
                            {
                                ImGui.OpenPopup("RightClickUse" + use.RowId);
                            }
                
                            if (ImGui.BeginPopup("RightClickUse"+ use.RowId))
                            {
                                var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(use.RowId);
                                if (itemEx != null)
                                {
                                    itemEx.DrawRightClickPopup();
                                }

                                ImGui.EndPopup();
                            }

                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                            ImGuiUtil.HoverTooltip(use.NameString);
                            if (index + 1 < uses.Count && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }
                        }

                        ImGui.PopID();
                    }
                }
                
                if (ImGui.CollapsingHeader("Rewards (" + DungeonRewards.Count + ")", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.CollapsingHeader))
                {
                    ImGuiStylePtr style = ImGui.GetStyle();
                    float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                    var uses = DungeonRewards.Select(c => Service.ExcelCache.GetItemExSheet().GetRow(c)).Where(c => c != null).Select(c => c!).ToList();
                    for (var index = 0; index < uses.Count; index++)
                    {
                        ImGui.PushID("Use"+index);
                        var use = uses[index];
                        
                        var useIcon = PluginService.IconStorage[use.Icon];
                        if (useIcon != null)
                        {
                            if (ImGui.ImageButton(useIcon.ImGuiHandle,
                                    new Vector2(32, 32) * ImGui.GetIO().FontGlobalScale, new(0, 0), new(1, 1), 0))
                            {
                                PluginService.WindowService.OpenItemWindow(use.RowId);
                            }
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled & ImGuiHoveredFlags.AllowWhenOverlapped & ImGuiHoveredFlags.AllowWhenBlockedByPopup & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem & ImGuiHoveredFlags.AnyWindow) && ImGui.IsMouseReleased(ImGuiMouseButton.Right)) 
                            {
                                ImGui.OpenPopup("RightClickUse" + use.RowId);
                            }
                
                            if (ImGui.BeginPopup("RightClickUse"+ use.RowId))
                            {
                                var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(use.RowId);
                                if (itemEx != null)
                                {
                                    itemEx.DrawRightClickPopup();
                                }

                                ImGui.EndPopup();
                            }

                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + style.ItemSpacing.X + 32;
                            ImGuiUtil.HoverTooltip(use.NameString);
                            if (index + 1 < uses.Count && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }
                        }

                        ImGui.PopID();
                    }
                }
                
                #if DEBUG
                if (ImGui.CollapsingHeader("Debug"))
                {
                    ImGui.TextUnformatted("Duty ID: " + _contentFinderConditionId);
                    Utils.PrintOutObject(ContentFinderCondition, 0, new List<string>());
                }
                #endif

            }
        }

        public override void Invalidate()
        {
            
        }
        public override FilterConfiguration? SelectedConfiguration => null;
        public override Vector2 DefaultSize { get; } = new Vector2(500, 800);
        public override Vector2 MaxSize => new (800, 1500);
        public override Vector2 MinSize => new (100, 100);
    }
}