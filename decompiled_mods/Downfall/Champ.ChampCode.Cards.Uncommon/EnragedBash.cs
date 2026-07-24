using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Interfaces;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class EnragedBash : ChampCardModel, IBerserkerComboCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public EnragedBash()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 3);
		((ConstructedCardModel)(object)this).WithRepeat(1);
	}

	public Task BerserkerComboEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		((DynamicVar)((CardModel)this).DynamicVars.Repeat).UpgradeValueBy(1m);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
