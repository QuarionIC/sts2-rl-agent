using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Common;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class Format : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	protected override bool HasEnergyCostX => true;

	public Format()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
		((ConstructedCardModel)(object)this).WithUpgradedCardTip<Fragment>((Action<Fragment, CardModel>?)null);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int x = ((CardModel)this).ResolveEnergyXValue();
		if (((CardModel)this).IsUpgraded)
		{
			x++;
		}
		for (int i = 0; i < x; i++)
		{
			Fragment fragment = ((CardModel)this).Owner.Creature.CombatState.CreateCard<Fragment>(((CardModel)this).Owner);
			((CardModel)fragment).UpgradeInternal();
			await AutomatonCmd.EncodeCard((CardModel)(object)fragment, ctx);
		}
		await PlayerCmd.GainEnergy(1m, ((CardModel)this).Owner);
	}
}
