using UnityEngine;

[CreateAssetMenu(menuName = "Team/Country")]
public class CountryInfo : ScriptableObject {
	public Country Country;
	public string Abbreviation;
	public Sprite Flag;
	public Color HomeJerseyColor;
	public Color HomeTrunksColor;
	public Color AwayJerseyColor;
	public Color AwayTrunksColor;
}

public enum Country {
	Armenia,
	Australia,
	Canada,
	China,
	England,
	France,
	Germany,
	Greece,
	India,
	Ireland,
	Italy,
	Japan,
	Korea,
	Mexico,
	Netherlands,
	Norway,
	Peru,
	Philippines,
	Poland,
	Portugal,
	Russia,
	Scotland,
	Sweden,
	Switzerland,
	Thailand,
	Turkey,
	USA,
	Vietnam,
	Wales
}
