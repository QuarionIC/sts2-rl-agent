using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
internal static class CustomCharacterSelectEntryPatch
{
	private sealed class CustomCharacterSelectScreenState
	{
		public bool Initialized { get; set; }

		public List<NCustomCharacterSelectEntryButton> Buttons { get; } = new List<NCustomCharacterSelectEntryButton>();

		public NCustomCharacterSelectEntryButton? ActiveButton { get; set; }

		public Control? ActiveScene { get; set; }

		public Control? ActiveForegroundScene { get; set; }

		public Control? ForegroundContainer { get; set; }

		public CustomCharacterSelectContext? Context { get; set; }
	}

	private static readonly SpireField<NCharacterSelectScreen, CustomCharacterSelectScreenState> ScreenStates = new SpireField<NCharacterSelectScreen, CustomCharacterSelectScreenState>(() => new CustomCharacterSelectScreenState());

	[HarmonyPatch(typeof(NCharacterSelectScreen), "InitCharacterButtons")]
	[HarmonyPostfix]
	private static void AddCustomEntryButtons(NCharacterSelectScreen __instance)
	{
		if (CustomCharacterSelectEntryRegistry.Entries.Count == 0)
		{
			return;
		}
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(__instance);
		if (customCharacterSelectScreenState.Initialized)
		{
			return;
		}
		customCharacterSelectScreenState.Initialized = true;
		NCharacterSelectButton randomCharacterButton = __instance._randomCharacterButton;
		if ((object)((randomCharacterButton != null) ? ((Node)randomCharacterButton).GetParent() : null) == __instance._charButtonContainer)
		{
			GodotTreeExtensions.RemoveChildSafely((Node)(object)__instance._charButtonContainer, (Node)(object)randomCharacterButton);
		}
		foreach (CustomCharacterSelectEntry entry in CustomCharacterSelectEntryRegistry.Entries)
		{
			if (entry.VisibleInCharacterSelect)
			{
				NCustomCharacterSelectEntryButton nCustomCharacterSelectEntryButton = new NCustomCharacterSelectEntryButton(entry, __instance, delegate(NCustomCharacterSelectEntryButton selected)
				{
					SelectCustomEntry(__instance, selected);
				});
				GodotTreeExtensions.AddChildSafely((Node)(object)__instance._charButtonContainer, (Node)(object)nCustomCharacterSelectEntryButton.Button);
				customCharacterSelectScreenState.Buttons.Add(nCustomCharacterSelectEntryButton);
			}
		}
		if (randomCharacterButton != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)__instance._charButtonContainer, (Node)(object)randomCharacterButton);
		}
		EnsureForegroundContainer(__instance, customCharacterSelectScreenState);
		RebuildFocusNeighbors(__instance);
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), "OnSubmenuOpened")]
	[HarmonyPostfix]
	private static void OnSubmenuOpenedPostfix(NCharacterSelectScreen __instance)
	{
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(__instance);
		if (customCharacterSelectScreenState == null)
		{
			return;
		}
		foreach (NCustomCharacterSelectEntryButton button in customCharacterSelectScreenState.Buttons)
		{
			button.Enable();
			button.Deselect();
		}
		ClearActiveEntry(__instance, clearScene: true);
		((CanvasItem)__instance._infoPanel).Visible = true;
		RebuildFocusNeighbors(__instance);
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), "OnSubmenuClosed")]
	[HarmonyPrefix]
	private static void OnSubmenuClosedPrefix(NCharacterSelectScreen __instance)
	{
		ClearActiveEntry(__instance, clearScene: true);
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
	[HarmonyPostfix]
	private static void SelectCharacterPostfix(NCharacterSelectScreen __instance)
	{
		ClearActiveEntry(__instance, clearScene: false);
		((CanvasItem)__instance._infoPanel).Visible = true;
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), "OnEmbarkPressed")]
	[HarmonyPrefix]
	private static bool OnEmbarkPressedPrefix(NCharacterSelectScreen __instance)
	{
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(__instance);
		if (customCharacterSelectScreenState?.ActiveButton == null)
		{
			return true;
		}
		CharacterModel val = customCharacterSelectScreenState.Context?.SelectedCharacter;
		if (val != null && !IsCharacterLocked(val))
		{
			return true;
		}
		((NClickableControl)__instance._embarkButton).Disable();
		return false;
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), "OnEmbarkPressed")]
	[HarmonyPostfix]
	private static void OnEmbarkPressedPostfix(NCharacterSelectScreen __instance)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(__instance);
		if (customCharacterSelectScreenState == null || !__instance.Lobby.LocalPlayer.isReady)
		{
			return;
		}
		foreach (NCustomCharacterSelectEntryButton button in customCharacterSelectScreenState.Buttons)
		{
			button.Disable();
		}
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), "OnUnreadyPressed")]
	[HarmonyPostfix]
	private static void OnUnreadyPressedPostfix(NCharacterSelectScreen __instance)
	{
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(__instance);
		if (customCharacterSelectScreenState == null)
		{
			return;
		}
		foreach (NCustomCharacterSelectEntryButton button in customCharacterSelectScreenState.Buttons)
		{
			button.Enable();
		}
		customCharacterSelectScreenState.ActiveButton?.TryGrabFocus();
		RefreshEmbarkAvailability(__instance);
	}

	private static void SelectCustomEntry(NCharacterSelectScreen screen, NCustomCharacterSelectEntryButton button)
	{
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(screen);
		HashSet<NCharacterSelectButton> hashSet = customCharacterSelectScreenState.Buttons.Select((NCustomCharacterSelectEntryButton customButton) => customButton.Button).ToHashSet();
		foreach (NCharacterSelectButton item in ((IEnumerable)((Node)screen._charButtonContainer).GetChildren(false)).OfType<NCharacterSelectButton>())
		{
			if (!hashSet.Contains(item))
			{
				item.Deselect();
			}
		}
		foreach (NCustomCharacterSelectEntryButton button2 in customCharacterSelectScreenState.Buttons)
		{
			if (button2 != button)
			{
				button2.Deselect();
			}
		}
		ClearBackground(screen);
		ClearActiveEntry(screen, clearScene: true);
		if (button.IsLocked)
		{
			customCharacterSelectScreenState.ActiveButton = button;
			ApplyLockedEntryPanel(screen, button);
			return;
		}
		Control val;
		try
		{
			val = button.Entry.CreateCharacterSelectScene();
		}
		catch (Exception value)
		{
			BaseLibMain.Logger.Error($"Failed to create custom character select scene for {button.Entry.EntryId}: {value}", 1);
			button.Deselect();
			return;
		}
		Control val2 = null;
		try
		{
			val2 = button.Entry.CreateCharacterSelectForegroundScene();
		}
		catch (Exception value2)
		{
			BaseLibMain.Logger.Error($"Failed to create custom character select foreground scene for {button.Entry.EntryId}: {value2}", 1);
		}
		((Node)val).Name = StringName.op_Implicit(button.Entry.EntryId + "_entry_bg");
		GodotTreeExtensions.AddChildSafely((Node)(object)screen._bgContainer, (Node)(object)val);
		if (val2 != null)
		{
			((Node)val2).Name = StringName.op_Implicit(button.Entry.EntryId + "_entry_fg");
			GodotTreeExtensions.AddChildSafely((Node)(object)EnsureForegroundContainer(screen, customCharacterSelectScreenState), (Node)(object)val2);
		}
		CustomCharacterSelectContext context = null;
		context = new CustomCharacterSelectContext(button.Entry, screen, val, val2, delegate(CharacterModel? character)
		{
			OnResolvedCharacterChanged(screen, context, character);
		});
		customCharacterSelectScreenState.ActiveButton = button;
		customCharacterSelectScreenState.ActiveScene = val;
		customCharacterSelectScreenState.ActiveForegroundScene = val2;
		customCharacterSelectScreenState.Context = context;
		ApplyEntryPanel(screen, button.Entry);
		try
		{
			button.Entry.RegisterScene(val, context);
		}
		catch (Exception value3)
		{
			BaseLibMain.Logger.Error($"Failed to register custom character select scene for {button.Entry.EntryId}: {value3}", 1);
		}
		if (val2 != null)
		{
			try
			{
				button.Entry.RegisterForegroundScene(val2, context);
			}
			catch (Exception value4)
			{
				BaseLibMain.Logger.Error($"Failed to register custom character select foreground scene for {button.Entry.EntryId}: {value4}", 1);
			}
		}
		if (context.SelectedCharacter == null)
		{
			CharacterModel initialCharacter = button.Entry.InitialCharacter;
			if (initialCharacter != null)
			{
				context.SetCharacter(initialCharacter);
				return;
			}
		}
		RefreshEmbarkAvailability(screen);
	}

	private static void OnResolvedCharacterChanged(NCharacterSelectScreen screen, CustomCharacterSelectContext context, CharacterModel? character)
	{
		if (ScreenStates.Get(screen)?.Context == context)
		{
			if (character == null)
			{
				ApplyEntryPanel(screen, context.Entry);
				RefreshEmbarkAvailability(screen);
			}
			else
			{
				ApplyCharacterPanel(screen, character, context.Entry);
			}
		}
	}

	private static void RefreshEmbarkAvailability(NCharacterSelectScreen screen)
	{
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(screen);
		if (customCharacterSelectScreenState?.ActiveButton != null)
		{
			if (customCharacterSelectScreenState.Context?.SelectedCharacter == null)
			{
				((NClickableControl)screen._embarkButton).Disable();
			}
			else if (IsCharacterLocked(customCharacterSelectScreenState.Context.SelectedCharacter))
			{
				((NClickableControl)screen._embarkButton).Disable();
			}
			else
			{
				((NClickableControl)screen._embarkButton).Enable();
			}
		}
	}

	private static void ClearActiveEntry(NCharacterSelectScreen screen, bool clearScene)
	{
		CustomCharacterSelectScreenState customCharacterSelectScreenState = ScreenStates.Get(screen);
		if (customCharacterSelectScreenState == null)
		{
			return;
		}
		customCharacterSelectScreenState.ActiveButton?.Deselect();
		if (clearScene && customCharacterSelectScreenState.ActiveScene != null && GodotObject.IsInstanceValid((GodotObject)(object)customCharacterSelectScreenState.ActiveScene))
		{
			if (((Node)customCharacterSelectScreenState.ActiveScene).GetParent() != null)
			{
				GodotTreeExtensions.RemoveChildSafely(((Node)customCharacterSelectScreenState.ActiveScene).GetParent(), (Node)(object)customCharacterSelectScreenState.ActiveScene);
			}
			GodotTreeExtensions.QueueFreeSafely((Node)(object)customCharacterSelectScreenState.ActiveScene);
		}
		if (clearScene && customCharacterSelectScreenState.ActiveForegroundScene != null && GodotObject.IsInstanceValid((GodotObject)(object)customCharacterSelectScreenState.ActiveForegroundScene))
		{
			if (((Node)customCharacterSelectScreenState.ActiveForegroundScene).GetParent() != null)
			{
				GodotTreeExtensions.RemoveChildSafely(((Node)customCharacterSelectScreenState.ActiveForegroundScene).GetParent(), (Node)(object)customCharacterSelectScreenState.ActiveForegroundScene);
			}
			GodotTreeExtensions.QueueFreeSafely((Node)(object)customCharacterSelectScreenState.ActiveForegroundScene);
		}
		customCharacterSelectScreenState.ActiveButton = null;
		customCharacterSelectScreenState.ActiveScene = null;
		customCharacterSelectScreenState.ActiveForegroundScene = null;
		customCharacterSelectScreenState.Context = null;
	}

	private static void ClearBackground(NCharacterSelectScreen screen)
	{
		foreach (Node child in ((Node)screen._bgContainer).GetChildren(false))
		{
			GodotTreeExtensions.RemoveChildSafely((Node)(object)screen._bgContainer, child);
			GodotTreeExtensions.QueueFreeSafely(child);
		}
	}

	private static Control EnsureForegroundContainer(NCharacterSelectScreen screen, CustomCharacterSelectScreenState state)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		if (state.ForegroundContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)state.ForegroundContainer))
		{
			((Node)screen).MoveChild((Node)(object)state.ForegroundContainer, ((Node)screen).GetChildCount(false) - 1);
			return state.ForegroundContainer;
		}
		Control val = new Control
		{
			Name = StringName.op_Implicit("BaseLibCharacterSelectForeground"),
			LayoutMode = 1,
			MouseFilter = (MouseFilterEnum)2
		};
		val.SetAnchorsPreset((LayoutPreset)15, false);
		GodotTreeExtensions.AddChildSafely((Node)(object)screen, (Node)(object)val);
		((Node)screen).MoveChild((Node)(object)val, ((Node)screen).GetChildCount(false) - 1);
		state.ForegroundContainer = val;
		return val;
	}

	private static void RebuildFocusNeighbors(NCharacterSelectScreen screen)
	{
		List<Control> list = (from c in ((IEnumerable)((Node)screen._charButtonContainer).GetChildren(false)).OfType<Control>()
			where ((CanvasItem)c).Visible
			select c).ToList();
		if (list.Count != 0)
		{
			for (int num = 0; num < list.Count; num++)
			{
				Control obj = list[num];
				obj.FocusNeighborTop = ((Node)obj).GetPath();
				obj.FocusNeighborBottom = ((Node)obj).GetPath();
				obj.FocusNeighborLeft = ((Node)list[(num - 1 + list.Count) % list.Count]).GetPath();
				obj.FocusNeighborRight = ((Node)list[(num + 1) % list.Count]).GetPath();
			}
		}
	}

	private static void AnimateInfoPanel(NCharacterSelectScreen screen)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if (screen._infoPanelTween != null)
		{
			screen._infoPanel.Position = screen._infoPanelPosFinalVal;
		}
		screen._infoPanelPosFinalVal = screen._infoPanel.Position;
		Tween infoPanelTween = screen._infoPanelTween;
		if (infoPanelTween != null)
		{
			infoPanelTween.Kill();
		}
		screen._infoPanelTween = ((Node)screen).CreateTween().SetParallel(true);
		screen._infoPanelTween.TweenProperty((GodotObject)(object)screen._infoPanel, NodePath.op_Implicit("position"), Variant.op_Implicit(screen._infoPanel.Position), 0.5).SetEase((EaseType)1).SetTrans((TransitionType)5)
			.From(Variant.op_Implicit(screen._infoPanel.Position - new Vector2(300f, 0f)));
	}

	private static void ApplyEntryPanel(NCharacterSelectScreen screen, CustomCharacterSelectEntry entry)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		screen._selectedButton = null;
		((NClickableControl)screen._embarkButton).Disable();
		screen._name.SetTextAutoSize(entry.EntryTitle);
		screen._description.Text = entry.EntryDescription;
		screen._hp.SetTextAutoSize("??/??");
		screen._gold.SetTextAutoSize("???");
		((CanvasItem)screen._relicIcon).SelfModulate = StsColors.transparentBlack;
		((CanvasItem)screen._relicIconOutline).SelfModulate = StsColors.transparentBlack;
		screen._relicTitle.Text = string.Empty;
		screen._relicDescription.Text = string.Empty;
		((CanvasItem)screen._ascensionPanel).Visible = false;
		ApplyInfoPanelVisibility(screen, entry.ShowVanillaInfoPanelWhenUnresolved);
	}

	private static void ApplyLockedEntryPanel(NCharacterSelectScreen screen, NCustomCharacterSelectEntryButton button)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (button.LockSourceCharacter != null)
		{
			ApplyLockedCharacterPanel(screen, button.LockSourceCharacter, button.Entry.ShowVanillaInfoPanelWhenUnresolved);
			return;
		}
		screen._selectedButton = null;
		((NClickableControl)screen._embarkButton).Disable();
		screen._name.SetTextAutoSize(button.Entry.LockedTitle);
		screen._description.Text = button.Entry.LockedDescription;
		screen._hp.SetTextAutoSize("??/??");
		screen._gold.SetTextAutoSize("???");
		((CanvasItem)screen._relicIcon).SelfModulate = StsColors.transparentBlack;
		((CanvasItem)screen._relicIconOutline).SelfModulate = StsColors.transparentBlack;
		screen._relicTitle.Text = string.Empty;
		screen._relicDescription.Text = string.Empty;
		((CanvasItem)screen._ascensionPanel).Visible = false;
		ApplyInfoPanelVisibility(screen, button.Entry.ShowVanillaInfoPanelWhenUnresolved);
	}

	private static void ApplyCharacterPanel(NCharacterSelectScreen screen, CharacterModel character, CustomCharacterSelectEntry entry)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		screen._selectedButton = null;
		if (IsCharacterLocked(character))
		{
			ApplyLockedCharacterPanel(screen, character, entry.ShowVanillaInfoPanelWhenResolved);
			return;
		}
		string formattedText = new LocString("characters", character.CharacterSelectTitle).GetFormattedText();
		screen._name.SetTextAutoSize(formattedText);
		screen._description.Text = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
		if (!(character is RandomCharacter))
		{
			screen._hp.SetTextAutoSize($"{character.StartingHp}/{character.StartingHp}");
			screen._gold.SetTextAutoSize($"{character.StartingGold}");
			RelicModel val = character.StartingRelics[0];
			screen._relicTitle.Text = val.Title.GetFormattedText();
			screen._relicDescription.Text = val.DynamicDescription.GetFormattedText();
			screen._relicIcon.Texture = val.Icon;
			screen._relicIconOutline.Texture = val.IconOutline;
			((CanvasItem)screen._relicIcon).SelfModulate = Colors.White;
			((CanvasItem)screen._relicIconOutline).SelfModulate = StsColors.halfTransparentBlack;
		}
		else
		{
			screen._hp.SetTextAutoSize("??/??");
			screen._gold.SetTextAutoSize("???");
			((CanvasItem)screen._relicIcon).SelfModulate = StsColors.transparentBlack;
			((CanvasItem)screen._relicIconOutline).SelfModulate = StsColors.transparentBlack;
			screen._relicTitle.Text = string.Empty;
			screen._relicDescription.Text = string.Empty;
		}
		((NClickableControl)screen._embarkButton).Enable();
		screen._lobby.SetLocalCharacter(character);
		if (!NetGameTypeExtensions.IsMultiplayer(screen._lobby.NetService.Type))
		{
			screen._ascensionPanel.AnimIn();
		}
		ApplyInfoPanelVisibility(screen, entry.ShowVanillaInfoPanelWhenResolved);
	}

	private static void ApplyLockedCharacterPanel(NCharacterSelectScreen screen, CharacterModel character, bool showInfoPanel)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		((NClickableControl)screen._embarkButton).Disable();
		screen._name.SetTextAutoSize(new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());
		screen._description.Text = character.GetUnlockText().GetFormattedText();
		screen._hp.SetTextAutoSize("??/??");
		screen._gold.SetTextAutoSize("???");
		if (!(character is RandomCharacter))
		{
			RelicModel val = character.StartingRelics[0];
			screen._relicTitle.Text = new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.title").GetFormattedText();
			screen._relicDescription.Text = new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.description").GetFormattedText();
			screen._relicIcon.Texture = val.Icon;
			screen._relicIconOutline.Texture = val.IconOutline;
			((CanvasItem)screen._relicIcon).SelfModulate = StsColors.ninetyPercentBlack;
			((CanvasItem)screen._relicIconOutline).SelfModulate = StsColors.halfTransparentWhite;
		}
		else
		{
			((CanvasItem)screen._relicIcon).SelfModulate = StsColors.transparentBlack;
			((CanvasItem)screen._relicIconOutline).SelfModulate = StsColors.transparentBlack;
			screen._relicTitle.Text = string.Empty;
			screen._relicDescription.Text = string.Empty;
		}
		((CanvasItem)screen._ascensionPanel).Visible = false;
		ApplyInfoPanelVisibility(screen, showInfoPanel);
	}

	private static void ApplyInfoPanelVisibility(NCharacterSelectScreen screen, bool visible)
	{
		((CanvasItem)screen._infoPanel).Visible = visible;
		if (visible)
		{
			AnimateInfoPanel(screen);
		}
	}

	private static bool IsCharacterLocked(CharacterModel character)
	{
		UnlockState unlockState = SaveManager.Instance.GenerateUnlockStateFromProgress();
		if (character is RandomCharacter)
		{
			return ModelDb.AllCharacters.Where((CharacterModel c) => !(c is CustomCharacterModel customCharacterModel) || customCharacterModel.AllowInVanillaRandomCharacterSelect).Any((CharacterModel c) => !unlockState.Characters.Contains(c));
		}
		return !unlockState.Characters.Contains(character);
	}
}
