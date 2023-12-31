﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class LineConnect : MonoBehaviour
{

	private void Awake()
	{
		this.m_lineRenderer = base.GetComponent<LineRenderer>();
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_linePeerID = ZDO.GetHashZDOID(this.m_netViewPrefix + "line_peer");
		this.m_slackHash = (this.m_netViewPrefix + "line_slack").GetStableHashCode();
	}

	private void LateUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			this.m_lineRenderer.enabled = false;
			return;
		}
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID(this.m_linePeerID);
		GameObject gameObject = ZNetScene.instance.FindInstance(zdoid);
		if (gameObject && !string.IsNullOrEmpty(this.m_childObject))
		{
			Transform transform = Utils.FindChild(gameObject.transform, this.m_childObject);
			if (transform)
			{
				gameObject = transform.gameObject;
			}
		}
		if (gameObject != null)
		{
			Vector3 vector = gameObject.transform.position;
			if (this.m_centerOfCharacter)
			{
				Character component = gameObject.GetComponent<Character>();
				if (component)
				{
					vector = component.GetCenterPoint();
				}
			}
			this.SetEndpoint(vector);
			this.m_lineRenderer.enabled = true;
			return;
		}
		if (this.m_hideIfNoConnection)
		{
			this.m_lineRenderer.enabled = false;
			return;
		}
		this.m_lineRenderer.enabled = true;
		this.SetEndpoint(base.transform.position + this.m_noConnectionWorldOffset);
	}

	private void SetEndpoint(Vector3 pos)
	{
		Vector3 vector = base.transform.InverseTransformPoint(pos);
		Vector3 vector2 = base.transform.InverseTransformDirection(Vector3.down);
		if (this.m_dynamicSlack)
		{
			float @float = this.m_nview.GetZDO().GetFloat(this.m_slackHash, this.m_slack);
			Vector3 position = this.m_lineRenderer.GetPosition(0);
			Vector3 vector3 = vector;
			float num = Vector3.Distance(position, vector3) / 2f;
			for (int i = 1; i < this.m_lineRenderer.positionCount; i++)
			{
				float num2 = (float)i / (float)(this.m_lineRenderer.positionCount - 1);
				float num3 = Mathf.Abs(0.5f - num2) * 2f;
				num3 *= num3;
				num3 = 1f - num3;
				Vector3 vector4 = Vector3.Lerp(position, vector3, num2);
				vector4 += vector2 * num * @float * num3;
				this.m_lineRenderer.SetPosition(i, vector4);
			}
		}
		else
		{
			this.m_lineRenderer.SetPosition(1, vector);
		}
		if (this.m_dynamicThickness)
		{
			float num4 = Vector3.Distance(base.transform.position, pos);
			float num5 = Utils.LerpStep(this.m_minDistance, this.m_maxDistance, num4);
			num5 = Mathf.Pow(num5, this.m_thicknessPower);
			this.m_lineRenderer.widthMultiplier = Mathf.Lerp(this.m_maxThickness, this.m_minThickness, num5);
		}
	}

	public void SetPeer(ZNetView other)
	{
		if (other)
		{
			this.SetPeer(other.GetZDO().m_uid);
			return;
		}
		this.SetPeer(ZDOID.None);
	}

	public void SetPeer(ZDOID zdoid)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(this.m_linePeerID, zdoid);
	}

	public void SetSlack(float slack)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(this.m_slackHash, slack);
	}

	public bool m_centerOfCharacter;

	public string m_childObject = "";

	public bool m_hideIfNoConnection = true;

	public Vector3 m_noConnectionWorldOffset = new Vector3(0f, -1f, 0f);

	[Header("Dynamic slack")]
	public bool m_dynamicSlack;

	public float m_slack = 0.5f;

	[Header("Thickness")]
	public bool m_dynamicThickness = true;

	public float m_minDistance = 6f;

	public float m_maxDistance = 30f;

	public float m_minThickness = 0.2f;

	public float m_maxThickness = 0.8f;

	public float m_thicknessPower = 0.2f;

	public string m_netViewPrefix = "";

	private LineRenderer m_lineRenderer;

	private ZNetView m_nview;

	private KeyValuePair<int, int> m_linePeerID;

	private int m_slackHash;
}
