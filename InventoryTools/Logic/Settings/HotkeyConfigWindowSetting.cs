using Dalamud.Game.ClientState.Keys;
using InventoryTools.Logic.Settings.Abstract;
using OtterGui.Classes;

namespace InventoryTools.Logic.Settings;

public class HotkeyConfigWindowSetting : HotKeySetting
{
    public override ModifiableHotkey DefaultValue { get; set; } = new ModifiableHotkey(VirtualKey.NO_KEY);
    public static string AsKey { get; } = "HotkeyConfigWindow";
    public override string Key { get; set; } = AsKey;
    public override string Name { get; set; } = "Toggle Configuration Window";
    public override string HelpText { get; set; } = "The hotkey to toggle the configuration window.";
    public override SettingCategory SettingCategory { get; set; } = SettingCategory.Hotkeys;
    public override SettingSubCategory SettingSubCategory { get; } = SettingSubCategory.General;
}