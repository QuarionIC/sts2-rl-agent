using System;
using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BaseLib.BaseLibScenes;

internal sealed class NCustomCharacterSelectEntryButton : ICharacterSelectButtonDelegate
{
	private const string ButtonScenePath = "res://scenes/screens/char_select/char_select_button.tscn";

	private static readonly FieldInfo? DelegateField = AccessTools.Field(typeof(NCharacterSelectButton), "_delegate");

	private static readonly FieldInfo? CharacterField = AccessTools.Field(typeof(NCharacterSelectButton), "_character");

	private static readonly FieldInfo? LockedField = AccessTools.Field(typeof(NCharacterSelectButton), "_isLocked");

	private readonly NCharacterSelectScreen _screen;

	private readonly Action<NCustomCharacterSelectEntryButton> _onSelected;

	public CustomCharacterSelectEntry Entry { get; }

	public NCharacterSelectButton Button { get; }

	public StartRunLobby Lobby => _screen.Lobby;

	public bool IsLocked => Button.IsLocked;

	public CharacterModel? LockSourceCharacter => Entry.AvailabilitySourceCharacter;

	public NCustomCharacterSelectEntryButton(CustomCharacterSelectEntry entry, NCharacterSelectScreen screen, Action<NCustomCharacterSelectEntryButton> onSelected)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		Entry = entry;
		_screen = screen;
		_onSelected = onSelected;
		PackedScene val = ResourceLoader.Load<PackedScene>("res://scenes/screens/char_select/char_select_button.tscn", (string)null, (CacheMode)1) ?? throw new InvalidOperationException("Failed to load res://scenes/screens/char_select/char_select_button.tscn.");
		Button = val.Instantiate<NCharacterSelectButton>((GenEditState)0);
		((Node)Button).Name = StringName.op_Implicit(entry.EntryId + "_entry_button");
		((GodotObject)Button).SetMeta(StringName.op_Implicit("BaseLibCustomCharacterSelectEntry"), Variant.op_Implicit(entry.EntryId));
		DelegateField?.SetValue(Button, this);
		UpdateInteractionState();
	}

	public void Enable()
	{
		((NClickableControl)Button).Enable();
		UpdateInteractionState();
	}

	public void Disable()
	{
		((NClickableControl)Button).Disable();
	}

	public void Deselect()
	{
		Button.Deselect();
	}

	public void TryGrabFocus()
	{
		if (((CanvasItem)Button).Visible && ((Node)Button).IsInsideTree())
		{
			((Control)Button).GrabFocus();
		}
	}

	public void SelectCharacter(NCharacterSelectButton charSelectButton, CharacterModel characterModel)
	{
		_onSelected(this);
	}

	private void ApplyVisuals()
	{
		TextureRect nodeOrNull = ((Node)Button).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("%Icon"));
		if (nodeOrNull != null)
		{
			nodeOrNull.Texture = ResourceLoader.Load<Texture2D>(Entry.ButtonIconPath, (string)null, (CacheMode)1);
		}
		TextureRect nodeOrNull2 = ((Node)Button).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("%IconAdd"));
		if (nodeOrNull2 != null)
		{
			nodeOrNull2.Texture = ((nodeOrNull != null) ? nodeOrNull.Texture : null);
		}
		TextureRect nodeOrNull3 = ((Node)Button).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("%Lock"));
		if (nodeOrNull3 != null)
		{
			((CanvasItem)nodeOrNull3).Visible = IsLocked;
		}
	}

	private void UpdateInteractionState()
	{
		CharacterField?.SetValue(Button, ((object)LockSourceCharacter) ?? ((object)ModelDb.Character<Ironclad>()));
		LockedField?.SetValue(Button, !Entry.UnlockedInCharacterSelect);
		ApplyVisuals();
	}
}
