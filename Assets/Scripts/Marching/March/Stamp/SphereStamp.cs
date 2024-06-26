﻿using System;
using Connection;
using Marching.Operations;
using UnityEngine;

namespace Marching
{
	public class SphereStamp : MonoBehaviour
	{
		public float radius;
		private OperationCollectionNetworkUpdater _netOpCollection;
		private float _distanceFromCamera = 5;
		public float maxSphereDistanceFromCamera;
		public float maxSphereSize;
		private OperationType _opType = OperationType.Add;
		private void Awake()
		{
			_netOpCollection = GetComponent<OperationCollectionNetworkUpdater>();
		}

		private void Update()
		{
			var pos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y,_distanceFromCamera));
			transform.position = pos;

			if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
			{
				if (Input.GetMouseButton(1))
				{
					radius += Input.mouseScrollDelta.y;
					radius = Mathf.Clamp(radius, 0, maxSphereSize);
				}
				else
				{
					_distanceFromCamera += Input.mouseScrollDelta.y;
					_distanceFromCamera = Mathf.Clamp(_distanceFromCamera, 1, maxSphereDistanceFromCamera);
				}
			}
			
			if (Input.GetMouseButtonDown(0))
			{
				if (Input.GetMouseButton(1))
				{
					_opType = OperationType.Remove;
				}
				else
				{
					_opType = OperationType.Add;
				}
				
				Stamp();
			}
			
		}

		[ContextMenu("Stamp")]
		private void Stamp()
		{
			SphereOp op = new SphereOp(transform.position,radius, _opType);
			_netOpCollection.Add(op);
			
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position,radius);
		}
	}
}