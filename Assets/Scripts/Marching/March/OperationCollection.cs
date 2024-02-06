using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Marching.Operations
{
	//This is the class that will handle network syncronization
	//that's why its separate from volume. Let's just handle getting this operations in sync.
	
	//this class will also handle "compacting" the base of operations down to a sampleOp.
	[CreateAssetMenu(fileName = "Op Collection",menuName = "Clayze/OperationCollection",order = 1)]
	public class OperationCollection : ScriptableObject
	{
		public Action<IOperation> OperationChanged;
		public List<IOperation> Operations => _operations;
		[SerializeReference, SubclassSelector]
		private List<IOperation> _operations = new List<IOperation>();
		
		public void Add(IOperation op, bool local = true)
		{
			Operations.Add(op);
			//add operationUpdated to listen events?
			
			OperationChanged?.Invoke(op);
		}

		public IEnumerator<IOperation> GetEnumerator()
		{
			return _operations.GetEnumerator();
		}

		public void Clear(bool local = true)
		{
			_operations.Clear();
			//UH gotta tell volume to hardrefresh
		}

		public void Insert(int index, IOperation op, bool local = true)
		{
			_operations.Insert(index,op);
			OperationChanged?.Invoke(op);
		}

		public void Remove(IOperation op, bool local = true)
		{
			_operations.Remove(op);
			//I guess this would get us to resample this region so....
			OperationChanged?.Invoke(op);

		}

		public void RemoveAt(int index, bool local = true)
		{
			_operations.RemoveAt(index);
			//UH gotta tell volume to hardrefresh
		}

		public void UpdateValue(IOperation oldVal, IOperation newVal, bool local = true)
		{
			//todo: test that this preserves order correctly.
			var i = _operations.IndexOf(oldVal);
			_operations.RemoveAt(i);
			_operations.Add(newVal);
			
			if (oldVal.OperationWorldBounds() != newVal.OperationWorldBounds())
			{
				OperationChanged?.Invoke(oldVal);
			}

			OperationChanged?.Invoke(newVal);
		}
	}
}