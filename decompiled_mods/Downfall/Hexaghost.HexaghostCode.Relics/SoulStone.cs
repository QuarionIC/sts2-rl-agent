using System.Threading.Tasks;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class SoulStone : HexaghostRelicModel
{
	private int _exhausted;

	private bool _isActivating;

	public override bool ShowCounter => CombatManager.Instance.IsInProgress;

	public override int DisplayAmount
	{
		get
		{
			if (!_isActivating)
			{
				return _exhausted;
			}
			return 4;
		}
	}

	public SoulStone()
		: base((RelicRarity)4)
	{
	}

	public override Task BeforeCombatStart()
	{
		_exhausted = 0;
		_isActivating = false;
		((RelicModel)this).Status = (RelicStatus)0;
		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
	{
		if (card.Owner == ((RelicModel)this).Owner)
		{
			_exhausted++;
			((RelicModel)this).Status = (RelicStatus)(_exhausted >= 3);
			((RelicModel)this).InvokeDisplayAmountChanged();
			if (_exhausted >= 4)
			{
				_exhausted = 0;
				DoActivateVisuals();
				await HexaghostCmd.Ignite(ctx, card.Owner);
			}
		}
	}

	private async Task DoActivateVisuals()
	{
		_isActivating = true;
		((RelicModel)this).Flash();
		((RelicModel)this).Status = (RelicStatus)0;
		((RelicModel)this).InvokeDisplayAmountChanged();
		await Cmd.Wait(1f, false);
		_isActivating = false;
		((RelicModel)this).InvokeDisplayAmountChanged();
	}
}
