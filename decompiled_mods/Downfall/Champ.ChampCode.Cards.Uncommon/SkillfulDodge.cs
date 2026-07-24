using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Interfaces;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class SkillfulDodge : ChampCardModel, IDefensiveComboCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Magerblutooth>();

	public SkillfulDodge()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(4, 1);
		((ConstructedCardModel)this).WithPower<CounterPower>(4, 1);
		((ConstructedCardModel)this).WithVar("Increase", 3, 1);
	}

	public Task DefensiveComboEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		((DynamicVar)((CardModel)this).DynamicVars.Block).UpgradeValueBy((decimal)((CardModel)this).DynamicVars["Increase"].IntValue);
		DynamicVarSetExtensions.Power<CounterPower>(((CardModel)this).DynamicVars).UpgradeValueBy((decimal)((CardModel)this).DynamicVars["Increase"].IntValue);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.ApplySelf<CounterPower>(ctx, (CardModel)(object)this, false);
	}
}
