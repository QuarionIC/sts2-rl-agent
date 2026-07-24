using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Interfaces;
using Downfall.DownfallCode.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Automaton.AutomatonCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class FunctionCard : CustomCardModel, ICustomPortrait
{
	private IReadOnlyList<CardModel> _cachedSourceCards = Array.Empty<CardModel>();

	private ImageTexture? _cachedTexture;

	private string _dynamicTitle = string.Empty;

	private IReadOnlyList<CardModel> _sourceCards = Array.Empty<CardModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => Encodable.All.Select((Encodable e) => e.FunctionDynamicVar);

	protected override IEnumerable<IHoverTip> ExtraHoverTips => Encodable.All.SelectMany((Encodable e) => (!(e.DynamicVar((AbstractModel)(object)this).BaseValue > 0m)) ? Array.Empty<IHoverTip>() : e.HoverTips((AbstractModel)(object)this));

	public override int MaxUpgradeLevel => 0;

	public override bool CanBeGeneratedInCombat => false;

	public override bool CanBeGeneratedByModifiers => false;

	public override bool GainsBlock => ((DynamicVar)((CardModel)this).DynamicVars.Block).BaseValue > 0m;

	public override TargetType TargetType => CalcTarget();

	public override CardType Type => CalcType();

	public override string CustomPortraitPath => "function_card.tres".CardImageAtlasPath<Automaton.AutomatonCode.Core.Automaton>();

	public override string Title
	{
		get
		{
			if (!_dynamicTitle.Equals(string.Empty))
			{
				return _dynamicTitle;
			}
			return ((CardModel)this).Title;
		}
	}

	public FunctionCard()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)2, true, true)
	{
	}

	public Texture2D? GetPortraitTexture()
	{
		return (Texture2D?)(object)GetTexture();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		foreach (Encodable item in Encodable.All)
		{
			if (item.DynamicVar((AbstractModel)(object)this).BaseValue > 0m)
			{
				await item.OnPlay((AbstractModel)(object)this, ctx, cardPlay.Target, cardPlay);
			}
		}
	}

	private CardType CalcType()
	{
		List<CardType> list = (from e in Encodable.All
			where e.DynamicVar((AbstractModel)(object)this).BaseValue > 0m
			select e.Type).Distinct().ToList();
		if (!list.Contains((CardType)3))
		{
			if (!list.Contains((CardType)1))
			{
				if (!list.Contains((CardType)2))
				{
					return (CardType)0;
				}
				return (CardType)2;
			}
			return (CardType)1;
		}
		return (CardType)3;
	}

	private TargetType CalcTarget()
	{
		List<TargetType> list = (from e in Encodable.All
			where e.DynamicVar((AbstractModel)(object)this).BaseValue > 0m
			select e.Target).Distinct().ToList();
		if (!list.Contains((TargetType)2))
		{
			if (!list.Contains((TargetType)3))
			{
				if (!list.Contains((TargetType)1))
				{
					return (TargetType)0;
				}
				return (TargetType)1;
			}
			return (TargetType)3;
		}
		return (TargetType)2;
	}

	public void SetSourceCards(IReadOnlyList<CardModel> sourceCards)
	{
		_sourceCards = sourceCards.ToList();
		foreach (DynamicVar canonicalVar in ((CardModel)this).CanonicalVars)
		{
			canonicalVar.BaseValue = 0m;
		}
		if (sourceCards.Count <= 0)
		{
			return;
		}
		_dynamicTitle = GetDynamicTitle(_sourceCards);
		int max = AutomatonCmd.GetMax(_sourceCards[0].Owner);
		int num = 1;
		foreach (CardModel sourceCard in _sourceCards)
		{
			FunctionPosition position = ((num != 1) ? ((num != max) ? FunctionPosition.Middle : FunctionPosition.End) : FunctionPosition.Start);
			if (!(sourceCard is IEncodable encodable))
			{
				continue;
			}
			encodable.ApplyEncode(this, position);
			foreach (Encodable encoding in encodable.Encodings)
			{
				encoding.ApplyEncode(this, sourceCard);
			}
			num++;
		}
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		List<string> source = (from encodable in Encodable.All
			where encodable.DynamicVar((AbstractModel)(object)this).BaseValue > 0m
			select encodable.GetDescription((AbstractModel)(object)this).GetFormattedText()).ToList();
		description.Add("effects", string.Join("\n", source.Where((string l) => !string.IsNullOrWhiteSpace(l))));
	}

	private string GetDynamicTitle(IReadOnlyList<CardModel> sourceCards)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		if (sourceCards.Count == 0)
		{
			return new LocString("cards", ((AbstractModel)this).Id.Entry + ".title").GetFormattedText();
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < sourceCards.Count; i++)
		{
			CardModel val = sourceCards[i];
			switch (i)
			{
			case 0:
			{
				LocString val3 = new LocString("encode", ((AbstractModel)val).Id.Entry + ".functionPrefix");
				stringBuilder.Append(val3.Exists() ? val3.GetFormattedText() : "");
				break;
			}
			case 1:
			{
				LocString val2 = new LocString("encode", ((AbstractModel)val).Id.Entry + ".functionName");
				stringBuilder.Append(val2.Exists() ? val2.GetFormattedText() : "");
				break;
			}
			case 2:
			case 3:
				stringBuilder.Append(val.Title[0]);
				break;
			}
		}
		stringBuilder.Append("()");
		return stringBuilder.ToString();
	}

	private ImageTexture? GetTexture()
	{
		if (_cachedTexture != null && _cachedSourceCards.SequenceEqual(_sourceCards))
		{
			return _cachedTexture;
		}
		ImageTexture val = PortraitCompositor.SliceHorizontally(_sourceCards.Select((CardModel c) => ResourceLoader.Load<Texture2D>(c.PortraitPath, (string)null, (CacheMode)1)).ToList());
		if (val == null)
		{
			return null;
		}
		_cachedTexture = val;
		_cachedSourceCards = _sourceCards;
		return _cachedTexture;
	}
}
