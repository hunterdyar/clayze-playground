using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class RuntimeUI : MonoBehaviour
{
	[SerializeField] private Button _shutdownButton;


	private void Awake()
	{
		_shutdownButton.onClick.AddListener(Shutdown);
	}

	void Shutdown()
	{
		NetworkManager.Singleton.Shutdown();
	}
	
	
}
