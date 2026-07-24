using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class Deathcoil : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Eudaimonia>();

	public Deathcoil()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<ManaburnPower>(8, 3);
		((ConstructedCardModel)(object)this).WithDrained(1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(((CardModel)this).Owner.Creature) : null);
			NCombatRoom instance2 = NCombatRoom.Instance;
			NCreature val2 = ((instance2 != null) ? instance2.GetCreatureNode(cardPlay.Target) : null);
			if (val != null && val2 != null)
			{
				Vector2 vfxSpawnPosition = val.VfxSpawnPosition;
				Vector2 vfxSpawnPosition2 = val2.VfxSpawnPosition;
				NHemokinesisEffect.Spawn(vfxSpawnPosition, vfxSpawnPosition2);
			}
			await CommonActions.Apply<ManaburnPower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
			await CommonActions.ApplySelf<DrainedPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
