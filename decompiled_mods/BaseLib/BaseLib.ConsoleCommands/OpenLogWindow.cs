using System;
using BaseLib.BaseLibScenes;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using Steamworks;

namespace BaseLib.ConsoleCommands;

public class OpenLogWindow : AbstractConsoleCmd
{
	public override string CmdName => "showlog";

	public override string Args => "";

	public override string Description => "Open log display window";

	public override bool IsNetworked => false;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		OpenWindow(stealFocus: true);
		return new CmdResult(true, "Opened log window.");
	}

	public static void OpenWindow(bool stealFocus)
	{
		if (!BaseLibMain.IsMainThread)
		{
			BaseLibMain.Logger.Info("OpenWindow called when not on main thread", 1);
			return;
		}
		if (SteamUtils.IsSteamRunningOnSteamDeck())
		{
			BaseLibMain.Logger.Info("OpenWindow cancelled; log window disabled on steam deck", 1);
			return;
		}
		NGame instance = NGame.Instance;
		if (instance == null)
		{
			return;
		}
		try
		{
			Window window = ((Node)instance).GetWindow();
			((Viewport)window).GuiEmbedSubwindows = false;
			NLogWindow nLogWindow = ResourceLoader.Load<PackedScene>("res://BaseLib/scenes/LogWindow.tscn", (string)null, (CacheMode)1).Instantiate<NLogWindow>((GenEditState)0);
			((Window)nLogWindow).Visible = false;
			GodotTreeExtensions.AddChildSafely((Node)(object)window, (Node)(object)nLogWindow);
			LogWindowPlacement.SetupPosition(nLogWindow, window);
			((Window)nLogWindow).Visible = true;
			if (!stealFocus)
			{
				window.GrabFocus();
			}
		}
		catch (Exception value)
		{
			BaseLibMain.Logger.Info($"Failed to open log window: {value}", 1);
		}
	}
}
