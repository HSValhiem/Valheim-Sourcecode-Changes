using System;
using System.Collections.Generic;
using UnityEngine;

public class Tail : MonoBehaviour
{

	private void Awake()
	{
		foreach (Transform transform in this.m_tailJoints)
		{
			float num = Vector3.Distance(transform.parent.position, transform.position);
			Vector3 position = transform.position;
			Tail.TailSegment tailSegment = new Tail.TailSegment();
			tailSegment.transform = transform;
			tailSegment.pos = position;
			tailSegment.rot = transform.rotation;
			tailSegment.distance = num;
			this.m_positions.Add(tailSegment);
		}
	}

	private void OnEnable()
	{
		Tail.Instances.Add(this);
	}

	private void OnDisable()
	{
		Tail.Instances.Remove(this);
	}

	public void CustomLateUpdate(float dt)
	{
		for (int i = 0; i < this.m_positions.Count; i++)
		{
			Tail.TailSegment tailSegment = this.m_positions[i];
			if (this.m_waterSurfaceCheck)
			{
				float liquidLevel = Floating.GetLiquidLevel(tailSegment.pos, 1f, LiquidType.All);
				if (tailSegment.pos.y + this.m_tailRadius > liquidLevel)
				{
					Tail.TailSegment tailSegment2 = tailSegment;
					tailSegment2.pos.y = tailSegment2.pos.y - this.m_gravity * dt;
				}
				else
				{
					Tail.TailSegment tailSegment3 = tailSegment;
					tailSegment3.pos.y = tailSegment3.pos.y - this.m_gravityInWater * dt;
				}
			}
			else
			{
				Tail.TailSegment tailSegment4 = tailSegment;
				tailSegment4.pos.y = tailSegment4.pos.y - this.m_gravity * dt;
			}
			Vector3 vector = tailSegment.transform.parent.position + tailSegment.transform.parent.up * tailSegment.distance * 0.5f;
			Vector3 vector2 = Vector3.Normalize(vector - tailSegment.pos);
			vector2 = Vector3.RotateTowards(-tailSegment.transform.parent.up, vector2, 0.0174532924f * this.m_maxAngle, 1f);
			Vector3 vector3 = vector - vector2 * tailSegment.distance * 0.5f;
			if (this.m_groundCheck)
			{
				float groundHeight = ZoneSystem.instance.GetGroundHeight(vector3);
				if (vector3.y - this.m_tailRadius < groundHeight)
				{
					vector3.y = groundHeight + this.m_tailRadius;
				}
			}
			vector3 = Vector3.Lerp(tailSegment.pos, vector3, this.m_smoothness);
			if (vector == vector3)
			{
				return;
			}
			Vector3 normalized = (vector - vector3).normalized;
			Vector3 vector4 = Vector3.Cross(Vector3.up, -normalized);
			Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(-normalized, vector4), -normalized);
			quaternion = Quaternion.Slerp(tailSegment.rot, quaternion, this.m_smoothness);
			tailSegment.transform.position = vector3;
			tailSegment.transform.rotation = quaternion;
			tailSegment.pos = vector3;
			tailSegment.rot = quaternion;
		}
		if (this.m_tailBody)
		{
			this.m_tailBody.velocity = Vector3.zero;
			this.m_tailBody.angularVelocity = Vector3.zero;
		}
	}

	public static List<Tail> Instances { get; } = new List<Tail>();

	public List<Transform> m_tailJoints = new List<Transform>();

	public float m_maxAngle = 80f;

	public float m_gravity = 2f;

	public float m_gravityInWater = 0.1f;

	public bool m_waterSurfaceCheck;

	public bool m_groundCheck;

	public float m_smoothness = 0.1f;

	public float m_tailRadius;

	public Character m_character;

	public Rigidbody m_characterBody;

	public Rigidbody m_tailBody;

	private readonly List<Tail.TailSegment> m_positions = new List<Tail.TailSegment>();

	private class TailSegment
	{

		public Transform transform;

		public Vector3 pos;

		public Quaternion rot;

		public float distance;
	}
}
