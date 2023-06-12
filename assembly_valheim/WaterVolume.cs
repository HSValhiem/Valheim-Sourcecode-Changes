using System;
using System.Collections.Generic;
using UnityEngine;

public class WaterVolume : MonoBehaviour
{

	private void Awake()
	{
		this.m_collider = base.GetComponent<Collider>();
		if (WaterVolume.s_createWaveTangents == null)
		{
			WaterVolume.s_createWaveTangents = new Vector2[]
			{
				new Vector2(-WaterVolume.s_createWaveDirections[0].y, WaterVolume.s_createWaveDirections[0].x),
				new Vector2(-WaterVolume.s_createWaveDirections[1].y, WaterVolume.s_createWaveDirections[1].x),
				new Vector2(-WaterVolume.s_createWaveDirections[2].y, WaterVolume.s_createWaveDirections[2].x),
				new Vector2(-WaterVolume.s_createWaveDirections[3].y, WaterVolume.s_createWaveDirections[3].x),
				new Vector2(-WaterVolume.s_createWaveDirections[4].y, WaterVolume.s_createWaveDirections[4].x),
				new Vector2(-WaterVolume.s_createWaveDirections[5].y, WaterVolume.s_createWaveDirections[5].x),
				new Vector2(-WaterVolume.s_createWaveDirections[6].y, WaterVolume.s_createWaveDirections[6].x),
				new Vector2(-WaterVolume.s_createWaveDirections[7].y, WaterVolume.s_createWaveDirections[7].x),
				new Vector2(-WaterVolume.s_createWaveDirections[8].y, WaterVolume.s_createWaveDirections[8].x),
				new Vector2(-WaterVolume.s_createWaveDirections[9].y, WaterVolume.s_createWaveDirections[9].x)
			};
		}
	}

	private void Start()
	{
		this.DetectWaterDepth();
		this.SetupMaterial();
	}

	private void OnEnable()
	{
		WaterVolume.Instances.Add(this);
	}

	private void OnDisable()
	{
		WaterVolume.Instances.Remove(this);
	}

	private void DetectWaterDepth()
	{
		if (this.m_heightmap)
		{
			float[] oceanDepth = this.m_heightmap.GetOceanDepth();
			this.m_normalizedDepth[0] = Mathf.Clamp01(oceanDepth[0] / 10f);
			this.m_normalizedDepth[1] = Mathf.Clamp01(oceanDepth[1] / 10f);
			this.m_normalizedDepth[2] = Mathf.Clamp01(oceanDepth[2] / 10f);
			this.m_normalizedDepth[3] = Mathf.Clamp01(oceanDepth[3] / 10f);
			return;
		}
		this.m_normalizedDepth[0] = this.m_forceDepth;
		this.m_normalizedDepth[1] = this.m_forceDepth;
		this.m_normalizedDepth[2] = this.m_forceDepth;
		this.m_normalizedDepth[3] = this.m_forceDepth;
	}

	public static void StaticUpdate()
	{
		WaterVolume.UpdateWaterTime(Time.deltaTime);
		if (EnvMan.instance)
		{
			EnvMan.instance.GetWindData(out WaterVolume.s_globalWind1, out WaterVolume.s_globalWind2, out WaterVolume.s_globalWindAlpha);
		}
	}

	public void Update1()
	{
		this.UpdateFloaters();
	}

	public void Update2()
	{
		this.m_waterSurface.material.SetFloat(WaterVolume.s_shaderWaterTime, WaterVolume.s_waterTime);
	}

	private static void UpdateWaterTime(float dt)
	{
		WaterVolume.s_wrappedDayTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
		float num = WaterVolume.s_wrappedDayTimeSeconds;
		WaterVolume.s_waterTime += dt;
		if (Mathf.Abs(num - WaterVolume.s_waterTime) > 10f)
		{
			WaterVolume.s_waterTime = num;
		}
		WaterVolume.s_waterTime = Mathf.Lerp(WaterVolume.s_waterTime, num, 0.05f);
	}

	private void SetupMaterial()
	{
		if (this.m_forceDepth >= 0f)
		{
			this.m_waterSurface.material.SetFloatArray(WaterVolume.s_shaderDepth, new float[] { this.m_forceDepth, this.m_forceDepth, this.m_forceDepth, this.m_forceDepth });
		}
		else
		{
			this.m_waterSurface.material.SetFloatArray(WaterVolume.s_shaderDepth, this.m_normalizedDepth);
		}
		this.m_waterSurface.material.SetFloat(WaterVolume.s_shaderUseGlobalWind, this.m_useGlobalWind ? 1f : 0f);
	}

	public LiquidType GetLiquidType()
	{
		return LiquidType.Water;
	}

	public float GetWaterSurface(Vector3 point, float waveFactor = 1f)
	{
		float num = WaterVolume.s_wrappedDayTimeSeconds;
		float num2 = this.Depth(point);
		float num3 = ((num2 == 0f) ? 0f : this.CalcWave(point, num2, num, waveFactor));
		float num4 = base.transform.position.y + num3 + this.m_surfaceOffset;
		if (this.m_forceDepth < 0f && Utils.LengthXZ(point) > 10500f)
		{
			num4 -= 100f;
		}
		return num4;
	}

	private float TrochSin(float x, float k)
	{
		return Mathf.Sin(x - Mathf.Cos(x) * k) * 0.5f + 0.5f;
	}

	private float CreateWave(Vector3 worldPos, float time, float waveSpeed, float waveLength, float waveHeight, Vector2 dir, Vector2 tangent, float sharpness)
	{
		Vector2 vector = -(worldPos.z * dir + worldPos.x * tangent);
		float num = time * waveSpeed;
		return (this.TrochSin(num + vector.y * waveLength, sharpness) * this.TrochSin(num * 0.123f + vector.x * 0.13123f * waveLength, sharpness) - 0.2f) * waveHeight;
	}

	private float CalcWave(Vector3 worldPos, float depth, Vector4 wind, float waterTime, float waveFactor)
	{
		WaterVolume.s_createWaveDirections[0].x = wind.x;
		WaterVolume.s_createWaveDirections[0].y = wind.z;
		WaterVolume.s_createWaveDirections[0].Normalize();
		WaterVolume.s_createWaveTangents[0].x = -WaterVolume.s_createWaveDirections[0].y;
		WaterVolume.s_createWaveTangents[0].y = WaterVolume.s_createWaveDirections[0].x;
		float w = wind.w;
		float num = Mathf.LerpUnclamped(0f, w, depth);
		float num2 = waterTime / 20f;
		float num3 = this.CreateWave(worldPos, num2, 10f, 0.04f, 8f, WaterVolume.s_createWaveDirections[0], WaterVolume.s_createWaveTangents[0], 0.5f);
		float num4 = this.CreateWave(worldPos, num2, 14.123f, 0.08f, 6f, WaterVolume.s_createWaveDirections[1], WaterVolume.s_createWaveTangents[1], 0.5f);
		float num5 = this.CreateWave(worldPos, num2, 22.312f, 0.1f, 4f, WaterVolume.s_createWaveDirections[2], WaterVolume.s_createWaveTangents[2], 0.5f);
		float num6 = this.CreateWave(worldPos, num2, 31.42f, 0.2f, 2f, WaterVolume.s_createWaveDirections[3], WaterVolume.s_createWaveTangents[3], 0.5f);
		float num7 = this.CreateWave(worldPos, num2, 35.42f, 0.4f, 1f, WaterVolume.s_createWaveDirections[4], WaterVolume.s_createWaveTangents[4], 0.5f);
		float num8 = this.CreateWave(worldPos, num2, 38.1223f, 1f, 0.8f, WaterVolume.s_createWaveDirections[5], WaterVolume.s_createWaveTangents[5], 0.7f);
		float num9 = this.CreateWave(worldPos, num2, 41.1223f, 1.2f, 0.6f * waveFactor, WaterVolume.s_createWaveDirections[6], WaterVolume.s_createWaveTangents[6], 0.8f);
		float num10 = this.CreateWave(worldPos, num2, 51.5123f, 1.3f, 0.4f * waveFactor, WaterVolume.s_createWaveDirections[7], WaterVolume.s_createWaveTangents[7], 0.9f);
		float num11 = this.CreateWave(worldPos, num2, 54.2f, 1.3f, 0.3f * waveFactor, WaterVolume.s_createWaveDirections[8], WaterVolume.s_createWaveTangents[8], 0.9f);
		float num12 = this.CreateWave(worldPos, num2, 56.123f, 1.5f, 0.2f * waveFactor, WaterVolume.s_createWaveDirections[9], WaterVolume.s_createWaveTangents[9], 0.9f);
		return (num3 + num4 + num5 + num6 + num7 + num8 + num9 + num10 + num11 + num12) * num;
	}

	public float CalcWave(Vector3 worldPos, float depth, float waterTime, float waveFactor)
	{
		if (WaterVolume.s_globalWindAlpha == 0f)
		{
			return this.CalcWave(worldPos, depth, WaterVolume.s_globalWind1, waterTime, waveFactor);
		}
		float num = this.CalcWave(worldPos, depth, WaterVolume.s_globalWind1, waterTime, waveFactor);
		float num2 = this.CalcWave(worldPos, depth, WaterVolume.s_globalWind2, waterTime, waveFactor);
		return Mathf.LerpUnclamped(num, num2, WaterVolume.s_globalWindAlpha);
	}

	public float Depth(Vector3 point)
	{
		Vector3 vector = base.transform.InverseTransformPoint(point);
		float num = (vector.x + this.m_collider.bounds.size.x / 2f) / this.m_collider.bounds.size.x;
		float num2 = (vector.z + this.m_collider.bounds.size.z / 2f) / this.m_collider.bounds.size.z;
		float num3 = Mathf.Lerp(this.m_normalizedDepth[3], this.m_normalizedDepth[2], num);
		float num4 = Mathf.Lerp(this.m_normalizedDepth[0], this.m_normalizedDepth[1], num);
		return Mathf.Lerp(num3, num4, num2);
	}

	private void OnTriggerEnter(Collider triggerCollider)
	{
		IWaterInteractable component = triggerCollider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component == null)
		{
			return;
		}
		component.Increment(LiquidType.Water);
		if (!this.m_inWater.Contains(component))
		{
			this.m_inWater.Add(component);
		}
	}

	private void UpdateFloaters()
	{
		if (this.m_inWater.Count == 0)
		{
			return;
		}
		WaterVolume.s_inWaterRemoveIndices.Clear();
		for (int i = 0; i < this.m_inWater.Count; i++)
		{
			IWaterInteractable waterInteractable = this.m_inWater[i];
			if (waterInteractable == null)
			{
				WaterVolume.s_inWaterRemoveIndices.Add(i);
			}
			else
			{
				Transform transform = waterInteractable.GetTransform();
				if (transform)
				{
					float waterSurface = this.GetWaterSurface(transform.position, 1f);
					waterInteractable.SetLiquidLevel(waterSurface, LiquidType.Water, this);
				}
				else
				{
					WaterVolume.s_inWaterRemoveIndices.Add(i);
				}
			}
		}
		for (int j = WaterVolume.s_inWaterRemoveIndices.Count - 1; j >= 0; j--)
		{
			this.m_inWater.RemoveAt(WaterVolume.s_inWaterRemoveIndices[j]);
		}
	}

	private void OnTriggerExit(Collider triggerCollider)
	{
		IWaterInteractable component = triggerCollider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component == null)
		{
			return;
		}
		if (component.Decrement(LiquidType.Water) == 0)
		{
			component.SetLiquidLevel(-10000f, LiquidType.Water, this);
		}
		this.m_inWater.Remove(component);
	}

	private void OnDestroy()
	{
		foreach (IWaterInteractable waterInteractable in this.m_inWater)
		{
			if (waterInteractable != null && waterInteractable.Decrement(LiquidType.Water) == 0)
			{
				waterInteractable.SetLiquidLevel(-10000f, LiquidType.Water, this);
			}
		}
		this.m_inWater.Clear();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_surfaceOffset, new Vector3(2f, 0.05f, 2f));
	}

	public static List<WaterVolume> Instances { get; } = new List<WaterVolume>();

	private Collider m_collider;

	private readonly float[] m_normalizedDepth = new float[4];

	private readonly List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	public MeshRenderer m_waterSurface;

	public Heightmap m_heightmap;

	public float m_forceDepth = -1f;

	public float m_surfaceOffset;

	public bool m_useGlobalWind = true;

	private const bool c_MenuWater = false;

	private static float s_waterTime = 0f;

	private static readonly int s_shaderWaterTime = Shader.PropertyToID("_WaterTime");

	private static readonly int s_shaderDepth = Shader.PropertyToID("_depth");

	private static readonly int s_shaderUseGlobalWind = Shader.PropertyToID("_UseGlobalWind");

	private static Vector4 s_globalWind1 = new Vector4(1f, 0f, 0f, 0f);

	private static Vector4 s_globalWind2 = new Vector4(1f, 0f, 0f, 0f);

	private static float s_globalWindAlpha = 0f;

	private static float s_wrappedDayTimeSeconds = 0f;

	private static readonly List<int> s_inWaterRemoveIndices = new List<int>();

	private static readonly Vector2[] s_createWaveDirections = new Vector2[]
	{
		new Vector2(1.0312f, 0.312f).normalized,
		new Vector2(1.0312f, 0.312f).normalized,
		new Vector2(-0.123f, 1.12f).normalized,
		new Vector2(0.423f, 0.124f).normalized,
		new Vector2(0.123f, -0.64f).normalized,
		new Vector2(-0.523f, -0.64f).normalized,
		new Vector2(0.223f, 0.74f).normalized,
		new Vector2(0.923f, -0.24f).normalized,
		new Vector2(-0.323f, 0.44f).normalized,
		new Vector2(0.5312f, -0.812f).normalized
	};

	private static Vector2[] s_createWaveTangents = null;
}
