using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Cards.Token;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Multiplayer;

[Pool(typeof(SneckoCardPool))]
public class DiceCase : SneckoCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public DiceCase()
		: base(3, (CardType)3, (CardRarity)3, (TargetType)6)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)2);
		((ConstructedCardModel)(object)this).WithPower<DiceCasePower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<SoulRoll>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		PowerModel val = ((PowerModel)ModelDb.Power<DiceCasePower>()).ToMutable(0);
		if (val is DiceCasePower diceCasePower)
		{
			diceCasePower.TargetCreature = cardPlay.Target;
		}
		await PowerCmd.Apply(ctx, val, ((CardModel)this).Owner.Creature, DynamicVarSetExtensions.Power<DiceCasePower>(((CardModel)this).DynamicVars).BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
	}
}
