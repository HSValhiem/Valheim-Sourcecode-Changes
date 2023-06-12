using System;
using System.Collections;
using TMPro;
using UnityEngine;

public abstract class LivePopupBase : PopupBase
{

	public LivePopupBase(RetrieveFromStringSource headerRetrievalFunc, RetrieveFromStringSource textRetrievalFunc, RetrieveFromBoolSource isActiveRetrievalFunc)
	{
		this.headerRetrievalFunc = headerRetrievalFunc;
		this.textRetrievalFunc = textRetrievalFunc;
		this.shouldCloseRetrievalFunc = isActiveRetrievalFunc;
	}

	protected void SetUpdateRoutine(IEnumerator updateRoutine)
	{
		this.updateRoutine = updateRoutine;
	}

	public void SetUpdateCoroutineReference(Coroutine updateCoroutine)
	{
		this.updateCoroutine = updateCoroutine;
	}

	public void SetTextReferences(TextMeshProUGUI headerText, TextMeshProUGUI bodyText)
	{
		this.headerText = headerText;
		this.bodyText = bodyText;
	}

	public IEnumerator updateRoutine { get; private set; }

	public Coroutine updateCoroutine { get; private set; }

	public bool ShouldClose { get; protected set; }

	protected TextMeshProUGUI headerText;

	protected TextMeshProUGUI bodyText;

	public readonly RetrieveFromStringSource headerRetrievalFunc;

	public readonly RetrieveFromStringSource textRetrievalFunc;

	public readonly RetrieveFromBoolSource shouldCloseRetrievalFunc;
}
