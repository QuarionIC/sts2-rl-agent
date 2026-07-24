using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Voting;

public record ArtData
{
	public required ModelId ModelId { get; init; }

	public string? Id { get; init; }

	public CardModel? Card => ModelDb.GetByIdOrNull<CardModel>(ModelId);

	public List<ArtEntry>? Entries;

	[CompilerGenerated]
	protected virtual bool PrintMembers(StringBuilder builder)
	{
		RuntimeHelpers.EnsureSufficientExecutionStack();
		builder.Append("Entries = ");
		builder.Append(Entries);
		builder.Append(", ModelId = ");
		builder.Append(ModelId);
		builder.Append(", Id = ");
		builder.Append((object?)Id);
		builder.Append(", Card = ");
		builder.Append(Card);
		return true;
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return ((EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<List<ArtEntry>>.Default.GetHashCode(Entries)) * -1521134295 + EqualityComparer<ModelId>.Default.GetHashCode(ModelId)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
	}

	[CompilerGenerated]
	public virtual bool Equals(ArtData? other)
	{
		if ((object)this != other)
		{
			if ((object)other != null && EqualityContract == other.EqualityContract && EqualityComparer<List<ArtEntry>>.Default.Equals(Entries, other.Entries) && EqualityComparer<ModelId>.Default.Equals(ModelId, other.ModelId))
			{
				return EqualityComparer<string>.Default.Equals(Id, other.Id);
			}
			return false;
		}
		return true;
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ArtData(ArtData original)
	{
		Entries = original.Entries;
		ModelId = original.ModelId;
		Id = original.Id;
	}

	public ArtData()
	{
	}
}
