using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class ConnectionMenuManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button _serverHostButton;
    [SerializeField] private Button _serverCloseButton;

    [SerializeField] private Button _clientJoinButton;
    [SerializeField] private Button _clientQuitButton;

    [SerializeField] private TMP_InputField _roomCodeInputField;


    public int maxConnections = 5;
    // Start is called before the first frame update
    private void Awake()
    {
        
        //
        _serverHostButton.onClick.AddListener(TryHostServer);
        _clientJoinButton.onClick.AddListener(TryJoinClient);
        _clientQuitButton.onClick.AddListener(ExitClient);
        _serverCloseButton.onClick.AddListener(ExitServer);
        
    }

    private void OnDisable()
    {
       // _serverHostButton.onClick.RemoveListener(TryHostServer);
       // _clientJoinButton.onClick.RemoveListener(TryJoinClient);
       // _clientQuitButton.onClick.RemoveListener(ExitClient);
       // _serverCloseButton.onClick.RemoveListener(ExitServer);

       // _networkManager.OnServerStopped -= OnServerOrClientStopped;
       // _networkManager.OnClientStopped -= OnServerOrClientStopped;
    }
    private void UpdateUI()
    {
        //can only quit when connected
        _clientQuitButton.interactable = NetworkManager.Singleton.IsClient;
        _serverCloseButton.interactable = NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsListening;

        //can't join if we have joined
        if (NetworkManager.Singleton.IsClient)
        {
            _serverHostButton.interactable = false;
            _clientJoinButton.interactable = false;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            _serverHostButton.interactable = false;
            _clientJoinButton.interactable = false;
        }
        //todo: notification while we are trying to join or connect to host...
        _serverCloseButton.interactable = true;
        _clientJoinButton.interactable = true;
    }

    async void Start()
    {
        //
        RuntimeConsole.Log("Initializing Unity Services");
        await UnityServices.InitializeAsync();
        RuntimeConsole.Log("Signing In");
    }

    private void ExitClient()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void ExitServer()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    
    void TryHostServer()
    {
        RuntimeConsole.Log("Trying to host server");
        _serverCloseButton.interactable = false;
        _serverHostButton.interactable = false;
        _clientJoinButton.interactable = false;
        _clientQuitButton.interactable = false;
        Initializer.TryHostServer();
    }

    void TryJoinClient()
    {
        _serverCloseButton.interactable = false;
        _serverHostButton.interactable = false;
        _clientJoinButton.interactable = false;
        _clientQuitButton.interactable = false;
        var code = GetInputCode();
        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            return;
        }
        
        Initializer.TryJoinAsClient(code);
    }

    string GetInputCode(){
        string code = _roomCodeInputField.text;
        return code.Trim().ToUpper();
    }
    
}
