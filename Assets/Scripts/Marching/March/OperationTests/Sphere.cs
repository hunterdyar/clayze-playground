using UnityEngine;

namespace Marching.Operations
{
	public class Sphere : Operation
	{
		public float radius;

		//monitor changes, set didChangeLastFrame in update
		private float _lastRadius;
		private Vector3 _lastPosition;

		public override bool DidUpdate()
		{
			return _lastRadius != radius || _lastPosition != transform.position;
		}

		private void LateUpdate()
		{
			_lastPosition = transform.position;
			_lastRadius = radius;
		}
		public override float Sample(Vector3 worldPoint)
		{
			return Mathf.Clamp(radius - Vector3.Distance(transform.position, worldPoint),-1,1);
		}

		public override (Vector3, Vector3) OperationWorldBounds()
		{
			var center = transform.position;
			var min = center - Vector3.one * radius;
			var max = center + Vector3.one * radius;
			return (new Vector3Int(Mathf.RoundToInt(min.x), Mathf.RoundToInt(min.y), Mathf.RoundToInt(min.z)),
				new Vector3Int(Mathf.CeilToInt(max.x), Mathf.CeilToInt(max.y), Mathf.CeilToInt(max.z)));
		}
	}
}