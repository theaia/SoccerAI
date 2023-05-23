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
	Home,
	Away,
	Armenia,
	Australia,
	Canada,
	Netherlands,
	Switzerland,
	USA,
}
