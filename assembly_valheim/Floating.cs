using System;
using System.Collections.Generic;
using UnityEngine;

public class Floating : MonoBehaviour, IWaterInteractable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_collider = base.GetComponentInChildren<Collider>();
		this.SetSurfaceEffect(false);
		Floating.s_waterVolumeMask = LayerMask.GetMask(new string[] { "WaterVolume" });
		base.InvokeRepeating("TerrainCheck", UnityEngine.Random.Range(10f, 30f), 30f);
	}

	private void OnEnable()
	{
		Floating.Instances.Add(this);
	}

	private void OnDisable()
	{
		Floating.Instances.Remove(this);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	private void TerrainCheck()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			Rigidbody component = base.GetComponent<Rigidbody>();
			if (component)
			{
				component.velocity = Vector3.zero;
			}
			ZLog.Log("Moved up item " + base.gameObject.name);
		}
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.HaveLiquidLevel())
		{
			this.SetSurfaceEffect(false);
			return;
		}
		this.UpdateImpactEffect();
		float floatDepth = this.GetFloatDepth();
		if (floatDepth > 0f)
		{
			this.SetSurfaceEffect(false);
			return;
		}
		this.SetSurfaceEffect(true);
		Vector3 vector = this.m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float num = Mathf.Clamp01(Mathf.Abs(floatDepth) / this.m_forceDistance);
		Vector3 vector2 = this.m_force * num * (fixedDeltaTime * 50f) * Vector3.up;
		this.m_body.WakeUp();
		this.m_body.AddForceAtPosition(vector2 * this.m_balanceForceFraction, vector, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(vector2, worldCenterOfMass, ForceMode.VelocityChange);
		this.m_body.velocity = this.m_body.velocity - this.m_damping * num * this.m_body.velocity;
		this.m_body.angularVelocity = this.m_body.angularVelocity - this.m_damping * num * this.m_body.angularVelocity;
	}

	public bool HaveLiquidLevel()
	{
		return this.m_waterLevel > -10000f || this.m_tarLevel > -10000f;
	}

	private void SetSurfaceEffect(bool enabled)
	{
		if (this.m_surfaceEffects != null)
		{
			this.m_surfaceEffects.SetActive(enabled);
		}
	}

	private void UpdateImpactEffect()
	{
		if (this.m_body.IsSleeping() || !this.m_impactEffects.HasEffects())
		{
			return;
		}
		Vector3 vector = this.m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		float num = Mathf.Max(this.m_waterLevel, this.m_tarLevel);
		if (vector.y < num)
		{
			if (!this.m_wasInWater)
			{
				this.m_wasInWater = true;
				Vector3 vector2 = vector;
				vector2.y = num;
				if (this.m_body.GetPointVelocity(vector).magnitude > 0.5f)
				{
					this.m_impactEffects.Create(vector2, Quaternion.identity, null, 1f, -1);
					return;
				}
			}
		}
		else
		{
			this.m_wasInWater = false;
		}
	}

	private float GetFloatDepth()
	{
		ref Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float num = Mathf.Max(this.m_waterLevel, this.m_tarLevel);
		return worldCenterOfMass.y - num - this.m_waterLevelOffset;
	}

	public bool IsInTar()
	{
		return this.m_tarLevel > -10000f && this.m_body.worldCenterOfMass.y - this.m_tarLevel - this.m_waterLevelOffset < -0.2f;
	}

	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type != LiquidType.Water && type != LiquidType.Tar)
		{
			return;
		}
		if (type == LiquidType.Water)
		{
			this.m_waterLevel = level;
		}
		else
		{
			this.m_tarLevel = level;
		}
		if (!this.m_beenFloating && level > -10000f && this.GetFloatDepth() < 0f)
		{
			this.m_beenFloating = true;
		}
	}

	public bool BeenFloating()
	{
		return this.m_beenFloating;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.down * this.m_waterLevelOffset, new Vector3(1f, 0.05f, 1f));
	}

	public static float GetLiquidLevel(Vector3 p, float waveFactor = 1f, LiquidType type = LiquidType.All)
	{
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, Floating.s_tempColliderArray, Floating.s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = Floating.s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			WaterVolume component;
			if (!Floating.s_waterVolumeCache.TryGetValue(instanceID, out component))
			{
				component = collider.GetComponent<WaterVolume>();
				Floating.s_waterVolumeCache[instanceID] = component;
			}
			if (component)
			{
				if (type == LiquidType.All || component.GetLiquidType() == type)
				{
					num = Mathf.Max(num, component.GetWaterSurface(p, waveFactor));
				}
			}
			else
			{
				LiquidSurface component2;
				if (!Floating.s_liquidSurfaceCache.TryGetValue(instanceID, out component2))
				{
					component2 = collider.GetComponent<LiquidSurface>();
					Floating.s_liquidSurfaceCache[instanceID] = component2;
				}
				if (component2 && (type == LiquidType.All || component2.GetLiquidType() == type))
				{
					num = Mathf.Max(num, component2.GetSurface(p));
				}
			}
		}
		return num;
	}

	public static float GetWaterLevel(Vector3 p, ref WaterVolume previousAndOut)
	{
		if (previousAndOut != null && previousAndOut.gameObject.GetComponent<Collider>().bounds.Contains(p))
		{
			return previousAndOut.GetWaterSurface(p, 1f);
		}
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, Floating.s_tempColliderArray, Floating.s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = Floating.s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			WaterVolume component;
			if (!Floating.s_waterVolumeCache.TryGetValue(instanceID, out component))
			{
				component = collider.GetComponent<WaterVolume>();
				Floating.s_waterVolumeCache[instanceID] = component;
			}
			if (component)
			{
				if (component.GetLiquidType() == LiquidType.Water)
				{
					float waterSurface = component.GetWaterSurface(p, 1f);
					if (waterSurface > num)
					{
						num = waterSurface;
						previousAndOut = component;
					}
				}
			}
			else
			{
				LiquidSurface component2;
				if (!Floating.s_liquidSurfaceCache.TryGetValue(instanceID, out component2))
				{
					component2 = collider.GetComponent<LiquidSurface>();
					Floating.s_liquidSurfaceCache[instanceID] = component2;
				}
				if (component2 && component2.GetLiquidType() == LiquidType.Water)
				{
					num = Mathf.Max(num, component2.GetSurface(p));
				}
			}
		}
		return num;
	}

	public int Increment(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] + 1;
		liquids[(int)type] = num;
		return num;
	}

	public int Decrement(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] - 1;
		liquids[(int)type] = num;
		return num;
	}

	public static List<Floating> Instances { get; } = new List<Floating>();

	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_balanceForceFraction = 0.02f;

	public float m_damping = 0.05f;

	public EffectList m_impactEffects = new EffectList();

	public GameObject m_surfaceEffects;

	private static int s_waterVolumeMask = 0;

	private static readonly Collider[] s_tempColliderArray = new Collider[256];

	private static readonly Dictionary<int, WaterVolume> s_waterVolumeCache = new Dictionary<int, WaterVolume>();

	private static readonly Dictionary<int, LiquidSurface> s_liquidSurfaceCache = new Dictionary<int, LiquidSurface>();

	private float m_waterLevel = -10000f;

	private float m_tarLevel = -10000f;

	private bool m_beenFloating;

	private bool m_wasInWater = true;

	private const float c_MinImpactEffectVelocity = 0.5f;

	private Rigidbody m_body;

	private Collider m_collider;

	private ZNetView m_nview;

	private readonly int[] m_liquids = new int[2];
}
