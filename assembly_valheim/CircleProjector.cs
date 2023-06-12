using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CircleProjector : MonoBehaviour
{

	private void Start()
	{
		this.CreateSegments();
	}

	private void Update()
	{
		this.CreateSegments();
		bool flag = this.m_turns == 1f;
		float num = 6.28318548f * this.m_turns / (float)(this.m_nrOfSegments - (flag ? 0 : 1));
		float num2 = ((flag && !this.m_sliceLines) ? (Time.time * this.m_speed) : 0f);
		for (int i = 0; i < this.m_nrOfSegments; i++)
		{
			float num3 = 0.0174532924f * this.m_start + (float)i * num + num2;
			Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(num3) * this.m_radius, 0f, Mathf.Cos(num3) * this.m_radius);
			GameObject gameObject = this.m_segments[i];
			RaycastHit raycastHit;
			if (Physics.Raycast(vector + Vector3.up * 500f, Vector3.down, out raycastHit, 1000f, this.m_mask.value))
			{
				vector.y = raycastHit.point.y;
			}
			gameObject.transform.position = vector;
		}
		for (int j = 0; j < this.m_nrOfSegments; j++)
		{
			GameObject gameObject2 = this.m_segments[j];
			GameObject gameObject3;
			GameObject gameObject4;
			if (flag)
			{
				gameObject3 = ((j == 0) ? this.m_segments[this.m_nrOfSegments - 1] : this.m_segments[j - 1]);
				gameObject4 = ((j == this.m_nrOfSegments - 1) ? this.m_segments[0] : this.m_segments[j + 1]);
			}
			else
			{
				gameObject3 = ((j == 0) ? gameObject2 : this.m_segments[j - 1]);
				gameObject4 = ((j == this.m_nrOfSegments - 1) ? gameObject2 : this.m_segments[j + 1]);
			}
			Vector3 normalized = (gameObject4.transform.position - gameObject3.transform.position).normalized;
			gameObject2.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
		}
		for (int k = this.m_nrOfSegments; k < this.m_segments.Count; k++)
		{
			Vector3 position = this.m_segments[k].transform.position;
			RaycastHit raycastHit2;
			if (Physics.Raycast(position + Vector3.up * 500f, Vector3.down, out raycastHit2, 1000f, this.m_mask.value))
			{
				position.y = raycastHit2.point.y;
			}
			this.m_segments[k].transform.position = position;
		}
	}

	private void CreateSegments()
	{
		if ((!this.m_sliceLines && this.m_segments.Count == this.m_nrOfSegments) || (this.m_sliceLines && this.m_calcStart == this.m_start && this.m_calcTurns == this.m_turns))
		{
			return;
		}
		foreach (GameObject gameObject in this.m_segments)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		this.m_segments.Clear();
		for (int i = 0; i < this.m_nrOfSegments; i++)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform.position, Quaternion.identity, base.transform);
			this.m_segments.Add(gameObject2);
		}
		this.m_calcStart = this.m_start;
		this.m_calcTurns = this.m_turns;
		if (this.m_sliceLines)
		{
			float start = this.m_start;
			float num = this.m_start + 6.28318548f * this.m_turns * 57.29578f;
			float num2 = 2f * this.m_radius * 3.14159274f * this.m_turns / (float)this.m_nrOfSegments;
			int num3 = (int)(this.m_radius / num2) - 2;
			this.<CreateSegments>g__placeSlices|2_0(start, num3);
			this.<CreateSegments>g__placeSlices|2_0(num, num3);
		}
	}

	[CompilerGenerated]
	private void <CreateSegments>g__placeSlices|2_0(float angle, int count)
	{
		for (int i = 0; i < count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform.position, Quaternion.Euler(0f, angle, 0f), base.transform);
			gameObject.transform.position += gameObject.transform.forward * this.m_radius * ((float)(i + 1) / (float)(count + 1));
			this.m_segments.Add(gameObject);
		}
	}

	public float m_radius = 5f;

	public int m_nrOfSegments = 20;

	public float m_speed = 0.1f;

	public float m_turns = 1f;

	public float m_start;

	public bool m_sliceLines;

	private float m_calcStart;

	private float m_calcTurns;

	public GameObject m_prefab;

	public LayerMask m_mask;

	private List<GameObject> m_segments = new List<GameObject>();
}
