using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.Cards.Basic;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Extensions;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Core;

public abstract class GemModel : CardModifier, ICustomModel
{
	private GemModel _canonicalInstance;

	private PowerModel? _power;

	public PowerModel Power
	{
		set
		{
			if (value == _power)
			{
				throw new Exception($"Power already initialized for {((AbstractModel)this).Id}");
			}
			((AbstractModel)this).AssertMutable();
			((AbstractModel)value).AssertMutable();
			_power = value;
		}
	}

	public int SocketIndex
	{
		get
		{
			if (!(Card is IGemSocketCard gemSocketCard))
			{
				return -1;
			}
			return ListExtensions.IndexOf<GemModel>(gemSocketCard.Gems, this);
		}
	}

	protected ICombatState CombatState
	{
		get
		{
			object obj = Card.CombatState;
			if (obj == null)
			{
				PowerModel? power = _power;
				obj = ((power != null) ? power.CombatState : null) ?? throw new InvalidOperationException($"Gem {((AbstractModel)this).Id} has no CombatState!");
			}
			return (ICombatState)obj;
		}
	}

	protected Player Player => Card.Owner;

	public override bool ShouldReceiveCombatHooks => true;

	private string IconName => StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant();

	public CardModel? Card
	{
		get
		{
			if (!((AbstractModel)this).IsCanonical)
			{
				return ((CardModifier)this).Owner;
			}
			return null;
		}
	}

	public GemModel CanonicalInstance
	{
		get
		{
			if (((AbstractModel)this).IsMutable)
			{
				return _canonicalInstance;
			}
			return this;
		}
		private set
		{
			((AbstractModel)this).AssertMutable();
			_canonicalInstance = value;
		}
	}

	public IEnumerable<IHoverTip> HoverTips
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			List<IHoverTip> list = new List<IHoverTip>();
			list.Add((IHoverTip)(object)ToHoverTip(GetFormattedText()));
			list.AddRange(ExtraHoverTips);
			return list;
		}
	}

	public string IconPath => (IconName + ".png").GemPath();

	private static string EmptyIconPath => "emptysocket.png".GemPath();

	public Texture2D Icon => PreloadManager.Cache.GetAsset<Texture2D>(IconPath);

	public static Texture2D EmptyIcon => PreloadManager.Cache.GetAsset<Texture2D>(EmptyIconPath);

	public abstract Color GemColor { get; }

	public abstract CardRarity Rarity { get; }

	public LocString Title => new LocString("gems", ((AbstractModel)this).Id.Entry + ".title");

	private LocString Description => new LocString("gems", ((AbstractModel)this).Id.Entry + ".description");

	public CardModel ToCard => (from c in ((CardPoolModel)ModelDb.CardPool<GuardianCardPool>()).AllCards.OfType<IGemCard>()
		where ((object)c.CanonicalGemModel).GetType() == ((object)this).GetType()
		select c).Cast<CardModel>().First();

	public virtual IEnumerable<IHoverTip> ExtraHoverTips => Array.Empty<IHoverTip>();

	public override void ModifyDescription(Creature? target, ref string description)
	{
	}

	public string GetFormattedText(bool cardText = false)
	{
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Invalid comparison between Unknown and I4
		StringBuilder stringBuilder = new StringBuilder();
		string formattedText;
		if (((AbstractModel)this).IsMutable)
		{
			LocString description = Description;
			description.Add("CardName", Card.Title);
			AddDumbVariablesToDescription(description);
			using (IEnumerator<DynamicVar> enumerator = ((CardModifier)this).DynamicVars.Values.GetEnumerator())
			{
				DynamicVar current;
				bool flag;
				bool flag2;
				for (; enumerator.MoveNext(); flag2 = flag, current.UpdateCardPreview(Card, (CardPreviewMode)1, (Creature)null, flag2))
				{
					current = enumerator.Current;
					CardModel card = Card;
					if (card != null && card.CombatState != null)
					{
						CardPile pile = card.Pile;
						if (pile != null)
						{
							PileType type = pile.Type;
							if ((int)type == 2 || (int)type == 5)
							{
								flag = true;
								continue;
							}
						}
					}
					flag = false;
				}
			}
			((CardModifier)this).DynamicVars.AddTo(description);
			formattedText = description.GetFormattedText();
		}
		else
		{
			LocString description2 = Description;
			AddDumbVariablesToDescription(description2);
			((DynamicVarSet)(((object)base._dynamicVars) ?? ((object)new DynamicVarSet(((CardModifier)this).CanonicalVars)))).AddTo(description2);
			formattedText = description2.GetFormattedText();
		}
		if (!formattedText.Equals(""))
		{
			stringBuilder.Append(formattedText);
		}
		return stringBuilder.ToString();
	}

	public GemModel ToMutable()
	{
		((AbstractModel)this).AssertCanonical();
		GemModel obj = (GemModel)(object)((AbstractModel)this).MutableClone();
		obj.CanonicalInstance = this;
		return obj;
	}

	public GemModel CreateClone()
	{
		((AbstractModel)this).AssertMutable();
		return (GemModel)(object)((AbstractModel)this).MutableClone();
	}

	private HoverTip ToHoverTip(string description)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		HoverTip result = default(HoverTip);
		((HoverTip)(ref result)).CanonicalModel = null;
		((HoverTip)(ref result)).ShouldOverrideTextOverflow = false;
		((HoverTip)(ref result)).Id = ((object)((AbstractModel)this).Id).ToString();
		((HoverTip)(ref result)).Title = Title.GetFormattedText();
		((HoverTip)(ref result)).Description = description;
		((HoverTip)(ref result)).Icon = Icon;
		((HoverTip)(ref result)).IsSmart = true;
		return result;
	}

	private static void AddDumbVariablesToDescription(LocString description)
	{
		description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix((AbstractModel)(object)ModelDb.Card<StrikeGuardian>()));
	}

	protected abstract Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay? cardPlay);

	public sealed override async Task OnPlay(PlayerChoiceContext ctx, CardPlay? cardPlay)
	{
		GuardianMainFile.Logger.Info("Played Gem : " + ((AbstractModel)this).Id.Entry, 1);
		int replay = ((!(((cardPlay != null) ? cardPlay.Card : null) is IGemSocketCard gemSocketCard)) ? 1 : gemSocketCard.GemReplayCount);
		for (int i = 0; i < replay; i++)
		{
			await OnPlayInternal(ctx, cardPlay);
		}
		await GuardianHook.AfterGemPlayed(CombatState, ctx, this, cardPlay);
	}

	public virtual int ModifyPlayCount(int originalPlayCount)
	{
		return originalPlayCount;
	}
}
