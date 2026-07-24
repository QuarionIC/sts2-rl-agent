using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class FaceTrader : CustomEventModel, IActRestricted, IShrineEvent
{
	private const int GoldReward = 50;

	public int[] AllowedActIndices => new int[2] { 1, 2 };

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("Damage", 0m),
		(DynamicVar)new GoldVar(50)
	};

	public override void CalculateVars()
	{
		int num = (int)((decimal)((EventModel)this).Owner.Creature.MaxHp / 10m);
		if (num == 0)
		{
			num = 1;
		}
		((EventModel)this).DynamicVars["Damage"].BaseValue = num;
	}

	public override bool IsAllowed(IRunState runState)
	{
		if (((IPlayerCollection)runState).Players.Count > 1)
		{
			return false;
		}
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.All(delegate(Player p)
		{
			int num = (int)((decimal)p.Creature.MaxHp / 10m);
			if (num == 0)
			{
				num = 1;
			}
			return p.Creature.CurrentHp > num;
		});
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Continue()
	{
		List<EventOption> list = new List<EventOption>
		{
			((CustomEventModel)this).Option((Func<Task>)Touch, "MAIN", Array.Empty<IHoverTip>()).ThatDoesDamage(((EventModel)this).DynamicVars["Damage"].BaseValue),
			((CustomEventModel)this).Option((Func<Task>)Trade, "MAIN", Array.Empty<IHoverTip>())
		};
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "MAIN", Array.Empty<IHoverTip>()));
		}
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("MAIN"), (IEnumerable<EventOption>)list);
		return Task.CompletedTask;
	}

	private async Task Touch()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["Damage"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
		await PlayerCmd.GainGold(50m, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TOUCH"));
	}

	private async Task Trade()
	{
		RelicModel relic = GetRandomFace();
		await RelicCmd.Obtain(relic.ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TRADE"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private RelicModel GetRandomFace()
	{
		List<RelicModel> list = new List<RelicModel>();
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is CultistHeadpiece))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<CultistHeadpiece>());
		}
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is FaceOfCleric))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<FaceOfCleric>());
		}
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is GremlinVisage))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<GremlinVisage>());
		}
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is NlothsHungryFace))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<NlothsHungryFace>());
		}
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is SsserpentHead))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<SsserpentHead>());
		}
		if (list.Count == 0)
		{
			return (RelicModel)(object)ModelDb.Relic<Circlet>();
		}
		return ((EventModel)this).Rng.NextItem<RelicModel>((IEnumerable<RelicModel>)list);
	}

	public FaceTrader()
		: base(true)
	{
	}
}
