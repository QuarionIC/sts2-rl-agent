using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class DevourFlame : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	protected override bool ShouldGlowGoldInternal => HexaghostCmd.IsPreviousIgnited(((CardModel)this).Owner);

	public DevourFlame()
		: base(0, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(9, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HexaghostKeyword.Retract));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (HexaghostCmd.IsPreviousIgnited(((CardModel)this).Owner))
		{
			await HexaghostCmd.Retract(ctx, ((CardModel)this).Owner, (AbstractModel?)(object)this);
			await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		}
	}
}
