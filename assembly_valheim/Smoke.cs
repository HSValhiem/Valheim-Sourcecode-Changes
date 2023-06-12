using System;
using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour
{

	private void Awake()
	{
		Smoke.s_smoke.Add(this);
		this.m_added = true;
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_mr = base.GetComponent<MeshRenderer>();
		this.m_body.maxDepenetrationVelocity = 1f;
		this.m_vel += Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * this.m_randomVel;
	}

	private void OnEnable()
	{
		Smoke.Instances.Add(this);
	}

	private void OnDisable()
	{
		Smoke.Instances.Remove(this);
	}

	private void OnDestroy()
	{
		if (this.m_added)
		{
			Smoke.s_smoke.Remove(this);
			this.m_added = false;
		}
	}

	public void StartFadeOut()
	{
		if (this.m_fadeTimer >= 0f)
		{
			return;
		}
		if (this.m_added)
		{
			Smoke.s_smoke.Remove(this);
			this.m_added = false;
		}
		this.m_fadeTimer = 0f;
	}

	public static int GetTotalSmoke()
	{
		return Smoke.s_smoke.Count;
	}

	public static void FadeOldest()
	{
		if (Smoke.s_smoke.Count == 0)
		{
			return;
		}
		Smoke.s_smoke[0].StartFadeOut();
	}

	public static void FadeMostDistant()
	{
		if (Smoke.s_smoke.Count == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < Smoke.s_smoke.Count; i++)
		{
			float num3 = Vector3.Distance(Smoke.s_smoke[i].transform.position, position);
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		if (num != -1)
		{
			Smoke.s_smoke[num].StartFadeOut();
		}
	}

	public void CustomUpdate(float deltaTime)
	{
		this.m_time += deltaTime;
		if (this.m_time > this.m_ttl && this.m_fadeTimer < 0f)
		{
			this.StartFadeOut();
		}
		float num = 1f - Mathf.Clamp01(this.m_time / this.m_ttl);
		this.m_body.mass = num * num;
		Vector3 velocity = this.m_body.velocity;
		Vector3 vel = this.m_vel;
		vel.y *= num;
		Vector3 vector = vel - velocity;
		this.m_body.AddForce(vector * this.m_force * deltaTime, ForceMode.VelocityChange);
		if (this.m_fadeTimer >= 0f)
		{
			this.m_fadeTimer += deltaTime;
			float num2 = 1f - Mathf.Clamp01(this.m_fadeTimer / this.m_fadetime);
			Color color = this.m_mr.material.color;
			color.a = num2;
			this.m_mr.material.color = color;
			if (this.m_fadeTimer >= this.m_fadetime)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}

	public static List<Smoke> Instances { get; } = new List<Smoke>();

	public Vector3 m_vel = Vector3.up;

	public float m_randomVel = 0.1f;

	public float m_force = 0.1f;

	public float m_ttl = 10f;

	public float m_fadetime = 3f;

	private Rigidbody m_body;

	private float m_time;

	private float m_fadeTimer = -1f;

	private bool m_added;

	private MeshRenderer m_mr;

	private static readonly List<Smoke> s_smoke = new List<Smoke>();
}
