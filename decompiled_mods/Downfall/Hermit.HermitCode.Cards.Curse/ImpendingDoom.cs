using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.CustomEnums;
using Godot;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Cards.Curse;

[Pool(typeof(CurseCardPool))]
public sealed class ImpendingDoom : HermitCardModel, IHasDeadOnEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public override int MaxUpgradeLevel => 0;

	protected override bool ShouldGlowGoldInternal => false;

	protected override bool ShouldGlowRedInternal => ((IHasDeadOnEffect)this)?.IsDeadOn ?? false;

	public override bool HasTurnEndInHandEffect => ((IHasDeadOnEffect)this)?.IsDeadOn ?? false;

	public override bool CanBeGeneratedByModifiers => false;

	private static bool IsMultiplayer
	{
		get
		{
			RunState obj = RunManager.Instance.DebugOnlyGetState();
			return ((obj == null) ? 1 : obj.Players.Count) > 1;
		}
	}

	public ImpendingDoom()
		: base(-2, (CardType)5, (CardRarity)9, DownfallTargetType.MeAndEnemies)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		((ConstructedCardModel)this).WithVar((DynamicVar)new DamageVar(13m, (ValueProp)12));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)4, (UpgradeType)0);
	}

	public async Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		foreach (Creature item in ((CardModel)this).CombatState.Creatures.Where((Creature e) => e != null && e.IsAlive && !e.IsPet))
		{
			NFireBurstVfx val = NFireBurstVfx.Create(item, 0.75f);
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
			}
		}
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}

	protected override async Task OnTurnEndInHand(PlayerChoiceContext ctx)
	{
		ImpendingDoom card = this;
		ResourceInfo resources = default(ResourceInfo);
		((ResourceInfo)(ref resources)).set_EnergySpent(0);
		((ResourceInfo)(ref resources)).set_EnergyValue(0);
		((ResourceInfo)(ref resources)).set_StarsSpent(0);
		((ResourceInfo)(ref resources)).set_StarValue(0);
		CardPlay cardPlay = CardPlayCompat.Create((CardModel)(object)card, null, (PileType)3, resources, isAutoPlay: true, 0, 1);
		await HermitCmd.TriggerDeadOnEffect(ctx, (CardModel)(object)this, cardPlay);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("Multiplayer", IsMultiplayer);
	}
}
