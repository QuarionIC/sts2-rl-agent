using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using BaseLib.Patches.UI;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BaseLib.Abstracts;

public abstract class CustomCharacterModel : CharacterModel, ICustomModel, ILocalizationProvider, ISceneConversions
{
	public virtual List<(string, string)>? Localization => null;

	public virtual bool HideFromVanillaCharacterSelect => false;

	public virtual bool AllowInVanillaRandomCharacterSelect => !HideFromVanillaCharacterSelect;

	public virtual bool HideInCompendium => false;

	public virtual string? CustomVisualPath => null;

	public virtual string? CustomTrailPath => null;

	public virtual string? CustomIconTexturePath => null;

	public virtual string? CustomIconOutlineTexturePath => null;

	public virtual string? CustomIconPath => null;

	public virtual Control? CustomIcon => null;

	public virtual CustomEnergyCounter? CustomEnergyCounter => null;

	public virtual string? CustomEnergyCounterPath => null;

	public virtual string? CustomRestSiteAnimPath => null;

	public virtual string? CustomMerchantAnimPath => null;

	public virtual string? CustomArmPointingTexturePath => null;

	public virtual string? CustomArmRockTexturePath => null;

	public virtual string? CustomArmPaperTexturePath => null;

	public virtual string? CustomArmScissorsTexturePath => null;

	public virtual RelicIconData? CustomYummyCookie => null;

	public virtual string? CustomCharacterSelectBg => null;

	public virtual string? CustomCharacterSelectIconPath => null;

	public virtual string? CustomCharacterSelectLockedIconPath => null;

	public virtual string? CustomCharacterSelectTransitionPath => null;

	public virtual string? CustomMapMarkerPath => null;

	public virtual string? CustomAttackSfx => null;

	public virtual string? CustomCastSfx => null;

	public virtual string? CustomDeathSfx => null;

	public override int StartingGold => 99;

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public virtual float DeathAnimTime => 1.5f;

	public CustomCharacterModel()
	{
		CustomContentDictionary.AddCharacter(this);
	}

	public virtual NCreatureVisuals? CreateCustomVisuals()
	{
		return null;
	}

	public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		return null;
	}

	public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, string? deadName = null, bool deadLoop = false, string? hitName = null, bool hitLoop = false, string? attackName = null, bool attackLoop = false, string? castName = null, bool castLoop = false, string? relaxedName = null, bool relaxedLoop = true)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		AnimState val = new AnimState(idleName, true);
		AnimState val2 = (AnimState)((deadName == null) ? ((object)val) : ((object)new AnimState(deadName, deadLoop)));
		AnimState val3 = (AnimState)((hitName == null) ? ((object)val) : ((object)new AnimState(hitName, hitLoop)
		{
			NextState = val
		}));
		AnimState val4 = (AnimState)((attackName == null) ? ((object)val) : ((object)new AnimState(attackName, attackLoop)
		{
			NextState = val
		}));
		AnimState val5 = (AnimState)((castName == null) ? ((object)val) : ((object)new AnimState(castName, castLoop)
		{
			NextState = val
		}));
		AnimState val6;
		if (relaxedName == null)
		{
			val6 = val;
		}
		else
		{
			val6 = new AnimState(relaxedName, relaxedLoop);
			val6.AddBranch("Idle", val, (Func<bool>)null);
		}
		CreatureAnimator val7 = new CreatureAnimator(val, controller);
		val7.AddAnyState("Idle", val, (Func<bool>)null);
		val7.AddAnyState("Dead", val2, (Func<bool>)null);
		val7.AddAnyState("Hit", val3, (Func<bool>)null);
		val7.AddAnyState("Attack", val4, (Func<bool>)null);
		val7.AddAnyState("Cast", val5, (Func<bool>)null);
		val7.AddAnyState("Relaxed", val6, (Func<bool>)null);
		return val7;
	}

	public void RegisterSceneConversions()
	{
		CustomVisualPath?.RegisterSceneForConversion<NCreatureVisuals>((Action<NCreatureVisuals>?)null);
		CustomRestSiteAnimPath?.RegisterSceneForConversion<NRestSiteCharacter>((Action<NRestSiteCharacter>?)null);
		CustomMerchantAnimPath?.RegisterSceneForConversion<NMerchantCharacter>((Action<NMerchantCharacter>?)null);
		CustomEnergyCounterPath?.RegisterSceneForConversion<NEnergyCounter>((Action<NEnergyCounter>?)null);
	}
}
