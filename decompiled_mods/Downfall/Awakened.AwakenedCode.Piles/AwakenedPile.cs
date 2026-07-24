using System.Collections.Generic;
using System.Linq;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace Awakened.AwakenedCode.Piles;

public class AwakenedPile : CustomPile
{
	[CustomEnum(null)]
	public static PileType Spellbook;

	private readonly List<CardModel> _dynamicTypes = new List<CardModel>();

	public CardModel? NextSpell { get; private set; }

	public AwakenedPile()
		: base(Spellbook)
	{
	}//IL_000c: Unknown result type (might be due to invalid IL or missing references)


	public void AddPersistentType(CardModel type)
	{
		_dynamicTypes.Add(type.CanonicalInstance);
	}

	public override bool CardShouldBeVisible(CardModel card)
	{
		return false;
	}

	public override Vector2 GetTargetPosition(CardModel model, Vector2 size)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature obj = ((instance != null) ? instance.GetCreatureNode(model.Owner.Creature) : null);
		if (obj == null)
		{
			return Vector2.Zero;
		}
		return ((Control)obj).GlobalPosition;
	}

	public void SetNextSpell(Rng rng)
	{
		List<CardModel> list = ((CardPile)this).Cards.Where((CardModel c) => c != NextSpell).ToList();
		NextSpell = ((list.Count > 0) ? rng.NextItem<CardModel>((IEnumerable<CardModel>)list) : ((((CardPile)this).Cards.Count > 0) ? ((CardPile)this).Cards[0] : null));
	}

	public void Refresh(Player owner)
	{
		ICombatState combatState = owner.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}
		Rng combatCardGeneration = combatState.RunState.Rng.CombatCardGeneration;
		foreach (CardModel item in ((CardPile)this).Cards.ToList())
		{
			item.RemoveFromState();
		}
		AddBaseSpells(owner, combatState);
		foreach (CardModel dynamicType in _dynamicTypes)
		{
			CreateAndAddSpell(owner, combatState, dynamicType);
		}
		SetNextSpell(combatCardGeneration);
	}

	private void AddBaseSpells(Player owner, ICombatState state)
	{
		CardModel[] original = (CardModel[])(object)new CardModel[4]
		{
			(CardModel)ModelDb.Card<BurningStudy>(),
			(CardModel)ModelDb.Card<Cryostasis>(),
			(CardModel)ModelDb.Card<Darkleech>(),
			(CardModel)ModelDb.Card<Thunderbolt>()
		};
		foreach (CardModel item in AwakenedHook.ModifyBaseSpells(state, owner, original))
		{
			CreateAndAddSpell(owner, state, item);
		}
	}

	private void CreateAndAddSpell(Player owner, ICombatState state, CardModel canonical)
	{
		CardModel val = state.CreateCard(canonical, owner);
		if (AwakenedModel.IsAwakened(owner) && val.IsUpgradable)
		{
			val.UpgradeInternal();
			val.FinalizeUpgradeInternal();
		}
		((CardPile)this).AddInternal(val, -1, false);
	}
}
