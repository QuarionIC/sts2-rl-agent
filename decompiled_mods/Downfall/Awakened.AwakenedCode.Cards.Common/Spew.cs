using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Events;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Spew : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Eudaimonia>();

	public Spew()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithTip(AwakenedTip.Drained.WithVars((DynamicVar)new EnergyVar(1)));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		ICombatState combatState = ((CardModel)this).CombatState;
		Player owner = ((CardModel)this).Owner;
		ResourceInfo resources = cardPlay.Resources;
		await AwakenedHook.OnDrained(combatState, ctx, owner, ((ResourceInfo)(ref resources)).EnergySpent);
	}
}
