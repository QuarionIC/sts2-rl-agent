using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.Localization;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class FullReleasePower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
	private string IconName => StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant();

	public override string CustomPackedIconPath => (IconName + ".tres").PowerImagePath<Automaton.AutomatonCode.Core.Automaton>();

	public override string CustomBigIconPath => (IconName + ".png").BigPowerImagePath<Automaton.AutomatonCode.Core.Automaton>();

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)2;

	public override PowerInstanceType InstanceType => (PowerInstanceType)1;

	protected override IEnumerable<DynamicVar> CanonicalVars => Encodable.All.Select((Encodable e) => e.FunctionDynamicVar);

	protected override IEnumerable<IHoverTip> ExtraHoverTips => Encodable.All.SelectMany((Encodable e) => (!(e.DynamicVar((AbstractModel)(object)this).BaseValue > 0m)) ? Array.Empty<IHoverTip>() : e.HoverTips((AbstractModel)(object)this));

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		List<string> source = (from encodable in Encodable.All
			where !(encodable is PowerEncode)
			where encodable.DynamicVar((AbstractModel)(object)this).BaseValue > 0m
			select encodable.GetDescription((AbstractModel)(object)this).GetFormattedText()).ToList();
		description.Add("effects", string.Join("\n", source.Where((string l) => !string.IsNullOrWhiteSpace(l))));
	}

	public void SetDynamicalVars(DynamicVarSet functionCardDynamicVars)
	{
		((PowerModel)this)._dynamicVars = functionCardDynamicVars.Clone((AbstractModel)(object)this);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (((PowerModel)this).Owner.Player != player || ((PowerModel)this).Owner.CombatState == null)
		{
			return;
		}
		Creature target = ((PowerModel)this).Owner.Player.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).Owner.CombatState.HittableEnemies);
		foreach (Encodable item in Encodable.All.Where((Encodable e) => !(e is PowerEncode)))
		{
			if (item.DynamicVar((AbstractModel)(object)this).BaseValue > 0m)
			{
				await item.OnPlay((AbstractModel)(object)this, ctx, target, null);
			}
		}
		((PowerModel)this).Flash();
	}
}
