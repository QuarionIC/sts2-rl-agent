using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class BadOmen : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public BadOmen()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await SelectGhostflame(ctx, ((CardModel)this).Owner);
	}

	private static async Task SelectGhostflame(PlayerChoiceContext ctx, Player owner)
	{
		List<BadOmenChoice> list = HexaghostModelDb.AllGhostflames.Select((GhostflameModel f) => BadOmenChoice.Create(f, owner)).ToList();
		if (await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, owner, false) is BadOmenChoice badOmenChoice)
		{
			GhostflameModel ghostflameModel = badOmenChoice.GhostflameModel;
			if (ghostflameModel != null)
			{
				HexaghostCmd.SetCurrentGhostflame(owner, ghostflameModel);
			}
		}
	}
}
