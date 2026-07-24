using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class WorldOfGoop : CustomEventModel
{
	private const int Damage = 11;

	private const int Gold = 75;

	private const int MinGoldLoss = 35;

	private const int MaxGoldLoss = 75;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("Damage", 11m),
		(DynamicVar)new GoldVar(75),
		(DynamicVar)new IntVar("GoldLoss", 0m)
	};

	public override void CalculateVars()
	{
		int num = 35 + ((EventModel)this).Rng.NextInt(41);
		if (num > ((EventModel)this).Owner.Gold)
		{
			num = ((EventModel)this).Owner.Gold;
		}
		((EventModel)this).DynamicVars["GoldLoss"].BaseValue = num;
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "spirits");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Gather, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage(11m),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Gather()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 11m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		await PlayerCmd.GainGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("GATHER"));
	}

	private async Task Leave()
	{
		await PlayerCmd.LoseGold(((EventModel)this).DynamicVars["GoldLoss"].BaseValue, ((EventModel)this).Owner, (GoldLossType)2);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public WorldOfGoop()
		: base(true)
	{
	}
}
