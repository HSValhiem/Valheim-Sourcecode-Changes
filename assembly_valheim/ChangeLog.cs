using System;
using UnityEngine;
using UnityEngine.UI;

public class ChangeLog : MonoBehaviour
{

	private void Start()
	{
		string text = this.m_changeLog.text;
		this.m_textField.text = text;
	}

	private void LateUpdate()
	{
		if (!this.m_hasSetScroll)
		{
			this.m_hasSetScroll = true;
			if (this.m_scrollbar != null)
			{
				this.m_scrollbar.value = 1f;
			}
		}
	}

	private bool m_hasSetScroll;

	public Text m_textField;

	public TextAsset m_changeLog;

	public TextAsset m_xboxChangeLog;

	public Scrollbar m_scrollbar;
}
