using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class FierceBash : GuardianCardModel, ITickCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public FierceBash()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(18, 4);
		((ConstructedCardModel)this).WithVar("Increase", 2, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Tick));
	}

	public Task OnTick(PlayerChoiceContext ctx)
	{
		((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy((decimal)((CardModel)this).DynamicVars["Increase"].IntValue);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await GuardianCmd.PutIntoStasis((CardModel)(object)this, ctx, (AbstractModel)(object)this);
	}
}
