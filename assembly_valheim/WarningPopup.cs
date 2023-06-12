using System;

public class WarningPopup : FixedPopupBase
{

	public WarningPopup(string header, string text, PopupButtonCallback okCallback, bool localizeText = true)
		: base(header, text, localizeText)
	{
		this.okCallback = okCallback;
	}

	public override PopupType Type
	{
		get
		{
			return PopupType.Warning;
		}
	}

	public readonly PopupButtonCallback okCallback;
}
