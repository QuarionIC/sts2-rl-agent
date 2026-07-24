using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ScrapOoze : CustomEventModel
{
	private const int BaseDamage = 5;

	private const int BaseRelicChance = 25;

	private const int ChanceIncreasePerAttempt = 10;

	private const int DamageIncreasePerAttempt = 1;

	private const int BloatGoldMin = 90;

	private const int BloatGoldMax = 130;

	private int _attempts;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	private int Attempts
	{
		get
		{
			return _attempts;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_attempts = value;
		}
	}

	private int CurrentDamage => 5 + Attempts;

	private int CurrentRelicChance => 25 + Attempts * 10;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("Damage", (decimal)CurrentDamage),
		(DynamicVar)new IntVar("RelicChance", (decimal)CurrentRelicChance),
		(DynamicVar)new GoldVar(130)
	};

	public override void CalculateVars()
	{
		((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = 130 - ((EventModel)this).Rng.NextInt(41);
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "scrap_ooze");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Reach, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)CurrentDamage),
				((CustomEventModel)this).Option((Func<Task>)Bloat, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Reach, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)CurrentDamage),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	public override bool IsAllowed(IRunState runState)
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return ((IPlayerCollection)runState).Players.All((Player p) => (decimal)p.Gold >= ((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue);
		}
		return true;
	}

	private async Task Reach()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)CurrentDamage, (ValueProp)6, (CardModel)null, (CardPlay)null);
		int roll = ((EventModel)this).Rng.NextInt(100);
		int threshold = 100 - CurrentRelicChance;
		if (roll >= threshold)
		{
			RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
			await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SUCCESS"));
			return;
		}
		Attempts++;
		((EventModel)this).DynamicVars["Damage"].BaseValue = CurrentDamage;
		((EventModel)this).DynamicVars["RelicChance"].BaseValue = CurrentRelicChance;
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("FAIL"), (IEnumerable<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Reach, "FAIL", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)CurrentDamage),
			((CustomEventModel)this).Option((Func<Task>)Leave, "FAIL", Array.Empty<IHoverTip>())
		});
	}

	private async Task Bloat()
	{
		await PlayerCmd.LoseGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, ((EventModel)this).Owner, (GoldLossType)1);
		SfxCmd.Play("event:/sfx/enemy/enemy_attacks/twig_slime_m/twig_slime_m_die", 1f);
		RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
		await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BLOAT"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public ScrapOoze()
		: base(true)
	{
	}
}
