using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class CultistStrike : AutomatonCardModel, IEncodable
{
	private int _currentDamage = 6;

	private int _increasedDamage;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

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

	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new DamageEncode());

	public CultistStrike()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(CurrentDamage, 0);
		((ConstructedCardModel)this).WithVar("Increase", 1, 1);
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int intValue = ((CardModel)this).DynamicVars["Increase"].IntValue;
		BuffFromPlay(intValue);
		if (!(((CardModel)this).DeckVersion is CultistStrike cultistStrike))
		{
			return Task.CompletedTask;
		}
		cultistStrike.BuffFromPlay(intValue);
		return Task.CompletedTask;
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
		CurrentDamage = 6 + IncreasedDamage;
	}
}
