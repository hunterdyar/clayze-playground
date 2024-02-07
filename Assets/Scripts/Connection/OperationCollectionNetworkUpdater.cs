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
		private NetworkList<OpContainer> _networkOperations;

		private void Awake()
		{
			_networkOperations = new NetworkList<OpContainer>();
		}

		private void Start()
		{
			Initialize();
		}

		private void NetworkOperationsOnOnListChanged(NetworkListEvent<OpContainer> changeEvent)
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
			foreach (var val in _networkOperations)
			{
				_opCol.Add(val.GetOperation());
			}
		}

		private void OnEnable()
		{
			_networkOperations.OnListChanged += NetworkOperationsOnOnListChanged;
		}

		private void OnDisable()
		{
			_networkOperations.OnListChanged -= NetworkOperationsOnOnListChanged;
		}

		public override void OnNetworkSpawn()
		{
			Initialize();
			// Do things with m_MeshRenderer

			base.OnNetworkSpawn();
		}

		public void Add(IOperation op)
		{
			//for testing in editor mode/local, things should still just work?
			if (NetworkManager.Singleton == null)
			{
				_opCol.Add(op);
				return;
			}
			
			if (IsClient)
			{
				AddServerRpc(new OpContainer(op));
			}else if (IsServer || IsHost)
			{
				_networkOperations.Add(new OpContainer(op));
			}
		}

		//Rpc(SendTo.Server)]//this is netcode 1.8, which came out 2 months ago but hasn't rolled into package manager yet.
		[ServerRpc(RequireOwnership = false)]
		public void AddServerRpc(OpContainer op)
		{
			_networkOperations.Add(op);
		}
	}
}