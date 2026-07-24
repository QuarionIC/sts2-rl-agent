using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace Hermit.HermitCode.Cards.Curse;

[Pool(typeof(CurseCardPool))]
public sealed class MementoCard : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public override int MaxUpgradeLevel => 0;

	public override bool CanBeGeneratedByModifiers => false;

	private static bool IsMultiplayer
	{
		get
		{
			RunState obj = RunManager.Instance.DebugOnlyGetState();
			return ((obj == null) ? 1 : obj.Players.Count) > 1;
		}
	}

	public MementoCard()
		: base(0, (CardType)5, (CardRarity)9, DownfallTargetType.MeAndEnemies)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<VulnerablePower>(1, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.Apply<VulnerablePower>(ctx, (CardModel)(object)this, play, false);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("Multiplayer", IsMultiplayer);
	}
}
