using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class Auth: MonoBehaviour {
	async void Awake() {
		try {
			await UnityServices.InitializeAsync();
		} catch (Exception e) {
			Debug.LogException(e);
		}
	}

	private void Start() {
		SignInAnonymouslyAsync();
	}

	async void SignInAnonymouslyAsync() {
		try {
			AuthenticationService.Instance.ClearSessionToken();
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log("Sign in anonymously succeeded!");

			// Shows how to get the playerID
			Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

		} catch (AuthenticationException ex) {
			// Compare error code to AuthenticationErrorCodes
			// Notify the player with the proper error message
			Debug.LogException(ex);
		} catch (RequestFailedException ex) {
			// Compare error code to CommonErrorCodes
			// Notify the player with the proper error message
			Debug.LogException(ex);
		}
	}
}