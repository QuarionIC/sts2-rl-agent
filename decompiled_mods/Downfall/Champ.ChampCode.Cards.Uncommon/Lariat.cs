using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class Lariat : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool HasEnergyCostX => true;

	public Lariat()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(5, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampKeyword.TriggerSkillBonus));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int amount = ((CardModel)this).ResolveEnergyXValue();
		for (int i = 0; i < amount; i++)
		{
			await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		}
		for (int i = 0; i < amount; i++)
		{
			await ((CardModel)this).Owner.ChampStance().SkillBonus(ctx);
		}
	}
}
