using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Downfall.DownfallCode.Abstract;

public abstract class CustomIntent : AbstractIntent, ICustomModel
{
	protected override string IntentPrefix => TypePrefix.GetPrefix(((object)this).GetType()) + StringExtensions.ToSnakeCase(((object)this).GetType().Name).ToUpperInvariant();

	protected override string? SpritePath => null;

	protected abstract string IntentSpritePath { get; }

	private void EnsureRegistered()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		string key = ((AbstractIntent)this).IntentPrefix.ToLowerInvariant();
		if (!IntentAnimData._data.ContainsKey(key))
		{
			IntentAnimData._data[key] = new InternalData
			{
				frames = new string[1] { IntentSpritePath }
			};
		}
	}

	public override string GetAnimation(IEnumerable<Creature> targets, Creature owner)
	{
		EnsureRegistered();
		return ((AbstractIntent)this).GetAnimation(targets, owner);
	}
}
