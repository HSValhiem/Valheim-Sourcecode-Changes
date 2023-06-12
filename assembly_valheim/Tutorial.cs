using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{

	public static Tutorial instance
	{
		get
		{
			return Tutorial.m_instance;
		}
	}

	private void Awake()
	{
		Tutorial.m_instance = this;
		this.m_windowRoot.gameObject.SetActive(false);
	}

	private void Update()
	{
		if (ZoneSystem.instance && Player.m_localPlayer && DateTime.Now > this.m_lastGlobalKeyCheck + TimeSpan.FromSeconds((double)this.m_GlobalKeyCheckRateSec))
		{
			this.m_lastGlobalKeyCheck = DateTime.Now;
			foreach (Tutorial.TutorialText tutorialText in this.m_texts)
			{
				if (!string.IsNullOrEmpty(tutorialText.m_globalKeyTrigger) && ZoneSystem.instance.GetGlobalKey(tutorialText.m_globalKeyTrigger))
				{
					Player.m_localPlayer.ShowTutorial(tutorialText.m_globalKeyTrigger, false);
				}
			}
		}
	}

	public void ShowText(string name, bool force)
	{
		Tutorial.TutorialText tutorialText = this.m_texts.Find((Tutorial.TutorialText x) => x.m_name == name);
		if (tutorialText != null)
		{
			this.SpawnRaven(tutorialText.m_name, tutorialText.m_topic, tutorialText.m_text, tutorialText.m_label, tutorialText.m_isMunin);
			return;
		}
		Debug.Log("Missing tutorial text for: " + name);
	}

	private void SpawnRaven(string key, string topic, string text, string label, bool munin)
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		Raven.AddTempText(key, topic, text, label, munin);
	}

	public List<Tutorial.TutorialText> m_texts = new List<Tutorial.TutorialText>();

	public int m_GlobalKeyCheckRateSec = 10;

	public RectTransform m_windowRoot;

	public Text m_topic;

	public Text m_text;

	public GameObject m_ravenPrefab;

	private static Tutorial m_instance;

	private Queue<string> m_tutQueue = new Queue<string>();

	private DateTime m_lastGlobalKeyCheck;

	[Serializable]
	public class TutorialText
	{

		public string m_name;

		public string m_globalKeyTrigger;

		public string m_topic = "";

		public string m_label = "";

		public bool m_isMunin;

		[TextArea]
		public string m_text = "";
	}
}
