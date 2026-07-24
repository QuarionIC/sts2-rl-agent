using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Basic;

[Pool(typeof(AutomatonCardPool))]
public class Branch : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Branch()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithBlock(6, 2);
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			BranchAttack attackOption = ((CardModel)this).CombatState.CreateCard<BranchAttack>(cardPlay.Card.Owner);
			BranchBlock blockOption = ((CardModel)this).CombatState.CreateCard<BranchBlock>(cardPlay.Card.Owner);
			if (((CardModel)this).IsUpgraded)
			{
				CardCmd.Upgrade((CardModel)(object)attackOption, (CardPreviewStyle)1);
				CardCmd.Upgrade((CardModel)(object)blockOption, (CardPreviewStyle)1);
			}
			if ((object)(await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[2]
			{
				(CardModel)attackOption,
				(CardModel)blockOption
			}), ((CardModel)this).Owner, false)) == attackOption)
			{
				await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
				await AutomatonCmd.EncodeCard((CardModel)(object)blockOption, ctx);
			}
			else
			{
				await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
				await AutomatonCmd.EncodeCard((CardModel)(object)attackOption, ctx);
			}
		}
	}
}
