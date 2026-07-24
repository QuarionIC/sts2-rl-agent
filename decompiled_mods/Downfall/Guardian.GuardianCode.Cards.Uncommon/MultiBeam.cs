using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class MultiBeam : GuardianCardModel, ITickCard, ICustomTickDuration
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Magerblutooth>();

	protected override bool HasEnergyCostX => true;

	public int TickDuration => 3;

	public MultiBeam()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(3, 3);
		((ConstructedCardModel)this).WithVar("Increase", 2, 1);
	}

	public Task OnTick(PlayerChoiceContext ctx)
	{
		((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy((decimal)((CardModel)this).DynamicVars["Increase"].IntValue);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = ((CardModel)this).ResolveEnergyXValue();
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(num).Execute(ctx);
	}
}
