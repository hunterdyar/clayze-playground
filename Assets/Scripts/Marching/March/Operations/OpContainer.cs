using System;
using Unity.Netcode;
using UnityEngine;

namespace Marching.Operations
{
	public struct OpContainer : IEquatable<OpContainer>, INetworkSerializable
	{
		public OperationName opName;
		public OperationType opType;
		public Vector3 PositionA;
		public float FloatA;
		
		public OpContainer(IOperation operation)
		{
			opName = OperationName.Pass;
			FloatA = default;
			PositionA = default;
			opType = operation.OperationType;
			if (operation is SphereOp sphere)
			{
				PositionA = sphere.Center;
				FloatA = sphere.Radius;
				opName = OperationName.Sphere;
			}
		}

		public IOperation GetOperation()
		{
			switch (opName)
			{
				case OperationName.Sphere:
				default:
					return new SphereOp(PositionA, FloatA, opType);
			}
		}

		public bool Equals(OpContainer other)
		{
			return opName == other.opName && opType == other.opType && PositionA.Equals(other.PositionA) && FloatA.Equals(other.FloatA);
		}

		public override bool Equals(object obj)
		{
			return obj is OpContainer other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)opName, (int)opType, PositionA, FloatA);
		}

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref opName);
			serializer.SerializeValue(ref opType);

			if (opName == OperationName.Sphere)
			{
				serializer.SerializeValue(ref PositionA);
				serializer.SerializeValue(ref FloatA);
			}
		}
	}
}