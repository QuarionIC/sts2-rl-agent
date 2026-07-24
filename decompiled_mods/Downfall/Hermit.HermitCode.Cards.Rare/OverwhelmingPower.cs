using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class OverwhelmingPower : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public OverwhelmingPower()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(3, 1);
		((ConstructedCardModel)this).WithEnergy(3, 0);
		((ConstructedCardModel)(object)this).WithPower<OverwhelmingPowerPower>(4, -1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await PlayerCmd.GainEnergy((decimal)((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, ((CardModel)this).Owner);
		await CommonActions.Draw((CardModel)(object)this, ctx);
		await CommonActions.ApplySelf<OverwhelmingPowerPower>(ctx, (CardModel)(object)this, false);
	}
}
