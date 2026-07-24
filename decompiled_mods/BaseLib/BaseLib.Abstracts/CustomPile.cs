using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Abstracts;

public abstract class CustomPile : CardPile
{
	public virtual string? IconPath => null;

	public virtual LocString? Name => null;

	public virtual bool NeedsCustomTransitionVisual => false;

	public CustomPile(PileType pileType)
		: base(pileType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)


	public abstract bool CardShouldBeVisible(CardModel card);

	public abstract Vector2 GetTargetPosition(CardModel model, Vector2 size);

	public virtual NCard? GetNCard(CardModel card)
	{
		return null;
	}

	public virtual bool CustomTween(Tween tween, CardModel card, NCard cardNode, CardPile oldPile)
	{
		return false;
	}
}
