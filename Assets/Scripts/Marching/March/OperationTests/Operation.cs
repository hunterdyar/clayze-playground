using System;
using UnityEngine;

namespace Marching.Operations
{
	public class Operation : MonoBehaviour
	{
		[HideInInspector]
		public Volume _volume;

		protected Vector3 _min;
		protected Vector3 _max;
		public void SetVolume(Volume volume)
		{
			_volume = volume;
		}
		protected virtual void Update()
		{
			var worldBounds = OperationWorldBounds();
			_min = worldBounds.Item1;
			_max = worldBounds.Item2;
			
			if (_volume != null)
			{
				if (DidUpdate())
				{
					_volume.OperationChanged(worldBounds); //should only call this once per frame
				}
			}
		}
		//Todo: I think it makes sense to flip this around? right now _volume.operationChanged(), we tell it that we update; but it could ask us? and tell us when it samples to reset.
		public virtual bool DidUpdate()
		{
			return false;
		}

		public virtual (Vector3, Vector3) OperationWorldBounds()
		{
			return (Vector3.zero, Vector3.zero);
		}

		public virtual float Sample(Vector3 worldPoint)
		{
			return 0;
		}
	}
}