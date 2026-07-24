using System;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomCharacterSelectEntry : ICustomModel
{
	public virtual string EntryId => StringHelper.Slugify(GetType().FullName ?? GetType().Name);

	public abstract string ButtonIconPath { get; }

	public virtual string EntryTitle => GetType().Name;

	public virtual string EntryDescription => string.Empty;

	public virtual int SortOrder => 0;

	public virtual bool VisibleInCharacterSelect => true;

	public virtual CharacterModel? AvailabilitySourceCharacter => null;

	public virtual bool UnlockedInCharacterSelect
	{
		get
		{
			if (AvailabilitySourceCharacter != null)
			{
				return CustomCharacterSelectEntryAvailability.IsUnlocked(AvailabilitySourceCharacter);
			}
			return true;
		}
	}

	public virtual CharacterModel? InitialCharacter => null;

	public virtual bool ShowVanillaInfoPanelWhenUnresolved => true;

	public virtual bool ShowVanillaInfoPanelWhenResolved => true;

	public virtual string LockedTitle => new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText();

	public virtual string LockedDescription => EntryDescription;

	public virtual string? CharacterSelectScenePath => null;

	public virtual string? CharacterSelectForegroundScenePath => null;

	protected CustomCharacterSelectEntry()
	{
		CustomCharacterSelectEntryRegistry.Register(this);
	}

	public virtual Control CreateCharacterSelectScene()
	{
		if (CharacterSelectScenePath == null)
		{
			throw new InvalidOperationException($"{GetType().FullName} must override either {"CharacterSelectScenePath"} or {"CreateCharacterSelectScene"}.");
		}
		PackedScene obj = ResourceLoader.Load<PackedScene>(CharacterSelectScenePath, (string)null, (CacheMode)1);
		return ((obj != null) ? obj.Instantiate<Control>((GenEditState)0) : null) ?? throw new InvalidOperationException($"Failed to load character select scene at path '{CharacterSelectScenePath}' for {GetType().FullName}.");
	}

	public virtual Control? CreateCharacterSelectForegroundScene()
	{
		if (CharacterSelectForegroundScenePath == null)
		{
			return null;
		}
		PackedScene obj = ResourceLoader.Load<PackedScene>(CharacterSelectForegroundScenePath, (string)null, (CacheMode)1);
		return ((obj != null) ? obj.Instantiate<Control>((GenEditState)0) : null) ?? throw new InvalidOperationException($"Failed to load character select foreground scene at path '{CharacterSelectForegroundScenePath}' for {GetType().FullName}.");
	}

	public virtual void RegisterScene(Control root, CustomCharacterSelectContext context)
	{
	}

	public virtual void RegisterForegroundScene(Control root, CustomCharacterSelectContext context)
	{
	}
}
