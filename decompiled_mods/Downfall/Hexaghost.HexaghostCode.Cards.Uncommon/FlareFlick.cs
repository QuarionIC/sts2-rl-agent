using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class FlareFlick : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public FlareFlick()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword(HexaghostKeyword.Advance, (UpgradeType)2);
		((ConstructedCardModel)this).WithDamage(10, 4);
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel c) => (!c.IsUpgraded) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new global::_003C_003Ez__ReadOnlyArray<IHoverTip>((IHoverTip[])(object)new IHoverTip[2]
		{
			HoverTipFactory.FromKeyword(HexaghostKeyword.Advance),
			HoverTipFactory.FromKeyword(HexaghostKeyword.Retract)
		}))));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target == null)
		{
			return;
		}
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await HexaghostCmd.Ignite(ctx, ((CardModel)this).Owner);
		if (!((CardModel)this).IsUpgraded || !cardPlay.Target.IsAlive)
		{
			return;
		}
		List<FlareFlickChoice> list = ((IEnumerable<CardKeyword>)(object)new CardKeyword[2]
		{
			(CardKeyword)(int)HexaghostKeyword.Retract,
			(CardKeyword)(int)HexaghostKeyword.Advance
		}).Select((CardKeyword f) => FlareFlickChoice.Create(f, ((CardModel)this).Owner)).ToList();
		if (await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, true) is FlareFlickChoice { Keyword: var keyword })
		{
			if (keyword == HexaghostKeyword.Advance)
			{
				await HexaghostCmd.Advance(ctx, ((CardModel)this).Owner, (AbstractModel?)(object)this);
			}
			else if (keyword == HexaghostKeyword.Retract)
			{
				await HexaghostCmd.Retract(ctx, ((CardModel)this).Owner, (AbstractModel?)(object)this);
			}
		}
	}
}
