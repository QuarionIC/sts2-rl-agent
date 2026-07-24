using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Afflictions;

public sealed class EntangledOriginal : AfflictionModel
{
	private bool _wasAlreadyUnplayable;

	private bool WasAlreadyUnplayable
	{
		get
		{
			return _wasAlreadyUnplayable;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_wasAlreadyUnplayable = value;
		}
	}

	public override bool CanAfflictCardType(CardType cardType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		return (int)cardType == 1;
	}

	public override void AfterApplied()
	{
		WasAlreadyUnplayable = ((AfflictionModel)this).Card.Keywords.Contains((CardKeyword)4);
		if (!WasAlreadyUnplayable)
		{
			((AfflictionModel)this).Card.AddKeyword((CardKeyword)4);
		}
	}

	public override void BeforeRemoved()
	{
		if (!WasAlreadyUnplayable)
		{
			((AfflictionModel)this).Card.RemoveKeyword((CardKeyword)4);
		}
	}
}
