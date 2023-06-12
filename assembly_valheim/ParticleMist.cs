using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleMist : MonoBehaviour
{

	public static ParticleMist instance
	{
		get
		{
			return ParticleMist.m_instance;
		}
	}

	private void Awake()
	{
		ParticleMist.m_instance = this;
		this.m_ps = base.GetComponent<ParticleSystem>();
		this.m_lastUpdatePos = base.transform.position;
	}

	private void OnDestroy()
	{
		if (ParticleMist.m_instance == this)
		{
			ParticleMist.m_instance = null;
		}
	}

	private void Update()
	{
		if (!this.m_ps.emission.enabled)
		{
			return;
		}
		this.m_accumulator += Time.fixedDeltaTime;
		if (this.m_accumulator < 0.1f)
		{
			return;
		}
		this.m_accumulator -= 0.1f;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		List<Mister> demistersSorted = Mister.GetDemistersSorted(localPlayer.transform.position);
		if (demistersSorted.Count == 0)
		{
			return;
		}
		this.m_haveActiveMist = demistersSorted.Count > 0;
		this.GetAllForcefields(this.fields);
		this.m_inMistAreaTimer += 0.1f;
		float num = Vector3.Distance(base.transform.position, this.m_lastUpdatePos);
		this.m_combinedMovement += Mathf.Clamp(num, 0f, 10f);
		this.m_lastUpdatePos = base.transform.position;
		float num2;
		float num3;
		this.FindMaxMistAlltitude(50f, out num2, out num3);
		int num4 = (int)(this.m_combinedMovement * (float)this.m_localEmissionPerUnit);
		if (num4 > 0)
		{
			this.m_combinedMovement = Mathf.Max(0f, this.m_combinedMovement - (float)num4 / (float)this.m_localEmissionPerUnit);
		}
		int num5 = (int)((float)this.m_localEmission * 0.1f) + num4;
		this.Emit(base.transform.position, 0f, this.m_localRange, num5, this.fields, null, num2);
		foreach (Demister demister in this.fields)
		{
			float endRange = demister.m_forceField.endRange;
			float num6 = Mathf.Max(0f, Vector3.Distance(demister.transform.position, base.transform.position) - endRange);
			if (num6 <= this.m_maxDistance)
			{
				float num7 = 12.566371f * (endRange * endRange);
				float num8 = Mathf.Lerp(this.m_emissionMax, 0f, Utils.LerpStep(this.m_minDistance, this.m_maxDistance, num6));
				int num9 = (int)(num7 * num8 * 0.1f);
				float movedDistance = demister.GetMovedDistance();
				num9 += (int)(movedDistance * this.m_emissionPerUnit);
				this.Emit(demister.transform.position, endRange, 0f, num9, this.fields, demister, num2);
			}
		}
		foreach (Mister mister in demistersSorted)
		{
			if (!mister.Inside(base.transform.position, 0f))
			{
				this.MisterEmit(mister, demistersSorted, this.fields, num2, 0.1f);
			}
		}
	}

	private void Emit(Vector3 center, float radius, float thickness, int toEmit, List<Demister> fields, Demister pf, float minAlt)
	{
		if (!Mister.InsideMister(center, radius + thickness))
		{
			return;
		}
		if (this.IsInsideOtherDemister(fields, center, radius + thickness, pf))
		{
			return;
		}
		ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
		for (int i = 0; i < toEmit; i++)
		{
			Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
			Vector3 vector = center + onUnitSphere * (radius + 0.1f + UnityEngine.Random.Range(0f, thickness));
			if (vector.y >= minAlt && !this.IsInsideOtherDemister(fields, vector, 0f, pf) && Mister.InsideMister(vector, 0f))
			{
				float num = Vector3.Distance(base.transform.position, vector);
				if (num <= this.m_maxDistance)
				{
					emitParams.startSize = Mathf.Lerp(this.m_minSize, this.m_maxSize, Utils.LerpStep(this.m_minDistance, this.m_maxDistance, num));
					emitParams.position = vector;
					this.m_ps.Emit(emitParams, 1);
				}
			}
		}
	}

	private void MisterEmit(Mister mister, List<Mister> allMisters, List<Demister> fields, float minAlt, float dt)
	{
		Vector3 position = mister.transform.position;
		float radius = mister.m_radius;
		float num = Mathf.Max(0f, Vector3.Distance(mister.transform.position, base.transform.position) - radius);
		if (num > this.m_distantMaxRange)
		{
			return;
		}
		if (mister.IsCompletelyInsideOtherMister(this.m_distantThickness))
		{
			return;
		}
		float num2 = 12.566371f * (radius * radius);
		float num3 = Mathf.Lerp(this.m_distantEmissionMax, 0f, Utils.LerpStep(0f, this.m_distantMaxRange, num));
		int num4 = (int)(num2 * num3 * dt);
		float num5 = mister.transform.position.y + mister.m_height;
		ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
		for (int i = 0; i < num4; i++)
		{
			Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
			Vector3 vector = position + onUnitSphere * (radius + 0.1f + UnityEngine.Random.Range(0f, this.m_distantThickness));
			if (vector.y >= minAlt)
			{
				if (vector.y > num5)
				{
					vector.y = num5;
				}
				if (!Mister.IsInsideOtherMister(vector, mister) && !this.IsInsideOtherDemister(fields, vector, 0f, null))
				{
					float num6 = Vector3.Distance(base.transform.position, vector);
					if (num6 <= this.m_distantMaxRange)
					{
						emitParams.startSize = Mathf.Lerp(this.m_distantMinSize, this.m_distantMaxSize, Utils.LerpStep(0f, this.m_distantMaxRange, num6));
						emitParams.position = vector;
						Vector3 vector2 = onUnitSphere * UnityEngine.Random.Range(0f, this.m_distantEmissionMaxVel);
						vector2.y = 0f;
						emitParams.velocity = vector2;
						this.m_ps.Emit(emitParams, 1);
					}
				}
			}
		}
	}

	private bool IsInsideOtherDemister(List<Demister> fields, Vector3 p, float radius, Demister ignore)
	{
		foreach (Demister demister in fields)
		{
			if (!(demister == ignore) && Vector3.Distance(demister.transform.position, p) + radius < demister.m_forceField.endRange)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsInMist(Vector3 p0)
	{
		return !(ParticleMist.m_instance == null) && ParticleMist.m_instance.m_haveActiveMist && Mister.InsideMister(p0, 0f) && !ParticleMist.m_instance.InsideDemister(p0);
	}

	public static bool IsMistBlocked(Vector3 p0, Vector3 p1)
	{
		return !(ParticleMist.m_instance == null) && ParticleMist.m_instance.IsMistBlocked_internal(p0, p1);
	}

	private bool IsMistBlocked_internal(Vector3 p0, Vector3 p1)
	{
		if (!this.m_haveActiveMist)
		{
			return false;
		}
		if (Vector3.Distance(p0, p1) < 10f)
		{
			return false;
		}
		Vector3 vector = (p0 + p1) * 0.5f;
		return (Mister.InsideMister(p0, 0f) && !this.InsideDemister(p0)) || (Mister.InsideMister(p1, 0f) && !this.InsideDemister(p1)) || (Mister.InsideMister(vector, 0f) && !this.InsideDemister(vector));
	}

	private bool InsideDemister(Vector3 p)
	{
		foreach (Demister demister in Demister.GetDemisters())
		{
			if (Vector3.Distance(demister.transform.position, p) < demister.m_forceField.endRange)
			{
				return true;
			}
		}
		return false;
	}

	private void GetAllForcefields(List<Demister> fields)
	{
		List<Demister> demisters = Demister.GetDemisters();
		this.sortList.Clear();
		foreach (Demister demister in demisters)
		{
			this.sortList.Add(new KeyValuePair<Demister, float>(demister, Vector3.Distance(base.transform.position, demister.transform.position)));
		}
		this.sortList.Sort((KeyValuePair<Demister, float> a, KeyValuePair<Demister, float> b) => a.Value.CompareTo(b.Value));
		fields.Clear();
		foreach (KeyValuePair<Demister, float> keyValuePair in this.sortList)
		{
			fields.Add(keyValuePair.Key);
		}
	}

	private void FindMaxMistAlltitude(float testRange, out float minMistHeight, out float maxMistHeight)
	{
		Vector3 position = base.transform.position;
		float num = 0f;
		int num2 = 20;
		minMistHeight = 99999f;
		for (int i = 0; i < num2; i++)
		{
			Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
			Vector3 vector = position + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * testRange;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(vector);
			num += groundHeight;
			if (groundHeight < minMistHeight)
			{
				minMistHeight = groundHeight;
			}
		}
		float num3 = num / (float)num2;
		maxMistHeight = num3 + this.m_maxMistAltitude;
	}

	private List<Heightmap> tempHeightmaps = new List<Heightmap>();

	private List<Demister> fields = new List<Demister>();

	private List<KeyValuePair<Demister, float>> sortList = new List<KeyValuePair<Demister, float>>();

	private static ParticleMist m_instance;

	private ParticleSystem m_ps;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome = Heightmap.Biome.Mistlands;

	public float m_localRange = 10f;

	public int m_localEmission = 50;

	public int m_localEmissionPerUnit = 50;

	public float m_maxMistAltitude = 50f;

	[Header("Misters")]
	public float m_distantMaxRange = 100f;

	public float m_distantMinSize = 5f;

	public float m_distantMaxSize = 20f;

	public float m_distantEmissionMax = 0.1f;

	public float m_distantEmissionMaxVel = 1f;

	public float m_distantThickness = 4f;

	[Header("Demisters")]
	public float m_minDistance = 10f;

	public float m_maxDistance = 50f;

	public float m_emissionMax = 0.2f;

	public float m_emissionPerUnit = 20f;

	public float m_minSize = 2f;

	public float m_maxSize = 10f;

	private float m_inMistAreaTimer;

	private float m_accumulator;

	private float m_combinedMovement;

	private Vector3 m_lastUpdatePos;

	private bool m_haveActiveMist;
}
