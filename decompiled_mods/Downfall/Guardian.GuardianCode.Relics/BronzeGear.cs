using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Guardian.GuardianCode.Cards.Token;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class BronzeGear : GuardianRelicModel
{
	public override bool HasUponPickupEffect => true;

	public BronzeGear()
		: base((RelicRarity)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		WithTip(typeof(GearUp));
		WithTip(GuardianKeyword.Gem);
	}

	public override RelicModel GetUpgradeReplacement()
	{
		return (RelicModel)(object)ModelDb.Relic<GuardianGear>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await DownfallCardCmd.GiveCard<GearUp>(player, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<GearUp>?)null, (Player?)null);
			}
		}
	}

	public override async Task AfterObtained()
	{
		GemModel gemModel = ((RelicModel)this).Owner.RunState.Rng.CombatCardGeneration.NextItem<GemModel>(GuardianModelDb.AllGems.Where((GemModel e) => (int)e.Rarity == 2));
		CardModel val = ((gemModel != null) ? gemModel.ToCard.ToMutable() : null);
		if (val != null)
		{
			((ICardScope)((RelicModel)this).Owner.RunState).AddCard(val, ((RelicModel)this).Owner);
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(val, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		}
	}
}
