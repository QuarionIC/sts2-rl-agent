using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class CursedWeapon : HermitCardModel
{
	private const string IncreaseKey = "Increase";

	private const int BaseDamage = 10;

	private int _currentDamage = 10;

	private int _increasedDamage;

	public override bool CanBeGeneratedInCombat => false;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

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

	public CursedWeapon()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithDamage(CurrentDamage, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithHpLoss(2);
		((ConstructedCardModel)this).WithVar("Increase", 1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await DownfallCreatureCmd.Damage(ctx, ((CardModel)this).Owner.Creature, ((DynamicVar)((CardModel)this).DynamicVars.HpLoss).BaseValue, (ValueProp)6, ((CardModel)this).Owner.Creature, (CardModel?)(object)this, play);
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitFireHitFx().Execute(ctx);
		int increase = ((CardModel)this).DynamicVars["Increase"].IntValue;
		((CardModel)this).Owner.GetAllCards().OfType<CursedWeapon>().ToList()
			.ForEach(delegate(CursedWeapon card)
			{
				card.BuffFromPlay(increase);
			});
		((CardModel)this).Owner.GetDeck().OfType<CursedWeapon>().ToList()
			.ForEach(delegate(CursedWeapon card)
			{
				card.BuffFromPlay(increase);
			});
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
		CurrentDamage = 10 + IncreasedDamage;
	}
}
