using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CharacterCustomization : MonoBehaviour {
	private Player player;
	[SerializeField] private SpriteRenderer jersey;
	[SerializeField] private SpriteRenderer trunks;
	private GameObject skin;


	private void Awake() {
		player = GetComponentInParent<Player>();
	}

	public async void SetCosmetics(string _skinId, string _countryId, int _teamInt) {
		await SetOutfit(_countryId, _teamInt);
		await SetSkin(_skinId);
	}

	private async Task SetSkin(string _value) {
		player.SetSkin(_value);
		if(_value == "-1") return;
		var _handleSkin = Addressables.LoadAssetAsync<GameObject>(_value);
		await _handleSkin.Task;
		if (_handleSkin.Status == AsyncOperationStatus.Succeeded) {
			if (skin != null){
				Destroy(skin);
			}

			skin = Instantiate(_handleSkin.Result, transform);
		}
	}
	
	private async Task SetOutfit(string _value, int _teamInt) {
		var _handleCountry = Addressables.LoadAssetAsync<CountryInfo>(_value);
		await _handleCountry.Task;
		if (_handleCountry.Status == AsyncOperationStatus.Succeeded) {
			CountryInfo _countryInfo = _handleCountry.Result;
			Team _team = _teamInt == 1 ? Team.Home : Team.Away;
			jersey.color = _team == Team.Home ? _countryInfo.HomeJerseyColor : _countryInfo.AwayJerseyColor;
			trunks.color = _team == Team.Home ? _countryInfo.HomeTrunksColor : _countryInfo.AwayTrunksColor;
		}
	}
}
