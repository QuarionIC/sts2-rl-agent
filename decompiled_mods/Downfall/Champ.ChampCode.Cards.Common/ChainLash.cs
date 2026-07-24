using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class ChainLash : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public ChainLash()
		: base(0, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(3, 2);
		((ConstructedCardModel)(object)this).WithPower<ChainLashPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampKeyword.TriggerSkillBonus));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.ApplySelf<ChainLashPower>(ctx, (CardModel)(object)this, false);
	}
}
