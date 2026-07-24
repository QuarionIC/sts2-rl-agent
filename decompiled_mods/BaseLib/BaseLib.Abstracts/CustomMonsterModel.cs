using System;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

public abstract class CustomMonsterModel : MonsterModel, ICustomModel, ISceneConversions
{
	public virtual string? CustomVisualPath => null;

	public virtual string? CustomAttackSfx => null;

	public virtual string? CustomCastSfx => null;

	public virtual string? CustomDeathSfx => null;

	public CustomMonsterModel()
	{
		CustomContentDictionary.RegisterType(((object)this).GetType());
	}

	public virtual NCreatureVisuals? CreateCustomVisuals()
	{
		return null;
	}

	public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		return null;
	}

	public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, string? deadName = null, bool deadLoop = false, string? hitName = null, bool hitLoop = false, string? attackName = null, bool attackLoop = false, string? castName = null, bool castLoop = false)
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
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
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
		CreatureAnimator val6 = new CreatureAnimator(val, controller);
		val6.AddAnyState("Idle", val, (Func<bool>)null);
		val6.AddAnyState("Dead", val2, (Func<bool>)null);
		val6.AddAnyState("Hit", val3, (Func<bool>)null);
		val6.AddAnyState("Attack", val4, (Func<bool>)null);
		val6.AddAnyState("Cast", val5, (Func<bool>)null);
		return val6;
	}

	public void RegisterSceneConversions()
	{
		CustomVisualPath?.RegisterSceneForConversion<NCreatureVisuals>((Action<NCreatureVisuals>?)null);
	}
}
