using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class Shatter : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			if (!((CardModel)this).Owner.ShouldBerserkerComboTrigger())
			{
				return ((CardModel)this).Owner.ShouldDefensiveComboTrigger();
			}
			return true;
		}
	}

	public Shatter()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(14, 2);
		((ConstructedCardModel)this).WithPower<VulnerablePower>(1, 1);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Combo));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if ((((CardModel)this).Owner.ShouldDefensiveComboTrigger() || ((CardModel)this).Owner.ShouldBerserkerComboTrigger()) && cardPlay.Target != null)
		{
			await CommonActions.Apply<VulnerablePower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
			await CommonActions.Apply<WeakPower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
		}
	}
}
