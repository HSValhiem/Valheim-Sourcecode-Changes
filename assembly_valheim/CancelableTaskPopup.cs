using System;
using System.Collections;

public class CancelableTaskPopup : LivePopupBase
{

	public CancelableTaskPopup(RetrieveFromStringSource headerRetrievalFunc, RetrieveFromStringSource textRetrievalFunc, RetrieveFromBoolSource shouldCloseRetrievalFunc, PopupButtonCallback cancelCallback)
		: base(headerRetrievalFunc, textRetrievalFunc, shouldCloseRetrievalFunc)
	{
		base.SetUpdateRoutine(this.UpdateRoutine());
		this.cancelCallback = cancelCallback;
	}

	private IEnumerator UpdateRoutine()
	{
		while (!this.shouldCloseRetrievalFunc())
		{
			this.headerText.text = this.headerRetrievalFunc();
			this.bodyText.text = this.textRetrievalFunc();
			yield return null;
		}
		base.ShouldClose = true;
		yield break;
	}

	public override PopupType Type
	{
		get
		{
			return PopupType.CancelableTask;
		}
	}

	public readonly PopupButtonCallback cancelCallback;
}
