using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Ancient;

[Pool(typeof(ChampCardPool))]
public class Execution : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public Execution()
		: base(2, (CardType)1, (CardRarity)5, (TargetType)2)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)(object)this).WithRepeat(4);
		((ConstructedCardModel)(object)this).WithFinisher();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 4, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public override async Task FinisherEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.PlayFinisher(ctx, cardPlay, skipClear: true, 2);
	}
}
