using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class LingeringShades : HexaghostCardModel
{
	public LingeringShades()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)2)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword(HexaghostKeyword.Retract, (UpgradeType)0);
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(15, 4);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)2));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<SoulBurnPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CardPileCmd.Add(from c in ((CardModel)this).Owner.GetDiscard()
			where c.Keywords.Contains((CardKeyword)2)
			select c, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
