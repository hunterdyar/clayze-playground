using UnityEngine;

namespace Marching
{
	public class GeometryUtility
	{
		public static bool CubesIntersect(Vector3Int aMin, Vector3Int aMax, Vector3Int bMin, Vector3Int bMax)
		{
			//todo: move to utilities
			return (aMax.x >= bMin.x && aMin.x <= bMax.x)
			       && (aMax.y >= bMin.y && aMin.y <= bMax.y)
			       && (aMax.z >= bMin.z && aMin.z <= bMax.z);
		}

		public static bool PointInBounds(Vector3 point, Vector3 min, Vector3 max)
		{
			return (point.x >= min.x && point.x <= max.x)
			       && (point.y >= min.y && point.y <= max.y)
			       && (point.z >= min.z && point.z <= max.z);
		}

	}
}