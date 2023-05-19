using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class CreateLobbyScreen : MonoBehaviour {

    public static event Action<LobbyData> LobbyCreated;

    public void OnCreateClicked() {
        var _lobbyData = new LobbyData {
            Name = $"Lobby #{Random.value * 1000f}",
            MaxPlayers = 2,
        };

        LobbyCreated?.Invoke(_lobbyData);
    }
}

public struct LobbyData {
    public string Name;
    public int MaxPlayers;
}