using System;
using System.Collections.Generic;
using UnityEngine;

public class WaterTrigger : MonoBehaviour
{

	private void Start()
	{
		this.m_cooldownTimer = UnityEngine.Random.Range(0f, 2f);
	}

	private void OnEnable()
	{
		WaterTrigger.Instances.Add(this);
	}

	private void OnDisable()
	{
		WaterTrigger.Instances.Remove(this);
	}

	public void CustomUpdate(float deltaTime)
	{
		this.m_cooldownTimer += deltaTime;
		if (this.m_cooldownTimer <= this.m_cooldownDelay)
		{
			return;
		}
		Transform transform = base.transform;
		Vector3 position = transform.position;
		float waterLevel = Floating.GetWaterLevel(position, ref this.m_previousAndOut);
		if (position.y < waterLevel)
		{
			this.m_effects.Create(position, transform.rotation, transform, 1f, -1);
			this.m_cooldownTimer = 0f;
		}
	}

	public static List<WaterTrigger> Instances { get; } = new List<WaterTrigger>();

	public EffectList m_effects = new EffectList();

	public float m_cooldownDelay = 2f;

	private float m_cooldownTimer;

	private WaterVolume m_previousAndOut;
}
