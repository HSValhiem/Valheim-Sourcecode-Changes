using System;

public class YesNoPopup : FixedPopupBase
{

	public YesNoPopup(string header, string text, PopupButtonCallback yesCallback, PopupButtonCallback noCallback, bool localizeText = true)
		: base(header, text, localizeText)
	{
		this.yesCallback = yesCallback;
		this.noCallback = noCallback;
	}

	public override PopupType Type
	{
		get
		{
			return PopupType.YesNo;
		}
	}

	public readonly PopupButtonCallback yesCallback;

	public readonly PopupButtonCallback noCallback;
}
