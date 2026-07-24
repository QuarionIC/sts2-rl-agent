using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Enchantments;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class EnchantCrown : ChampCardModel
{
	public EnchantCrown()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)(object)this).WithTip<Crowned>();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(DownfallCardSelectorPrefs.ApplySelectionPrompt, 1, 1);
		CardModel val2 = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)((EnchantmentModel)ModelDb.Enchantment<Crowned>()).CanEnchant, (AbstractModel)(object)this)).FirstOrDefault();
		if (val2 != null)
		{
			CardCmd.Enchant<Crowned>(val2, 1m);
		}
	}
}
