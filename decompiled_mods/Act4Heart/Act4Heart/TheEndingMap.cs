using MegaCrit.Sts2.Core.Map;

namespace Act4Heart;

internal class TheEndingMap : ActMap
{
	public override MapPoint[,] Grid { get; }

	public override MapPoint StartingMapPoint { get; }

	public override MapPoint BossMapPoint { get; }

	public TheEndingMap()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		MapPoint[,] array = new MapPoint[7, 13];
		MapPoint val = new MapPoint(3, 0)
		{
			PointType = (MapPointType)4
		};
		array[3, 1] = new MapPoint(3, 1)
		{
			PointType = (MapPointType)2
		};
		array[3, 2] = new MapPoint(3, 2)
		{
			PointType = (MapPointType)6
		};
		MapPoint val2 = new MapPoint(3, 3)
		{
			PointType = (MapPointType)7
		};
		base.startMapPoints.Add(val);
		val.AddChildPoint(array[3, 1]);
		array[3, 1].AddChildPoint(array[3, 2]);
		array[3, 2].AddChildPoint(val2);
		Grid = array;
		StartingMapPoint = val;
		BossMapPoint = val2;
	}
}
