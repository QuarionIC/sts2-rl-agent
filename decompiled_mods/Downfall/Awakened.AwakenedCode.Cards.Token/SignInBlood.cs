using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class SignInBlood : AwakenedCardModel
{
	public SignInBlood()
		: base(0, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		((ConstructedCardModel)this).WithPower<StrengthPower>(2, 0);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithVars((DynamicVar[])(object)new DynamicVar[1] { (DynamicVar)new HpLossVar(2m) });
		((ConstructedCardModel)this).WithCards(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		VfxCmd.PlayOnCreatureCenter(((CardModel)this).Owner.Creature, "vfx/vfx_bloody_impact");
		await DownfallCreatureCmd.Damage(ctx, ((CardModel)this).Owner.Creature, ((DynamicVar)((CardModel)this).DynamicVars.HpLoss).BaseValue, (ValueProp)14, (CardModel)(object)this, cardPlay);
		await CommonActions.Draw((CardModel)(object)this, ctx);
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
	}
}
