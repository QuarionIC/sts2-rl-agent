using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class HyperBeamAutomaton : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public HyperBeamAutomaton()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(18, 4);
		((ConstructedCardModel)this).WithPower<VulnerablePower>(1, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Stash));
		((ConstructedCardModel)(object)this).WithTip<Void>();
		((ConstructedCardModel)this).WithCards(3, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithAttackerAnim("Cast", 0.5f, (Creature)null).BeforeDamage((Func<Task>)BeforeDamageAction)
			.Execute(ctx);
		await CommonActions.Apply<VulnerablePower>(ctx, (CardModel)(object)this, cardPlay, false);
		await StashCmd.Stash<Void>(((CardModel)this).Owner, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue);
	}

	private async Task BeforeDamageAction()
	{
		ICombatState combatState = ((CardModel)this).CombatState;
		List<Creature> enemies = ((combatState != null) ? combatState.Enemies.Where((Creature e) => e.IsAlive).ToList() : null);
		if (enemies != null)
		{
			NHyperbeamVfx val = NHyperbeamVfx.Create(((CardModel)this).Owner.Creature, enemies.Last());
			if (val != null)
			{
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
				}
			}
		}
		await Cmd.Wait(0.5f, false);
		if (enemies == null)
		{
			return;
		}
		foreach (NHyperbeamImpactVfx item in enemies.Select((Creature enemy) => NHyperbeamImpactVfx.Create(((CardModel)this).Owner.Creature, enemy)).OfType<NHyperbeamImpactVfx>())
		{
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)item);
			}
		}
	}
}
