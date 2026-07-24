using System.Collections.Generic;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Minigames;

public sealed class PortalBuilderActMap : ActMap
{
	private const int Width = 7;

	private const int Middle = 3;

	public override MapPoint BossMapPoint { get; }

	public override MapPoint StartingMapPoint { get; }

	protected override MapPoint?[,] Grid { get; }

	public IReadOnlyList<MapCoord> NewVisitedCoords { get; }

	public PortalBuilderActMap(RunState runState, MapPointType[] chosenNodes, int availableNodeCount)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Expected O, but got Unknown
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Expected O, but got Unknown
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Expected O, but got Unknown
		IReadOnlyList<MapCoord> visitedMapCoords = runState.VisitedMapCoords;
		ActMap map = runState.Map;
		MapPoint startingMapPoint = map.StartingMapPoint;
		List<MapPointType> list = new List<MapPointType>();
		foreach (MapCoord item in visitedMapCoords)
		{
			if (item.col != startingMapPoint.coord.col || item.row != startingMapPoint.coord.row)
			{
				MapPoint point = map.GetPoint(item);
				if (point != null)
				{
					list.Add(point.PointType);
				}
			}
		}
		List<MapPointType> list2 = new List<MapPointType>();
		for (int i = 0; i < chosenNodes.Length && i < availableNodeCount; i++)
		{
			if (chosenNodes[i])
			{
				list2.Add(chosenNodes[i]);
			}
		}
		int num = list.Count + list2.Count;
		int num2 = num + 1;
		int count = list.Count;
		int count2 = list2.Count;
		Grid = new MapPoint[7, num2];
		StartingMapPoint = new MapPoint(3, 0)
		{
			PointType = startingMapPoint.PointType
		};
		BossMapPoint = new MapPoint(3, num2)
		{
			PointType = (MapPointType)7
		};
		MapPoint val = ((ActMap)this).StartingMapPoint;
		List<MapCoord> list3 = new List<MapCoord>();
		int num3 = 1;
		foreach (MapPointType item2 in list)
		{
			MapPoint val2 = new MapPoint(3, num3)
			{
				PointType = item2
			};
			((ActMap)this).Grid[3, num3] = val2;
			val.AddChildPoint(val2);
			if (base.startMapPoints.Count == 0)
			{
				base.startMapPoints.Add(val2);
			}
			list3.Add(new MapCoord(3, num3));
			val = val2;
			num3++;
		}
		int num4 = list2.Count - 1;
		while (num4 >= 0 && num3 < num2)
		{
			MapPoint val3 = new MapPoint(3, num3)
			{
				PointType = list2[num4]
			};
			((ActMap)this).Grid[3, num3] = val3;
			val.AddChildPoint(val3);
			if (base.startMapPoints.Count == 0)
			{
				base.startMapPoints.Add(val3);
			}
			val = val3;
			num3++;
			num4--;
		}
		val.AddChildPoint(((ActMap)this).BossMapPoint);
		NewVisitedCoords = list3;
	}
}
