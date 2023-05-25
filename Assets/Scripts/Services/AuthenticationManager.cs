using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;

public class AuthenticationManager : MonoBehaviour {

    public async void LoginAnonymously() {
        Debug.Log("Logging in Anonymously.");
        using (new Load("Logging you in...")) {
            await Authentication.Login();
            SceneManager.LoadSceneAsync("MainMenu");
        }
    }
}