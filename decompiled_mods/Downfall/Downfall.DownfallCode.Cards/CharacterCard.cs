using System;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using Downfall.DownfallCode.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Random;

namespace Downfall.DownfallCode.Cards;

[Pool(typeof(TokenCardPool))]
public class CharacterCard : ConstructedCardModel, IModfyCardDescription, ICustomPortrait
{
	private ImageTexture? _cachedTexture;

	internal CharacterModel? CharacterModel;

	public CardModel? RandomCommonCard;

	public CardModel? RandomRareCard;

	public CardModel? RandomUncommonCard;

	protected override bool IsPlayable => false;

	public override string Title
	{
		get
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			if (CharacterModel != null)
			{
				return new LocString("characters", CharacterModel.CharacterSelectTitle).GetFormattedText();
			}
			return "???";
		}
	}

	public CharacterCard()
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1, true, true)
	{
	}

	public Texture2D? GetPortraitTexture()
	{
		if (_cachedTexture != null)
		{
			return (Texture2D?)(object)_cachedTexture;
		}
		Texture2D[] array = new Texture2D[3];
		CardModel? randomCommonCard = RandomCommonCard;
		array[0] = ((randomCommonCard != null) ? randomCommonCard.Portrait : null);
		CardModel? randomUncommonCard = RandomUncommonCard;
		array[1] = ((randomUncommonCard != null) ? randomUncommonCard.Portrait : null);
		CardModel? randomRareCard = RandomRareCard;
		array[2] = ((randomRareCard != null) ? randomRareCard.Portrait : null);
		_cachedTexture = PortraitCompositor.SliceHorizontally(new global::_003C_003Ez__ReadOnlyArray<Texture2D>((Texture2D[])(object)array));
		return (Texture2D?)(object)_cachedTexture;
	}

	public LocString ModifyDescription(LocString oldLocString)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		if (CharacterModel == null)
		{
			return oldLocString;
		}
		string characterSelectDesc = CharacterModel.CharacterSelectDesc;
		if (!string.IsNullOrEmpty(characterSelectDesc))
		{
			return new LocString("characters", characterSelectDesc);
		}
		return oldLocString;
	}

	public static CharacterCard Create(CharacterModel characterModel)
	{
		CharacterCard obj = (((CardModel)ModelDb.Card<CharacterCard>()).ToMutable() as CharacterCard) ?? throw new Exception("CharacterCard model is not a CharacterCard");
		obj.CharacterModel = characterModel;
		((CardModel)obj)._pool = characterModel.CardPool;
		obj.RandomCommonCard = Rng.Chaotic.NextItem<CardModel>(characterModel.CardPool.AllCards.Where((CardModel e) => (int)e.Rarity == 2));
		obj.RandomUncommonCard = Rng.Chaotic.NextItem<CardModel>(characterModel.CardPool.AllCards.Where((CardModel e) => (int)e.Rarity == 3));
		obj.RandomRareCard = Rng.Chaotic.NextItem<CardModel>(characterModel.CardPool.AllCards.Where((CardModel e) => (int)e.Rarity == 4));
		NCard obj2 = NCard.FindOnTable((CardModel)(object)obj, (PileType?)null);
		if (obj2 != null)
		{
			obj2.Reload();
			return obj;
		}
		return obj;
	}
}
