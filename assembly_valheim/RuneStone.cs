using System;
using System.Collections.Generic;
using UnityEngine;

public class RuneStone : MonoBehaviour, Hoverable, Interactable
{

	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_rune_read");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = character as Player;
		if (!string.IsNullOrEmpty(this.m_locationName))
		{
			Game.instance.DiscoverClosestLocation(this.m_locationName, base.transform.position, this.m_pinName, (int)this.m_pinType, this.m_showMap);
		}
		RuneStone.RandomRuneText randomText = this.GetRandomText();
		if (randomText != null)
		{
			if (randomText.m_label.Length > 0)
			{
				player.AddKnownText(randomText.m_label, randomText.m_text);
			}
			TextViewer.instance.ShowText(TextViewer.Style.Rune, randomText.m_topic, randomText.m_text, true);
		}
		else
		{
			if (this.m_label.Length > 0)
			{
				player.AddKnownText(this.m_label, this.m_text);
			}
			TextViewer.instance.ShowText(TextViewer.Style.Rune, this.m_topic, this.m_text, true);
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private RuneStone.RandomRuneText GetRandomText()
	{
		if (this.m_randomTexts.Count == 0)
		{
			return null;
		}
		Vector3 position = base.transform.position;
		int num = (int)position.x * (int)position.z;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(num);
		RuneStone.RandomRuneText randomRuneText = this.m_randomTexts[UnityEngine.Random.Range(0, this.m_randomTexts.Count)];
		UnityEngine.Random.state = state;
		return randomRuneText;
	}

	public string m_name = "Rune stone";

	public string m_topic = "";

	public string m_label = "";

	[TextArea]
	public string m_text = "";

	public List<RuneStone.RandomRuneText> m_randomTexts;

	public string m_locationName = "";

	public string m_pinName = "Pin";

	public Minimap.PinType m_pinType = Minimap.PinType.Boss;

	public bool m_showMap;

	[Serializable]
	public class RandomRuneText
	{

		public string m_topic = "";

		public string m_label = "";

		public string m_text = "";
	}
}
