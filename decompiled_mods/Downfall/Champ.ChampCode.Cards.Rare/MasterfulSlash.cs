using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class MasterfulSlash : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public MasterfulSlash()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(9, 3);
		((ConstructedCardModel)(object)this).WithTip<VigorPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = (await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx)).Results.SelectMany((List<DamageResult> r) => r).Sum((DamageResult r) => r.UnblockedDamage);
		await CommonActions.ApplySelf<VigorNextTurnPower>(ctx, (CardModel)(object)this, (decimal)num, false);
	}
}
