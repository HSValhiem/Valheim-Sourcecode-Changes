using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillsDialog : MonoBehaviour
{

	private void Awake()
	{
		this.m_baseListSize = this.m_listRoot.rect.height;
	}

	private IEnumerator SelectFirstEntry()
	{
		yield return null;
		yield return null;
		if (this.m_elements.Count > 0)
		{
			this.m_selectionIndex = 0;
			EventSystem.current.SetSelectedGameObject(this.m_elements[this.m_selectionIndex]);
			base.StartCoroutine(this.FocusOnCurrentLevel(this.m_elements[this.m_selectionIndex].transform as RectTransform));
			this.skillListScrollRect.verticalNormalizedPosition = 1f;
		}
		yield return null;
		yield break;
	}

	private IEnumerator FocusOnCurrentLevel(RectTransform element)
	{
		yield return null;
		yield return null;
		Canvas.ForceUpdateCanvases();
		this.SnapTo(element);
		yield break;
	}

	private void SnapTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		this.m_listRoot.anchoredPosition = this.skillListScrollRect.transform.InverseTransformPoint(this.m_listRoot.position) - this.skillListScrollRect.transform.InverseTransformPoint(target.position) - new Vector2(target.sizeDelta.x / 2f, 0f);
	}

	private void Update()
	{
		if (this.m_inputDelayTimer > 0f)
		{
			this.m_inputDelayTimer -= Time.unscaledDeltaTime;
			return;
		}
		if (ZInput.IsGamepadActive() && this.m_elements.Count > 0)
		{
			float joyRightStickY = ZInput.GetJoyRightStickY();
			float joyLeftStickY = ZInput.GetJoyLeftStickY(true);
			bool buttonDown = ZInput.GetButtonDown("JoyDPadUp");
			bool flag = joyLeftStickY < -0.1f || joyRightStickY < -0.1f;
			bool buttonDown2 = ZInput.GetButtonDown("JoyDPadDown");
			bool flag2 = joyLeftStickY > 0.1f || joyRightStickY > 0.1f;
			if ((flag || buttonDown) && this.m_selectionIndex > 0)
			{
				this.m_selectionIndex--;
			}
			if ((buttonDown2 || flag2) && this.m_selectionIndex < this.m_elements.Count - 1)
			{
				this.m_selectionIndex++;
			}
			GameObject gameObject = this.m_elements[this.m_selectionIndex];
			EventSystem.current.SetSelectedGameObject(gameObject);
			base.StartCoroutine(this.FocusOnCurrentLevel(gameObject.transform as RectTransform));
			gameObject.GetComponentInChildren<UITooltip>().OnHoverStart(gameObject);
			if (flag || flag2)
			{
				this.m_inputDelayTimer = this.m_inputDelay;
			}
		}
		if (this.m_elements.Count > 0)
		{
			RectTransform rectTransform = this.skillListScrollRect.transform as RectTransform;
			RectTransform listRoot = this.m_listRoot;
			this.scrollbar.size = rectTransform.rect.height / listRoot.rect.height;
		}
	}

	public void Setup(Player player)
	{
		base.gameObject.SetActive(true);
		List<Skills.Skill> skillList = player.GetSkills().GetSkillList();
		int num = skillList.Count - this.m_elements.Count;
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			this.m_elements.Add(gameObject);
		}
		for (int j = 0; j < skillList.Count; j++)
		{
			Skills.Skill skill = skillList[j];
			GameObject gameObject2 = this.m_elements[j];
			gameObject2.SetActive(true);
			RectTransform rectTransform = gameObject2.transform as RectTransform;
			rectTransform.anchoredPosition = new Vector2(0f, (float)(-(float)j) * this.m_spacing);
			gameObject2.GetComponentInChildren<UITooltip>().Set("", skill.m_info.m_description, this.m_tooltipAnchor, new Vector2(0f, Math.Min(255f, rectTransform.localPosition.y + 10f)));
			Utils.FindChild(gameObject2.transform, "icon").GetComponent<Image>().sprite = skill.m_info.m_icon;
			Utils.FindChild(gameObject2.transform, "name").GetComponent<Text>().text = Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
			float skillLevel = player.GetSkills().GetSkillLevel(skill.m_info.m_skill);
			Utils.FindChild(gameObject2.transform, "leveltext").GetComponent<Text>().text = ((int)skill.m_level).ToString();
			Text component = Utils.FindChild(gameObject2.transform, "bonustext").GetComponent<Text>();
			if (skillLevel != skill.m_level)
			{
				component.text = (skillLevel - skill.m_level).ToString("+0");
			}
			else
			{
				component.gameObject.SetActive(false);
			}
			Utils.FindChild(gameObject2.transform, "levelbar_total").GetComponent<GuiBar>().SetValue(skillLevel / 100f);
			Utils.FindChild(gameObject2.transform, "levelbar").GetComponent<GuiBar>().SetValue(skill.m_level / 100f);
			Utils.FindChild(gameObject2.transform, "currentlevel").GetComponent<GuiBar>().SetValue(skill.GetLevelPercentage());
		}
		float num2 = Mathf.Max(this.m_baseListSize, (float)skillList.Count * this.m_spacing);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		this.m_totalSkillText.text = string.Concat(new string[]
		{
			"<color=orange>",
			player.GetSkills().GetTotalSkill().ToString("0"),
			"</color><color=white> / </color><color=orange>",
			player.GetSkills().GetTotalSkillCap().ToString("0"),
			"</color>"
		});
		base.StartCoroutine(this.SelectFirstEntry());
	}

	public void OnClose()
	{
		base.gameObject.SetActive(false);
		foreach (GameObject gameObject in this.m_elements)
		{
			gameObject.SetActive(false);
		}
		this.m_elements.Clear();
	}

	public void SkillClicked(GameObject selectedObject)
	{
		this.m_selectionIndex = this.m_elements.IndexOf(selectedObject);
	}

	public RectTransform m_listRoot;

	[SerializeField]
	private ScrollRect skillListScrollRect;

	[SerializeField]
	private Scrollbar scrollbar;

	public RectTransform m_tooltipAnchor;

	public GameObject m_elementPrefab;

	public Text m_totalSkillText;

	public float m_spacing = 80f;

	public float m_inputDelay = 0.1f;

	private int m_selectionIndex;

	private float m_inputDelayTimer;

	private float m_baseListSize;

	private readonly List<GameObject> m_elements = new List<GameObject>();
}
