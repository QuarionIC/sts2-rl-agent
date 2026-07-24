using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Abstracts;

public abstract class CustomSingletonModel : SingletonModel, ICustomModel
{
	public enum HookType
	{
		None,
		Combat,
		Run
	}

	public override bool ShouldReceiveCombatHooks { get; }

	public CustomSingletonModel(HookType hookType)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		switch (hookType)
		{
		case HookType.Combat:
			ShouldReceiveCombatHooks = true;
			ModHelper.SubscribeForCombatStateHooks(((AbstractModel)this).Id.Entry, new CombatHookSubscriptionDelegate(CombatSubModels));
			break;
		case HookType.Run:
			ModHelper.SubscribeForRunStateHooks(((AbstractModel)this).Id.Entry, new RunHookSubscriptionDelegate(RunSubModels));
			break;
		case HookType.None:
			break;
		}
	}

	[Obsolete("Use the constructor receiving a HookType instead. A singleton receiving both types of hooks will receive some hooks twice, so this constructor is being replaced.")]
	public CustomSingletonModel(bool receiveCombatHooks, bool receiveRunHooks)
		: this(receiveCombatHooks ? HookType.Combat : (receiveRunHooks ? HookType.Run : HookType.None))
	{
	}

	private IEnumerable<AbstractModel> RunSubModels(RunState runState)
	{
		return new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)this);
	}

	private IEnumerable<AbstractModel> CombatSubModels(CombatState combatState)
	{
		return new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)this);
	}
}
