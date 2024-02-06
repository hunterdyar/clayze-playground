using System;
using Connection;
using Marching.Operations;
using UnityEngine;

namespace Marching
{
	public class SphereStamp : MonoBehaviour
	{
		public float radius;
		private OperationCollectionNetworkUpdater _netOpCollection;

		private void Awake()
		{
			_netOpCollection = GetComponent<OperationCollectionNetworkUpdater>();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.S))
			{
				Stamp();
			}
		}

		[ContextMenu("Stamp")]
		private void Stamp()
		{
			SphereOp op = new SphereOp(transform.position,radius, OperationType.Add);
			_netOpCollection.Add(op);
			
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position,radius);
		}
	}
}