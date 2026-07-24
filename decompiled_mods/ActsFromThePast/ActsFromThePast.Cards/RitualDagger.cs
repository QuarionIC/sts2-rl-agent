using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Cards;

[Pool(typeof(EventCardPool))]
public sealed class RitualDagger : CustomCardModel
{
	private const string _increaseKey = "Increase";

	private const int _baseDamage = 15;

	private int _currentDamage = 15;

	private int _increasedDamage;

	[SavedProperty]
	public int CurrentDamage
	{
		get
		{
			return _currentDamage;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_currentDamage = value;
			((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue = _currentDamage;
		}
	}

	[SavedProperty]
	public int IncreasedDamage
	{
		get
		{
			return _increasedDamage;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_increasedDamage = value;
		}
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new DamageVar((decimal)CurrentDamage, (ValueProp)8),
		(DynamicVar)new IntVar("Increase", 3m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)6, Array.Empty<DynamicVar>()) };

	public RitualDagger()
		: base(1, (CardType)1, (CardRarity)6, (TargetType)2, true, true)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "Target");
		bool shouldTriggerFatal = cardPlay.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
		AttackCommand attackCommand = await DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
			.Execute(choiceContext);
		if (shouldTriggerFatal && attackCommand.Results.SelectMany((List<DamageResult> r) => r).Any((DamageResult r) => r.WasTargetKilled))
		{
			int increase = ((CardModel)this).DynamicVars["Increase"].IntValue;
			BuffFromPlay(increase);
			CardModel deckVersion = ((CardModel)this).DeckVersion;
			if (deckVersion is RitualDagger deckVersion2)
			{
				deckVersion2.BuffFromPlay(increase);
			}
		}
	}

	protected override void OnUpgrade()
	{
		((CardModel)this).DynamicVars["Increase"].UpgradeValueBy(2m);
	}

	protected override void AfterDowngraded()
	{
		UpdateDamage();
	}

	private void BuffFromPlay(int extraDamage)
	{
		IncreasedDamage += extraDamage;
		UpdateDamage();
	}

	private void UpdateDamage()
	{
		CurrentDamage = 15 + IncreasedDamage;
	}
}
