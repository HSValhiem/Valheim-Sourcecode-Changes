using System;
using TMPro;
using UnityEngine;

public class Sign : MonoBehaviour, Hoverable, Interactable, TextReceiver
{

	private void Awake()
	{
		this.m_currentText = this.m_defaultText;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.UpdateText();
		base.InvokeRepeating("UpdateText", 2f, 2f);
	}

	public string GetHoverText()
	{
		string text = (this.m_isViewable ? ("\"" + this.GetText().RemoveRichTextTags() + "\"") : "[TEXT HIDDEN DUE TO UGC SETTINGS]");
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return text;
		}
		return text + "\n" + Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
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
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return false;
		}
		TextInput.instance.RequestText(this, "$piece_sign_input", this.m_characterLimit);
		return true;
	}

	private void UpdateText()
	{
		string text = this.m_nview.GetZDO().GetString(ZDOVars.s_text, this.m_defaultText);
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_author, "");
		if (this.m_currentText == text)
		{
			return;
		}
		PrivilegeManager.CanViewUserGeneratedContent(@string, delegate(PrivilegeManager.Result access)
		{
			switch (access)
			{
			case PrivilegeManager.Result.Allowed:
				this.m_currentText = text;
				this.m_textWidget.text = this.m_currentText;
				this.m_isViewable = true;
				return;
			case PrivilegeManager.Result.NotAllowed:
				this.m_currentText = "";
				this.m_textWidget.text = "ᚬᛏᛁᛚᛚᚴᛅᚾᚴᛚᛁᚴ";
				this.m_isViewable = false;
				return;
			}
			this.m_currentText = "";
			this.m_textWidget.text = "ᚬᛏᛁᛚᛚᚴᛅᚾᚴᛚᛁᚴ";
			this.m_isViewable = false;
			ZLog.LogError("Failed to check UGC privilege");
		});
	}

	public string GetText()
	{
		return this.m_currentText;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetText(string text)
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return;
		}
		this.m_nview.ClaimOwnership();
		this.m_nview.GetZDO().Set(ZDOVars.s_text, text);
		this.m_nview.GetZDO().Set(ZDOVars.s_author, PrivilegeManager.GetNetworkUserId());
		this.UpdateText();
	}

	public TextMeshProUGUI m_textWidget;

	public string m_name = "Sign";

	public string m_defaultText = "Sign";

	public int m_characterLimit = 50;

	private ZNetView m_nview;

	private bool m_isViewable = true;

	private string m_currentText;
}
