using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Initializer : MonoBehaviour
{
	public static Action<string> OnRoomCodeChange;
	private static string _playerId;
	private static Guid _hostAllocationID;
	public static string RoomCode => _roomCode;
	private static string _roomCode;

	private static Initializer _instance;

	public string gameplayScene;
	public string menuScene;
	private void Awake()
	{
		if (_instance != null)
		{
			Debug.LogError("Multiple Instances of Initializer. Bad.");
		}
		_instance = this;
	}

	private async void Start()
	{
		await UnityServices.InitializeAsync();
		SignIn();
		LoadMenuScene();
		NetworkManager.Singleton.OnClientStarted += OnClientStarted;
		
		NetworkManager.Singleton.OnServerStopped += OnOnServerStopped;
		NetworkManager.Singleton.OnClientStopped += OnOnServerStopped;
	}

	private void OnClientStarted()
	{
		//RelayService.Instance.GetJoinCodeAsync()
	}
	private void OnOnServerStopped(bool obj)
	{
		SetRoomCode("");
		LoadMenuScene();
	}

	public void LoadMenuScene()
	{
		//todo: Check target settings to load appropriate UI.
		//this does not have to be additive because networkmanager calls dontdestroyonload
		SceneManager.LoadScene(menuScene, LoadSceneMode.Single);
	}

	#region NetworkCode

	public async void SignIn()
	{
		if (AuthenticationService.Instance.IsSignedIn)
		{
			return;
		}

		await AuthenticationService.Instance.SignInAnonymouslyAsync();
		_playerId = AuthenticationService.Instance.PlayerId;

		RuntimeConsole.Log($"Signed in. Player ID: {_playerId}");
	}
	
	//SERVER

	#region ServerConnection

	public void StartServerHost()
	{
		StartCoroutine(StartServerHostRoutine());
	}
	IEnumerator StartServerHostRoutine()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Tried to start host but not signed in? Giving up.");
            yield break;
        }

        RuntimeConsole.Log("Getting regions.");

        var regions = RelayService.Instance.ListRegionsAsync();
        bool haveRegions = false;
        while (!haveRegions)
        {
            if (regions.Status == TaskStatus.RanToCompletion)
            {
                haveRegions = true;
            }
            else
            {
                if (regions.Status == TaskStatus.Canceled || regions.Status == TaskStatus.Faulted)
                {
                    RuntimeConsole.Log("Getting Regions Failed");
                    yield break;
                }
                yield return null;
            }
        }
        if (regions.Result.Count == 0)
        {
            Debug.LogError("No Regions?");
            yield break;
        }
        
        RuntimeConsole.Log($"Got {regions.Result.Count} regions.");

        RuntimeConsole.Log("Getting relay allocation.");
            
        var allocation = RelayService.Instance.CreateAllocationAsync(4, regions.Result[0].Id);
        bool haveAlloc = false;
        while (!haveAlloc)
        {
            if (allocation.Status == TaskStatus.RanToCompletion)
            {
                haveAlloc = true;
            }
            else
            {
                if (allocation.Status == TaskStatus.Canceled || allocation.Status == TaskStatus.Faulted)
                {
                    RuntimeConsole.Log("Getting Allocation Failed");
                    yield break;
                }

                yield return null;
            }
        }

        _hostAllocationID = allocation.Result.AllocationId;
        RuntimeConsole.Log($"Alloction ID: {_hostAllocationID}");
        var allocationRegion = allocation.Result.Region;
        RuntimeConsole.Log("Allocation Region:" + allocationRegion);
        
        RuntimeConsole.Log("Setting relay service in network manager...");
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(new RelayServerData(allocation.Result, "dtls"));
        
        RuntimeConsole.Log("Getting Join Code...");

        var joinCode = RelayService.Instance.GetJoinCodeAsync(_hostAllocationID);
        bool havejoin = false;
        while (!havejoin)
        {
            if (joinCode.Status == TaskStatus.RanToCompletion)
            {
                havejoin = true;
            }
            else
            {
                if (joinCode.Status == TaskStatus.Canceled || joinCode.Status == TaskStatus.Faulted)
                {
                    RuntimeConsole.Log("Getting Join Failed");
                    yield break;
                }

                yield return null;
            }
        }

        string roomCode = joinCode.Result;
        RuntimeConsole.Log($"Got Code: {roomCode}");
        Initializer.SetRoomCode(roomCode);
        
        RuntimeConsole.Log("Starting Server as Host");
        NetworkManager.Singleton.StartHost();
        
        //todo: disconnect UI from making a thing do a thing.
        NetworkManager.Singleton.SceneManager.LoadScene(gameplayScene,LoadSceneMode.Single);
    }
	#endregion

	#region ClientConnection
	public static void TryJoinAsClient(string room)
	{
		_instance.StartCoroutine(StartClientRoutine(room));
	}

	static IEnumerator StartClientRoutine(string joinCode)
	{
		if (string.IsNullOrEmpty(joinCode))
		{
			yield break;
		}

		if (!AuthenticationService.Instance.IsSignedIn)
		{
			Debug.LogError("Tried to join client when not authenticated");
			yield break;
		}

		var ja = RelayService.Instance.JoinAllocationAsync(joinCode);
		bool joined = false;
		while (!joined)
		{
			yield return null;
			if (ja.IsCompleted || ja.IsCompletedSuccessfully)
			{
				joined = true;
			}

			if (ja.IsFaulted || ja.IsCanceled)
			{
				joined = true;
				RuntimeConsole.Log("Unable to join.");
				yield break;
			}
		}

		RuntimeConsole.Log("Setting relay service in network manager...");
		NetworkManager.Singleton.GetComponent<UnityTransport>()
			.SetRelayServerData(new RelayServerData(ja.Result, "dtls"));

		RuntimeConsole.Log("Starting Client.");
		NetworkManager.Singleton.StartClient();
		//if we haven't thrown any errors, just update the joincode from here instead of another call to RelayService.
		SetRoomCode(joinCode);
	}

	#endregion

	#endregion

	#region  StaticAccess
	
	public static void TryHostServer()
	{
		_instance.StartServerHost();
	}

	public static void SetRoomCode(string newRoomCode)
	{
		_roomCode = newRoomCode;
		//event
		OnRoomCodeChange?.Invoke(_roomCode);
	}

	#endregion

}
