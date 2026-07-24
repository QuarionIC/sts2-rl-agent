using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomTemporaryPowerModelWrapper<TModel, TPower> : CustomTemporaryPowerModel where TModel : AbstractModel where TPower : PowerModel
{
	public override string CustomBigBetaIconPath
	{
		get
		{
			if (((PowerModel)this).Amount >= 0 != !InvertInternalPowerAmount)
			{
				return "BaseLib/images/powers/big/baselib-power_temp_down.png";
			}
			return "BaseLib/images/powers/big/baselib-power_temp_up.png";
		}
	}

	public override string CustomPackedIconPath
	{
		get
		{
			if (((PowerModel)this).Amount >= 0 != !InvertInternalPowerAmount)
			{
				return "BaseLib/images/powers/baselib-power_temp_down.png";
			}
			return "BaseLib/images/powers/baselib-power_temp_up.png";
		}
	}

	public override string CustomBigIconPath
	{
		get
		{
			if (((PowerModel)this).Amount >= 0 != !InvertInternalPowerAmount)
			{
				return "BaseLib/images/powers/big/baselib-power_temp_down_big.png";
			}
			return "BaseLib/images/powers/big/baselib-power_temp_up_big.png";
		}
	}

	public override AbstractModel OriginModel => ModelDb.GetById<AbstractModel>(ModelDb.GetId<TModel>());

	public override PowerModel InternallyAppliedPower => (PowerModel)(object)ModelDb.Power<TPower>();

	protected override Func<PlayerChoiceContext, Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc => (PlayerChoiceContext context, Creature target, decimal amt, Creature? src, CardModel? srcCard, bool silent) => BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<TPower>, TPower>(null, new object[6] { context, target, amt, src, srcCard, silent }) ?? Task.CompletedTask;

	public override LocString Title
	{
		get
		{
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			//IL_019b: Expected O, but got Unknown
			//IL_015b: Unknown result type (might be due to invalid IL or missing references)
			AbstractModel originModel = OriginModel;
			CardModel val = (CardModel)(object)((originModel is CardModel) ? originModel : null);
			if (val == null)
			{
				PotionModel val2 = (PotionModel)(object)((originModel is PotionModel) ? originModel : null);
				if (val2 == null)
				{
					RelicModel val3 = (RelicModel)(object)((originModel is RelicModel) ? originModel : null);
					if (val3 == null)
					{
						PowerModel val4 = (PowerModel)(object)((originModel is PowerModel) ? originModel : null);
						if (val4 == null)
						{
							OrbModel val5 = (OrbModel)(object)((originModel is OrbModel) ? originModel : null);
							if (val5 == null)
							{
								CharacterModel val6 = (CharacterModel)(object)((originModel is CharacterModel) ? originModel : null);
								if (val6 == null)
								{
									MonsterModel val7 = (MonsterModel)(object)((originModel is MonsterModel) ? originModel : null);
									if (val7 == null)
									{
										ActModel val8 = (ActModel)(object)((originModel is ActModel) ? originModel : null);
										if (val8 == null)
										{
											EnchantmentModel val9 = (EnchantmentModel)(object)((originModel is EnchantmentModel) ? originModel : null);
											if (val9 == null)
											{
												AfflictionModel val10 = (AfflictionModel)(object)((originModel is AfflictionModel) ? originModel : null);
												if (val10 == null)
												{
													EncounterModel val11 = (EncounterModel)(object)((originModel is EncounterModel) ? originModel : null);
													if (val11 == null)
													{
														EventModel val12 = (EventModel)(object)((originModel is EventModel) ? originModel : null);
														if (val12 == null)
														{
															ModifierModel val13 = (ModifierModel)(object)((originModel is ModifierModel) ? originModel : null);
															if (val13 == null)
															{
																if (originModel is CardModifier cardModifier)
																{
																	CardModel? owner = cardModifier.Owner;
																	return (LocString)(((object)((owner != null) ? owner.TitleLocString : null)) ?? ((object)new LocString("powers", "BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.title")));
																}
																BaseLibMain.Logger.Warn("Getting the 'Title' for the base model type of '" + ((object)OriginModel).GetType().Name + "' has not been implemented yet. Using default title.", 1);
																return new LocString("powers", "BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.title");
															}
															return val13.Title;
														}
														return val12.Title;
													}
													return val11.Title;
												}
												return val10.Title;
											}
											return val9.Title;
										}
										return val8.Title;
									}
									return val7.Title;
								}
								return val6.Title;
							}
							return val5.Title;
						}
						return val4.Title;
					}
					return val3.Title;
				}
				return val2.Title;
			}
			return val.TitleLocString;
		}
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_0184: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
			AbstractModel originModel = OriginModel;
			CardModel val = (CardModel)(object)((originModel is CardModel) ? originModel : null);
			List<IHoverTip> list2;
			if (val == null)
			{
				PotionModel val2 = (PotionModel)(object)((originModel is PotionModel) ? originModel : null);
				if (val2 == null)
				{
					RelicModel val3 = (RelicModel)(object)((originModel is RelicModel) ? originModel : null);
					if (val3 == null)
					{
						PowerModel val4 = (PowerModel)(object)((originModel is PowerModel) ? originModel : null);
						if (val4 == null)
						{
							if (!(originModel is ActModel) && !(originModel is EncounterModel) && !(originModel is EventModel))
							{
								EnchantmentModel val5 = (EnchantmentModel)(object)((originModel is EnchantmentModel) ? originModel : null);
								if (val5 == null)
								{
									AfflictionModel val6 = (AfflictionModel)(object)((originModel is AfflictionModel) ? originModel : null);
									if (val6 == null)
									{
										ModifierModel val7 = (ModifierModel)(object)((originModel is ModifierModel) ? originModel : null);
										if (val7 == null)
										{
											if (originModel is CardModifier cardModifier)
											{
												List<IHoverTip> list;
												if (cardModifier.Owner == null)
												{
													list = new List<IHoverTip>();
												}
												else
												{
													int num = 1;
													list = new List<IHoverTip>(num);
													CollectionsMarshal.SetCount(list, num);
													Span<IHoverTip> span = CollectionsMarshal.AsSpan(list);
													int index = 0;
													span[index] = HoverTipFactory.FromCard(cardModifier.Owner, false);
												}
												list2 = list;
											}
											else
											{
												BaseLibMain.Logger.Warn("Getting the Hover Tips for the base model type of '" + ((object)OriginModel).GetType().Name + "' has not been implemented yet.", 1);
												list2 = new List<IHoverTip>();
											}
										}
										else
										{
											list2 = val7.HoverTips.ToList();
										}
									}
									else
									{
										AfflictionModel val8 = (AfflictionModel)((AbstractModel)val6).MutableClone();
										val8.Amount = ((PowerModel)this).Amount;
										list2 = val8.HoverTips.ToList();
									}
								}
								else
								{
									EnchantmentModel val9 = (EnchantmentModel)((AbstractModel)val5).MutableClone();
									val9.Amount = ((PowerModel)this).Amount;
									val9.RecalculateValues();
									list2 = val9.HoverTips.ToList();
								}
							}
							else
							{
								list2 = new List<IHoverTip>();
							}
						}
						else
						{
							int index = 1;
							List<IHoverTip> list3 = new List<IHoverTip>(index);
							CollectionsMarshal.SetCount(list3, index);
							Span<IHoverTip> span2 = CollectionsMarshal.AsSpan(list3);
							int num = 0;
							span2[num] = HoverTipFactory.FromPower(val4, (int?)null);
							list2 = list3;
						}
					}
					else
					{
						list2 = HoverTipFactory.FromRelic(val3).ToList();
					}
				}
				else
				{
					int num = 1;
					List<IHoverTip> list4 = new List<IHoverTip>(num);
					CollectionsMarshal.SetCount(list4, num);
					Span<IHoverTip> span3 = CollectionsMarshal.AsSpan(list4);
					int index = 0;
					span3[index] = HoverTipFactory.FromPotion(val2);
					list2 = list4;
				}
			}
			else
			{
				int index = 1;
				List<IHoverTip> list5 = new List<IHoverTip>(index);
				CollectionsMarshal.SetCount(list5, index);
				Span<IHoverTip> span4 = CollectionsMarshal.AsSpan(list5);
				int num = 0;
				span4[num] = HoverTipFactory.FromCard(val, false);
				list2 = list5;
			}
			list2.Add(HoverTipFactory.FromPower(InternallyAppliedPower, (int?)null));
			return list2;
		}
	}

	public override LocString Description => new LocString("powers", (((PowerModel)this).Amount > 0 == !InvertInternalPowerAmount) ? "BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.UP.description" : "BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.DOWN.description");

	protected override string SmartDescriptionLocKey
	{
		get
		{
			if (((PowerModel)this).Amount > 0 != !InvertInternalPowerAmount)
			{
				return "BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.DOWN.smartDescription";
			}
			return "BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.UP.smartDescription";
		}
	}
}
