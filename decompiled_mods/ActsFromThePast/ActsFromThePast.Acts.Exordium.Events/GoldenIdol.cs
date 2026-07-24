using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class GoldenIdol : CustomEventModel
{
	private const decimal HpLossPercent = 0.35m;

	private const decimal MaxHpLossPercent = 0.10m;

	private const int JamGoldMin = 30;

	private const int JamGoldMax = 50;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[4]
	{
		(DynamicVar)new IntVar("Damage", 0m),
		(DynamicVar)new IntVar("MaxHpLoss", 0m),
		(DynamicVar)new GoldVar(0),
		(DynamicVar)new StringVar("Relic", "Relic")
	};

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["Damage"].BaseValue = Math.Floor((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.35m);
		decimal num = Math.Floor((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.10m);
		if (num < 1m)
		{
			num = 1m;
		}
		((EventModel)this).DynamicVars["MaxHpLoss"].BaseValue = num;
		((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = 30 + ((EventModel)this).Rng.NextInt(21);
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "golden_idol");
	}

	private IEnumerable<RelicModel> GetTradableRelics()
	{
		return ((EventModel)this).Owner.Relics.Where((RelicModel r) => !r.IsUsedUp && !r.IsMelted && !r.SpawnsPets && (int)r.Rarity != 1 && (int)r.Rarity != 6 && (int)r.Rarity != 7);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (ActsFromThePastConfig.RebalancedMode)
		{
			RelicModel relic = ((EventModel)this).Rng.NextItem<RelicModel>(GetTradableRelics());
			if (relic != null)
			{
				((StringVar)((EventModel)this).DynamicVars["Relic"]).StringValue = relic.Title.GetFormattedText();
				return (IReadOnlyList<EventOption>)(object)new EventOption[2]
				{
					((CustomEventModel)this).Option((Func<Task>)Take, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<ActsFromThePast.Relics.GoldenIdol>()).ToArray()),
					new EventOption((EventModel)(object)this, (Func<Task>)async delegate
					{
						await Switcheroo(relic);
					}, ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.SWITCHEROO.title"), ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.SWITCHEROO.description"), ((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.SWITCHEROO", relic.HoverTips).ThatHasDynamicTitle()
				};
			}
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Take, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<ActsFromThePast.Relics.GoldenIdol>()).ToArray()),
				new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.SWITCHEROO_LOCKED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Take, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<ActsFromThePast.Relics.GoldenIdol>()).ToArray()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Take()
	{
		RelicModel relic = ((RelicModel)ModelDb.Relic<ActsFromThePast.Relics.GoldenIdol>()).ToMutable();
		await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
		if (ActsFromThePastConfig.RebalancedMode)
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("BOULDER_REBALANCED"), (IEnumerable<EventOption>)(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Jam, "BOULDER_REBALANCED", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Smash, "BOULDER", Array.Empty<IHoverTip>()).ThatDoesDamage(((EventModel)this).DynamicVars["Damage"].BaseValue),
				((CustomEventModel)this).Option((Func<Task>)Crawl, "BOULDER", Array.Empty<IHoverTip>())
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("BOULDER"), (IEnumerable<EventOption>)(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Outrun, "BOULDER", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Injury>(), false) }),
				((CustomEventModel)this).Option((Func<Task>)Smash, "BOULDER", Array.Empty<IHoverTip>()).ThatDoesDamage(((EventModel)this).DynamicVars["Damage"].BaseValue),
				((CustomEventModel)this).Option((Func<Task>)Crawl, "BOULDER", Array.Empty<IHoverTip>())
			});
		}
	}

	private async Task Switcheroo(RelicModel relic)
	{
		await RelicCmd.Remove(relic);
		await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<ActsFromThePast.Relics.GoldenIdol>()).ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SWITCHEROO"));
	}

	private async Task Jam()
	{
		RelicModel goldenIdol = ((IEnumerable<RelicModel>)((EventModel)this).Owner.Relics).FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => r is ActsFromThePast.Relics.GoldenIdol));
		if (goldenIdol != null)
		{
			await RelicCmd.Remove(goldenIdol);
		}
		await PlayerCmd.GainGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("JAM"));
	}

	private async Task Outrun()
	{
		await CardPileCmd.AddCurseToDeck<Injury>(((EventModel)this).Owner);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OUTRUN"));
	}

	private async Task Smash()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["Damage"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SMASH"));
	}

	private async Task Crawl()
	{
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["MaxHpLoss"].BaseValue, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("CRAWL"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public GoldenIdol()
		: base(true)
	{
	}
}
