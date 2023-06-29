using System.Collections;
using UnityEngine;

public class CharacterCustomization : MonoBehaviour {
	private Player player;
	[SerializeField] private SpriteRenderer jersey;
	[SerializeField] private SpriteRenderer trunks;
	
	private void Awake() {
		player = GetComponentInParent<Player>();
	}
	IEnumerator Start() {
		yield return new WaitUntil(() => GameManager.Instance);
		yield return new WaitForEndOfFrame();
		Instantiate(GameManager.Instance.GetRandomSkin(), transform);
		CountryInfo _countryInfo = GameManager.Instance.GetCountryInfo(player.GetTeam());
		yield return new WaitUntil(() => _countryInfo != null);
		jersey.color = player.GetTeam() == Team.Home ? _countryInfo.HomeJerseyColor : _countryInfo.AwayJerseyColor;
		trunks.color = player.GetTeam() == Team.Home ? _countryInfo.HomeTrunksColor : _countryInfo.AwayTrunksColor;
	}

}
