using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public Button connectButton;
    public Button joinLobbyButton;
    public Button createRoomButton;
    public Button leaveRoomButton;
    public TextMeshProUGUI connectionStatusText;
    public TMP_InputField roomNameInput;
    public TMP_InputField nicknameInput;
    public TMP_InputField maxPlayersInput;

    public GameObject roomListItemPrefab; // Prefab for displaying each room
    public Transform roomListParent; // Parent object for room list items
    public GameObject playerListItemPrefab; // Prefab for displaying each player
    public Transform playerListParent; // Parent object for player list items

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private List<GameObject> roomListUI = new List<GameObject>();
    private List<GameObject> playerListUI = new List<GameObject>();

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        connectButton.onClick.AddListener(ConnectToServer);
        joinLobbyButton.onClick.AddListener(() => JoinLobby("DefaultLobby"));
        createRoomButton.onClick.AddListener(CreateRoom);
        leaveRoomButton.onClick.AddListener(LeaveRoom);
        UpdateUI();
    }

    void ConnectToServer()
    {
        PhotonNetwork.NickName = string.IsNullOrEmpty(nicknameInput.text) ? "Guest" : nicknameInput.text;
        PhotonNetwork.ConnectUsingSettings();
        connectionStatusText.text = "Connecting to Server...";
    }

    public override void OnConnectedToMaster()
    {
        connectionStatusText.text = $"Connected as {PhotonNetwork.NickName}";
        UpdateUI();
    }

    void JoinLobby(string lobbyName)
    {
        PhotonNetwork.JoinLobby(new TypedLobby(lobbyName, LobbyType.Default));
        connectionStatusText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        connectionStatusText.text = "Joined Lobby";
        UpdateUI();
        DisplayRoomList(); // Ensure UI updates on lobby join
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {

        UpdateRoomList(roomList);
    }

    void UpdateRoomList(List<RoomInfo> roomList)
    {
        cachedRoomList.Clear(); // Clear cached list to avoid duplicates or stale entries
        Debug.Log("Updating Room List with " + roomList.Count + " rooms");

        foreach (var roomInfo in roomList)
        {
            if (!roomInfo.RemovedFromList && roomInfo.IsVisible)
            {
                cachedRoomList[roomInfo.Name] = roomInfo;
            }
        }
        DisplayRoomList();
    }

    void DisplayRoomList()
    {
        foreach (var roomUI in roomListUI) Destroy(roomUI);
        roomListUI.Clear();

        foreach (var roomInfo in cachedRoomList.Values)
        {
            GameObject roomUI = Instantiate(roomListItemPrefab, roomListParent);
            var roomNameText = roomUI.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            var playerCountText = roomUI.transform.Find("Size").GetComponent<TextMeshProUGUI>();

            roomNameText.text = roomInfo.Name;
            playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

            Button joinRoomButton = roomUI.GetComponentInChildren<Button>();
            joinRoomButton.onClick.AddListener(() => JoinRoom(roomInfo.Name));

            roomListUI.Add(roomUI);
        }
    }

    void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        connectionStatusText.text = "Joining Room...";
    }

    void CreateRoom()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            connectionStatusText.text = "Room name cannot be empty!";
            return;
        }

        byte maxPlayers;
        if (!byte.TryParse(maxPlayersInput.text, out maxPlayers) || maxPlayers < 1)
        {
            connectionStatusText.text = "Invalid max players value!";
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        connectionStatusText.text = "Creating Room...";
        
    }

    public override void OnJoinedRoom()
    {
        connectionStatusText.text = "Joined Room";
        DisplayPlayerList();
        UpdateUI();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        connectionStatusText.text = $"Failed to join room: {message}";
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        connectionStatusText.text = $"Failed to create room: {message}";
    }

    void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        connectionStatusText.text = "Leaving Room...";
    }

    public override void OnLeftRoom()
    {
        connectionStatusText.text = "Left Room";
        UpdateUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        DisplayPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        DisplayPlayerList();
    }

    void DisplayPlayerList()
    {
        foreach (var playerUI in playerListUI) Destroy(playerUI);
        playerListUI.Clear();

        foreach (var player in PhotonNetwork.PlayerList)
        {
            GameObject playerUI = Instantiate(playerListItemPrefab, playerListParent);
            var playerNameText = playerUI.GetComponentInChildren<TextMeshProUGUI>();

            playerNameText.text = player.NickName;
            playerListUI.Add(playerUI);
        }
    }

    void UpdateUI()
    {
        connectButton.interactable = !PhotonNetwork.IsConnected;
        joinLobbyButton.interactable = PhotonNetwork.IsConnected && !PhotonNetwork.InLobby;
        createRoomButton.interactable = PhotonNetwork.InLobby && !PhotonNetwork.InRoom;
        leaveRoomButton.interactable = PhotonNetwork.InRoom;
    }
}
