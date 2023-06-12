using System;

public abstract class FixedPopupBase : PopupBase
{

	public FixedPopupBase(string header, string text, bool localizeText = true)
	{
		this.header = (localizeText ? Localization.instance.Localize(header) : header);
		this.text = (localizeText ? Localization.instance.Localize(text) : text);
	}

	public readonly string header;

	public readonly string text;
}
