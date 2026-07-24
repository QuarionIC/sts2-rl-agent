using System;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public interface ITomeCard
{
	CharacterModel TomeCharacter
	{
		get
		{
			CardModel card = (CardModel)((this is CardModel) ? this : null);
			if (card != null)
			{
				CharacterModel val = ModelDb.AllCharacters.FirstOrDefault((Func<CharacterModel, bool>)((CharacterModel c) => c.CardPool.AllCardIds.Contains(((AbstractModel)card).Id)));
				if (val != null)
				{
					return val;
				}
			}
			throw new InvalidOperationException("Default implementation of TomeCharacter in ITomeCard failed; override it manually.");
		}
	}
}
