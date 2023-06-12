using System;
using System.Collections.Generic;
using UnityEngine;

public class LuredWisp : MonoBehaviour
{

	private void Awake()
	{
		LuredWisp.m_wisps.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_targetPoint = base.transform.position;
		this.m_time = (float)UnityEngine.Random.Range(0, 1000);
		base.InvokeRepeating("UpdateTarget", UnityEngine.Random.Range(0f, 2f), 2f);
	}

	private void OnDestroy()
	{
		LuredWisp.m_wisps.Remove(this);
	}

	private void UpdateTarget()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_despawnTimer > 0f)
		{
			return;
		}
		WispSpawner bestSpawner = WispSpawner.GetBestSpawner(base.transform.position, this.m_maxLureDistance);
		if (bestSpawner == null || (this.m_despawnInDaylight && EnvMan.instance.IsDaylight()))
		{
			this.m_despawnTimer = 3f;
			this.m_targetPoint = base.transform.position + Quaternion.Euler(-20f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * 100f;
			return;
		}
		this.m_despawnTimer = 0f;
		this.m_targetPoint = bestSpawner.m_spawnPoint.position;
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateMovement(this.m_targetPoint, Time.fixedDeltaTime);
	}

	private void UpdateMovement(Vector3 targetPos, float dt)
	{
		if (this.m_despawnTimer > 0f)
		{
			this.m_despawnTimer -= dt;
			if (this.m_despawnTimer <= 0f)
			{
				this.m_despawnEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				this.m_nview.Destroy();
				return;
			}
		}
		this.m_time += dt;
		float num = this.m_time * this.m_noiseSpeed;
		targetPos += new Vector3(Mathf.Sin(num * 4f), Mathf.Sin(num * 2f) * this.m_noiseDistanceYScale, Mathf.Cos(num * 5f)) * this.m_noiseDistance;
		Vector3 normalized = (targetPos - base.transform.position).normalized;
		this.m_ballVel += normalized * this.m_acceleration * dt;
		if (this.m_ballVel.magnitude > this.m_maxSpeed)
		{
			this.m_ballVel = this.m_ballVel.normalized * this.m_maxSpeed;
		}
		this.m_ballVel -= this.m_ballVel * this.m_friction;
		base.transform.position = base.transform.position + this.m_ballVel * dt;
	}

	public static int GetWispsInArea(Vector3 p, float r)
	{
		float num = r * r;
		int num2 = 0;
		foreach (LuredWisp luredWisp in LuredWisp.m_wisps)
		{
			if (Utils.DistanceSqr(p, luredWisp.transform.position) < num)
			{
				num2++;
			}
		}
		return num2;
	}

	public bool m_despawnInDaylight = true;

	public float m_maxLureDistance = 20f;

	public float m_acceleration = 6f;

	public float m_noiseDistance = 1.5f;

	public float m_noiseDistanceYScale = 0.2f;

	public float m_noiseSpeed = 0.5f;

	public float m_maxSpeed = 40f;

	public float m_friction = 0.03f;

	public EffectList m_despawnEffects = new EffectList();

	private static List<LuredWisp> m_wisps = new List<LuredWisp>();

	private Vector3 m_ballVel = Vector3.zero;

	private ZNetView m_nview;

	private Vector3 m_targetPoint;

	private float m_despawnTimer;

	private float m_time;
}
