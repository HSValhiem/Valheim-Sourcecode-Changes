﻿using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectEnsureVisible : MonoBehaviour
{

	private void Awake()
	{
		if (!this.mInitialized)
		{
			this.Initialize();
		}
	}

	private void Initialize()
	{
		this.mScrollRect = base.GetComponent<ScrollRect>();
		this.mScrollTransform = this.mScrollRect.transform as RectTransform;
		this.mContent = this.mScrollRect.content;
		this.Reset();
		this.mInitialized = true;
	}

	public void CenterOnItem(RectTransform target)
	{
		if (!this.mInitialized)
		{
			this.Initialize();
		}
		Vector3 worldPointInWidget = this.GetWorldPointInWidget(this.mScrollTransform, this.GetWidgetWorldPoint(target));
		Vector3 vector = this.GetWorldPointInWidget(this.mScrollTransform, this.GetWidgetWorldPoint(this.maskTransform)) - worldPointInWidget;
		vector.z = 0f;
		if (!this.mScrollRect.horizontal)
		{
			vector.x = 0f;
		}
		if (!this.mScrollRect.vertical)
		{
			vector.y = 0f;
		}
		Vector2 vector2 = new Vector2(vector.x / (this.mContent.rect.size.x - this.mScrollTransform.rect.size.x), vector.y / (this.mContent.rect.size.y - this.mScrollTransform.rect.size.y));
		Vector2 vector3 = this.mScrollRect.normalizedPosition - vector2;
		if (this.mScrollRect.movementType != ScrollRect.MovementType.Unrestricted)
		{
			vector3.x = Mathf.Clamp01(vector3.x);
			vector3.y = Mathf.Clamp01(vector3.y);
		}
		this.mScrollRect.normalizedPosition = vector3;
	}

	private void Reset()
	{
		if (this.maskTransform == null)
		{
			Mask componentInChildren = base.GetComponentInChildren<Mask>(true);
			if (componentInChildren)
			{
				this.maskTransform = componentInChildren.rectTransform;
			}
			if (this.maskTransform == null)
			{
				RectMask2D componentInChildren2 = base.GetComponentInChildren<RectMask2D>(true);
				if (componentInChildren2)
				{
					this.maskTransform = componentInChildren2.rectTransform;
				}
			}
		}
	}

	private Vector3 GetWidgetWorldPoint(RectTransform target)
	{
		Vector3 vector = new Vector3((0.5f - target.pivot.x) * target.rect.size.x, (0.5f - target.pivot.y) * target.rect.size.y, 0f);
		Vector3 vector2 = target.localPosition + vector;
		return target.parent.TransformPoint(vector2);
	}

	private Vector3 GetWorldPointInWidget(RectTransform target, Vector3 worldPoint)
	{
		return target.InverseTransformPoint(worldPoint);
	}

	private RectTransform maskTransform;

	private ScrollRect mScrollRect;

	private RectTransform mScrollTransform;

	private RectTransform mContent;

	private bool mInitialized;
}
