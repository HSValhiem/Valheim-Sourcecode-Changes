using System;
using UnityEngine;

public class Gibber : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
	}

	private void Start()
	{
		Vector3 vector = base.transform.position;
		Vector3 vector2 = Vector3.zero;
		if (this.m_nview && this.m_nview.IsValid())
		{
			vector = this.m_nview.GetZDO().GetVec3(ZDOVars.s_hitPoint, vector);
			vector2 = this.m_nview.GetZDO().GetVec3(ZDOVars.s_hitDir, vector2);
		}
		if (this.m_delay > 0f)
		{
			base.Invoke("Explode", this.m_delay);
			return;
		}
		this.Explode(vector, vector2);
	}

	public void Setup(Vector3 hitPoint, Vector3 hitDir)
	{
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hitPoint, hitPoint);
			this.m_nview.GetZDO().Set(ZDOVars.s_hitDir, hitDir);
		}
	}

	private void DestroyAll()
	{
		if (this.m_nview)
		{
			if (!this.m_nview.GetZDO().HasOwner())
			{
				this.m_nview.ClaimOwnership();
			}
			if (this.m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void CreateBodies()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (this.m_chanceToRemoveGib > 0f && UnityEngine.Random.value < this.m_chanceToRemoveGib)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
			else if (!gameObject.GetComponent<Rigidbody>())
			{
				gameObject.AddComponent<BoxCollider>();
				gameObject.AddComponent<Rigidbody>().maxDepenetrationVelocity = 2f;
				TimedDestruction timedDestruction = gameObject.AddComponent<TimedDestruction>();
				timedDestruction.m_timeout = UnityEngine.Random.Range(this.m_timeout / 2f, this.m_timeout);
				timedDestruction.Trigger();
			}
		}
	}

	private void Explode()
	{
		this.Explode(Vector3.zero, Vector3.zero);
	}

	private void Explode(Vector3 hitPoint, Vector3 hitDir)
	{
		base.InvokeRepeating("DestroyAll", this.m_timeout, 1f);
		float num = (((double)hitDir.magnitude > 0.01) ? this.m_impactDirectionMix : 0f);
		this.CreateBodies();
		Rigidbody[] componentsInChildren = base.gameObject.GetComponentsInChildren<Rigidbody>();
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		int num2 = 0;
		foreach (Rigidbody rigidbody in componentsInChildren)
		{
			vector += rigidbody.worldCenterOfMass;
			num2++;
		}
		vector /= (float)num2;
		foreach (Rigidbody rigidbody2 in componentsInChildren)
		{
			float num3 = UnityEngine.Random.Range(this.m_minVel, this.m_maxVel);
			Vector3 vector2 = Vector3.Lerp(Vector3.Normalize(rigidbody2.worldCenterOfMass - vector), hitDir, num);
			rigidbody2.velocity = vector2 * num3;
			rigidbody2.angularVelocity = new Vector3(UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel), UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel), UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel));
		}
		foreach (Gibber.GibbData gibbData in this.m_gibbs)
		{
			if (gibbData.m_object && gibbData.m_chanceToSpawn < 1f && UnityEngine.Random.value > gibbData.m_chanceToSpawn)
			{
				UnityEngine.Object.Destroy(gibbData.m_object);
			}
		}
		if ((double)hitDir.magnitude > 0.01)
		{
			Quaternion quaternion = Quaternion.LookRotation(hitDir);
			this.m_punchEffector.Create(hitPoint, quaternion, null, 1f, -1);
		}
	}

	public EffectList m_punchEffector = new EffectList();

	public GameObject m_gibHitEffect;

	public GameObject m_gibDestroyEffect;

	public float m_gibHitDestroyChance;

	public Gibber.GibbData[] m_gibbs = new Gibber.GibbData[0];

	public float m_minVel = 10f;

	public float m_maxVel = 20f;

	public float m_maxRotVel = 20f;

	public float m_impactDirectionMix = 0.5f;

	public float m_timeout = 5f;

	public float m_delay;

	[Range(0f, 1f)]
	public float m_chanceToRemoveGib;

	private ZNetView m_nview;

	[Serializable]
	public class GibbData
	{

		public GameObject m_object;

		public float m_chanceToSpawn = 1f;
	}
}
