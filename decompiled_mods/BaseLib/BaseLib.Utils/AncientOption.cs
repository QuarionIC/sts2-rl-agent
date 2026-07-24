using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public abstract class AncientOption(int weight) : IWeighted
{
	private class BasicAncientOption : AncientOption
	{
		public override IEnumerable<RelicModel> AllVariants { get; }

		public override RelicModel ModelForOption => _003Cmodel_003EP.ToMutable();

		public BasicAncientOption(RelicModel model, int weight)
		{
			_003Cmodel_003EP = model;
			AllVariants = new _003C_003Ez__ReadOnlySingleElementList<RelicModel>(_003Cmodel_003EP.ToMutable());
			base._002Ector(weight);
		}
	}

	public int Weight { get; } = weight;

	public abstract IEnumerable<RelicModel> AllVariants { get; }

	public abstract RelicModel ModelForOption { get; }

	public static explicit operator AncientOption(RelicModel model)
	{
		return new BasicAncientOption(model, 1);
	}
}
public class AncientOption<T> : AncientOption where T : RelicModel
{
	private readonly T _model = ModelDb.Relic<T>();

	public Func<T, RelicModel>? ModelPrep { get; init; }

	public Func<T, IEnumerable<RelicModel>>? Variants { get; init; }

	public override IEnumerable<RelicModel> AllVariants
	{
		get
		{
			if (Variants != null)
			{
				return Variants(_model);
			}
			return new _003C_003Ez__ReadOnlySingleElementList<RelicModel>(((RelicModel)_model).ToMutable());
		}
	}

	public override RelicModel ModelForOption
	{
		get
		{
			RelicModel obj = ((RelicModel)_model).ToMutable();
			T val = ((T)(object)((obj is T) ? obj : null)) ?? throw new InvalidOperationException($"RelicModel ToMutable for {((object)_model).GetType()} did not produce instance of {typeof(T)}");
			return (RelicModel)(ModelPrep?.Invoke(val) ?? ((object)val));
		}
	}

	public AncientOption(int weight)
		: base(weight)
	{
	}
}
