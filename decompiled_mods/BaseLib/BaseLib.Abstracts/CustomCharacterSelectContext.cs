using System;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BaseLib.Abstracts;

public sealed class CustomCharacterSelectContext
{
	private readonly Action<CharacterModel?> _setCharacter;

	public CustomCharacterSelectEntry Entry { get; }

	public NCharacterSelectScreen Screen { get; }

	public StartRunLobby Lobby => Screen.Lobby;

	public Control SceneRoot { get; }

	public Control? ForegroundSceneRoot { get; }

	public CharacterModel? SelectedCharacter { get; private set; }

	public bool VanillaInfoPanelVisible => ((CanvasItem)Screen._infoPanel).Visible;

	internal CustomCharacterSelectContext(CustomCharacterSelectEntry entry, NCharacterSelectScreen screen, Control sceneRoot, Control? foregroundSceneRoot, Action<CharacterModel?> setCharacter)
	{
		Entry = entry;
		Screen = screen;
		SceneRoot = sceneRoot;
		ForegroundSceneRoot = foregroundSceneRoot;
		_setCharacter = setCharacter;
	}

	public void SetCharacter(CharacterModel? character)
	{
		SelectedCharacter = character;
		_setCharacter(character);
	}

	public void ClearCharacter()
	{
		SetCharacter(null);
	}

	public void SetVanillaInfoPanelVisible(bool visible)
	{
		((CanvasItem)Screen._infoPanel).Visible = visible;
	}
}
