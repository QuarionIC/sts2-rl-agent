using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class DownfallCardModel : ConstructedCardModel
{
	protected virtual Artist? Artist => null;

	protected DownfallCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => (!(e is DownfallCardModel { Artist: not null } downfallCardModel)) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(downfallCardModel.Artist.HoverTip))));
	}

	protected virtual Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	protected sealed override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (!(await CardExecutionRegistry.BeforeOnPlayInternal((CardModel)(object)this, ctx, cardPlay)))
		{
			await OnPlayInternal(ctx, cardPlay);
			await CardExecutionRegistry.AfterOnPlayInternal((CardModel)(object)this, ctx, cardPlay);
		}
	}
}
public abstract class DownfallCardModel<T> : DownfallCardModel where T : DownfallCharacterModel
{
	public override string CustomPortraitPath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".tres").CardImageAtlasPath<T>();

	protected DownfallCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_0003: Unknown result type (might be due to invalid IL or missing references)
	//IL_0004: Unknown result type (might be due to invalid IL or missing references)

}
