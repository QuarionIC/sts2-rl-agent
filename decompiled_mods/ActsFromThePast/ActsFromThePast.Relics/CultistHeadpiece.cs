using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class CultistHeadpiece : CustomRelicModel
{
	private static readonly LocString CultistBanter = new LocString("monsters", "DAMP_CULTIST.moves.INCANTATION.banter");

	private static readonly string[] CultistSfx = new string[2] { "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_damp", "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_calcified" };

	public override RelicRarity Rarity => (RelicRarity)6;

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((RelicModel)this).Owner && ((RelicModel)this).Owner.Creature.CombatState.RoundNumber == 1)
		{
			TalkCmd.Play(CultistBanter, ((RelicModel)this).Owner.Creature, ((RelicModel)this).Owner.Character.SpeechBubbleColor, (VfxDuration)6);
			string sfx = CultistSfx[Rng.Chaotic.NextInt(CultistSfx.Length)];
			SfxCmd.Play(sfx, 1f);
		}
	}

	public CultistHeadpiece()
		: base(true)
	{
	}
}
