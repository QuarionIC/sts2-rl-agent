using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheMausoleum : CustomEventModel
{
	private const int MaxHpGain = 10;

	private const string _sacrificeRelicKey = "SacrificeRelic";

	private RelicModel? _sacrificeRelic;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new StringVar("SacrificeRelic", ""),
		(DynamicVar)new IntVar("MaxHpGain", 10m)
	};

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Relics.Any((RelicModel r) => r.IsTradable));
	}

	public override void CalculateVars()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (ActsFromThePastConfig.RebalancedMode)
		{
			_sacrificeRelic = ((EventModel)this).Rng.NextItem<RelicModel>(((EventModel)this).Owner.Relics.Where((RelicModel r) => r.IsTradable));
			if (_sacrificeRelic != null)
			{
				((StringVar)((EventModel)this).DynamicVars["SacrificeRelic"]).StringValue = _sacrificeRelic.Title.GetFormattedText();
			}
		}
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "ghosts");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if (ActsFromThePastConfig.RebalancedMode)
		{
			EventOption[] obj = new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Open, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Writhe>(false).ToArray()),
				default(EventOption)
			};
			Func<Task> func = PayRespects;
			LocString obj2 = ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.PAY_RESPECTS.title");
			LocString obj3 = ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.PAY_RESPECTS.description");
			string text = ((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.PAY_RESPECTS";
			RelicModel? sacrificeRelic = _sacrificeRelic;
			obj[1] = new EventOption((EventModel)(object)this, func, obj2, obj3, text, ((sacrificeRelic != null) ? sacrificeRelic.HoverTips : null) ?? Array.Empty<IHoverTip>()).ThatHasDynamicTitle();
			return (IReadOnlyList<EventOption>)(object)obj;
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Open, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Writhe>(), false) }),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Open()
	{
		NDebugAudioManager.Instance.Play("blunt_attack.mp3", 1f, (PitchVariance)0);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)2, (ShakeDuration)3, -1f);
		}
		RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
		await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
		CardModel writhe = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Writhe>(), ((EventModel)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(writhe, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OPEN_CURSED"));
	}

	private async Task PayRespects()
	{
		if (_sacrificeRelic != null)
		{
			await RelicCmd.Remove(_sacrificeRelic);
		}
		await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, 10m);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PAY_RESPECTS"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public TheMausoleum()
		: base(true)
	{
	}
}
