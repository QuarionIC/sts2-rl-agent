using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Commands;

public readonly record struct ScryResult
{
	public IReadOnlyList<CardModel> Discarded => _003CDiscarded_003Ek__BackingField ?? Array.Empty<CardModel>();

	public static ScryResult Empty => default(ScryResult);

	[CompilerGenerated]
	private readonly IReadOnlyList<CardModel> _003CDiscarded_003Ek__BackingField;

	public ScryResult(IReadOnlyList<CardModel> discarded)
	{
		_003CDiscarded_003Ek__BackingField = discarded;
	}

	[CompilerGenerated]
	private bool PrintMembers(StringBuilder builder)
	{
		builder.Append("Discarded = ");
		builder.Append(Discarded);
		return true;
	}
}
