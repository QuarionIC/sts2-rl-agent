using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Cards.Basic;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class TripleStrike : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public TripleStrike()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithTip(new TooltipSource((Func<CardModel, IHoverTip>)StrikeTip));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(DownfallKeyword.Echo));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampKeyword.TriggerSkillBonus));
		((ConstructedCardModel)this).WithCards(2, 0);
	}

	private static IHoverTip StrikeTip(CardModel card)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = ((CardModel)ModelDb.Card<StrikeChamp>()).ToMutable();
		if (card.IsUpgraded)
		{
			val.UpgradeInternal();
		}
		val.EnergyCost.SetThisCombat(0, false);
		val.AddKeyword(ChampKeyword.TriggerSkillBonus);
		val.ToEcho();
		return HoverTipFactory.FromCard(val, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		List<CardModel> list = new List<CardModel>();
		StrikeChamp strikeChamp = ModelDb.Card<StrikeChamp>();
		for (int i = 0; i < ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue; i++)
		{
			CardModel val = ((CardModel)this).CombatState.CreateCard((CardModel)(object)strikeChamp, ((CardModel)this).Owner);
			val.AddKeyword(ChampKeyword.TriggerSkillBonus);
			val.ToEcho();
			if (((CardModel)this).IsUpgraded)
			{
				val.UpgradeInternal();
			}
			val.EnergyCost.SetThisCombat(0, false);
			list.Add(val);
		}
		await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
	}
}
