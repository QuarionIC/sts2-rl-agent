using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Dolso;

internal record struct SeBuff : IPacketSerializable
{
	public float act_mult;

	public float add;

	public SeBuff(float act_mult, float add)
	{
		this.act_mult = act_mult;
		this.add = add;
	}

	public readonly decimal GetAmount(int act_index)
	{
		return (decimal)((float)act_index * act_mult + add);
	}

	public void Deserialize(PacketReader reader)
	{
		act_mult = reader.ReadFloat((QuantizeParams?)null);
		add = reader.ReadFloat((QuantizeParams?)null);
	}

	public readonly void Serialize(PacketWriter writer)
	{
		writer.WriteFloat(act_mult, (QuantizeParams?)null);
		writer.WriteFloat(add, (QuantizeParams?)null);
	}

	[CompilerGenerated]
	public readonly void Deconstruct(out float act_mult, out float add)
	{
		act_mult = this.act_mult;
		add = this.add;
	}
}
