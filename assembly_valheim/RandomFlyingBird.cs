using System;
using UnityEngine;

public class RandomFlyingBird : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_anim = base.GetComponentInChildren<ZSyncAnimation>();
		this.m_lodGroup = base.GetComponent<LODGroup>();
		this.m_landedModel.SetActive(true);
		this.m_flyingModel.SetActive(true);
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, this.m_spawnPoint);
		}
		this.m_randomNoiseTimer = UnityEngine.Random.Range(this.m_randomNoiseIntervalMin, this.m_randomNoiseIntervalMax);
		if (this.m_nview.IsOwner())
		{
			this.RandomizeWaypoint(false);
		}
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		bool flag = EnvMan.instance.IsDaylight();
		this.m_randomNoiseTimer -= fixedDeltaTime;
		if (this.m_randomNoiseTimer <= 0f)
		{
			if (flag || !this.m_noNoiseAtNight)
			{
				this.m_randomNoise.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
			}
			this.m_randomNoiseTimer = UnityEngine.Random.Range(this.m_randomNoiseIntervalMin, this.m_randomNoiseIntervalMax);
		}
		bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_landed, false);
		this.m_landedModel.SetActive(@bool);
		this.m_flyingModel.SetActive(!@bool);
		this.SetVisible(this.m_nview.HasOwner());
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_flyTimer += fixedDeltaTime;
		this.m_modeTimer += fixedDeltaTime;
		if (@bool)
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			forward.Normalize();
			base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			this.m_landedTimer += fixedDeltaTime;
			if (((flag || !this.m_noRandomFlightAtNight) && this.m_landedTimer > this.m_landDuration) || this.DangerNearby(base.transform.position))
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_landed, false);
				this.RandomizeWaypoint(false);
				return;
			}
		}
		else
		{
			if (this.m_flapping)
			{
				if (this.m_modeTimer > this.m_flapDuration)
				{
					this.m_modeTimer = 0f;
					this.m_flapping = false;
				}
			}
			else if (this.m_modeTimer > this.m_sailDuration)
			{
				this.m_flapping = true;
				this.m_modeTimer = 0f;
			}
			this.m_anim.SetBool(RandomFlyingBird.s_flapping, this.m_flapping);
			Vector3 vector = Vector3.Normalize(this.m_waypoint - base.transform.position);
			float num = (this.m_groundwp ? (this.m_turnRate * 4f) : this.m_turnRate);
			Vector3 vector2 = Vector3.RotateTowards(base.transform.forward, vector, num * 0.0174532924f * fixedDeltaTime, 1f);
			float num2 = Vector3.SignedAngle(base.transform.forward, vector, Vector3.up);
			Vector3 vector3 = Vector3.Cross(vector2, Vector3.up);
			Vector3 vector4 = Vector3.up;
			if (num2 > 0f)
			{
				vector4 += -vector3 * 1.5f * Utils.LerpStep(0f, 45f, num2);
			}
			else
			{
				vector4 += vector3 * 1.5f * Utils.LerpStep(0f, 45f, -num2);
			}
			float num3 = this.m_speed;
			bool flag2 = false;
			if (this.m_groundwp)
			{
				float num4 = Vector3.Distance(base.transform.position, this.m_waypoint);
				if (num4 < 5f)
				{
					num3 *= Mathf.Clamp(num4 / 5f, 0.2f, 1f);
					vector2.y = 0f;
					vector2.Normalize();
					vector4 = Vector3.up;
					flag2 = true;
				}
				if (num4 < 0.2f)
				{
					base.transform.position = this.m_waypoint;
					this.m_nview.GetZDO().Set(ZDOVars.s_landed, true);
					this.m_landedTimer = 0f;
					this.m_flapping = true;
					this.m_modeTimer = 0f;
				}
			}
			else if (this.m_flyTimer >= this.m_wpDuration)
			{
				bool flag3 = UnityEngine.Random.value < this.m_landChance;
				this.RandomizeWaypoint(flag3);
			}
			Quaternion quaternion = Quaternion.LookRotation(vector2, vector4.normalized);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, 200f * fixedDeltaTime);
			if (flag2)
			{
				base.transform.position += vector * num3 * fixedDeltaTime;
				return;
			}
			base.transform.position += base.transform.forward * num3 * fixedDeltaTime;
		}
	}

	private void RandomizeWaypoint(bool ground)
	{
		this.m_flyTimer = 0f;
		Vector3 vector;
		if (ground && this.FindLandingPoint(out vector))
		{
			this.m_waypoint = vector;
			this.m_groundwp = true;
			return;
		}
		Vector2 vector2 = UnityEngine.Random.insideUnitCircle * this.m_flyRange;
		this.m_waypoint = this.m_spawnPoint + new Vector3(vector2.x, 0f, vector2.y);
		float num;
		if (ZoneSystem.instance.GetSolidHeight(this.m_waypoint, out num, 1000))
		{
			float num2 = ZoneSystem.instance.m_waterLevel + 2f;
			if (num < num2)
			{
				num = num2;
			}
			this.m_waypoint.y = num + UnityEngine.Random.Range(this.m_minAlt, this.m_maxAlt);
		}
		this.m_groundwp = false;
	}

	private bool FindLandingPoint(out Vector3 waypoint)
	{
		waypoint = new Vector3(0f, -999f, 0f);
		bool flag = false;
		for (int i = 0; i < 10; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_flyRange;
			Vector3 vector2 = this.m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
			float num;
			if (ZoneSystem.instance.GetSolidHeight(vector2, out num, 1000) && num > ZoneSystem.instance.m_waterLevel && num > waypoint.y)
			{
				vector2.y = num;
				if (!this.DangerNearby(vector2))
				{
					waypoint = vector2;
					flag = true;
				}
			}
		}
		return flag;
	}

	private bool DangerNearby(Vector3 p)
	{
		return Player.IsPlayerInRange(p, this.m_avoidDangerDistance);
	}

	private void SetVisible(bool visible)
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		if (this.m_lodVisible == visible)
		{
			return;
		}
		this.m_lodVisible = visible;
		if (this.m_lodVisible)
		{
			this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
			return;
		}
		this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
	}

	public float m_flyRange = 20f;

	public float m_minAlt = 5f;

	public float m_maxAlt = 20f;

	public float m_speed = 10f;

	public float m_turnRate = 10f;

	public float m_wpDuration = 4f;

	public float m_flapDuration = 2f;

	public float m_sailDuration = 4f;

	public float m_landChance = 0.5f;

	public float m_landDuration = 2f;

	public float m_avoidDangerDistance = 4f;

	public bool m_noRandomFlightAtNight = true;

	public float m_randomNoiseIntervalMin = 3f;

	public float m_randomNoiseIntervalMax = 6f;

	public bool m_noNoiseAtNight = true;

	public EffectList m_randomNoise = new EffectList();

	public GameObject m_flyingModel;

	public GameObject m_landedModel;

	private Vector3 m_spawnPoint;

	private Vector3 m_waypoint;

	private bool m_groundwp;

	private float m_flyTimer;

	private float m_modeTimer;

	private float m_randomNoiseTimer;

	private ZSyncAnimation m_anim;

	private bool m_flapping = true;

	private float m_landedTimer;

	private static readonly int s_flapping = ZSyncAnimation.GetHash("flapping");

	private ZNetView m_nview;

	protected LODGroup m_lodGroup;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;
}
