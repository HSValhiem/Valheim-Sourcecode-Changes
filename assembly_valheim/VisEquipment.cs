using System;
using System.Collections.Generic;
using UnityEngine;

public class VisEquipment : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = ((this.m_nViewOverride != null) ? this.m_nViewOverride : base.GetComponent<ZNetView>());
		Transform transform = base.transform.Find("Visual");
		if (transform == null)
		{
			transform = base.transform;
		}
		this.m_visual = transform.gameObject;
		this.m_lodGroup = this.m_visual.GetComponentInChildren<LODGroup>();
		if (this.m_bodyModel != null && this.m_bodyModel.material.HasProperty("_ChestTex"))
		{
			this.m_emptyBodyTexture = this.m_bodyModel.material.GetTexture("_ChestTex");
		}
		if (this.m_bodyModel != null && this.m_bodyModel.material.HasProperty("_LegsTex"))
		{
			this.m_emptyLegsTexture = this.m_bodyModel.material.GetTexture("_LegsTex");
		}
	}

	private void OnEnable()
	{
		VisEquipment.Instances.Add(this);
	}

	private void OnDisable()
	{
		VisEquipment.Instances.Remove(this);
	}

	private void Start()
	{
		this.UpdateVisuals();
	}

	public void SetWeaponTrails(bool enabled)
	{
		if (this.m_useAllTrails)
		{
			MeleeWeaponTrail[] array = base.gameObject.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Emit = enabled;
			}
			return;
		}
		if (this.m_rightItemInstance)
		{
			MeleeWeaponTrail[] array = this.m_rightItemInstance.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Emit = enabled;
			}
		}
	}

	public void SetModel(int index)
	{
		if (this.m_modelIndex == index)
		{
			return;
		}
		if (index < 0 || index >= this.m_models.Length)
		{
			return;
		}
		ZLog.Log("Vis equip model set to " + index.ToString());
		this.m_modelIndex = index;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_modelIndex, this.m_modelIndex, false);
		}
	}

	public void SetSkinColor(Vector3 color)
	{
		if (color == this.m_skinColor)
		{
			return;
		}
		this.m_skinColor = color;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_skinColor, this.m_skinColor);
		}
	}

	public void SetHairColor(Vector3 color)
	{
		if (this.m_hairColor == color)
		{
			return;
		}
		this.m_hairColor = color;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hairColor, this.m_hairColor);
		}
	}

	public void SetItem(VisSlot slot, string name, int variant = 0)
	{
		switch (slot)
		{
		case VisSlot.HandLeft:
			this.SetLeftItem(name, variant);
			return;
		case VisSlot.HandRight:
			this.SetRightItem(name);
			return;
		case VisSlot.BackLeft:
			this.SetLeftBackItem(name, variant);
			return;
		case VisSlot.BackRight:
			this.SetRightBackItem(name);
			return;
		case VisSlot.Chest:
			this.SetChestItem(name);
			return;
		case VisSlot.Legs:
			this.SetLegItem(name);
			return;
		case VisSlot.Helmet:
			this.SetHelmetItem(name);
			return;
		case VisSlot.Shoulder:
			this.SetShoulderItem(name, variant);
			return;
		case VisSlot.Utility:
			this.SetUtilityItem(name);
			return;
		case VisSlot.Beard:
			this.SetBeardItem(name);
			return;
		case VisSlot.Hair:
			this.SetHairItem(name);
			return;
		default:
			throw new NotImplementedException("Unknown slot: " + slot.ToString());
		}
	}

	public void SetLeftItem(string name, int variant)
	{
		if (this.m_leftItem == name && this.m_leftItemVariant == variant)
		{
			return;
		}
		this.m_leftItem = name;
		this.m_leftItemVariant = variant;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_leftItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
			this.m_nview.GetZDO().Set(ZDOVars.s_leftItemVariant, variant, false);
		}
	}

	public void SetRightItem(string name)
	{
		if (this.m_rightItem == name)
		{
			return;
		}
		this.m_rightItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_rightItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetLeftBackItem(string name, int variant)
	{
		if (this.m_leftBackItem == name && this.m_leftBackItemVariant == variant)
		{
			return;
		}
		this.m_leftBackItem = name;
		this.m_leftBackItemVariant = variant;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_leftBackItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
			this.m_nview.GetZDO().Set(ZDOVars.s_leftBackItemVariant, variant, false);
		}
	}

	public void SetRightBackItem(string name)
	{
		if (this.m_rightBackItem == name)
		{
			return;
		}
		this.m_rightBackItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_rightBackItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetChestItem(string name)
	{
		if (this.m_chestItem == name)
		{
			return;
		}
		this.m_chestItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_chestItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetLegItem(string name)
	{
		if (this.m_legItem == name)
		{
			return;
		}
		this.m_legItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_legItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetHelmetItem(string name)
	{
		if (this.m_helmetItem == name)
		{
			return;
		}
		this.m_helmetItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_helmetItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetShoulderItem(string name, int variant)
	{
		if (this.m_shoulderItem == name && this.m_shoulderItemVariant == variant)
		{
			return;
		}
		this.m_shoulderItem = name;
		this.m_shoulderItemVariant = variant;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_shoulderItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
			this.m_nview.GetZDO().Set(ZDOVars.s_shoulderItemVariant, variant, false);
		}
	}

	public void SetBeardItem(string name)
	{
		if (this.m_beardItem == name)
		{
			return;
		}
		this.m_beardItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_beardItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetHairItem(string name)
	{
		if (this.m_hairItem == name)
		{
			return;
		}
		this.m_hairItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hairItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void SetUtilityItem(string name)
	{
		if (this.m_utilityItem == name)
		{
			return;
		}
		this.m_utilityItem = name;
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_utilityItem, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode(), false);
		}
	}

	public void CustomUpdate()
	{
		this.UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		this.UpdateEquipmentVisuals();
		if (this.m_isPlayer)
		{
			this.UpdateBaseModel();
			this.UpdateColors();
		}
	}

	private void UpdateColors()
	{
		Color color = Utils.Vec3ToColor(this.m_skinColor);
		Color color2 = Utils.Vec3ToColor(this.m_hairColor);
		if (this.m_nview.GetZDO() != null)
		{
			color = Utils.Vec3ToColor(this.m_nview.GetZDO().GetVec3(ZDOVars.s_skinColor, Vector3.one));
			color2 = Utils.Vec3ToColor(this.m_nview.GetZDO().GetVec3(ZDOVars.s_hairColor, Vector3.one));
		}
		this.m_bodyModel.materials[0].SetColor("_SkinColor", color);
		this.m_bodyModel.materials[1].SetColor("_SkinColor", color2);
		if (this.m_beardItemInstance)
		{
			Renderer[] array = this.m_beardItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].material.SetColor("_SkinColor", color2);
			}
		}
		if (this.m_hairItemInstance)
		{
			Renderer[] array = this.m_hairItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].material.SetColor("_SkinColor", color2);
			}
		}
	}

	private void UpdateBaseModel()
	{
		if (this.m_models.Length == 0)
		{
			return;
		}
		int num = this.m_modelIndex;
		if (this.m_nview.GetZDO() != null)
		{
			num = this.m_nview.GetZDO().GetInt(ZDOVars.s_modelIndex, 0);
		}
		if (this.m_currentModelIndex != num || this.m_bodyModel.sharedMesh != this.m_models[num].m_mesh)
		{
			this.m_currentModelIndex = num;
			this.m_bodyModel.sharedMesh = this.m_models[num].m_mesh;
			this.m_bodyModel.materials[0].SetTexture("_MainTex", this.m_models[num].m_baseMaterial.GetTexture("_MainTex"));
			this.m_bodyModel.materials[0].SetTexture("_SkinBumpMap", this.m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
		}
	}

	private void UpdateEquipmentVisuals()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num12 = this.m_shoulderItemVariant;
		int num13 = this.m_leftItemVariant;
		int num14 = this.m_leftBackItemVariant;
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo != null)
		{
			num = zdo.GetInt(ZDOVars.s_leftItem, 0);
			num2 = zdo.GetInt(ZDOVars.s_rightItem, 0);
			num3 = zdo.GetInt(ZDOVars.s_chestItem, 0);
			num4 = zdo.GetInt(ZDOVars.s_legItem, 0);
			num5 = zdo.GetInt(ZDOVars.s_helmetItem, 0);
			num8 = zdo.GetInt(ZDOVars.s_shoulderItem, 0);
			num9 = zdo.GetInt(ZDOVars.s_utilityItem, 0);
			if (this.m_isPlayer)
			{
				num6 = zdo.GetInt(ZDOVars.s_beardItem, 0);
				num7 = zdo.GetInt(ZDOVars.s_hairItem, 0);
				num10 = zdo.GetInt(ZDOVars.s_leftBackItem, 0);
				num11 = zdo.GetInt(ZDOVars.s_rightBackItem, 0);
				num12 = zdo.GetInt(ZDOVars.s_shoulderItemVariant, 0);
				num13 = zdo.GetInt(ZDOVars.s_leftItemVariant, 0);
				num14 = zdo.GetInt(ZDOVars.s_leftBackItemVariant, 0);
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(this.m_leftItem))
			{
				num = this.m_leftItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_rightItem))
			{
				num2 = this.m_rightItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_chestItem))
			{
				num3 = this.m_chestItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_legItem))
			{
				num4 = this.m_legItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_helmetItem))
			{
				num5 = this.m_helmetItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_shoulderItem))
			{
				num8 = this.m_shoulderItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_utilityItem))
			{
				num9 = this.m_utilityItem.GetStableHashCode();
			}
			if (this.m_isPlayer)
			{
				if (!string.IsNullOrEmpty(this.m_beardItem))
				{
					num6 = this.m_beardItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_hairItem))
				{
					num7 = this.m_hairItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_leftBackItem))
				{
					num10 = this.m_leftBackItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_rightBackItem))
				{
					num11 = this.m_rightBackItem.GetStableHashCode();
				}
			}
		}
		bool flag = false;
		flag = this.SetRightHandEquipped(num2) || flag;
		flag = this.SetLeftHandEquipped(num, num13) || flag;
		flag = this.SetChestEquipped(num3) || flag;
		flag = this.SetLegEquipped(num4) || flag;
		flag = this.SetHelmetEquipped(num5, num7) || flag;
		flag = this.SetShoulderEquipped(num8, num12) || flag;
		flag = this.SetUtilityEquipped(num9) || flag;
		if (this.m_isPlayer)
		{
			if (this.m_helmetHideBeard)
			{
				num6 = 0;
			}
			flag = this.SetBeardEquipped(num6) || flag;
			flag = this.SetBackEquipped(num10, num11, num14) || flag;
			if (this.m_helmetHideHair)
			{
				num7 = 0;
			}
			flag = this.SetHairEquipped(num7) || flag;
		}
		if (flag)
		{
			this.UpdateLodgroup();
		}
	}

	private void UpdateLodgroup()
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		List<Renderer> list = new List<Renderer>(this.m_visual.GetComponentsInChildren<Renderer>());
		for (int i = list.Count - 1; i >= 0; i--)
		{
			Renderer renderer = list[i];
			LODGroup componentInParent = renderer.GetComponentInParent<LODGroup>();
			if (componentInParent != null && componentInParent != this.m_lodGroup)
			{
				LOD[] lods = componentInParent.GetLODs();
				for (int j = 0; j < lods.Length; j++)
				{
					if (Array.IndexOf<Renderer>(lods[j].renderers, renderer) >= 0)
					{
						list.RemoveAt(i);
						break;
					}
				}
			}
		}
		LOD[] lods2 = this.m_lodGroup.GetLODs();
		lods2[0].renderers = list.ToArray();
		this.m_lodGroup.SetLODs(lods2);
	}

	private bool SetRightHandEquipped(int hash)
	{
		if (this.m_currentRightItemHash == hash)
		{
			return false;
		}
		if (this.m_rightItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_rightItemInstance);
			this.m_rightItemInstance = null;
		}
		this.m_currentRightItemHash = hash;
		if (hash != 0)
		{
			this.m_rightItemInstance = this.AttachItem(hash, 0, this.m_rightHand, true, false);
		}
		return true;
	}

	private bool SetLeftHandEquipped(int hash, int variant)
	{
		if (this.m_currentLeftItemHash == hash && this.m_currentLeftItemVariant == variant)
		{
			return false;
		}
		if (this.m_leftItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_leftItemInstance);
			this.m_leftItemInstance = null;
		}
		this.m_currentLeftItemHash = hash;
		this.m_currentLeftItemVariant = variant;
		if (hash != 0)
		{
			this.m_leftItemInstance = this.AttachItem(hash, variant, this.m_leftHand, true, false);
		}
		return true;
	}

	private bool SetBackEquipped(int leftItem, int rightItem, int leftVariant)
	{
		if (this.m_currentLeftBackItemHash == leftItem && this.m_currentRightBackItemHash == rightItem && this.m_currentLeftBackItemVariant == leftVariant)
		{
			return false;
		}
		if (this.m_leftBackItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_leftBackItemInstance);
			this.m_leftBackItemInstance = null;
		}
		if (this.m_rightBackItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_rightBackItemInstance);
			this.m_rightBackItemInstance = null;
		}
		this.m_currentLeftBackItemHash = leftItem;
		this.m_currentRightBackItemHash = rightItem;
		this.m_currentLeftBackItemVariant = leftVariant;
		if (this.m_currentLeftBackItemHash != 0)
		{
			this.m_leftBackItemInstance = this.AttachBackItem(leftItem, leftVariant, false);
		}
		if (this.m_currentRightBackItemHash != 0)
		{
			this.m_rightBackItemInstance = this.AttachBackItem(rightItem, 0, true);
		}
		return true;
	}

	private GameObject AttachBackItem(int hash, int variant, bool rightHand)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing back attach item prefab: " + hash.ToString());
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		ItemDrop.ItemData.ItemType itemType = ((component.m_itemData.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None) ? component.m_itemData.m_shared.m_attachOverride : component.m_itemData.m_shared.m_itemType);
		if (itemType == ItemDrop.ItemData.ItemType.Torch)
		{
			if (rightHand)
			{
				return this.AttachItem(hash, variant, this.m_backMelee, false, true);
			}
			return this.AttachItem(hash, variant, this.m_backTool, false, true);
		}
		else
		{
			switch (itemType)
			{
			case ItemDrop.ItemData.ItemType.OneHandedWeapon:
				return this.AttachItem(hash, variant, this.m_backMelee, false, true);
			case ItemDrop.ItemData.ItemType.Bow:
				return this.AttachItem(hash, variant, this.m_backBow, false, true);
			case ItemDrop.ItemData.ItemType.Shield:
				return this.AttachItem(hash, variant, this.m_backShield, false, true);
			default:
				if (itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon)
				{
					switch (itemType)
					{
					case ItemDrop.ItemData.ItemType.Tool:
						return this.AttachItem(hash, variant, this.m_backTool, false, true);
					case ItemDrop.ItemData.ItemType.Attach_Atgeir:
						return this.AttachItem(hash, variant, this.m_backAtgeir, false, true);
					case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
						goto IL_10B;
					}
					return null;
				}
				IL_10B:
				return this.AttachItem(hash, variant, this.m_backTwohandedMelee, false, true);
			}
		}
	}

	private bool SetChestEquipped(int hash)
	{
		if (this.m_currentChestItemHash == hash)
		{
			return false;
		}
		this.m_currentChestItemHash = hash;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_chestItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_chestItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_chestItemInstances = null;
			this.m_bodyModel.material.SetTexture("_ChestTex", this.m_emptyBodyTexture);
			this.m_bodyModel.material.SetTexture("_ChestBumpMap", null);
			this.m_bodyModel.material.SetTexture("_ChestMetal", null);
		}
		if (this.m_currentChestItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing chest item " + hash.ToString());
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component.m_itemData.m_shared.m_armorMaterial)
		{
			this.m_bodyModel.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
			this.m_bodyModel.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
			this.m_bodyModel.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
		}
		this.m_chestItemInstances = this.AttachArmor(hash, -1);
		return true;
	}

	private bool SetShoulderEquipped(int hash, int variant)
	{
		if (this.m_currentShoulderItemHash == hash && this.m_currentShoulderItemVariant == variant)
		{
			return false;
		}
		this.m_currentShoulderItemHash = hash;
		this.m_currentShoulderItemVariant = variant;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_shoulderItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_shoulderItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_shoulderItemInstances = null;
		}
		if (this.m_currentShoulderItemHash == 0)
		{
			return true;
		}
		if (ObjectDB.instance.GetItemPrefab(hash) == null)
		{
			ZLog.Log("Missing shoulder item " + hash.ToString());
			return true;
		}
		this.m_shoulderItemInstances = this.AttachArmor(hash, variant);
		return true;
	}

	private bool SetLegEquipped(int hash)
	{
		if (this.m_currentLegItemHash == hash)
		{
			return false;
		}
		this.m_currentLegItemHash = hash;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_legItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_legItemInstances)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_legItemInstances = null;
			this.m_bodyModel.material.SetTexture("_LegsTex", this.m_emptyLegsTexture);
			this.m_bodyModel.material.SetTexture("_LegsBumpMap", null);
			this.m_bodyModel.material.SetTexture("_LegsMetal", null);
		}
		if (this.m_currentLegItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing legs item " + hash.ToString());
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component.m_itemData.m_shared.m_armorMaterial)
		{
			this.m_bodyModel.material.SetTexture("_LegsTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsTex"));
			this.m_bodyModel.material.SetTexture("_LegsBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsBumpMap"));
			this.m_bodyModel.material.SetTexture("_LegsMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsMetal"));
		}
		this.m_legItemInstances = this.AttachArmor(hash, -1);
		return true;
	}

	private bool SetBeardEquipped(int hash)
	{
		if (this.m_currentBeardItemHash == hash)
		{
			return false;
		}
		if (this.m_beardItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_beardItemInstance);
			this.m_beardItemInstance = null;
		}
		this.m_currentBeardItemHash = hash;
		if (hash != 0)
		{
			this.m_beardItemInstance = this.AttachItem(hash, 0, this.m_helmet, true, false);
		}
		return true;
	}

	private bool SetHairEquipped(int hash)
	{
		if (this.m_currentHairItemHash == hash)
		{
			return false;
		}
		if (this.m_hairItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_hairItemInstance);
			this.m_hairItemInstance = null;
		}
		this.m_currentHairItemHash = hash;
		if (hash != 0)
		{
			this.m_hairItemInstance = this.AttachItem(hash, 0, this.m_helmet, true, false);
		}
		return true;
	}

	private bool SetHelmetEquipped(int hash, int hairHash)
	{
		if (this.m_currentHelmetItemHash == hash)
		{
			return false;
		}
		if (this.m_helmetItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_helmetItemInstance);
			this.m_helmetItemInstance = null;
		}
		this.m_currentHelmetItemHash = hash;
		VisEquipment.HelmetHides(hash, out this.m_helmetHideHair, out this.m_helmetHideBeard);
		if (hash != 0)
		{
			this.m_helmetItemInstance = this.AttachItem(hash, 0, this.m_helmet, true, false);
		}
		return true;
	}

	private bool SetUtilityEquipped(int hash)
	{
		if (this.m_currentUtilityItemHash == hash)
		{
			return false;
		}
		if (this.m_utilityItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_utilityItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_utilityItemInstances = null;
		}
		this.m_currentUtilityItemHash = hash;
		if (hash != 0)
		{
			this.m_utilityItemInstances = this.AttachArmor(hash, -1);
		}
		return true;
	}

	private static void HelmetHides(int itemHash, out bool hideHair, out bool hideBeard)
	{
		hideHair = false;
		hideBeard = false;
		if (itemHash == 0)
		{
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		hideHair = component.m_itemData.m_shared.m_helmetHideHair;
		hideBeard = component.m_itemData.m_shared.m_helmetHideBeard;
	}

	private List<GameObject> AttachArmor(int itemHash, int variant = -1)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing attach item: " + itemHash.ToString() + "  ob:" + base.gameObject.name);
			return null;
		}
		List<GameObject> list = new List<GameObject>();
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (child.gameObject.name.CustomStartsWith("attach_"))
			{
				string text = child.gameObject.name.Substring(7);
				GameObject gameObject;
				if (text == "skin")
				{
					gameObject = UnityEngine.Object.Instantiate<GameObject>(child.gameObject, this.m_bodyModel.transform.position, this.m_bodyModel.transform.parent.rotation, this.m_bodyModel.transform.parent);
					gameObject.SetActive(true);
					foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
					{
						skinnedMeshRenderer.rootBone = this.m_bodyModel.rootBone;
						skinnedMeshRenderer.bones = this.m_bodyModel.bones;
					}
					foreach (Cloth cloth in gameObject.GetComponentsInChildren<Cloth>())
					{
						if (this.m_clothColliders.Length != 0)
						{
							if (cloth.capsuleColliders.Length != 0)
							{
								List<CapsuleCollider> list2 = new List<CapsuleCollider>(this.m_clothColliders);
								list2.AddRange(cloth.capsuleColliders);
								cloth.capsuleColliders = list2.ToArray();
							}
							else
							{
								cloth.capsuleColliders = this.m_clothColliders;
							}
						}
					}
				}
				else
				{
					Transform transform = Utils.FindChild(this.m_visual.transform, text);
					if (transform == null)
					{
						ZLog.LogWarning("Missing joint " + text + " in item " + itemPrefab.name);
						goto IL_255;
					}
					gameObject = UnityEngine.Object.Instantiate<GameObject>(child.gameObject);
					gameObject.SetActive(true);
					gameObject.transform.SetParent(transform);
					gameObject.transform.localPosition = Vector3.zero;
					gameObject.transform.localRotation = Quaternion.identity;
				}
				if (variant >= 0)
				{
					IEquipmentVisual componentInChildren = gameObject.GetComponentInChildren<IEquipmentVisual>();
					if (componentInChildren != null)
					{
						componentInChildren.Setup(variant);
					}
				}
				VisEquipment.CleanupInstance(gameObject);
				VisEquipment.EnableEquippedEffects(gameObject);
				list.Add(gameObject);
			}
			IL_255:;
		}
		return list;
	}

	private GameObject AttachItem(int itemHash, int variant, Transform joint, bool enableEquipEffects = true, bool backAttach = false)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log(string.Concat(new string[]
			{
				"Missing attach item: ",
				itemHash.ToString(),
				"  ob:",
				base.gameObject.name,
				"  joint:",
				joint ? joint.name : "none"
			}));
			return null;
		}
		GameObject gameObject = null;
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (backAttach && child.gameObject.name == "attach_back")
			{
				gameObject = child.gameObject;
				break;
			}
			if (child.gameObject.name == "attach" || (!backAttach && child.gameObject.name == "attach_skin"))
			{
				gameObject = child.gameObject;
				break;
			}
		}
		if (gameObject == null)
		{
			return null;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject);
		gameObject2.SetActive(true);
		VisEquipment.CleanupInstance(gameObject2);
		if (enableEquipEffects)
		{
			VisEquipment.EnableEquippedEffects(gameObject2);
		}
		if (gameObject.name == "attach_skin")
		{
			gameObject2.transform.SetParent(this.m_bodyModel.transform.parent);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject2.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				skinnedMeshRenderer.rootBone = this.m_bodyModel.rootBone;
				skinnedMeshRenderer.bones = this.m_bodyModel.bones;
			}
		}
		else
		{
			gameObject2.transform.SetParent(joint);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
		}
		IEquipmentVisual componentInChildren = gameObject2.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(variant);
		}
		return gameObject2;
	}

	private static void CleanupInstance(GameObject instance)
	{
		Collider[] componentsInChildren = instance.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	private static void EnableEquippedEffects(GameObject instance)
	{
		Transform transform = instance.transform.Find("equiped");
		if (transform)
		{
			transform.gameObject.SetActive(true);
		}
	}

	public int GetModelIndex()
	{
		int num = this.m_modelIndex;
		if (this.m_nview.IsValid())
		{
			num = this.m_nview.GetZDO().GetInt(ZDOVars.s_modelIndex, 0);
		}
		return num;
	}

	public static List<VisEquipment> Instances { get; } = new List<VisEquipment>();

	public SkinnedMeshRenderer m_bodyModel;

	public ZNetView m_nViewOverride;

	[Header("Attachment points")]
	public Transform m_leftHand;

	public Transform m_rightHand;

	public Transform m_helmet;

	public Transform m_backShield;

	public Transform m_backMelee;

	public Transform m_backTwohandedMelee;

	public Transform m_backBow;

	public Transform m_backTool;

	public Transform m_backAtgeir;

	public CapsuleCollider[] m_clothColliders = Array.Empty<CapsuleCollider>();

	public VisEquipment.PlayerModel[] m_models = Array.Empty<VisEquipment.PlayerModel>();

	public bool m_isPlayer;

	public bool m_useAllTrails;

	private string m_leftItem = "";

	private string m_rightItem = "";

	private string m_chestItem = "";

	private string m_legItem = "";

	private string m_helmetItem = "";

	private string m_shoulderItem = "";

	private string m_beardItem = "";

	private string m_hairItem = "";

	private string m_utilityItem = "";

	private string m_leftBackItem = "";

	private string m_rightBackItem = "";

	private int m_shoulderItemVariant;

	private int m_leftItemVariant;

	private int m_leftBackItemVariant;

	private GameObject m_leftItemInstance;

	private GameObject m_rightItemInstance;

	private GameObject m_helmetItemInstance;

	private List<GameObject> m_chestItemInstances;

	private List<GameObject> m_legItemInstances;

	private List<GameObject> m_shoulderItemInstances;

	private List<GameObject> m_utilityItemInstances;

	private GameObject m_beardItemInstance;

	private GameObject m_hairItemInstance;

	private GameObject m_leftBackItemInstance;

	private GameObject m_rightBackItemInstance;

	private int m_currentLeftItemHash;

	private int m_currentRightItemHash;

	private int m_currentChestItemHash;

	private int m_currentLegItemHash;

	private int m_currentHelmetItemHash;

	private int m_currentShoulderItemHash;

	private int m_currentBeardItemHash;

	private int m_currentHairItemHash;

	private int m_currentUtilityItemHash;

	private int m_currentLeftBackItemHash;

	private int m_currentRightBackItemHash;

	private int m_currentShoulderItemVariant;

	private int m_currentLeftItemVariant;

	private int m_currentLeftBackItemVariant;

	private bool m_helmetHideHair;

	private bool m_helmetHideBeard;

	private Texture m_emptyBodyTexture;

	private Texture m_emptyLegsTexture;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private int m_currentModelIndex;

	private ZNetView m_nview;

	private GameObject m_visual;

	private LODGroup m_lodGroup;

	[Serializable]
	public class PlayerModel
	{

		public Mesh m_mesh;

		public Material m_baseMaterial;
	}
}
