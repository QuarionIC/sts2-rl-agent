using System;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace BaseLib.Utils;

public sealed record ExtendedSaveInfo<DataSourceType, DataHolderType>(string Id, Action<DataSourceType, DataHolderType> Getter, Action<DataSourceType, DataHolderType> Setter, Action<DataHolderType, PacketWriter> Serializer, Action<DataHolderType, PacketReader> Deserializer) : IComparable<ExtendedSaveInfo<DataSourceType, DataHolderType>>
{
	public int CompareTo(ExtendedSaveInfo<DataSourceType, DataHolderType>? other)
	{
		return string.Compare(Id, other?.Id, StringComparison.Ordinal);
	}
}
