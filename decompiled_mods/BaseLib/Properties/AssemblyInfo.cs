using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using BaseLib.BaseLibScenes;
using BaseLib.BaseLibScenes.Acts;
using BaseLib.Config.UI;
using Godot;

[assembly: IgnoresAccessChecksTo("sts2")]
[assembly: AssemblyCompany("Alchyr")]
[assembly: AssemblyConfiguration("ExportRelease")]
[assembly: AssemblyDescription("Mod for Slay the Spire 2 providing utilities and features for other mods.")]
[assembly: AssemblyFileVersion("3.3.8.0")]
[assembly: AssemblyInformationalVersion("3.3.8+8dfbd9367f458fbc076d341708cd93a3e336b905")]
[assembly: AssemblyProduct("BaseLib")]
[assembly: AssemblyTitle("BaseLib")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/Alchyr/BaseLib-StS2")]
[assembly: AssemblyHasScripts(new Type[]
{
	typeof(NCustomTreasureRoomChest),
	typeof(NDynamicCombatBackground),
	typeof(NHorizontalScrollContainer),
	typeof(NLogWindow),
	typeof(NConfigButton),
	typeof(NConfigCollapsibleSection),
	typeof(NConfigColorPicker),
	typeof(NConfigDropdown),
	typeof(NConfigDropdownItem),
	typeof(NConfigLineEdit),
	typeof(NConfigOptionRow),
	typeof(NConfigSlider),
	typeof(NConfigTickbox),
	typeof(NModConfigSubmenu),
	typeof(NModListButton),
	typeof(NNativeScrollableContainer)
})]
[assembly: AssemblyVersion("3.3.8.0")]
[module: RefSafetyRules(11)]
