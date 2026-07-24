using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class BrilliantScalesPower : GuardianPowerModel, ICustomPowerIcon
{
	private class BrilliantScalesDynamicVar : DynamicVar
	{
		private BrilliantScalesPower? _power;

		public BrilliantScalesDynamicVar()
			: base("effects", 0m)
		{
		}

		public override void SetOwner(AbstractModel model)
		{
			((DynamicVar)this).SetOwner(model);
			_power = model as BrilliantScalesPower;
		}

		public override string ToString()
		{
			if (_power == null)
			{
				return "";
			}
			List<string> list = _power.Gems.Select((GemModel loc) => loc.GetFormattedText()).ToList();
			if (list.Count <= 0)
			{
				return "";
			}
			return string.Join("\n", list);
		}
	}

	private IGemSocketCard? _sourceCard;

	public override PowerInstanceType InstanceType => (PowerInstanceType)1;

	private IReadOnlyList<GemModel> Gems => _sourceCard?.Gems ?? Array.Empty<GemModel>();

	public event Action? IconChanged;

	public BrilliantScalesPower()
		: base((PowerType)1, (PowerStackType)2)
	{
		WithTips((PowerModel power) => ((BrilliantScalesPower)(object)power).Gems?.SelectMany((GemModel gem) => gem.HoverTips) ?? Array.Empty<IHoverTip>());
		WithVars(new BrilliantScalesDynamicVar());
	}

	public void DecorateIcon(TextureRect icon)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		List<GemModel> list = Gems.ToList();
		if (list.Count != 0)
		{
			float[] array = list.Count switch
			{
				1 => new float[1], 
				2 => new float[2] { -45f, 135f }, 
				_ => new float[3] { 0f, 120f, -120f }, 
			};
			ShaderMaterial material = (ShaderMaterial)((CanvasItem)icon).Material;
			for (int i = 0; i < list.Count; i++)
			{
				icon.AddDecoration((Control)new TextureRect
				{
					Texture = list[i].Icon,
					Material = (Material)(object)material,
					OffsetLeft = 10f,
					OffsetTop = -2f,
					OffsetRight = 30f,
					OffsetBottom = 18f,
					PivotOffset = new Vector2(10f, 22f),
					Rotation = Mathf.DegToRad(array[i]),
					ExpandMode = (ExpandModeEnum)1,
					StretchMode = (StretchModeEnum)4
				}, i);
			}
		}
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (_sourceCard == null || ((PowerModel)this).Owner != player.Creature)
		{
			return;
		}
		foreach (GemModel gem in _sourceCard.Gems)
		{
			await ((CardModifier)gem).OnPlay(ctx, (CardPlay)null);
		}
	}

	public void SetCard(IGemSocketCard cardModel)
	{
		_sourceCard = cardModel;
		foreach (GemModel gem in _sourceCard.Gems)
		{
			gem.Power = (PowerModel)(object)this;
		}
		this.IconChanged?.Invoke();
	}
}
