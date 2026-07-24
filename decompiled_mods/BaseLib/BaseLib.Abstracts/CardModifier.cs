using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Patches.Localization;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Abstracts;

public abstract class CardModifier : AbstractModel, IComparable<CardModifier>
{
	public sealed class ModifierSave : IPacketSerializable
	{
		public ModelId? Id { get; set; }

		public int Amount { get; set; }

		public Dictionary<string, int> IntProperties { get; set; } = new Dictionary<string, int>();

		public Dictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();

		public static ModifierSave FromModifier(CardModifier modifier)
		{
			ModifierSave modifierSave = new ModifierSave
			{
				Id = ((AbstractModel)modifier).Id,
				Amount = modifier.Amount
			};
			modifier.StoreSaveData(modifierSave);
			return modifierSave;
		}

		public CardModifier ToRealMod(CardModel owner)
		{
			CardModifier obj = (CardModifier)(object)((AbstractModel)ModelDb.GetById<CardModifier>(Id)).MutableClone();
			obj.Owner = owner;
			obj.Amount = Amount;
			obj.LoadSaveData(this);
			return obj;
		}

		public void Serialize(PacketWriter writer)
		{
			PacketWriterExtensions.WriteModelEntry(writer, Id);
			writer.WriteInt(Amount, 32);
			writer.WriteInt(IntProperties.Count, 32);
			foreach (KeyValuePair<string, int> intProperty in IntProperties)
			{
				writer.WriteString(intProperty.Key);
				writer.WriteInt(intProperty.Value, 32);
			}
			writer.WriteInt(AdditionalProperties.Count, 32);
			foreach (KeyValuePair<string, string> additionalProperty in AdditionalProperties)
			{
				writer.WriteString(additionalProperty.Key);
				writer.WriteString(additionalProperty.Value);
			}
		}

		public void Deserialize(PacketReader reader)
		{
			Id = PacketReaderExtensions.ReadModelIdAssumingType<CardModifier>(reader);
			Amount = reader.ReadInt(32);
			int num = reader.ReadInt(32);
			IntProperties = new Dictionary<string, int>(num);
			for (int i = 0; i < num; i++)
			{
				string key = reader.ReadString();
				IntProperties[key] = reader.ReadInt(32);
			}
			num = reader.ReadInt(32);
			AdditionalProperties = new Dictionary<string, string>(num);
			for (int j = 0; j < num; j++)
			{
				string key2 = reader.ReadString();
				AdditionalProperties[key2] = reader.ReadString();
			}
		}
	}

	private static readonly NotNullSpireField<CardModel, List<CardModifier>> _modifiers;

	private DynamicVarSet? _dynamicVars;

	[CompilerGenerated]
	private int _003CAmount_003Ek__BackingField;

	public override bool ShouldReceiveCombatHooks
	{
		get
		{
			CardModel? owner = Owner;
			if (owner == null)
			{
				return false;
			}
			return ((AbstractModel)owner).ShouldReceiveCombatHooks;
		}
	}

	public int Amount
	{
		[CompilerGenerated]
		get
		{
			return _003CAmount_003Ek__BackingField;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_003CAmount_003Ek__BackingField = value;
		}
	}

	public CardModel? Owner { get; private set; }

	public int Priority { get; set; }

	public DynamicVarSet DynamicVars
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected O, but got Unknown
			if (_dynamicVars != null)
			{
				return _dynamicVars;
			}
			_dynamicVars = new DynamicVarSet(CanonicalVars);
			_dynamicVars.InitializeWithOwner((AbstractModel)(object)this);
			return _dynamicVars;
		}
	}

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

	public static T Get<T>() where T : CardModifier
	{
		return ModelDbExtensions.CardModifier<T>();
	}

	internal static void RegisterSave()
	{
		ExtendedSaveTypes.RegisterListSaveType<ModifierSave>();
		ExtendedSaveTypes.RegisterDictionarySaveType<string, int>();
		ExtendedSaveTypes.RegisterObjectSaveType<ModifierSave>(new Func<JsonSerializerOptions, JsonPropertyInfo>[3]
		{
			ExtendedSaveTypes.PropertyFunc<ModifierSave, ModelId>("Id"),
			ExtendedSaveTypes.PropertyFunc<ModifierSave, Dictionary<string, int>>("IntProperties"),
			ExtendedSaveTypes.PropertyFunc<ModifierSave, Dictionary<string, string>>("AdditionalProperties")
		});
		ExtendedSaveHandlers<CardModel, SerializableCard>.RegisterSave<List<ModifierSave>>("BaseLibCardModifiers", (Func<CardModel, List<ModifierSave>?>)((CardModel card) => DirectModifiers(card).Select(ModifierSave.FromModifier).ToList()), (Action<CardModel, List<ModifierSave>?>)LoadModifierSaves, (Action<List<ModifierSave>, PacketWriter>)delegate(List<ModifierSave> saves, PacketWriter writer)
		{
			writer.WriteInt(saves.Count, 32);
			foreach (ModifierSave safe in saves)
			{
				safe.Serialize(writer);
			}
		}, (Func<PacketReader, List<ModifierSave>>)delegate(PacketReader reader)
		{
			List<ModifierSave> list = new List<ModifierSave>();
			int num = reader.ReadInt(32);
			for (int i = 0; i < num; i++)
			{
				ModifierSave modifierSave = new ModifierSave();
				modifierSave.Deserialize(reader);
				list.Add(modifierSave);
			}
			return list;
		});
	}

	private static void LoadModifierSaves(CardModel card, List<ModifierSave>? modifiers)
	{
		_modifiers[card] = modifiers?.Select((ModifierSave mod) => mod.ToRealMod(card)).ToList() ?? new List<CardModifier>();
	}

	public virtual void StoreSaveData(ModifierSave save)
	{
	}

	public virtual void LoadSaveData(ModifierSave save)
	{
	}

	public static ReadOnlyCollection<CardModifier> Modifiers(CardModel card)
	{
		return _modifiers[card]?.AsReadOnly() ?? throw new Exception("Card modifiers not found");
	}

	public static List<CardModifier> DirectModifiers(CardModel card)
	{
		return _modifiers[card] ?? throw new Exception("Card modifiers not found");
	}

	public static void AddModifier<T>(CardModel card) where T : CardModifier
	{
		AddModifier(card, ModelDbExtensions.CardModifier<T>(mutableClone: true));
	}

	public static void AddModifier<T>(CardModel card, int amount) where T : CardModifier
	{
		T val = ModelDbExtensions.CardModifier<T>(mutableClone: true);
		val.Amount = amount;
		AddModifier(card, val);
	}

	public static void AddModifier(CardModel card, CardModifier modifier)
	{
		modifier.ApplyInternal(card);
	}

	public static bool RemoveModifier(CardModel card, CardModifier modifier)
	{
		return modifier.RemoveInternal(card);
	}

	static CardModifier()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		_modifiers = new NotNullSpireField<CardModel, List<CardModifier>>(() => new List<CardModifier>()).CopyOnClone(delegate(CardModel src, CardModel dst, List<CardModifier> modifiers)
		{
			foreach (CardModifier modifier in modifiers)
			{
				CardModifier cardModifier = (CardModifier)(object)((AbstractModel)modifier).MutableClone();
				dst.AddModifier(cardModifier);
				cardModifier.AfterClonedOnCard(dst);
			}
		});
		ModHelper.SubscribeForCombatStateHooks("BaseLibCardModifiers", (CombatHookSubscriptionDelegate)delegate(CombatState combatState)
		{
			List<CardModifier> list = new List<CardModifier>();
			foreach (IReadOnlyList<CardPile> item in combatState.Players.Select(delegate(Player p)
			{
				PlayerCombatState playerCombatState = p.PlayerCombatState;
				return (playerCombatState == null) ? null : playerCombatState.AllPiles;
			}))
			{
				if (item != null)
				{
					foreach (CardPile item2 in item)
					{
						foreach (CardModel card in item2.Cards)
						{
							list.AddRange(DirectModifiers(card));
						}
					}
				}
			}
			return (IEnumerable<AbstractModel>)list;
		});
		DescriptionOverrides.CustomizeDescription += delegate(CardModel card, Creature? target, ref string description)
		{
			foreach (CardModifier item3 in DirectModifiers(card))
			{
				item3.ModifyDescription(target, ref description);
			}
		};
		DescriptionOverrides.CustomizeDescriptionPost += delegate(CardModel card, Creature? target, ref string description)
		{
			foreach (CardModifier item4 in DirectModifiers(card))
			{
				item4.ModifyDescriptionPost(target, ref description);
			}
		};
	}

	private void ApplyInternal(CardModel card)
	{
		if (!card.TryGetModifier(((AbstractModel)this).Id, out CardModifier modifier) || !modifier.ApplyStacked(this))
		{
			DirectModifiers(card).InsertSorted(this);
			Owner = card;
			OnInitialApplication();
		}
	}

	private bool RemoveInternal(CardModel card)
	{
		if (Owner == card)
		{
			Owner = null;
		}
		return DirectModifiers(card).Remove(this);
	}

	public virtual bool ApplyStacked(CardModifier newApplied)
	{
		return false;
	}

	public virtual LocString GetLoc(string subKey = "description")
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		LocString val = new LocString("card_modifiers", ((AbstractModel)this).Id.Entry + "." + subKey);
		val.Add("Amount", (decimal)Amount);
		DynamicVars.AddTo(val);
		val.Add("TargetType", (Owner == null) ? "None" : ((object)Owner.TargetType/*cast due to .constrained prefix*/).ToString());
		return val;
	}

	public virtual void ModifyDescription(Creature? target, ref string description)
	{
	}

	public virtual void ModifyDescriptionPost(Creature? target, ref string description)
	{
	}

	public virtual void AddTips(List<IHoverTip> tips)
	{
	}

	public virtual void OnInitialApplication()
	{
	}

	public virtual void OnUpgrade()
	{
	}

	public virtual void OnDowngrade()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		_dynamicVars = new DynamicVarSet(CanonicalVars);
		_dynamicVars.InitializeWithOwner((AbstractModel)(object)Owner);
	}

	public virtual void UpdateDynamicVarPreview(CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		foreach (DynamicVar value in DynamicVars.Values)
		{
			value.UpdateCardPreview(Owner, previewMode, target, runGlobalHooks);
		}
	}

	public virtual void AfterClonedOnCard(CardModel card)
	{
	}

	public virtual decimal ModifyBaseDamageAdditive(decimal originalDamage, ValueProp props)
	{
		return 0m;
	}

	public virtual decimal ModifyBaseDamageMultiplicative(decimal originalDamage, ValueProp props)
	{
		return 1m;
	}

	public virtual decimal ModifyBaseBlockAdditive(decimal originalBlock)
	{
		return 0m;
	}

	public virtual decimal ModifyBaseBlockMultiplicative(decimal originalBlock)
	{
		return 1m;
	}

	public virtual Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	int IComparable<CardModifier>.CompareTo(CardModifier? other)
	{
		return Priority.CompareTo(other?.Priority ?? 0);
	}

	protected override void DeepCloneFields()
	{
		_dynamicVars = DynamicVars.Clone((AbstractModel)(object)this);
	}
}
