using System;
using UnityEngine;

public class SE_Demister : StatusEffect
{

	public override void Setup(Character character)
	{
		base.Setup(character);
		if (this.m_coverRayMask == 0)
		{
			this.m_coverRayMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "terrain" });
		}
	}

	private bool IsUnderRoof()
	{
		RaycastHit raycastHit;
		return Physics.Raycast(this.m_character.GetCenterPoint(), Vector3.up, out raycastHit, 4f, this.m_coverRayMask);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_ballInstance)
		{
			Vector3 vector = this.m_character.GetCenterPoint() + this.m_character.transform.forward * 0.5f;
			this.m_ballInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_ballPrefab, vector, Quaternion.identity);
			return;
		}
		Character character = this.m_character;
		bool flag = this.IsUnderRoof();
		Vector3 position = this.m_character.transform.position;
		Vector3 vector2 = this.m_ballInstance.transform.position;
		Vector3 vector3 = (flag ? this.m_offsetInterior : this.m_offset);
		float num = (flag ? this.m_noiseDistanceInterior : this.m_noiseDistance);
		Vector3 vector4 = position + this.m_character.transform.TransformVector(vector3);
		float num2 = Time.time * this.m_noiseSpeed;
		vector4 += new Vector3(Mathf.Sin(num2 * 4f), Mathf.Sin(num2 * 2f) * this.m_noiseDistanceYScale, Mathf.Cos(num2 * 5f)) * num;
		float num3 = Vector3.Distance(vector4, vector2);
		if (num3 > this.m_maxDistance * 2f)
		{
			vector2 = vector4;
		}
		else if (num3 > this.m_maxDistance)
		{
			Vector3 normalized = (vector2 - vector4).normalized;
			vector2 = vector4 + normalized * this.m_maxDistance;
		}
		Vector3 normalized2 = (vector4 - vector2).normalized;
		this.m_ballVel += normalized2 * this.m_ballAcceleration * dt;
		if (this.m_ballVel.magnitude > this.m_ballMaxSpeed)
		{
			this.m_ballVel = this.m_ballVel.normalized * this.m_ballMaxSpeed;
		}
		if (!flag)
		{
			Vector3 velocity = this.m_character.GetVelocity();
			this.m_ballVel += velocity * this.m_characterVelocityFactor * dt;
		}
		this.m_ballVel -= this.m_ballVel * this.m_ballFriction;
		Vector3 vector5 = vector2 + this.m_ballVel * dt;
		this.m_ballInstance.transform.position = vector5;
		Quaternion quaternion = this.m_ballInstance.transform.rotation;
		quaternion *= Quaternion.Euler(this.m_rotationSpeed, 0f, this.m_rotationSpeed * 0.5321f);
		this.m_ballInstance.transform.rotation = quaternion;
	}

	private void RemoveEffects()
	{
		if (this.m_ballInstance != null)
		{
			ZNetView component = this.m_ballInstance.GetComponent<ZNetView>();
			if (component.IsValid())
			{
				component.ClaimOwnership();
				component.Destroy();
			}
		}
	}

	protected override void OnApplicationQuit()
	{
		base.OnApplicationQuit();
		this.m_ballInstance = null;
	}

	public override void Stop()
	{
		base.Stop();
		this.RemoveEffects();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		this.RemoveEffects();
	}

	[Header("SE_Demister")]
	public GameObject m_ballPrefab;

	public Vector3 m_offset = new Vector3(0f, 2f, 0f);

	public Vector3 m_offsetInterior = new Vector3(0.5f, 1.8f, 0f);

	public float m_maxDistance = 50f;

	public float m_ballAcceleration = 4f;

	public float m_ballMaxSpeed = 10f;

	public float m_ballFriction = 0.1f;

	public float m_noiseDistance = 1f;

	public float m_noiseDistanceInterior = 0.2f;

	public float m_noiseDistanceYScale = 1f;

	public float m_noiseSpeed = 1f;

	public float m_characterVelocityFactor = 1f;

	public float m_rotationSpeed = 1f;

	private int m_coverRayMask;

	private GameObject m_ballInstance;

	private Vector3 m_ballVel = new Vector3(0f, 0f, 0f);
}
