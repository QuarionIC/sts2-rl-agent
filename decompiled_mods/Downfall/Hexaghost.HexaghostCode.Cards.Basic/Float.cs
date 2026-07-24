using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Basic;

[Pool(typeof(HexaghostCardPool))]
public class Float : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Float()
		: base(0, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCards(1, 0);
		((ConstructedCardModel)this).WithKeyword(HexaghostKeyword.Advance, (UpgradeType)2);
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => (!e.IsUpgraded) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new global::_003C_003Ez__ReadOnlyArray<IHoverTip>((IHoverTip[])(object)new IHoverTip[2]
		{
			HoverTipFactory.FromKeyword(HexaghostKeyword.Retract),
			HoverTipFactory.FromKeyword(HexaghostKeyword.Advance)
		}))));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Draw((CardModel)(object)this, ctx);
		if (!((CardModel)this).IsUpgraded || ((CardModel)this).CombatState == null)
		{
			return;
		}
		List<HexaghostCardModel> list = new List<HexaghostCardModel>(2)
		{
			((CardModel)this).CombatState.CreateCard<FloatChoiceRetract>(((CardModel)this).Owner),
			((CardModel)this).CombatState.CreateCard<FloatChoiceAdvance>(((CardModel)this).Owner)
		};
		CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, true);
		if (!(val is FloatChoiceAdvance))
		{
			if (val is FloatChoiceRetract)
			{
				await HexaghostCmd.Retract(ctx, ((CardModel)this).Owner, (AbstractModel?)(object)this);
			}
		}
		else
		{
			await HexaghostCmd.Advance(ctx, ((CardModel)this).Owner, (AbstractModel?)(object)this);
		}
	}
}
