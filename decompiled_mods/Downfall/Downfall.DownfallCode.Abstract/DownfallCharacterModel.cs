using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Utils.Sound;
using Godot;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallCharacterModel : CustomCharacterModel
{
	public abstract string ModId { get; }

	public abstract string? CharId { get; }

	public virtual Color LabOutlineColor => new Color(1f, 1f, 1f, 1f);

	public virtual Color DeckEntryCardColor => new Color(1f, 1f, 1f, 1f);

	public abstract float CardColorH { get; }

	public abstract float CardColorS { get; }

	public abstract float CardColorV { get; }

	public override string CustomCharacterSelectBg => "res://" + ModId + "/scenes/character/selection_screen.tscn";

	public override string CustomCharacterSelectIconPath => "res://" + ModId + "/images/character/char_select.png";

	public override string CustomCharacterSelectLockedIconPath => "res://" + ModId + "/images/character/char_select_locked.png";

	public override string CustomIconTexturePath => "res://" + ModId + "/images/character/character_icon.png";

	public override string CustomEnergyCounterPath
	{
		get
		{
			string text = "res://" + ModId + "/scenes/character/energy_counter.tscn";
			if (!ResourceLoader.Exists(text, ""))
			{
				return "res://Downfall/scenes/character/energy_counter_empty.tscn";
			}
			return text;
		}
	}

	public override string CustomMapMarkerPath => "res://" + ModId + "/images/character/map_marker.png";

	public override string CustomArmPointingTexturePath => "res://" + ModId + "/images/character/mp_point.png";

	public override string CustomArmRockTexturePath => "res://" + ModId + "/images/character/mp_rock.png";

	public override string CustomArmPaperTexturePath => "res://" + ModId + "/images/character/mp_paper.png";

	public override string CustomArmScissorsTexturePath => "res://" + ModId + "/images/character/mp_scissors.png";

	public override string CustomCharacterSelectTransitionPath => "res://" + ModId + "/material/character/transition_mat.tres";

	public override string CustomVisualPath => "res://" + ModId + "/scenes/character/combat.tscn";

	public override string CustomIconPath => "res://" + ModId + "/scenes/character/char_icon.tscn";

	public override string CustomIconOutlineTexturePath => ModId + "/images/character/character_icon_outline.png";

	public override string CustomTrailPath => "res://" + ModId + "/scenes/character/card_trail.tscn";

	public override string CustomRestSiteAnimPath => "res://Downfall/scenes/character/error_rest_site.tscn";

	public override string CustomMerchantAnimPath => "res://" + ModId + "/scenes/character/merchant.tscn";

	public override string CustomAttackSfx => "event:/sfx/characters/ironclad/ironclad_attack";

	public override string CustomDeathSfx => "event:/sfx/characters/ironclad/ironclad_die";

	public override string CharacterSelectSfx => "res://" + ModId + "/audio/character_select.ogg";

	public virtual ModSoundEffect? CharacterSelectSfxEntry => null;

	protected DownfallCharacterModel()
	{
		DownfallMainFile.Logger.Info("Creating " + ((object)this).GetType().Name, 1);
	}

	private string EnergyCounterPaths(int i)
	{
		return $"res://{ModId}/images/character/orb_layer_{i}.png";
	}

	public override List<string> GetArchitectAttackVfx()
	{
		int num = 5;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		span[0] = "vfx/vfx_attack_blunt";
		span[1] = "vfx/vfx_heavy_blunt";
		span[2] = "vfx/vfx_attack_slash";
		span[3] = "vfx/vfx_bloody_impact";
		span[4] = "vfx/vfx_rock_shatter";
		return list;
	}
}
