using System;
using System.Threading.Tasks;
using BaseLib.Patches.Localization;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Cards.Token;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class DiceCasePower : SneckoPowerModel, IAddDumbVariablesToPowerDescription, IInstancedPerTarget
{
	public Creature? TargetCreature { get; set; }

	public override PowerInstanceType InstanceType => CustomPowerInstanceType.InstancedPerTarget;

	public DiceCasePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<SoulRoll>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext crx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			Creature? targetCreature = TargetCreature;
			Player val = ((targetCreature != null) ? targetCreature.Player : null);
			if (val != null)
			{
				await DownfallCardCmd.GiveCards<SoulRoll>(val, (PileType)2, (decimal)((PowerModel)this).Amount, (CardPilePosition)2, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<SoulRoll>?)null, ((PowerModel)this).Owner.Player);
			}
		}
	}

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		Creature targetCreature = TargetCreature;
		if (targetCreature != null)
		{
			if (targetCreature.IsMonster && targetCreature.Monster != null)
			{
				description.Add("OtherName", targetCreature.Monster.Title);
				return;
			}
			if (targetCreature.Player != null)
			{
				description.Add("OtherName", PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, targetCreature.Player.NetId));
				return;
			}
		}
		description.Add("OtherName", "???");
	}
}
