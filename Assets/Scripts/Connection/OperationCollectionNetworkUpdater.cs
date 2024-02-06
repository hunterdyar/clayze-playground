using System;
using Marching.Operations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Connection
{
	public class OperationCollectionNetworkUpdater : NetworkBehaviour
	{
		[SerializeField] private OperationCollection _opCol;
		public NetworkList<OpContainer> _networkOperation = new NetworkList<OpContainer>();

		private void Start()
		{
			Initialize();
		}

		private void NetworkOperationOnOnListChanged(NetworkListEvent<OpContainer> changeEvent)
		{
			if (!IsLocalPlayer)
			{
				return;
			}

			Debug.Log("list changed " + changeEvent.Type);
			switch (changeEvent.Type)
			{
				case NetworkListEvent<OpContainer>.EventType.Clear:
					_opCol.Clear(false);
					break;
				case NetworkListEvent<OpContainer>.EventType.Add:
					_opCol.Add(changeEvent.Value.GetOperation(), false);
					break;
				case NetworkListEvent<OpContainer>.EventType.Insert:
					_opCol.Insert(changeEvent.Index, changeEvent.Value.GetOperation(), false);
					break;
				case NetworkListEvent<OpContainer>.EventType.Remove:
					_opCol.Remove(changeEvent.Value.GetOperation(), false);
					break;
				case NetworkListEvent<OpContainer>.EventType.RemoveAt:
					_opCol.RemoveAt(changeEvent.Index, false);
					break;
				case NetworkListEvent<OpContainer>.EventType.Value:
					_opCol.UpdateValue(changeEvent.PreviousValue.GetOperation(),
						changeEvent.Value.GetOperation(), false);
					break;
				default:
					break;
			}
		}

		private void Initialize()
		{
			_opCol.Clear();
			foreach (var val in _networkOperation)
			{
				_opCol.Add(val.GetOperation());
			}
		}

		private void OnEnable()
		{
			_networkOperation.OnListChanged += NetworkOperationOnOnListChanged;
		}

		private void OnDisable()
		{
			_networkOperation.OnListChanged -= NetworkOperationOnOnListChanged;
		}

		public override void OnNetworkSpawn()
		{
			Initialize();
			// Do things with m_MeshRenderer

			base.OnNetworkSpawn();
		}

		public void Add(IOperation op)
		{
			if (IsClient)
			{
				AddServerRpc(new OpContainer(op));
			}else if (IsServer || IsHost)
			{
				_networkOperation.Add(new OpContainer(op));
			}
		}

		//Rpc(SendTo.Server)]//this is netcode 1.8, which came out 2 months ago but hasn't rolled into package manager yet.
		[ServerRpc(RequireOwnership = false)]
		public void AddServerRpc(OpContainer op)
		{
			_networkOperation.Add(op);
		}
	}
}