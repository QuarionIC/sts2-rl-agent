using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Dolso;

internal abstract class ModelHook : AbstractModel
{
	private static readonly List<ModelHook> hooks;

	internal static RunState? run_state => RunManager.Instance.State;

	internal static CombatState? combat_state => CombatManager.Instance._state;

	public override bool ShouldReceiveCombatHooks => true;

	internal static void Register<T>() where T : ModelHook, new()
	{
		hooks.Add(new T());
	}

	private static IEnumerable<AbstractModel> RunHookSubscriptionDelegate(RunState runState)
	{
		return (IEnumerable<AbstractModel>)hooks;
	}

	static ModelHook()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		hooks = new List<ModelHook>();
		ModHelper.SubscribeForRunStateHooks(typeof(ModelHook).Assembly.GetName().Name, new RunHookSubscriptionDelegate(RunHookSubscriptionDelegate));
	}
}
