using System;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour, IDestructible
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.Register<HitData>("Damage", new Action<long, HitData>(this.RPC_Damage));
			if (this.m_autoCreateFragments)
			{
				this.m_nview.Register("CreateFragments", new Action<long>(this.RPC_CreateFragments));
			}
			if (this.m_ttl > 0f)
			{
				base.InvokeRepeating("DestroyNow", this.m_ttl, 1f);
			}
		}
	}

	private void Start()
	{
		this.m_firstFrame = false;
	}

	public GameObject GetParentObject()
	{
		return null;
	}

	public DestructibleType GetDestructibleType()
	{
		return this.m_destructibleType;
	}

	public void Damage(HitData hit)
	{
		if (this.m_firstFrame)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("Damage", new object[] { hit });
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_destroyed)
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
		HitData.DamageModifier damageModifier;
		hit.ApplyResistance(this.m_damages, out damageModifier);
		float totalDamage = hit.GetTotalDamage();
		if (this.m_body)
		{
			this.m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce, hit.m_point, ForceMode.Impulse);
		}
		if ((int)hit.m_toolTier < this.m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		DamageText.instance.ShowText(damageModifier, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		num -= totalDamage;
		this.m_nview.GetZDO().Set(ZDOVars.s_health, num);
		if (this.m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if (attacker)
			{
				bool flag = num <= 0f;
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, flag);
			}
		}
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged();
		}
		if (this.m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_hitNoise);
			}
		}
		if (num <= 0f)
		{
			this.Destroy(hit.m_point, hit.m_dir);
		}
	}

	public void DestroyNow()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.Destroy(Vector3.zero, Vector3.zero);
		}
	}

	public void Destroy(Vector3 hitPoint, Vector3 hitDir)
	{
		this.CreateDestructionEffects(hitPoint, hitDir);
		if (this.m_destroyNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_destroyNoise);
			}
		}
		if (this.m_spawnWhenDestroyed)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnWhenDestroyed, base.transform.position, base.transform.rotation);
			gameObject.GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
			Gibber component = gameObject.GetComponent<Gibber>();
			if (component)
			{
				component.Setup(hitPoint, hitDir);
			}
		}
		if (this.m_onDestroyed != null)
		{
			this.m_onDestroyed();
		}
		ZNetScene.instance.Destroy(base.gameObject);
		this.m_destroyed = true;
	}

	private void CreateDestructionEffects(Vector3 hitPoint, Vector3 hitDir)
	{
		GameObject[] array = this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Gibber component = array[i].GetComponent<Gibber>();
			if (component)
			{
				component.Setup(hitPoint, hitDir);
			}
		}
		if (this.m_autoCreateFragments)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "CreateFragments", Array.Empty<object>());
		}
	}

	private void RPC_CreateFragments(long peer)
	{
		Destructible.CreateFragments(base.gameObject, true);
	}

	public static void CreateFragments(GameObject rootObject, bool visibleOnly = true)
	{
		MeshRenderer[] componentsInChildren = rootObject.GetComponentsInChildren<MeshRenderer>(true);
		int num = LayerMask.NameToLayer("effect");
		List<Rigidbody> list = new List<Rigidbody>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (meshRenderer.gameObject.activeInHierarchy && (!visibleOnly || meshRenderer.isVisible))
			{
				MeshFilter component = meshRenderer.gameObject.GetComponent<MeshFilter>();
				if (!(component == null))
				{
					if (component.sharedMesh == null)
					{
						ZLog.Log("Meshfilter missing mesh " + component.gameObject.name);
					}
					else
					{
						GameObject gameObject = new GameObject();
						gameObject.layer = num;
						gameObject.transform.position = component.gameObject.transform.position;
						gameObject.transform.rotation = component.gameObject.transform.rotation;
						gameObject.transform.localScale = component.gameObject.transform.lossyScale * 0.9f;
						gameObject.AddComponent<MeshFilter>().sharedMesh = component.sharedMesh;
						MeshRenderer meshRenderer2 = gameObject.AddComponent<MeshRenderer>();
						meshRenderer2.sharedMaterials = meshRenderer.sharedMaterials;
						meshRenderer2.material.SetFloat("_RippleDistance", 0f);
						meshRenderer2.material.SetFloat("_ValueNoise", 0f);
						Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
						gameObject.AddComponent<BoxCollider>();
						list.Add(rigidbody);
						gameObject.AddComponent<TimedDestruction>().Trigger((float)UnityEngine.Random.Range(2, 4));
					}
				}
			}
		}
		if (list.Count > 0)
		{
			Vector3 vector = Vector3.zero;
			int num2 = 0;
			foreach (Rigidbody rigidbody2 in list)
			{
				vector += rigidbody2.worldCenterOfMass;
				num2++;
			}
			vector /= (float)num2;
			foreach (Rigidbody rigidbody3 in list)
			{
				Vector3 vector2 = (rigidbody3.worldCenterOfMass - vector).normalized * 4f;
				vector2 += UnityEngine.Random.onUnitSphere * 1f;
				rigidbody3.AddForce(vector2, ForceMode.VelocityChange);
			}
		}
	}

	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Destruction")]
	public DestructibleType m_destructibleType = DestructibleType.Default;

	public float m_health = 1f;

	public HitData.DamageModifiers m_damages;

	public float m_minDamageTreshold;

	public int m_minToolTier;

	public float m_hitNoise;

	public float m_destroyNoise;

	public bool m_triggerPrivateArea;

	public float m_ttl;

	public GameObject m_spawnWhenDestroyed;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public bool m_autoCreateFragments;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private bool m_firstFrame = true;

	private bool m_destroyed;
}
