using System;
using TMPro;
using UnityEngine;

public class KeyHints : MonoBehaviour
{

	private void OnDestroy()
	{
		KeyHints.m_instance = null;
	}

	public static KeyHints instance
	{
		get
		{
			return KeyHints.m_instance;
		}
	}

	private void Awake()
	{
		KeyHints.m_instance = this;
		this.ApplySettings();
	}

	public void SetGamePadBindings()
	{
		if (this.m_buildMenuKey != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_buildMenuKey);
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default)
			{
				if (inputLayout == InputLayout.Alternative1)
				{
					this.m_buildMenuKey.text = "<mspace=0.6em>$KEY_BuildMenu </mspace>$hud_buildmenu";
				}
			}
			else
			{
				this.m_buildMenuKey.text = "<mspace=0.6em>$KEY_Use </mspace>$hud_buildmenu";
			}
			Localization.instance.Localize(this.m_buildMenuKey.transform);
		}
		if (this.m_buildRotateKey != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_buildRotateKey);
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default)
			{
				if (inputLayout == InputLayout.Alternative1)
				{
					this.m_buildRotateKey.text = "<mspace=0.6em>$KEY_LTrigger / $KEY_RTrigger </mspace>$hud_rotate";
				}
			}
			else
			{
				this.m_buildRotateKey.text = "<mspace=0.6em>$KEY_Block + $KEY_RightStick </mspace>$hud_rotate";
			}
			Localization.instance.Localize(this.m_buildRotateKey.transform);
		}
		if (this.m_dodgeKey != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_dodgeKey);
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default)
			{
				if (inputLayout == InputLayout.Alternative1)
				{
					this.m_dodgeKey.text = "<mspace=0.6em>$KEY_Block + $KEY_Dodge </mspace>$settings_dodge";
				}
			}
			else
			{
				this.m_dodgeKey.text = "<mspace=0.6em>$KEY_Block + $KEY_Jump </mspace>$settings_dodge";
			}
			Localization.instance.Localize(this.m_dodgeKey.transform);
		}
	}

	private void Start()
	{
	}

	public void ApplySettings()
	{
		this.m_keyHintsEnabled = PlayerPrefs.GetInt("KeyHints", 1) == 1;
		this.SetGamePadBindings();
	}

	private void Update()
	{
		this.UpdateHints();
		if (Input.GetKeyDown(KeyCode.F9))
		{
			InputLayout inputLayout = ZInput.InputLayout;
			if (inputLayout != InputLayout.Default && inputLayout == InputLayout.Alternative1)
			{
				ZInput.instance.ChangeLayout(InputLayout.Default);
			}
			else
			{
				ZInput.instance.ChangeLayout(InputLayout.Alternative1);
			}
			this.ApplySettings();
		}
	}

	private void UpdateHints()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!this.m_keyHintsEnabled || localPlayer == null || localPlayer.IsDead() || Chat.instance.IsChatDialogWindowVisible() || Game.IsPaused() || (InventoryGui.instance != null && (InventoryGui.instance.IsSkillsPanelOpen || InventoryGui.instance.IsTrophisPanelOpen || InventoryGui.instance.IsTextPanelOpen)))
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			return;
		}
		bool activeSelf = this.m_buildHints.activeSelf;
		bool activeSelf2 = this.m_buildHints.activeSelf;
		ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
		if (InventoryGui.IsVisible())
		{
			bool flag = InventoryGui.instance.IsContainerOpen();
			bool flag2 = InventoryGui.instance.ActiveGroup == 0;
			ItemDrop.ItemData itemData = (flag2 ? InventoryGui.instance.ContainerGrid.GetGamepadSelectedItem() : InventoryGui.instance.m_playerGrid.GetGamepadSelectedItem());
			bool flag3 = itemData != null && itemData.IsEquipable();
			bool flag4 = itemData != null && itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(!flag);
			this.m_inventoryWithContainerHints.SetActive(flag);
			for (int i = 0; i < this.m_equipButtons.Length; i++)
			{
				this.m_equipButtons[i].SetActive(flag4 || (flag3 && !flag2));
			}
			this.m_fishingHints.SetActive(false);
			return;
		}
		if (localPlayer.InPlaceMode())
		{
			if (ZInput.InputLayout == InputLayout.Alternative1)
			{
				string text = Localization.instance.Localize("<mspace=0.6em>$KEY_AltKeys + $KEY_AltPlace</mspace>  $hud_altplacement");
				string text2 = (localPlayer.AlternativePlacementActive ? Localization.instance.Localize("$hud_off") : Localization.instance.Localize("$hud_on"));
				this.m_buildAlternativePlacingKey.text = text + " " + text2;
			}
			this.m_buildHints.SetActive(true);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			return;
		}
		if (localPlayer.GetDoodadController() != null)
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			return;
		}
		if (currentWeapon != null && currentWeapon.m_shared.m_animationState == ItemDrop.ItemData.AnimationState.FishingRod)
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(true);
			return;
		}
		if (currentWeapon != null && (currentWeapon != localPlayer.m_unarmedWeapon.m_itemData || localPlayer.IsTargeted()))
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(true);
			this.m_inventoryHints.SetActive(false);
			this.m_inventoryWithContainerHints.SetActive(false);
			this.m_fishingHints.SetActive(false);
			bool flag5 = currentWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow && currentWeapon.m_shared.m_skillType != Skills.SkillType.Crossbows;
			bool flag6 = !flag5 && currentWeapon.HavePrimaryAttack();
			bool flag7 = !flag5 && currentWeapon.HaveSecondaryAttack();
			this.m_bowDrawGP.SetActive(flag5);
			this.m_bowDrawKB.SetActive(flag5);
			this.m_primaryAttackGP.SetActive(flag6);
			this.m_primaryAttackKB.SetActive(flag6);
			this.m_secondaryAttackGP.SetActive(flag7);
			this.m_secondaryAttackKB.SetActive(flag7);
			return;
		}
		this.m_buildHints.SetActive(false);
		this.m_combatHints.SetActive(false);
		this.m_inventoryHints.SetActive(false);
		this.m_inventoryWithContainerHints.SetActive(false);
		this.m_fishingHints.SetActive(false);
	}

	private static KeyHints m_instance;

	[Header("Key hints")]
	public GameObject m_buildHints;

	public GameObject m_combatHints;

	public GameObject m_inventoryHints;

	public GameObject m_inventoryWithContainerHints;

	public GameObject m_fishingHints;

	public GameObject[] m_equipButtons;

	public GameObject m_primaryAttackGP;

	public GameObject m_primaryAttackKB;

	public GameObject m_secondaryAttackGP;

	public GameObject m_secondaryAttackKB;

	public GameObject m_bowDrawGP;

	public GameObject m_bowDrawKB;

	private bool m_keyHintsEnabled = true;

	public TextMeshProUGUI m_buildMenuKey;

	public TextMeshProUGUI m_buildRotateKey;

	public TextMeshProUGUI m_buildAlternativePlacingKey;

	public TextMeshProUGUI m_dodgeKey;
}
