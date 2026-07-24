using BaseLib.Diagnostics;
using Godot;

namespace BaseLib.Config;

[ConfigHoverTipsByDefault]
internal class BaseLibConfig : SimpleModConfig
{
	public static bool ShowModConfigInMainMenu { get; set; } = true;

	[ConfigSection("GeneralSettings")]
	[ConfigSlider(1.0, 64.0, 1.0)]
	public static int SfxPlayerLimit { get; set; } = 16;

	[ConfigSection("LogSection")]
	public static bool OpenLogWindowOnStartup { get; set; } = false;

	public static bool OpenLogWindowOnError { get; set; } = false;

	[ConfigSlider(128.0, 2048.0, 64.0)]
	public static int LimitedLogSize { get; set; } = 256;

	[ConfigSlider(8.0, 48.0, 1.0)]
	public static int LogFontSize { get; set; } = 14;

	[ConfigSection("WhatModSection")]
	public static bool IncludeModId { get; set; } = true;

	public static bool ShowCardModSource { get; set; } = false;

	public static bool ShowRelicModSource { get; set; } = true;

	public static bool ShowPotionModSource { get; set; } = true;

	public static bool ShowAncientModSource { get; set; } = true;

	public static bool ShowEventModSource { get; set; } = true;

	public static bool ShowMonsterModSource { get; set; } = true;

	public static bool ShowCombatElementModSource { get; set; } = false;

	[ConfigSection("HarmonyDumpSection")]
	[ConfigTextInput(MaxLength = 1024)]
	public static string HarmonyPatchDumpOutputPath { get; set; } = "";

	public static bool HarmonyPatchDumpOnFirstMainMenu { get; set; }

	[ConfigHideInUI]
	public static int LastLogLevel { get; set; } = 3;

	[ConfigHideInUI]
	public static bool LogUseRegex { get; set; } = false;

	[ConfigHideInUI]
	public static bool LogInvertFilter { get; set; } = false;

	[ConfigHideInUI]
	public static string LogLastFilter { get; set; } = "";

	[ConfigHideInUI]
	public static int LogLastSizeX { get; set; } = 0;

	[ConfigHideInUI]
	public static int LogLastSizeY { get; set; } = 0;

	[ConfigHideInUI]
	public static int LogLastPosX { get; set; } = int.MinValue;

	[ConfigHideInUI]
	public static int LogLastPosY { get; set; } = int.MinValue;

	[ConfigHideInUI]
	public static string LastModConfigModId { get; set; } = "";

	[ConfigButton("HarmonyDumpBrowse")]
	public static void HarmonyDumpBrowseForOutput(ModConfig config)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		MainLoop mainLoop = Engine.GetMainLoop();
		MainLoop obj = ((mainLoop is SceneTree) ? mainLoop : null);
		if (((obj != null) ? ((SceneTree)obj).Root : null) == null)
		{
			BaseLibMain.Logger.Warn("[HarmonyDump] Cannot open file dialog: SceneTree root is not available.", 1);
			return;
		}
		FileDialog dialog = new FileDialog
		{
			Title = ModConfig.GetBaseLibLabelText("HarmonyDumpBrowseTitle"),
			FileMode = (FileModeEnum)4,
			Access = (AccessEnum)2,
			CurrentFile = "baselib_harmony_patch_dump.log"
		};
		if (!string.IsNullOrWhiteSpace(HarmonyPatchDumpOutputPath))
		{
			dialog.CurrentPath = HarmonyPatchDumpOutputPath;
		}
		dialog.AddFilter("*.log", "Log");
		dialog.AddFilter("*.txt", "Text");
		dialog.FileSelected += (FileSelectedEventHandler)delegate(string path)
		{
			HarmonyPatchDumpOutputPath = path;
			config.Save();
			config.ConfigReloaded();
			((Node)dialog).QueueFree();
		};
		NativeFileDialogChrome.Popup(dialog);
	}

	[ConfigButton("HarmonyDumpNow")]
	public static void HarmonyDumpWriteNow(ModConfig _)
	{
		HarmonyPatchDumpCoordinator.TryManualDumpFromSettings();
	}
}
