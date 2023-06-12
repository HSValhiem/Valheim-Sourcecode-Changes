using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WearNTear : MonoBehaviour, IDestructible
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<HitData>("WNTDamage", new Action<long, HitData>(this.RPC_Damage));
		this.m_nview.Register("WNTRemove", new Action<long>(this.RPC_Remove));
		this.m_nview.Register("WNTRepair", new Action<long>(this.RPC_Repair));
		this.m_nview.Register<float>("WNTHealthChanged", new Action<long, float>(this.RPC_HealthChanged));
		if (this.m_autoCreateFragments)
		{
			this.m_nview.Register("WNTCreateFragments", new Action<long>(this.RPC_CreateFragments));
		}
		if (WearNTear.s_rayMask == 0)
		{
			WearNTear.s_rayMask = LayerMask.GetMask(new string[] { "piece", "Default", "static_solid", "Default_small", "terrain" });
		}
		WearNTear.s_allInstances.Add(this);
		this.m_myIndex = WearNTear.s_allInstances.Count - 1;
		this.m_createTime = Time.time;
		this.m_support = this.GetMaxSupport();
		if (WearNTear.m_randomInitialDamage)
		{
			float num = UnityEngine.Random.Range(0.1f * this.m_health, this.m_health * 0.6f);
			this.m_nview.GetZDO().Set(ZDOVars.s_health, num);
		}
		this.UpdateCover(5f);
		this.m_updateCoverTimer = UnityEngine.Random.Range(0f, 4f);
		this.UpdateVisual(false);
	}

	private void OnDestroy()
	{
		if (this.m_myIndex != -1)
		{
			WearNTear.s_allInstances[this.m_myIndex] = WearNTear.s_allInstances[WearNTear.s_allInstances.Count - 1];
			WearNTear.s_allInstances[this.m_myIndex].m_myIndex = this.m_myIndex;
			WearNTear.s_allInstances.RemoveAt(WearNTear.s_allInstances.Count - 1);
		}
	}

	public bool Repair()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health) >= this.m_health)
		{
			return false;
		}
		if (Time.time - this.m_lastRepair < 1f)
		{
			return false;
		}
		this.m_lastRepair = Time.time;
		this.m_nview.InvokeRPC("WNTRepair", Array.Empty<object>());
		return true;
	}

	private void RPC_Repair(long sender)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_health, this.m_health);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", new object[] { this.m_health });
	}

	private float GetSupport()
	{
		if (!this.m_nview.IsValid())
		{
			return this.GetMaxSupport();
		}
		if (!this.m_nview.HasOwner())
		{
			return this.GetMaxSupport();
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_support;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_support, this.GetMaxSupport());
	}

	private float GetSupportColorValue()
	{
		float num = this.GetSupport();
		float num2;
		float num3;
		float num4;
		float num5;
		this.GetMaterialProperties(out num2, out num3, out num4, out num5);
		if (num >= num2)
		{
			return -1f;
		}
		num -= num3;
		return Mathf.Clamp01(num / (num2 * 0.5f - num3));
	}

	public void OnPlaced()
	{
		this.m_createTime = -1f;
	}

	private List<Renderer> GetHighlightRenderers()
	{
		MeshRenderer[] componentsInChildren = base.GetComponentsInChildren<MeshRenderer>(true);
		SkinnedMeshRenderer[] componentsInChildren2 = base.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		List<Renderer> list = new List<Renderer>();
		list.AddRange(componentsInChildren);
		list.AddRange(componentsInChildren2);
		return list;
	}

	public void Highlight()
	{
		if (this.m_oldMaterials == null)
		{
			this.m_oldMaterials = new List<WearNTear.OldMeshData>();
			foreach (Renderer renderer in this.GetHighlightRenderers())
			{
				WearNTear.OldMeshData oldMeshData = default(WearNTear.OldMeshData);
				oldMeshData.m_materials = renderer.sharedMaterials;
				oldMeshData.m_color = new Color[oldMeshData.m_materials.Length];
				oldMeshData.m_emissiveColor = new Color[oldMeshData.m_materials.Length];
				for (int i = 0; i < oldMeshData.m_materials.Length; i++)
				{
					if (oldMeshData.m_materials[i].HasProperty("_Color"))
					{
						oldMeshData.m_color[i] = oldMeshData.m_materials[i].GetColor("_Color");
					}
					if (oldMeshData.m_materials[i].HasProperty("_EmissionColor"))
					{
						oldMeshData.m_emissiveColor[i] = oldMeshData.m_materials[i].GetColor("_EmissionColor");
					}
				}
				oldMeshData.m_renderer = renderer;
				this.m_oldMaterials.Add(oldMeshData);
			}
		}
		float supportColorValue = this.GetSupportColorValue();
		Color color = new Color(0.6f, 0.8f, 1f);
		if (supportColorValue >= 0f)
		{
			color = Color.Lerp(new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), supportColorValue);
			float num;
			float num2;
			float num3;
			Color.RGBToHSV(color, out num, out num2, out num3);
			num2 = Mathf.Lerp(1f, 0.5f, supportColorValue);
			num3 = Mathf.Lerp(1.2f, 0.9f, supportColorValue);
			color = Color.HSVToRGB(num, num2, num3);
		}
		foreach (WearNTear.OldMeshData oldMeshData2 in this.m_oldMaterials)
		{
			if (oldMeshData2.m_renderer)
			{
				foreach (Material material in oldMeshData2.m_renderer.materials)
				{
					material.SetColor("_EmissionColor", color * 0.4f);
					material.color = color;
				}
			}
		}
		base.CancelInvoke("ResetHighlight");
		base.Invoke("ResetHighlight", 0.2f);
	}

	private void ResetHighlight()
	{
		if (this.m_oldMaterials != null)
		{
			foreach (WearNTear.OldMeshData oldMeshData in this.m_oldMaterials)
			{
				if (oldMeshData.m_renderer)
				{
					Material[] materials = oldMeshData.m_renderer.materials;
					if (materials.Length != 0)
					{
						if (materials[0] == oldMeshData.m_materials[0])
						{
							if (materials.Length == oldMeshData.m_color.Length)
							{
								for (int i = 0; i < materials.Length; i++)
								{
									if (materials[i].HasProperty("_Color"))
									{
										materials[i].SetColor("_Color", oldMeshData.m_color[i]);
									}
									if (materials[i].HasProperty("_EmissionColor"))
									{
										materials[i].SetColor("_EmissionColor", oldMeshData.m_emissiveColor[i]);
									}
								}
							}
						}
						else if (materials.Length == oldMeshData.m_materials.Length)
						{
							oldMeshData.m_renderer.materials = oldMeshData.m_materials;
						}
					}
				}
			}
			this.m_oldMaterials = null;
		}
	}

	private void SetupColliders()
	{
		this.m_colliders = base.GetComponentsInChildren<Collider>(true);
		this.m_bounds = new List<WearNTear.BoundData>();
		foreach (Collider collider in this.m_colliders)
		{
			if (!collider.isTrigger && !(collider.attachedRigidbody != null))
			{
				WearNTear.BoundData boundData = default(WearNTear.BoundData);
				if (collider is BoxCollider)
				{
					BoxCollider boxCollider = collider as BoxCollider;
					boundData.m_rot = boxCollider.transform.rotation;
					boundData.m_pos = boxCollider.transform.position + boxCollider.transform.TransformVector(boxCollider.center);
					boundData.m_size = new Vector3(boxCollider.transform.lossyScale.x * boxCollider.size.x, boxCollider.transform.lossyScale.y * boxCollider.size.y, boxCollider.transform.lossyScale.z * boxCollider.size.z);
				}
				else
				{
					boundData.m_rot = Quaternion.identity;
					boundData.m_pos = collider.bounds.center;
					boundData.m_size = collider.bounds.size;
				}
				boundData.m_size.x = boundData.m_size.x + 0.3f;
				boundData.m_size.y = boundData.m_size.y + 0.3f;
				boundData.m_size.z = boundData.m_size.z + 0.3f;
				boundData.m_size *= 0.5f;
				this.m_bounds.Add(boundData);
			}
		}
	}

	private bool ShouldUpdate()
	{
		return this.m_createTime < 0f || Time.time - this.m_createTime > 30f;
	}

	public void UpdateWear(float time)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.ShouldUpdate())
		{
			if (ZNetScene.instance.OutsideActiveArea(base.transform.position))
			{
				this.m_support = this.GetMaxSupport();
				this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
				return;
			}
			float num = 0f;
			bool flag = !this.m_haveRoof && EnvMan.instance.IsWet();
			if (this.m_wet)
			{
				this.m_wet.SetActive(flag);
			}
			if (this.m_noRoofWear && this.GetHealthPercentage() > 0.5f)
			{
				if (flag || this.IsUnderWater())
				{
					if (this.m_rainTimer == 0f)
					{
						this.m_rainTimer = time;
					}
					else if (time - this.m_rainTimer > 60f)
					{
						this.m_rainTimer = time;
						num += 5f;
					}
				}
				else
				{
					this.m_rainTimer = 0f;
				}
			}
			if (this.m_noSupportWear)
			{
				this.UpdateSupport();
				if (!this.HaveSupport())
				{
					num = 100f;
				}
			}
			if (num > 0f && this.CanBeRemoved())
			{
				float num2 = num / 100f * this.m_health;
				this.ApplyDamage(num2);
			}
		}
		this.UpdateVisual(true);
	}

	private Vector3 GetCOM()
	{
		return base.transform.position + base.transform.rotation * this.m_comOffset;
	}

	private void UpdateSupport()
	{
		if (this.m_colliders == null)
		{
			this.SetupColliders();
		}
		float num;
		float num2;
		float num3;
		float num4;
		this.GetMaterialProperties(out num, out num2, out num3, out num4);
		WearNTear.s_tempSupportPoints.Clear();
		WearNTear.s_tempSupportPointValues.Clear();
		Vector3 com = this.GetCOM();
		float num5 = 0f;
		foreach (WearNTear.BoundData boundData in this.m_bounds)
		{
			int num6 = Physics.OverlapBoxNonAlloc(boundData.m_pos, boundData.m_size, WearNTear.s_tempColliders, boundData.m_rot, WearNTear.s_rayMask);
			for (int i = 0; i < num6; i++)
			{
				Collider collider = WearNTear.s_tempColliders[i];
				if (!this.m_colliders.Contains(collider) && !(collider.attachedRigidbody != null) && !collider.isTrigger)
				{
					WearNTear componentInParent = collider.GetComponentInParent<WearNTear>();
					if (componentInParent == null)
					{
						this.m_support = num;
						this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
						return;
					}
					if (componentInParent.m_supports)
					{
						float num7 = Vector3.Distance(com, componentInParent.transform.position) + 0.1f;
						float support = componentInParent.GetSupport();
						num5 = Mathf.Max(num5, support - num3 * num7 * support);
						Vector3 vector = WearNTear.FindSupportPoint(com, componentInParent, collider);
						if (vector.y < com.y + 0.05f)
						{
							Vector3 normalized = (vector - com).normalized;
							if (normalized.y < 0f)
							{
								float num8 = Mathf.Acos(1f - Mathf.Abs(normalized.y)) / 1.57079637f;
								float num9 = Mathf.Lerp(num3, num4, num8);
								float num10 = support - num9 * num7 * support;
								num5 = Mathf.Max(num5, num10);
							}
							float num11 = support - num4 * num7 * support;
							WearNTear.s_tempSupportPoints.Add(vector);
							WearNTear.s_tempSupportPointValues.Add(num11);
						}
					}
				}
			}
		}
		if (WearNTear.s_tempSupportPoints.Count > 0)
		{
			for (int j = 0; j < WearNTear.s_tempSupportPoints.Count - 1; j++)
			{
				Vector3 vector2 = WearNTear.s_tempSupportPoints[j] - com;
				vector2.y = 0f;
				for (int k = j + 1; k < WearNTear.s_tempSupportPoints.Count; k++)
				{
					float num12 = (WearNTear.s_tempSupportPointValues[j] + WearNTear.s_tempSupportPointValues[k]) * 0.5f;
					if (num12 > num5)
					{
						Vector3 vector3 = WearNTear.s_tempSupportPoints[k] - com;
						vector3.y = 0f;
						if (Vector3.Angle(vector2, vector3) >= 100f)
						{
							num5 = num12;
						}
					}
				}
			}
		}
		this.m_support = Mathf.Min(num5, num);
		this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
	}

	private static Vector3 FindSupportPoint(Vector3 com, WearNTear wnt, Collider otherCollider)
	{
		MeshCollider meshCollider = otherCollider as MeshCollider;
		if (!(meshCollider != null) || meshCollider.convex)
		{
			return otherCollider.ClosestPoint(com);
		}
		RaycastHit raycastHit;
		if (meshCollider.Raycast(new Ray(com, Vector3.down), out raycastHit, 10f))
		{
			return raycastHit.point;
		}
		return (com + wnt.GetCOM()) * 0.5f;
	}

	private bool HaveSupport()
	{
		return this.m_support >= this.GetMinSupport();
	}

	private bool IsUnderWater()
	{
		return base.transform.position.y < Floating.GetLiquidLevel(base.transform.position, 1f, LiquidType.All);
	}

	public void UpdateCover(float dt)
	{
		this.m_updateCoverTimer += dt;
		if (this.m_updateCoverTimer <= 4f)
		{
			return;
		}
		if (EnvMan.instance.IsWet())
		{
			this.m_haveRoof = this.HaveRoof();
		}
		this.m_updateCoverTimer = 0f;
	}

	private bool HaveRoof()
	{
		if (this.m_roof)
		{
			return true;
		}
		int num = Physics.SphereCastNonAlloc(base.transform.position, 0.1f, Vector3.up, WearNTear.s_raycastHits, 100f, WearNTear.s_rayMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = WearNTear.s_raycastHits[i];
			if (!raycastHit.collider.gameObject.CompareTag("leaky"))
			{
				this.m_roof = raycastHit.collider.gameObject;
				return true;
			}
		}
		return false;
	}

	private void RPC_HealthChanged(long peer, float health)
	{
		float num = health / this.m_health;
		this.SetHealthVisual(num, true);
	}

	private void UpdateVisual(bool triggerEffects)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.SetHealthVisual(this.GetHealthPercentage(), triggerEffects);
	}

	private void SetHealthVisual(float health, bool triggerEffects)
	{
		if (this.m_worn == null && this.m_broken == null && this.m_new == null)
		{
			return;
		}
		if (health > 0.75f)
		{
			if (this.m_worn != this.m_new)
			{
				this.m_worn.SetActive(false);
			}
			if (this.m_broken != this.m_new)
			{
				this.m_broken.SetActive(false);
			}
			this.m_new.SetActive(true);
			return;
		}
		if (health > 0.25f)
		{
			if (triggerEffects && !this.m_worn.activeSelf)
			{
				this.m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
			}
			if (this.m_new != this.m_worn)
			{
				this.m_new.SetActive(false);
			}
			if (this.m_broken != this.m_worn)
			{
				this.m_broken.SetActive(false);
			}
			this.m_worn.SetActive(true);
			return;
		}
		if (triggerEffects && !this.m_broken.activeSelf)
		{
			this.m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		}
		if (this.m_new != this.m_broken)
		{
			this.m_new.SetActive(false);
		}
		if (this.m_worn != this.m_broken)
		{
			this.m_worn.SetActive(false);
		}
		this.m_broken.SetActive(true);
	}

	public float GetHealthPercentage()
	{
		if (!this.m_nview.IsValid())
		{
			return 1f;
		}
		return Mathf.Clamp01(this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health) / this.m_health);
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	public void Damage(HitData hit)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("WNTDamage", new object[] { hit });
	}

	private bool CanBeRemoved()
	{
		return !this.m_piece || this.m_piece.CanBeRemoved();
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health) <= 0f)
		{
			return;
		}
		HitData.DamageModifier damageModifier;
		hit.ApplyResistance(this.m_damages, out damageModifier);
		float totalDamage = hit.GetTotalDamage();
		DamageText.instance.ShowText(damageModifier, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		if (this.m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if (attacker)
			{
				bool flag = totalDamage >= this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, flag);
			}
		}
		this.ApplyDamage(totalDamage);
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		if (this.m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_hitNoise);
			}
		}
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged();
		}
	}

	public bool ApplyDamage(float damage)
	{
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
		if (num <= 0f)
		{
			return false;
		}
		num -= damage;
		this.m_nview.GetZDO().Set(ZDOVars.s_health, num);
		if (num <= 0f)
		{
			this.Destroy();
		}
		else
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", new object[] { num });
		}
		return true;
	}

	public void Remove()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("WNTRemove", Array.Empty<object>());
	}

	private void RPC_Remove(long sender)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.Destroy();
	}

	private void Destroy()
	{
		Bed component = base.GetComponent<Bed>();
		if (component != null && this.m_nview.IsOwner() && Game.instance != null)
		{
			Game.instance.RemoveCustomSpawnPoint(component.GetSpawnPoint());
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_health, 0f);
		if (this.m_piece)
		{
			this.m_piece.DropResources();
		}
		if (this.m_onDestroyed != null)
		{
			this.m_onDestroyed();
		}
		if (this.m_destroyNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_destroyNoise);
			}
		}
		this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		if (this.m_autoCreateFragments)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "WNTCreateFragments", Array.Empty<object>());
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private void RPC_CreateFragments(long peer)
	{
		this.ResetHighlight();
		if (this.m_fragmentRoots != null && this.m_fragmentRoots.Length != 0)
		{
			foreach (GameObject gameObject in this.m_fragmentRoots)
			{
				gameObject.SetActive(true);
				Destructible.CreateFragments(gameObject, false);
			}
			return;
		}
		Destructible.CreateFragments(base.gameObject, true);
	}

	private float GetMaxSupport()
	{
		float num;
		float num2;
		float num3;
		float num4;
		this.GetMaterialProperties(out num, out num2, out num3, out num4);
		return num;
	}

	private float GetMinSupport()
	{
		float num;
		float num2;
		float num3;
		float num4;
		this.GetMaterialProperties(out num, out num2, out num3, out num4);
		return num2;
	}

	private void GetMaterialProperties(out float maxSupport, out float minSupport, out float horizontalLoss, out float verticalLoss)
	{
		switch (this.m_materialType)
		{
		case WearNTear.MaterialType.Wood:
			maxSupport = 100f;
			minSupport = 10f;
			verticalLoss = 0.125f;
			horizontalLoss = 0.2f;
			return;
		case WearNTear.MaterialType.Stone:
			maxSupport = 1000f;
			minSupport = 100f;
			verticalLoss = 0.125f;
			horizontalLoss = 1f;
			return;
		case WearNTear.MaterialType.Iron:
			maxSupport = 1500f;
			minSupport = 20f;
			verticalLoss = 0.07692308f;
			horizontalLoss = 0.07692308f;
			return;
		case WearNTear.MaterialType.HardWood:
			maxSupport = 140f;
			minSupport = 10f;
			verticalLoss = 0.1f;
			horizontalLoss = 0.166666672f;
			return;
		case WearNTear.MaterialType.Marble:
			maxSupport = 1500f;
			minSupport = 100f;
			verticalLoss = 0.125f;
			horizontalLoss = 0.5f;
			return;
		default:
			maxSupport = 0f;
			minSupport = 0f;
			verticalLoss = 0f;
			horizontalLoss = 0f;
			return;
		}
	}

	public static List<WearNTear> GetAllInstances()
	{
		return WearNTear.s_allInstances;
	}

	public static bool m_randomInitialDamage = false;

	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Wear")]
	public GameObject m_new;

	public GameObject m_worn;

	public GameObject m_broken;

	public GameObject m_wet;

	public bool m_noRoofWear = true;

	public bool m_noSupportWear = true;

	public WearNTear.MaterialType m_materialType;

	public bool m_supports = true;

	public Vector3 m_comOffset = Vector3.zero;

	[Header("Destruction")]
	public float m_health = 100f;

	public HitData.DamageModifiers m_damages;

	public float m_hitNoise;

	public float m_destroyNoise;

	public bool m_triggerPrivateArea = true;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public EffectList m_switchEffect = new EffectList();

	public bool m_autoCreateFragments = true;

	public GameObject[] m_fragmentRoots;

	private const float c_RainDamageTime = 60f;

	private const float c_RainDamage = 5f;

	private const float c_ComTestWidth = 0.2f;

	private const float c_ComMinAngle = 100f;

	private static readonly RaycastHit[] s_raycastHits = new RaycastHit[128];

	private static readonly Collider[] s_tempColliders = new Collider[128];

	private static int s_rayMask = 0;

	private static readonly List<WearNTear> s_allInstances = new List<WearNTear>();

	private static readonly List<Vector3> s_tempSupportPoints = new List<Vector3>();

	private static readonly List<float> s_tempSupportPointValues = new List<float>();

	private ZNetView m_nview;

	private Collider[] m_colliders;

	private float m_support = 1f;

	private float m_createTime;

	private int m_myIndex = -1;

	private float m_rainTimer;

	private float m_lastRepair;

	private Piece m_piece;

	private GameObject m_roof;

	private List<WearNTear.BoundData> m_bounds;

	private List<WearNTear.OldMeshData> m_oldMaterials;

	private float m_updateCoverTimer;

	private bool m_haveRoof = true;

	private const float c_UpdateCoverFrequency = 4f;

	public enum MaterialType
	{

		Wood,

		Stone,

		Iron,

		HardWood,

		Marble
	}

	private struct BoundData
	{

		public Vector3 m_pos;

		public Quaternion m_rot;

		public Vector3 m_size;
	}

	private struct OldMeshData
	{

		public Renderer m_renderer;

		public Material[] m_materials;

		public Color[] m_color;

		public Color[] m_emissiveColor;
	}
}
