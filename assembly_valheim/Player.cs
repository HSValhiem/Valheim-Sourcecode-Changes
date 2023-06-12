using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Player : Humanoid
{

	protected override void Awake()
	{
		base.Awake();
		Player.s_players.Add(this);
		this.m_skills = base.GetComponent<Skills>();
		this.SetupAwake();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_placeRayMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle" });
		this.m_placeWaterRayMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "Water", "vehicle" });
		this.m_removeRayMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle" });
		this.m_interactMask = LayerMask.GetMask(new string[] { "item", "piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "character", "character_net", "terrain", "vehicle" });
		this.m_autoPickupMask = LayerMask.GetMask(new string[] { "item" });
		Inventory inventory = this.m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnInventoryChanged));
		if (Player.s_attackMask == 0)
		{
			Player.s_attackMask = LayerMask.GetMask(new string[]
			{
				"Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox",
				"character_noenv", "vehicle"
			});
		}
		this.m_nview.Register("OnDeath", new Action<long>(this.RPC_OnDeath));
		if (this.m_nview.IsOwner())
		{
			this.m_nview.Register<int, string, int>("Message", new Action<long, int, string, int>(this.RPC_Message));
			this.m_nview.Register<bool, bool>("OnTargeted", new Action<long, bool, bool>(this.RPC_OnTargeted));
			this.m_nview.Register<float>("UseStamina", new Action<long, float>(this.RPC_UseStamina));
			if (MusicMan.instance)
			{
				MusicMan.instance.TriggerMusic("Wakeup");
			}
			this.UpdateKnownRecipesList();
			this.UpdateAvailablePiecesList();
			this.SetupPlacementGhost();
		}
		this.m_placeRotation = UnityEngine.Random.Range(0, 16);
		float num = UnityEngine.Random.Range(0f, 6.28318548f);
		base.SetLookDir(new Vector3(Mathf.Cos(num), 0f, Mathf.Sin(num)), 0f);
		this.FaceLookDirection();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
	}

	public void SetLocalPlayer()
	{
		if (Player.m_localPlayer == this)
		{
			return;
		}
		Player.m_localPlayer = this;
		ZNet.instance.SetReferencePosition(base.transform.position);
		EnvMan.instance.SetForceEnvironment("");
	}

	public void SetPlayerID(long playerID, string name)
	{
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.GetPlayerID() != 0L)
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_playerID, playerID);
		this.m_nview.GetZDO().Set(ZDOVars.s_playerName, name);
	}

	public long GetPlayerID()
	{
		if (!this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_playerID, 0L);
	}

	public string GetPlayerName()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_playerName, "...");
	}

	public override string GetHoverText()
	{
		return "";
	}

	public override string GetHoverName()
	{
		return this.GetPlayerName();
	}

	protected override void Start()
	{
		base.Start();
	}

	protected override void OnDestroy()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo != null && ZNet.instance != null)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Player destroyed sec:",
				zdo.GetSector().ToString(),
				"  pos:",
				base.transform.position.ToString(),
				"  zdopos:",
				zdo.GetPosition().ToString(),
				"  ref ",
				ZNet.instance.GetReferencePosition().ToString()
			}));
		}
		if (this.m_placementGhost)
		{
			UnityEngine.Object.Destroy(this.m_placementGhost);
			this.m_placementGhost = null;
		}
		base.OnDestroy();
		Player.s_players.Remove(this);
		if (Player.m_localPlayer == this)
		{
			ZLog.LogWarning("Local player destroyed");
			Player.m_localPlayer = null;
		}
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateAwake(fixedDeltaTime);
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.UpdateTargeted(fixedDeltaTime);
		if (this.m_nview.IsOwner())
		{
			if (Player.m_localPlayer != this)
			{
				ZLog.Log("Destroying old local player");
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
			if (this.IsDead())
			{
				return;
			}
			this.UpdateActionQueue(fixedDeltaTime);
			this.PlayerAttackInput(fixedDeltaTime);
			this.UpdateAttach();
			this.UpdateDoodadControls(fixedDeltaTime);
			this.UpdateCrouch(fixedDeltaTime);
			this.UpdateDodge(fixedDeltaTime);
			this.UpdateCover(fixedDeltaTime);
			this.UpdateStations(fixedDeltaTime);
			this.UpdateGuardianPower(fixedDeltaTime);
			this.UpdateBaseValue(fixedDeltaTime);
			this.UpdateStats(fixedDeltaTime);
			this.UpdateTeleport(fixedDeltaTime);
			this.AutoPickup(fixedDeltaTime);
			this.EdgeOfWorldKill(fixedDeltaTime);
			this.UpdateBiome(fixedDeltaTime);
			this.UpdateStealth(fixedDeltaTime);
			if (GameCamera.instance && Vector3.Distance(GameCamera.instance.transform.position, base.transform.position) < 2f)
			{
				base.SetVisible(false);
			}
			AudioMan.instance.SetIndoor(this.InShelter());
		}
	}

	private void Update()
	{
		bool flag = InventoryGui.IsVisible();
		if (ZInput.InputLayout != InputLayout.Default && ZInput.IsGamepadActive() && !flag && (ZInput.GetButtonUp("JoyAltPlace") && ZInput.GetButton("JoyAltKeys")))
		{
			this.m_altPlace = !this.m_altPlace;
			if (MessageHud.instance != null)
			{
				string text = Localization.instance.Localize("$hud_altplacement");
				string text2 = (this.m_altPlace ? Localization.instance.Localize("$hud_on") : Localization.instance.Localize("$hud_off"));
				MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text + " " + text2, 0, null);
			}
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		bool flag2 = this.TakeInput();
		this.UpdateHover();
		if (flag2)
		{
			if (Player.m_debugMode && global::Console.instance.IsCheatsEnabled())
			{
				if (Input.GetKeyDown(KeyCode.Z))
				{
					this.ToggleDebugFly();
				}
				if (Input.GetKeyDown(KeyCode.B))
				{
					this.ToggleNoPlacementCost();
				}
				if (Input.GetKeyDown(KeyCode.K))
				{
					global::Console.instance.TryRunCommand("killenemies", false, false);
				}
				if (Input.GetKeyDown(KeyCode.L))
				{
					global::Console.instance.TryRunCommand("removedrops", false, false);
				}
			}
			bool flag3 = ((ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive()) ? ZInput.GetButton("JoyAltKeys") : (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace")));
			if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))
			{
				if (this.m_hovering)
				{
					this.Interact(this.m_hovering, false, flag3);
				}
				else if (this.m_doodadController != null)
				{
					this.StopDoodadControl();
				}
			}
			else if ((ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && this.m_hovering)
			{
				this.Interact(this.m_hovering, true, flag3);
			}
			if ((ZInput.InputLayout != InputLayout.Default && ZInput.IsGamepadActive()) ? (!this.InPlaceMode() && ZInput.GetButtonDown("JoyHide") && !ZInput.GetButton("JoyAltKeys")) : (ZInput.GetButtonDown("Hide") || (ZInput.GetButtonDown("JoyHide") && !ZInput.GetButton("JoyAltKeys"))))
			{
				if (base.GetRightItem() != null || base.GetLeftItem() != null)
				{
					if (!this.InAttack() && !this.InDodge())
					{
						base.HideHandItems();
					}
				}
				else if ((!base.IsSwimming() || base.IsOnGround()) && !this.InDodge())
				{
					base.ShowHandItems();
				}
			}
			if (ZInput.GetButtonDown("ToggleWalk"))
			{
				base.SetWalk(!base.GetWalk());
				if (base.GetWalk())
				{
					this.Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_on", 0, null);
				}
				else
				{
					this.Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_off", 0, null);
				}
			}
			if (ZInput.GetButtonDown("Sit") || (!this.InPlaceMode() && ZInput.GetButtonDown("JoySit")))
			{
				if (this.InEmote() && this.IsSitting())
				{
					this.StopEmote();
				}
				else
				{
					this.StartEmote("sit", false);
				}
			}
			bool flag4 = ZInput.IsGamepadActive() && !ZInput.GetButton("JoyAltKeys");
			bool flag5 = ZInput.InputLayout == InputLayout.Default && ZInput.GetButtonDown("JoyGP");
			bool flag6 = ZInput.InputLayout == InputLayout.Alternative1 && ZInput.GetButton("JoyLStick") && ZInput.GetButton("JoyRStick");
			if (ZInput.GetButtonDown("GP") || (flag4 && (flag5 || flag6)))
			{
				this.StartGuardianPower();
			}
			bool flag7 = ZInput.GetButtonDown("JoyAutoPickup") && ZInput.GetButton("JoyAltKeys");
			if (ZInput.GetButtonDown("AutoPickup") || flag7)
			{
				this.m_enableAutoPickup = !this.m_enableAutoPickup;
				this.Message(MessageHud.MessageType.TopLeft, "$hud_autopickup:" + (this.m_enableAutoPickup ? "$hud_on" : "$hud_off"), 0, null);
			}
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				this.UseHotbarItem(1);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				this.UseHotbarItem(2);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				this.UseHotbarItem(3);
			}
			if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				this.UseHotbarItem(4);
			}
			if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				this.UseHotbarItem(5);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6))
			{
				this.UseHotbarItem(6);
			}
			if (Input.GetKeyDown(KeyCode.Alpha7))
			{
				this.UseHotbarItem(7);
			}
			if (Input.GetKeyDown(KeyCode.Alpha8))
			{
				this.UseHotbarItem(8);
			}
		}
		this.UpdatePlacement(flag2, Time.deltaTime);
	}

	private void UpdatePlacement(bool takeInput, float dt)
	{
		this.UpdateWearNTearHover();
		if (!this.InPlaceMode())
		{
			if (this.m_placementGhost)
			{
				this.m_placementGhost.SetActive(false);
			}
			return;
		}
		if (!takeInput)
		{
			return;
		}
		this.UpdateBuildGuiInput();
		if (Hud.IsPieceSelectionVisible())
		{
			return;
		}
		ItemDrop.ItemData rightItem = base.GetRightItem();
		if (ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove"))
		{
			this.m_removePressedTime = Time.time;
		}
		if (Time.time - this.m_removePressedTime < 0.2f && rightItem.m_shared.m_buildPieces.m_canRemovePieces && Time.time - this.m_lastToolUseTime > this.m_removeDelay)
		{
			this.m_removePressedTime = -9999f;
			if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
			{
				if (this.RemovePiece())
				{
					this.m_lastToolUseTime = Time.time;
					base.AddNoise(50f);
					this.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
					if (rightItem.m_shared.m_useDurability)
					{
						rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
					}
				}
			}
			else
			{
				Hud.instance.StaminaBarEmptyFlash();
			}
		}
		Piece selectedPiece = this.m_buildPieces.GetSelectedPiece();
		if (selectedPiece != null)
		{
			if (ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace"))
			{
				this.m_placePressedTime = Time.time;
			}
			if (Time.time - this.m_placePressedTime < 0.2f && Time.time - this.m_lastToolUseTime > this.m_placeDelay)
			{
				this.m_placePressedTime = -9999f;
				if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
				{
					if (selectedPiece.m_repairPiece)
					{
						this.Repair(rightItem, selectedPiece);
					}
					else if (this.m_placementGhost != null)
					{
						if (this.m_noPlacementCost || this.HaveRequirements(selectedPiece, Player.RequirementMode.CanBuild))
						{
							if (this.PlacePiece(selectedPiece))
							{
								this.m_lastToolUseTime = Time.time;
								this.ConsumeResources(selectedPiece.m_resources, 0, -1);
								this.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
								if (rightItem.m_shared.m_useDurability)
								{
									rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
								}
							}
						}
						else
						{
							this.Message(MessageHud.MessageType.Center, "$msg_missingrequirement", 0, null);
						}
					}
				}
				else
				{
					Hud.instance.StaminaBarEmptyFlash();
				}
			}
		}
		if (this.m_placementGhost != null)
		{
			IPieceMarker component = this.m_placementGhost.gameObject.GetComponent<IPieceMarker>();
			if (component != null)
			{
				component.ShowBuildMarker();
			}
		}
		Piece hoveringPiece = this.GetHoveringPiece();
		if (hoveringPiece)
		{
			IPieceMarker component2 = hoveringPiece.gameObject.GetComponent<IPieceMarker>();
			if (component2 != null)
			{
				component2.ShowHoverMarker();
			}
		}
		if (Input.GetAxis("Mouse ScrollWheel") < 0f)
		{
			this.m_placeRotation--;
		}
		if (Input.GetAxis("Mouse ScrollWheel") > 0f)
		{
			this.m_placeRotation++;
		}
		float num = ZInput.GetJoyRightStickX();
		bool flag = ZInput.GetButton("JoyRotate") && Mathf.Abs(num) > 0.5f;
		if (ZInput.IsGamepadActive() && ZInput.InputLayout == InputLayout.Alternative1)
		{
			flag = ZInput.GetButton("JoyRotate") || ZInput.GetButton("JoyRotateRight");
			num = (ZInput.GetButton("JoyRotate") ? 0.5f : (-0.5f));
		}
		if (flag)
		{
			if (this.m_rotatePieceTimer == 0f)
			{
				if (num < 0f)
				{
					this.m_placeRotation++;
				}
				else
				{
					this.m_placeRotation--;
				}
			}
			else if (this.m_rotatePieceTimer > 0.25f)
			{
				if (num < 0f)
				{
					this.m_placeRotation++;
				}
				else
				{
					this.m_placeRotation--;
				}
				this.m_rotatePieceTimer = 0.17f;
			}
			this.m_rotatePieceTimer += dt;
			return;
		}
		this.m_rotatePieceTimer = 0f;
	}

	private void UpdateBuildGuiInputAlternative1()
	{
		if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("JoyBuildMenu"))
		{
			for (int i = 0; i < this.m_buildPieces.m_selectedPiece.Length; i++)
			{
				this.m_buildPieces.m_lastSelectedPiece[i] = this.m_buildPieces.m_selectedPiece[i];
			}
			Hud.instance.TogglePieceSelection();
			return;
		}
		if (Hud.IsPieceSelectionVisible())
		{
			if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
			{
				for (int j = 0; j < this.m_buildPieces.m_selectedPiece.Length; j++)
				{
					this.m_buildPieces.m_selectedPiece[j] = this.m_buildPieces.m_lastSelectedPiece[j];
				}
				Hud.HidePieceSelection();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyButtonA"))
			{
				Hud.HidePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("TabLeft") || ZInput.GetAxis("Mouse ScrollWheel") > 0f)
			{
				this.m_buildPieces.PrevCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("TabRight") || ZInput.GetAxis("Mouse ScrollWheel") < 0f)
			{
				this.m_buildPieces.NextCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				this.m_buildPieces.LeftPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				this.m_buildPieces.RightPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.m_buildPieces.UpPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.m_buildPieces.DownPiece();
				this.SetupPlacementGhost();
			}
		}
	}

	private void UpdateBuildGuiInput()
	{
		if (ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive())
		{
			this.UpdateBuildGuiInputAlternative1();
			return;
		}
		if (Hud.instance.IsQuickPieceSelectEnabled())
		{
			if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("BuildMenu"))
			{
				Hud.instance.TogglePieceSelection();
			}
		}
		else if (ZInput.GetButtonDown("BuildMenu"))
		{
			Hud.instance.TogglePieceSelection();
		}
		if (ZInput.GetButtonDown("JoyUse"))
		{
			Hud.instance.TogglePieceSelection();
		}
		if (Hud.IsPieceSelectionVisible())
		{
			if (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
			{
				Hud.HidePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("TabLeft") || Input.GetAxis("Mouse ScrollWheel") > 0f)
			{
				this.m_buildPieces.PrevCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("TabRight") || Input.GetAxis("Mouse ScrollWheel") < 0f)
			{
				this.m_buildPieces.NextCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				this.m_buildPieces.LeftPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				this.m_buildPieces.RightPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.m_buildPieces.UpPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.m_buildPieces.DownPiece();
				this.SetupPlacementGhost();
			}
		}
	}

	public void SetSelectedPiece(Vector2Int p)
	{
		if (this.m_buildPieces && this.m_buildPieces.GetSelectedIndex() != p)
		{
			this.m_buildPieces.SetSelected(p);
			this.SetupPlacementGhost();
		}
	}

	public Piece GetPiece(Vector2Int p)
	{
		if (!(this.m_buildPieces != null))
		{
			return null;
		}
		return this.m_buildPieces.GetPiece(p);
	}

	public bool IsPieceAvailable(Piece piece)
	{
		return this.m_buildPieces != null && this.m_buildPieces.IsPieceAvailable(piece);
	}

	public Piece GetSelectedPiece()
	{
		if (!(this.m_buildPieces != null))
		{
			return null;
		}
		return this.m_buildPieces.GetSelectedPiece();
	}

	private void LateUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateEmote();
		if (this.m_nview.IsOwner())
		{
			ZNet.instance.SetReferencePosition(base.transform.position);
			this.UpdatePlacementGhost(false);
		}
	}

	private void SetupAwake()
	{
		if (this.m_nview.GetZDO() == null)
		{
			this.m_animator.SetBool("wakeup", false);
			return;
		}
		bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_wakeup, true);
		this.m_animator.SetBool("wakeup", @bool);
		if (@bool)
		{
			this.m_wakeupTimer = 0f;
		}
	}

	private void UpdateAwake(float dt)
	{
		if (this.m_wakeupTimer >= 0f)
		{
			this.m_wakeupTimer += dt;
			if (this.m_wakeupTimer > 1f)
			{
				this.m_wakeupTimer = -1f;
				this.m_animator.SetBool("wakeup", false);
				if (this.m_nview.IsOwner())
				{
					this.m_nview.GetZDO().Set(ZDOVars.s_wakeup, false);
				}
			}
		}
	}

	private void EdgeOfWorldKill(float dt)
	{
		if (this.IsDead())
		{
			return;
		}
		float num = Utils.DistanceXZ(Vector3.zero, base.transform.position);
		float num2 = 10420f;
		if (num > num2 && (base.IsSwimming() || base.transform.position.y < ZoneSystem.instance.m_waterLevel))
		{
			Vector3 vector = Vector3.Normalize(base.transform.position);
			float num3 = Utils.LerpStep(num2, 10500f, num) * 10f;
			this.m_body.MovePosition(this.m_body.position + vector * num3 * dt);
		}
		if (num > num2 && base.transform.position.y < ZoneSystem.instance.m_waterLevel - 40f)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 99999f;
			base.Damage(hitData);
		}
	}

	private void AutoPickup(float dt)
	{
		if (this.IsTeleporting())
		{
			return;
		}
		if (!this.m_enableAutoPickup)
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		foreach (Collider collider in Physics.OverlapSphere(vector, this.m_autoPickupRange, this.m_autoPickupMask))
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && component.m_autoPickup && !this.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
				{
					if (!component.CanPickup(true))
					{
						component.RequestOwn();
					}
					else if (!component.InTar())
					{
						component.Load();
						if (this.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight() + this.m_inventory.GetTotalWeight() <= this.GetMaxCarryWeight())
						{
							float num = Vector3.Distance(component.transform.position, vector);
							if (num <= this.m_autoPickupRange)
							{
								if (num < 0.3f)
								{
									base.Pickup(component.gameObject, true, true);
								}
								else
								{
									Vector3 vector2 = Vector3.Normalize(vector - component.transform.position);
									float num2 = 15f;
									component.transform.position = component.transform.position + vector2 * num2 * dt;
								}
							}
						}
					}
				}
			}
		}
	}

	private void PlayerAttackInput(float dt)
	{
		if (this.InPlaceMode())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		this.UpdateWeaponLoading(currentWeapon, dt);
		if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_bowDraw)
		{
			this.UpdateAttackBowDraw(currentWeapon, dt);
		}
		else
		{
			if (this.m_attack)
			{
				this.m_queuedAttackTimer = 0.5f;
				this.m_queuedSecondAttackTimer = 0f;
			}
			if (this.m_secondaryAttack)
			{
				this.m_queuedSecondAttackTimer = 0.5f;
				this.m_queuedAttackTimer = 0f;
			}
			this.m_queuedAttackTimer -= Time.fixedDeltaTime;
			this.m_queuedSecondAttackTimer -= Time.fixedDeltaTime;
			if ((this.m_queuedAttackTimer > 0f || this.m_attackHold) && this.StartAttack(null, false))
			{
				this.m_queuedAttackTimer = 0f;
			}
			if ((this.m_queuedSecondAttackTimer > 0f || this.m_secondaryAttackHold) && this.StartAttack(null, true))
			{
				this.m_queuedSecondAttackTimer = 0f;
			}
		}
		if (this.m_currentAttack != null && this.m_currentAttack.m_loopingAttack && !(this.m_currentAttackIsSecondary ? this.m_secondaryAttackHold : this.m_attackHold))
		{
			this.m_currentAttack.Abort();
		}
	}

	private void UpdateWeaponLoading(ItemDrop.ItemData weapon, float dt)
	{
		if (weapon == null || !weapon.m_shared.m_attack.m_requiresReload)
		{
			this.SetWeaponLoaded(null);
			return;
		}
		if (this.m_weaponLoaded == weapon)
		{
			return;
		}
		if (weapon.m_shared.m_attack.m_requiresReload && !this.IsReloadActionQueued())
		{
			this.QueueReloadAction();
		}
	}

	private void CancelReloadAction()
	{
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if (minorActionData.m_type == Player.MinorActionData.ActionType.Reload)
			{
				this.m_actionQueue.Remove(minorActionData);
				break;
			}
		}
	}

	public override void ResetLoadedWeapon()
	{
		this.SetWeaponLoaded(null);
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if (minorActionData.m_type == Player.MinorActionData.ActionType.Reload)
			{
				this.m_actionQueue.Remove(minorActionData);
				break;
			}
		}
	}

	private void SetWeaponLoaded(ItemDrop.ItemData weapon)
	{
		if (weapon == this.m_weaponLoaded)
		{
			return;
		}
		this.m_weaponLoaded = weapon;
		this.m_nview.GetZDO().Set(ZDOVars.s_weaponLoaded, weapon != null);
	}

	public override bool IsWeaponLoaded()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetBool(ZDOVars.s_weaponLoaded, false);
		}
		return this.m_weaponLoaded != null;
	}

	private void UpdateAttackBowDraw(ItemDrop.ItemData weapon, float dt)
	{
		if (this.m_blocking || this.InMinorAction() || this.IsAttached())
		{
			this.m_attackDrawTime = -1f;
			if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
			{
				this.m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, false);
			}
			return;
		}
		float num = weapon.GetDrawStaminaDrain();
		if ((double)base.GetAttackDrawPercentage() >= 1.0)
		{
			num *= 0.5f;
		}
		bool flag = num <= 0f || this.HaveStamina(0f);
		if (this.m_attackDrawTime < 0f)
		{
			if (!this.m_attackHold)
			{
				this.m_attackDrawTime = 0f;
				return;
			}
		}
		else
		{
			if (this.m_attackHold && flag && this.m_attackDrawTime >= 0f)
			{
				if (this.m_attackDrawTime == 0f)
				{
					if (!weapon.m_shared.m_attack.StartDraw(this, weapon))
					{
						this.m_attackDrawTime = -1f;
						return;
					}
					weapon.m_shared.m_holdStartEffect.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
				}
				this.m_attackDrawTime += Time.fixedDeltaTime;
				if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
				{
					this.m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, true);
				}
				this.UseStamina(num * dt);
				return;
			}
			if (this.m_attackDrawTime > 0f)
			{
				if (flag)
				{
					this.StartAttack(null, false);
				}
				if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
				{
					this.m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, false);
				}
				this.m_attackDrawTime = 0f;
			}
		}
	}

	protected override bool HaveQueuedChain()
	{
		return (this.m_queuedAttackTimer > 0f || this.m_attackHold) && base.GetCurrentWeapon() != null && this.m_currentAttack != null && this.m_currentAttack.CanStartChainAttack();
	}

	private void UpdateBaseValue(float dt)
	{
		this.m_baseValueUpdateTimer += dt;
		if (this.m_baseValueUpdateTimer > 2f)
		{
			this.m_baseValueUpdateTimer = 0f;
			this.m_baseValue = EffectArea.GetBaseValue(base.transform.position, 20f);
			this.m_nview.GetZDO().Set(ZDOVars.s_baseValue, this.m_baseValue, false);
			this.m_comfortLevel = SE_Rested.CalculateComfortLevel(this);
		}
	}

	public int GetComfortLevel()
	{
		if (this.m_nview == null)
		{
			return 0;
		}
		return this.m_comfortLevel;
	}

	public int GetBaseValue()
	{
		if (!this.m_nview.IsValid())
		{
			return 0;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_baseValue;
		}
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_baseValue, 0);
	}

	public bool IsSafeInHome()
	{
		return this.m_safeInHome;
	}

	private void UpdateBiome(float dt)
	{
		if (this.InIntro())
		{
			return;
		}
		this.m_biomeTimer += dt;
		if (this.m_biomeTimer > 1f)
		{
			this.m_biomeTimer = 0f;
			Heightmap.Biome biome = Heightmap.FindBiome(base.transform.position);
			if (this.m_currentBiome != biome)
			{
				this.m_currentBiome = biome;
				this.AddKnownBiome(biome);
			}
		}
	}

	public Heightmap.Biome GetCurrentBiome()
	{
		return this.m_currentBiome;
	}

	public override void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		if (skill == Skills.SkillType.None)
		{
			return;
		}
		float num = 1f;
		this.m_seman.ModifyRaiseSkill(skill, ref num);
		value *= num;
		this.m_skills.RaiseSkill(skill, value);
	}

	private void UpdateStats(float dt)
	{
		if (this.InIntro() || this.IsTeleporting())
		{
			return;
		}
		this.m_timeSinceDeath += dt;
		this.UpdateMovementModifier();
		this.UpdateFood(dt, false);
		bool flag = this.IsEncumbered();
		float maxStamina = this.GetMaxStamina();
		float num = 1f;
		if (this.IsBlocking())
		{
			num *= 0.8f;
		}
		if ((base.IsSwimming() && !base.IsOnGround()) || this.InAttack() || this.InDodge() || this.m_wallRunning || flag)
		{
			num = 0f;
		}
		float num2 = (this.m_staminaRegen + (1f - this.m_stamina / maxStamina) * this.m_staminaRegen * this.m_staminaRegenTimeMultiplier) * num;
		float num3 = 1f;
		this.m_seman.ModifyStaminaRegen(ref num3);
		num2 *= num3;
		this.m_staminaRegenTimer -= dt;
		if (this.m_stamina < maxStamina && this.m_staminaRegenTimer <= 0f)
		{
			this.m_stamina = Mathf.Min(maxStamina, this.m_stamina + num2 * dt);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_stamina, this.m_stamina);
		float maxEitr = this.GetMaxEitr();
		float num4 = 1f;
		if (this.IsBlocking())
		{
			num4 *= 0.8f;
		}
		if (this.InAttack() || this.InDodge())
		{
			num4 = 0f;
		}
		float num5 = (this.m_eiterRegen + (1f - this.m_eitr / maxEitr) * this.m_eiterRegen) * num4;
		float num6 = 1f;
		this.m_seman.ModifyEitrRegen(ref num6);
		num6 += this.GetEquipmentEitrRegenModifier();
		num5 *= num6;
		this.m_eitrRegenTimer -= dt;
		if (this.m_eitr < maxEitr && this.m_eitrRegenTimer <= 0f)
		{
			this.m_eitr = Mathf.Min(maxEitr, this.m_eitr + num5 * dt);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_eitr, this.m_eitr);
		if (flag)
		{
			if (this.m_moveDir.magnitude > 0.1f)
			{
				this.UseStamina(this.m_encumberedStaminaDrain * dt);
			}
			this.m_seman.AddStatusEffect(Player.s_statusEffectEncumbered, false, 0, 0f);
			this.ShowTutorial("encumbered", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectEncumbered, false);
		}
		if (!this.HardDeath())
		{
			this.m_seman.AddStatusEffect(Player.s_statusEffectSoftDeath, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectSoftDeath, false);
		}
		this.UpdateEnvStatusEffects(dt);
	}

	public float GetEquipmentEitrRegenModifier()
	{
		float num = 0f;
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_leftItem != null)
		{
			num += this.m_leftItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_rightItem != null)
		{
			num += this.m_rightItem.m_shared.m_eitrRegenModifier;
		}
		if (this.m_utilityItem != null)
		{
			num += this.m_utilityItem.m_shared.m_eitrRegenModifier;
		}
		return num;
	}

	private void UpdateEnvStatusEffects(float dt)
	{
		this.m_nearFireTimer += dt;
		HitData.DamageModifiers damageModifiers = base.GetDamageModifiers(null);
		bool flag = this.m_nearFireTimer < 0.25f;
		bool flag2 = this.m_seman.HaveStatusEffect("Burning");
		bool flag3 = this.InShelter();
		HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
		bool flag4 = EnvMan.instance.IsFreezing();
		bool flag5 = EnvMan.instance.IsCold();
		bool flag6 = EnvMan.instance.IsWet();
		bool flag7 = this.IsSensed();
		bool flag8 = this.m_seman.HaveStatusEffect("Wet");
		bool flag9 = this.IsSitting();
		bool flag10 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.WarmCozyArea, 1f);
		bool flag11 = flag4 && !flag && !flag3;
		bool flag12 = (flag5 && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
		if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant || flag10)
		{
			flag11 = false;
			flag12 = false;
		}
		if (flag6 && !this.m_underRoof)
		{
			this.m_seman.AddStatusEffect(Player.s_statusEffectWet, true, 0, 0f);
		}
		if (flag3)
		{
			this.m_seman.AddStatusEffect(Player.s_statusEffectShelter, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectShelter, false);
		}
		if (flag)
		{
			this.m_seman.AddStatusEffect(Player.s_statusEffectCampFire, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectCampFire, false);
		}
		bool flag13 = !flag7 && (flag9 || flag3) && !flag12 && !flag11 && (!flag8 || flag10) && !flag2 && flag;
		if (flag13)
		{
			this.m_seman.AddStatusEffect(Player.s_statusEffectResting, false, 0, 0f);
		}
		else
		{
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectResting, false);
		}
		this.m_safeInHome = flag13 && flag3 && (float)this.GetBaseValue() >= 1f;
		if (flag11)
		{
			if (!this.m_seman.RemoveStatusEffect(Player.s_statusEffectCold, true))
			{
				this.m_seman.AddStatusEffect(Player.s_statusEffectFreezing, false, 0, 0f);
				return;
			}
		}
		else if (flag12)
		{
			if (!this.m_seman.RemoveStatusEffect(Player.s_statusEffectFreezing, true) && this.m_seman.AddStatusEffect(Player.s_statusEffectCold, false, 0, 0f))
			{
				this.ShowTutorial("cold", false);
				return;
			}
		}
		else
		{
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectCold, false);
			this.m_seman.RemoveStatusEffect(Player.s_statusEffectFreezing, false);
		}
	}

	private bool CanEat(ItemDrop.ItemData item, bool showMessages)
	{
		foreach (Player.Food food in this.m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					return true;
				}
				this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", new string[] { item.m_shared.m_name }), 0, null);
				return false;
			}
		}
		using (List<Player.Food>.Enumerator enumerator = this.m_foods.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.CanEatAgain())
				{
					return true;
				}
			}
		}
		if (this.m_foods.Count >= 3)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_isfull", 0, null);
			return false;
		}
		return true;
	}

	private Player.Food GetMostDepletedFood()
	{
		Player.Food food = null;
		foreach (Player.Food food2 in this.m_foods)
		{
			if (food2.CanEatAgain() && (food == null || food2.m_time < food.m_time))
			{
				food = food2;
			}
		}
		return food;
	}

	public void ClearFood()
	{
		this.m_foods.Clear();
	}

	public bool RemoveOneFood()
	{
		if (this.m_foods.Count == 0)
		{
			return false;
		}
		this.m_foods.RemoveAt(UnityEngine.Random.Range(0, this.m_foods.Count));
		return true;
	}

	private bool EatFood(ItemDrop.ItemData item)
	{
		if (!this.CanEat(item, false))
		{
			return false;
		}
		string text = "";
		if (item.m_shared.m_food > 0f)
		{
			text = text + " +" + item.m_shared.m_food.ToString() + " $item_food_health ";
		}
		if (item.m_shared.m_foodStamina > 0f)
		{
			text = text + " +" + item.m_shared.m_foodStamina.ToString() + " $item_food_stamina ";
		}
		if (item.m_shared.m_foodEitr > 0f)
		{
			text = text + " +" + item.m_shared.m_foodEitr.ToString() + " $item_food_eitr ";
		}
		this.Message(MessageHud.MessageType.Center, text, 0, null);
		foreach (Player.Food food in this.m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					food.m_time = item.m_shared.m_foodBurnTime;
					food.m_health = item.m_shared.m_food;
					food.m_stamina = item.m_shared.m_foodStamina;
					food.m_eitr = item.m_shared.m_foodEitr;
					this.UpdateFood(0f, true);
					return true;
				}
				return false;
			}
		}
		if (this.m_foods.Count < 3)
		{
			Player.Food food2 = new Player.Food();
			food2.m_name = item.m_dropPrefab.name;
			food2.m_item = item;
			food2.m_time = item.m_shared.m_foodBurnTime;
			food2.m_health = item.m_shared.m_food;
			food2.m_stamina = item.m_shared.m_foodStamina;
			food2.m_eitr = item.m_shared.m_foodEitr;
			this.m_foods.Add(food2);
			this.UpdateFood(0f, true);
			return true;
		}
		Player.Food mostDepletedFood = this.GetMostDepletedFood();
		if (mostDepletedFood != null)
		{
			mostDepletedFood.m_name = item.m_dropPrefab.name;
			mostDepletedFood.m_item = item;
			mostDepletedFood.m_time = item.m_shared.m_foodBurnTime;
			mostDepletedFood.m_health = item.m_shared.m_food;
			mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
			this.UpdateFood(0f, true);
			return true;
		}
		return false;
	}

	private void UpdateFood(float dt, bool forceUpdate)
	{
		this.m_foodUpdateTimer += dt;
		if (this.m_foodUpdateTimer >= 1f || forceUpdate)
		{
			this.m_foodUpdateTimer -= 1f;
			foreach (Player.Food food in this.m_foods)
			{
				food.m_time -= 1f;
				float num = Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime);
				num = Mathf.Pow(num, 0.3f);
				food.m_health = food.m_item.m_shared.m_food * num;
				food.m_stamina = food.m_item.m_shared.m_foodStamina * num;
				food.m_eitr = food.m_item.m_shared.m_foodEitr * num;
				if (food.m_time <= 0f)
				{
					this.Message(MessageHud.MessageType.Center, "$msg_food_done", 0, null);
					this.m_foods.Remove(food);
					break;
				}
			}
			float num2;
			float num3;
			float num4;
			this.GetTotalFoodValue(out num2, out num3, out num4);
			this.SetMaxHealth(num2, true);
			this.SetMaxStamina(num3, true);
			this.SetMaxEitr(num4, true);
			if (num4 > 0f)
			{
				this.ShowTutorial("eitr", false);
			}
		}
		if (!forceUpdate)
		{
			this.m_foodRegenTimer += dt;
			if (this.m_foodRegenTimer >= 10f)
			{
				this.m_foodRegenTimer = 0f;
				float num5 = 0f;
				foreach (Player.Food food2 in this.m_foods)
				{
					num5 += food2.m_item.m_shared.m_foodRegen;
				}
				if (num5 > 0f)
				{
					float num6 = 1f;
					this.m_seman.ModifyHealthRegen(ref num6);
					num5 *= num6;
					base.Heal(num5, true);
				}
			}
		}
	}

	private void GetTotalFoodValue(out float hp, out float stamina, out float eitr)
	{
		hp = this.m_baseHP;
		stamina = this.m_baseStamina;
		eitr = 0f;
		foreach (Player.Food food in this.m_foods)
		{
			hp += food.m_health;
			stamina += food.m_stamina;
			eitr += food.m_eitr;
		}
	}

	public float GetBaseFoodHP()
	{
		return this.m_baseHP;
	}

	public List<Player.Food> GetFoods()
	{
		return this.m_foods;
	}

	public void OnSpawned()
	{
		this.m_spawnEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (this.m_firstSpawn)
		{
			if (this.m_valkyrie != null)
			{
				UnityEngine.Object.Instantiate<GameObject>(this.m_valkyrie, base.transform.position, Quaternion.identity);
			}
			this.m_firstSpawn = false;
		}
	}

	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!base.CheckRun(moveDir, dt))
		{
			return false;
		}
		bool flag = this.HaveStamina(0f);
		float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		float num = Mathf.Lerp(1f, 0.5f, skillFactor);
		float num2 = this.m_runStaminaDrain * num;
		this.m_seman.ModifyRunStaminaDrain(num2, ref num2);
		this.UseStamina(dt * num2);
		if (this.HaveStamina(0f))
		{
			this.m_runSkillImproveTimer += dt;
			if (this.m_runSkillImproveTimer > 1f)
			{
				this.m_runSkillImproveTimer = 0f;
				this.RaiseSkill(Skills.SkillType.Run, 1f);
			}
			this.ClearActionQueue();
			return true;
		}
		if (flag)
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		return false;
	}

	private void UpdateMovementModifier()
	{
		this.m_equipmentMovementModifier = 0f;
		if (this.m_rightItem != null)
		{
			this.m_equipmentMovementModifier += this.m_rightItem.m_shared.m_movementModifier;
		}
		if (this.m_leftItem != null)
		{
			this.m_equipmentMovementModifier += this.m_leftItem.m_shared.m_movementModifier;
		}
		if (this.m_chestItem != null)
		{
			this.m_equipmentMovementModifier += this.m_chestItem.m_shared.m_movementModifier;
		}
		if (this.m_legItem != null)
		{
			this.m_equipmentMovementModifier += this.m_legItem.m_shared.m_movementModifier;
		}
		if (this.m_helmetItem != null)
		{
			this.m_equipmentMovementModifier += this.m_helmetItem.m_shared.m_movementModifier;
		}
		if (this.m_shoulderItem != null)
		{
			this.m_equipmentMovementModifier += this.m_shoulderItem.m_shared.m_movementModifier;
		}
		if (this.m_utilityItem != null)
		{
			this.m_equipmentMovementModifier += this.m_utilityItem.m_shared.m_movementModifier;
		}
	}

	public void OnSkillLevelup(Skills.SkillType skill, float level)
	{
		this.m_skillLevelupEffects.Create(this.m_head.position, this.m_head.rotation, this.m_head, 1f, -1);
	}

	protected override void OnJump()
	{
		this.ClearActionQueue();
		float num = this.m_jumpStaminaUsage - this.m_jumpStaminaUsage * this.m_equipmentMovementModifier;
		this.m_seman.ModifyJumpStaminaUsage(num, ref num);
		this.UseStamina(num);
	}

	protected override void OnSwimming(Vector3 targetVel, float dt)
	{
		base.OnSwimming(targetVel, dt);
		if (targetVel.magnitude > 0.1f)
		{
			float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Swim);
			float num = Mathf.Lerp(this.m_swimStaminaDrainMinSkill, this.m_swimStaminaDrainMaxSkill, skillFactor);
			this.UseStamina(dt * num);
			this.m_swimSkillImproveTimer += dt;
			if (this.m_swimSkillImproveTimer > 1f)
			{
				this.m_swimSkillImproveTimer = 0f;
				this.RaiseSkill(Skills.SkillType.Swim, 1f);
			}
		}
		if (!this.HaveStamina(0f))
		{
			this.m_drownDamageTimer += dt;
			if (this.m_drownDamageTimer > 1f)
			{
				this.m_drownDamageTimer = 0f;
				float num2 = Mathf.Ceil(base.GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = num2;
				hitData.m_point = base.GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				base.Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = base.GetLiquidLevel();
				this.m_drownEffects.Create(position, base.transform.rotation, null, 1f, -1);
			}
		}
	}

	protected override bool TakeInput()
	{
		bool flag = (!Chat.instance || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible() && (!StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen()) && !GameCamera.InFreeFly();
		if (this.IsDead() || this.InCutscene() || this.IsTeleporting())
		{
			flag = false;
		}
		return flag;
	}

	public void UseHotbarItem(int index)
	{
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(index - 1, 0);
		if (itemAt != null)
		{
			base.UseItem(null, itemAt, false);
		}
	}

	public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
	{
		CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
		if (requiredStation != null)
		{
			if (this.m_currentStation == null)
			{
				return false;
			}
			if (requiredStation.m_name != this.m_currentStation.m_name)
			{
				return false;
			}
			if (checkLevel)
			{
				int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
				if (this.m_currentStation.GetLevel() < requiredStationLevel)
				{
					return false;
				}
			}
		}
		else if (this.m_currentStation != null && !this.m_currentStation.m_showBasicRecipies)
		{
			return false;
		}
		return true;
	}

	public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel)
	{
		if (discover)
		{
			if (recipe.m_craftingStation && !this.KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
			{
				return false;
			}
		}
		else if (!this.RequiredCraftingStation(recipe, qualityLevel, true))
		{
			return false;
		}
		return (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && this.HaveRequirementItems(recipe, discover, qualityLevel);
	}

	private bool HaveRequirementItems(Recipe piece, bool discover, int qualityLevel)
	{
		foreach (Piece.Requirement requirement in piece.m_resources)
		{
			if (requirement.m_resItem)
			{
				if (discover)
				{
					if (requirement.m_amount > 0)
					{
						if (piece.m_requireOnlyOneIngredient)
						{
							if (this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
							{
								return true;
							}
						}
						else if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
						{
							return false;
						}
					}
				}
				else
				{
					int amount = requirement.GetAmount(qualityLevel);
					int num = this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, -1);
					if (piece.m_requireOnlyOneIngredient)
					{
						if (num >= amount)
						{
							return true;
						}
					}
					else if (num < amount)
					{
						return false;
					}
				}
			}
		}
		return !piece.m_requireOnlyOneIngredient;
	}

	public ItemDrop.ItemData GetFirstRequiredItem(Inventory inventory, Recipe recipe, int qualityLevel, out int amount, out int extraAmount)
	{
		foreach (Piece.Requirement requirement in recipe.m_resources)
		{
			if (requirement.m_resItem)
			{
				int amount2 = requirement.GetAmount(qualityLevel);
				for (int j = 0; j <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; j++)
				{
					if (this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, j) >= amount2)
					{
						amount = amount2;
						extraAmount = requirement.m_extraAmountOnlyOneIngredient;
						return inventory.GetItem(requirement.m_resItem.m_itemData.m_shared.m_name, j, false);
					}
				}
			}
		}
		amount = 0;
		extraAmount = 0;
		return null;
	}

	public bool HaveRequirements(Piece piece, Player.RequirementMode mode)
	{
		if (piece.m_craftingStation)
		{
			if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
			{
				if (!this.m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
				{
					return false;
				}
			}
			else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
			{
				return false;
			}
		}
		if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
		{
			return false;
		}
		foreach (Piece.Requirement requirement in piece.m_resources)
		{
			if (requirement.m_resItem && requirement.m_amount > 0)
			{
				if (mode == Player.RequirementMode.IsKnown)
				{
					if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return false;
					}
				}
				else if (mode == Player.RequirementMode.CanAlmostBuild)
				{
					if (!this.m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return false;
					}
				}
				else if (mode == Player.RequirementMode.CanBuild && this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, -1) < requirement.m_amount)
				{
					return false;
				}
			}
		}
		return true;
	}

	public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel, int itemQuality = -1)
	{
		foreach (Piece.Requirement requirement in requirements)
		{
			if (requirement.m_resItem)
			{
				int amount = requirement.GetAmount(qualityLevel);
				if (amount > 0)
				{
					this.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
				}
			}
		}
	}

	private void UpdateHover()
	{
		if (this.InPlaceMode() || this.IsDead() || this.m_doodadController != null)
		{
			this.m_hovering = null;
			this.m_hoveringCreature = null;
			return;
		}
		this.FindHoverObject(out this.m_hovering, out this.m_hoveringCreature);
	}

	private bool CheckCanRemovePiece(Piece piece)
	{
		if (!this.m_noPlacementCost && piece.m_craftingStation != null && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_missingstation", 0, null);
			return false;
		}
		return true;
	}

	private bool RemovePiece()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(raycastHit.point, this.m_eye.position) < this.m_maxPlaceDistance)
		{
			Piece piece = raycastHit.collider.GetComponentInParent<Piece>();
			if (piece == null && raycastHit.collider.GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(raycastHit.point, 2.5f);
			}
			if (piece)
			{
				if (!piece.m_canBeRemoved)
				{
					return false;
				}
				if (Location.IsInsideNoBuildLocation(piece.transform.position))
				{
					this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
					return false;
				}
				if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true, false))
				{
					this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
					return false;
				}
				if (!this.CheckCanRemovePiece(piece))
				{
					return false;
				}
				ZNetView component = piece.GetComponent<ZNetView>();
				if (component == null)
				{
					return false;
				}
				if (!piece.CanBeRemoved())
				{
					this.Message(MessageHud.MessageType.Center, "$msg_cantremovenow", 0, null);
					return false;
				}
				WearNTear component2 = piece.GetComponent<WearNTear>();
				if (component2)
				{
					component2.Remove();
				}
				else
				{
					ZLog.Log("Removing non WNT object with hammer " + piece.name);
					component.ClaimOwnership();
					piece.DropResources();
					piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform, 1f, -1);
					this.m_removeEffects.Create(piece.transform.position, Quaternion.identity, null, 1f, -1);
					ZNetScene.instance.Destroy(piece.gameObject);
				}
				ItemDrop.ItemData rightItem = base.GetRightItem();
				if (rightItem != null)
				{
					this.FaceLookDirection();
					this.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
				}
				return true;
			}
		}
		return false;
	}

	public void FaceLookDirection()
	{
		base.transform.rotation = base.GetLookYaw();
	}

	private bool PlacePiece(Piece piece)
	{
		this.UpdatePlacementGhost(true);
		Vector3 position = this.m_placementGhost.transform.position;
		Quaternion rotation = this.m_placementGhost.transform.rotation;
		GameObject gameObject = piece.gameObject;
		switch (this.m_placementStatus)
		{
		case Player.PlacementStatus.Invalid:
			this.Message(MessageHud.MessageType.Center, "$msg_invalidplacement", 0, null);
			return false;
		case Player.PlacementStatus.BlockedbyPlayer:
			this.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
			return false;
		case Player.PlacementStatus.NoBuildZone:
			this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
			return false;
		case Player.PlacementStatus.PrivateZone:
			this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
			return false;
		case Player.PlacementStatus.MoreSpace:
			this.Message(MessageHud.MessageType.Center, "$msg_needspace", 0, null);
			return false;
		case Player.PlacementStatus.NoTeleportArea:
			this.Message(MessageHud.MessageType.Center, "$msg_noteleportarea", 0, null);
			return false;
		case Player.PlacementStatus.ExtensionMissingStation:
			this.Message(MessageHud.MessageType.Center, "$msg_extensionmissingstation", 0, null);
			return false;
		case Player.PlacementStatus.WrongBiome:
			this.Message(MessageHud.MessageType.Center, "$msg_wrongbiome", 0, null);
			return false;
		case Player.PlacementStatus.NeedCultivated:
			this.Message(MessageHud.MessageType.Center, "$msg_needcultivated", 0, null);
			return false;
		case Player.PlacementStatus.NeedDirt:
			this.Message(MessageHud.MessageType.Center, "$msg_needdirt", 0, null);
			return false;
		case Player.PlacementStatus.NotInDungeon:
			this.Message(MessageHud.MessageType.Center, "$msg_notindungeon", 0, null);
			return false;
		default:
		{
			TerrainModifier.SetTriggerOnPlaced(true);
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, position, rotation);
			TerrainModifier.SetTriggerOnPlaced(false);
			CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
			if (componentInChildren)
			{
				this.AddKnownStation(componentInChildren);
			}
			Piece component = gameObject2.GetComponent<Piece>();
			if (component)
			{
				component.SetCreator(this.GetPlayerID());
			}
			PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
			if (component2)
			{
				component2.Setup(Game.instance.GetPlayerProfile().GetName());
			}
			WearNTear component3 = gameObject2.GetComponent<WearNTear>();
			if (component3)
			{
				component3.OnPlaced();
			}
			ItemDrop.ItemData rightItem = base.GetRightItem();
			if (rightItem != null)
			{
				this.FaceLookDirection();
				this.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
			}
			piece.m_placeEffect.Create(position, rotation, gameObject2.transform, 1f, -1);
			base.AddNoise(50f);
			Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
			ZLog.Log("Placed " + gameObject.name);
			Gogan.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
			return true;
		}
		}
	}

	public override bool IsPlayer()
	{
		return true;
	}

	public void GetBuildSelection(out Piece go, out Vector2Int id, out int total, out Piece.PieceCategory category, out bool useCategory)
	{
		category = this.m_buildPieces.m_selectedCategory;
		useCategory = this.m_buildPieces.m_useCategories;
		if (this.m_buildPieces.GetAvailablePiecesInSelectedCategory() == 0)
		{
			go = null;
			id = Vector2Int.zero;
			total = 0;
			return;
		}
		GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
		go = (selectedPrefab ? selectedPrefab.GetComponent<Piece>() : null);
		id = this.m_buildPieces.GetSelectedIndex();
		total = this.m_buildPieces.GetAvailablePiecesInSelectedCategory();
	}

	public List<Piece> GetBuildPieces()
	{
		if (!(this.m_buildPieces != null))
		{
			return null;
		}
		return this.m_buildPieces.GetPiecesInSelectedCategory();
	}

	public int GetAvailableBuildPiecesInCategory(Piece.PieceCategory cat)
	{
		if (!(this.m_buildPieces != null))
		{
			return 0;
		}
		return this.m_buildPieces.GetAvailablePiecesInCategory(cat);
	}

	private void RPC_OnDeath(long sender)
	{
		this.m_visual.SetActive(false);
	}

	private void CreateDeathEffects()
	{
		GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if (component)
			{
				Vector3 vector = this.m_body.velocity;
				if (this.m_pushForce.magnitude * 0.5f > vector.magnitude)
				{
					vector = this.m_pushForce * 0.5f;
				}
				component.Setup(vector, 0f, 0f, 0f, null);
				this.OnRagdollCreated(component);
				this.m_ragdoll = component;
			}
		}
	}

	public void UnequipDeathDropItems()
	{
		if (this.m_rightItem != null)
		{
			base.UnequipItem(this.m_rightItem, false);
		}
		if (this.m_leftItem != null)
		{
			base.UnequipItem(this.m_leftItem, false);
		}
		if (this.m_ammoItem != null)
		{
			base.UnequipItem(this.m_ammoItem, false);
		}
		if (this.m_utilityItem != null)
		{
			base.UnequipItem(this.m_utilityItem, false);
		}
	}

	public void CreateTombStone()
	{
		if (this.m_inventory.NrOfItems() == 0)
		{
			return;
		}
		base.UnequipAllItems();
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_tombstone, base.GetCenterPoint(), base.transform.rotation);
		gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(this.m_inventory);
		TombStone component = gameObject.GetComponent<TombStone>();
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
	}

	private bool HardDeath()
	{
		return this.m_timeSinceDeath > this.m_hardDeathCooldown;
	}

	public void ClearHardDeath()
	{
		this.m_timeSinceDeath = this.m_hardDeathCooldown + 1f;
	}

	protected override void OnDeath()
	{
		bool flag = this.HardDeath();
		this.m_nview.GetZDO().Set(ZDOVars.s_dead, true);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath", Array.Empty<object>());
		Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;
		Game.instance.GetPlayerProfile().SetDeathPoint(base.transform.position);
		this.CreateDeathEffects();
		this.CreateTombStone();
		this.m_foods.Clear();
		if (flag)
		{
			this.m_skills.OnDeath();
		}
		this.m_seman.RemoveAllStatusEffects(false);
		Game.instance.RequestRespawn(10f);
		this.m_timeSinceDeath = 0f;
		if (!flag)
		{
			this.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath", 0, null);
		}
		this.Message(MessageHud.MessageType.Center, "$msg_youdied", 0, null);
		this.ShowTutorial("death", false);
		Minimap.instance.AddPin(base.transform.position, Minimap.PinType.Death, string.Format("$hud_mapday {0}", EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())), true, false, 0L);
		if (this.m_onDeath != null)
		{
			this.m_onDeath();
		}
		string text = "biome:" + this.GetCurrentBiome().ToString();
		Gogan.LogEvent("Game", "Death", text, 0L);
	}

	public void OnRespawn()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_dead, false);
		base.SetHealth(base.GetMaxHealth());
	}

	private void SetupPlacementGhost()
	{
		if (this.m_placementGhost)
		{
			UnityEngine.Object.Destroy(this.m_placementGhost);
			this.m_placementGhost = null;
		}
		if (this.m_buildPieces == null)
		{
			return;
		}
		GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
		if (selectedPrefab == null)
		{
			return;
		}
		if (selectedPrefab.GetComponent<Piece>().m_repairPiece)
		{
			return;
		}
		bool flag = false;
		TerrainModifier componentInChildren = selectedPrefab.GetComponentInChildren<TerrainModifier>();
		if (componentInChildren)
		{
			flag = componentInChildren.enabled;
			componentInChildren.enabled = false;
		}
		TerrainOp.m_forceDisableTerrainOps = true;
		ZNetView.m_forceDisableInit = true;
		this.m_placementGhost = UnityEngine.Object.Instantiate<GameObject>(selectedPrefab);
		ZNetView.m_forceDisableInit = false;
		TerrainOp.m_forceDisableTerrainOps = false;
		this.m_placementGhost.name = selectedPrefab.name;
		if (componentInChildren)
		{
			componentInChildren.enabled = flag;
		}
		Joint[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Joint>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Rigidbody[] componentsInChildren2 = this.m_placementGhost.GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[i]);
		}
		ParticleSystemForceField[] componentsInChildren3 = this.m_placementGhost.GetComponentsInChildren<ParticleSystemForceField>();
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren3[i]);
		}
		Demister[] componentsInChildren4 = this.m_placementGhost.GetComponentsInChildren<Demister>();
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren4[i]);
		}
		foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
		{
			if (((1 << collider.gameObject.layer) & this.m_placeRayMask) == 0)
			{
				ZLog.Log("Disabling " + collider.gameObject.name + "  " + LayerMask.LayerToName(collider.gameObject.layer));
				collider.enabled = false;
			}
		}
		Transform[] componentsInChildren6 = this.m_placementGhost.GetComponentsInChildren<Transform>();
		int num = LayerMask.NameToLayer("ghost");
		Transform[] array = componentsInChildren6;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = num;
		}
		TerrainModifier[] componentsInChildren7 = this.m_placementGhost.GetComponentsInChildren<TerrainModifier>();
		for (int i = 0; i < componentsInChildren7.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren7[i]);
		}
		GuidePoint[] componentsInChildren8 = this.m_placementGhost.GetComponentsInChildren<GuidePoint>();
		for (int i = 0; i < componentsInChildren8.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren8[i]);
		}
		Light[] componentsInChildren9 = this.m_placementGhost.GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren9.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren9[i]);
		}
		AudioSource[] componentsInChildren10 = this.m_placementGhost.GetComponentsInChildren<AudioSource>();
		for (int i = 0; i < componentsInChildren10.Length; i++)
		{
			componentsInChildren10[i].enabled = false;
		}
		ZSFX[] componentsInChildren11 = this.m_placementGhost.GetComponentsInChildren<ZSFX>();
		for (int i = 0; i < componentsInChildren11.Length; i++)
		{
			componentsInChildren11[i].enabled = false;
		}
		WispSpawner componentInChildren2 = this.m_placementGhost.GetComponentInChildren<WispSpawner>();
		if (componentInChildren2)
		{
			UnityEngine.Object.Destroy(componentInChildren2);
		}
		Windmill componentInChildren3 = this.m_placementGhost.GetComponentInChildren<Windmill>();
		if (componentInChildren3)
		{
			componentInChildren3.enabled = false;
		}
		ParticleSystem[] componentsInChildren12 = this.m_placementGhost.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren12.Length; i++)
		{
			componentsInChildren12[i].gameObject.SetActive(false);
		}
		Transform transform = this.m_placementGhost.transform.Find("_GhostOnly");
		if (transform)
		{
			transform.gameObject.SetActive(true);
		}
		this.m_placementGhost.transform.position = base.transform.position;
		this.m_placementGhost.transform.localScale = selectedPrefab.transform.localScale;
		this.CleanupGhostMaterials<MeshRenderer>(this.m_placementGhost);
		this.CleanupGhostMaterials<SkinnedMeshRenderer>(this.m_placementGhost);
	}

	private void CleanupGhostMaterials<T>(GameObject ghost) where T : Renderer
	{
		foreach (T t in this.m_placementGhost.GetComponentsInChildren<T>())
		{
			if (!(t.sharedMaterial == null))
			{
				Material[] sharedMaterials = t.sharedMaterials;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					Material material = new Material(sharedMaterials[j]);
					material.SetFloat("_RippleDistance", 0f);
					material.SetFloat("_ValueNoise", 0f);
					material.SetFloat("_TriplanarLocalPos", 1f);
					sharedMaterials[j] = material;
				}
				t.sharedMaterials = sharedMaterials;
				t.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	private void SetPlacementGhostValid(bool valid)
	{
		this.m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(!valid);
	}

	protected override void SetPlaceMode(PieceTable buildPieces)
	{
		base.SetPlaceMode(buildPieces);
		this.m_buildPieces = buildPieces;
		this.UpdateAvailablePiecesList();
	}

	public void SetBuildCategory(int index)
	{
		if (this.m_buildPieces != null)
		{
			this.m_buildPieces.SetCategory(index);
			this.UpdateAvailablePiecesList();
		}
	}

	public override bool InPlaceMode()
	{
		return this.m_buildPieces != null;
	}

	private void Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
	{
		if (!this.InPlaceMode())
		{
			return;
		}
		Piece hoveringPiece = this.GetHoveringPiece();
		if (hoveringPiece)
		{
			if (!this.CheckCanRemovePiece(hoveringPiece))
			{
				return;
			}
			if (!PrivateArea.CheckAccess(hoveringPiece.transform.position, 0f, true, false))
			{
				return;
			}
			bool flag = false;
			WearNTear component = hoveringPiece.GetComponent<WearNTear>();
			if (component && component.Repair())
			{
				flag = true;
			}
			if (flag)
			{
				this.FaceLookDirection();
				this.m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
				hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation, null, 1f, -1);
				this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_repaired", new string[] { hoveringPiece.m_name }), 0, null);
				this.UseStamina(toolItem.m_shared.m_attack.m_attackStamina);
				this.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);
				if (toolItem.m_shared.m_useDurability)
				{
					toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
					return;
				}
			}
			else
			{
				this.Message(MessageHud.MessageType.TopLeft, hoveringPiece.m_name + " $msg_doesnotneedrepair", 0, null);
			}
		}
	}

	private void UpdateWearNTearHover()
	{
		if (!this.InPlaceMode())
		{
			this.m_hoveringPiece = null;
			return;
		}
		this.m_hoveringPiece = null;
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(this.m_eye.position, raycastHit.point) < this.m_maxPlaceDistance)
		{
			Piece componentInParent = raycastHit.collider.GetComponentInParent<Piece>();
			this.m_hoveringPiece = componentInParent;
			if (componentInParent)
			{
				WearNTear component = componentInParent.GetComponent<WearNTear>();
				if (component)
				{
					component.Highlight();
				}
			}
		}
	}

	public Piece GetHoveringPiece()
	{
		if (!this.InPlaceMode())
		{
			return null;
		}
		return this.m_hoveringPiece;
	}

	private void UpdatePlacementGhost(bool flashGuardStone)
	{
		if (this.m_placementGhost == null)
		{
			if (this.m_placementMarkerInstance)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			return;
		}
		bool flag = ((ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive()) ? this.m_altPlace : (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace")));
		Piece component = this.m_placementGhost.GetComponent<Piece>();
		bool flag2 = component.m_waterPiece || component.m_noInWater;
		Vector3 vector;
		Vector3 up;
		Piece piece;
		Heightmap heightmap;
		Collider collider;
		if (this.PieceRayTest(out vector, out up, out piece, out heightmap, out collider, flag2))
		{
			this.m_placementStatus = Player.PlacementStatus.Valid;
			Quaternion quaternion = Quaternion.Euler(0f, 22.5f * (float)this.m_placeRotation, 0f);
			if (this.m_placementMarkerInstance == null)
			{
				this.m_placementMarkerInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_placeMarker, vector, Quaternion.identity);
			}
			this.m_placementMarkerInstance.SetActive(true);
			this.m_placementMarkerInstance.transform.position = vector;
			this.m_placementMarkerInstance.transform.rotation = Quaternion.LookRotation(up, quaternion * Vector3.forward);
			if (component.m_groundOnly || component.m_groundPiece || component.m_cultivatedGroundOnly)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			WearNTear wearNTear = ((piece != null) ? piece.GetComponent<WearNTear>() : null);
			StationExtension component2 = component.GetComponent<StationExtension>();
			if (component2 != null)
			{
				CraftingStation craftingStation = component2.FindClosestStationInRange(vector);
				if (craftingStation)
				{
					component2.StartConnectionEffect(craftingStation, 1f);
				}
				else
				{
					component2.StopConnectionEffect();
					this.m_placementStatus = Player.PlacementStatus.ExtensionMissingStation;
				}
				if (component2.OtherExtensionInRange(component.m_spaceRequirement))
				{
					this.m_placementStatus = Player.PlacementStatus.MoreSpace;
				}
			}
			if (component.m_blockRadius > 0f && component.m_blockingPieces.Count > 0)
			{
				Collider[] array = Physics.OverlapSphere(vector, component.m_blockRadius, LayerMask.GetMask(new string[] { "piece" }));
				for (int i = 0; i < array.Length; i++)
				{
					Piece componentInParent = array[i].gameObject.GetComponentInParent<Piece>();
					if (componentInParent != null && componentInParent != component)
					{
						using (List<Piece>.Enumerator enumerator = component.m_blockingPieces.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								if (enumerator.Current.m_name == componentInParent.m_name)
								{
									this.m_placementStatus = Player.PlacementStatus.MoreSpace;
									break;
								}
							}
						}
					}
				}
			}
			if (component.m_mustConnectTo != null)
			{
				ZNetView znetView = null;
				Collider[] array = Physics.OverlapSphere(component.transform.position, component.m_connectRadius);
				for (int i = 0; i < array.Length; i++)
				{
					ZNetView componentInParent2 = array[i].GetComponentInParent<ZNetView>();
					if (componentInParent2 != null && componentInParent2 != this.m_nview && componentInParent2.name.Contains(component.m_mustConnectTo.name))
					{
						if (component.m_mustBeAboveConnected)
						{
							RaycastHit raycastHit;
							Physics.Raycast(component.transform.position, Vector3.down, out raycastHit);
							if (raycastHit.transform.GetComponentInParent<ZNetView>() != componentInParent2)
							{
								goto IL_30D;
							}
						}
						znetView = componentInParent2;
						break;
					}
					IL_30D:;
				}
				if (!znetView)
				{
					this.m_placementStatus = Player.PlacementStatus.Invalid;
				}
			}
			if (wearNTear && !wearNTear.m_supports)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_waterPiece && collider == null && !flag)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_noInWater && collider != null)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_groundPiece && heightmap == null)
			{
				this.m_placementGhost.SetActive(false);
				this.m_placementStatus = Player.PlacementStatus.Invalid;
				return;
			}
			if (component.m_groundOnly && heightmap == null)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_cultivatedGroundOnly && (heightmap == null || !heightmap.IsCultivated(vector)))
			{
				this.m_placementStatus = Player.PlacementStatus.NeedCultivated;
			}
			if (component.m_vegetationGroundOnly && (heightmap == null || heightmap.GetVegetationMask(vector) < 0.25f))
			{
				this.m_placementStatus = Player.PlacementStatus.NeedDirt;
			}
			if (component.m_notOnWood && piece && wearNTear && (wearNTear.m_materialType == WearNTear.MaterialType.Wood || wearNTear.m_materialType == WearNTear.MaterialType.HardWood))
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_notOnTiltingSurface && up.y < 0.8f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_inCeilingOnly && up.y > -0.5f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_notOnFloor && up.y > 0.1f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(vector, EffectArea.Type.Teleport, 0f))
			{
				this.m_placementStatus = Player.PlacementStatus.NoTeleportArea;
			}
			if (!component.m_allowedInDungeons && base.InInterior() && !EnvMan.instance.CheckInteriorBuildingOverride())
			{
				this.m_placementStatus = Player.PlacementStatus.NotInDungeon;
			}
			if (heightmap)
			{
				up = Vector3.up;
			}
			this.m_placementGhost.SetActive(true);
			if (((component.m_groundPiece || component.m_clipGround) && heightmap) || component.m_clipEverything)
			{
				GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
				TerrainModifier component3 = selectedPrefab.GetComponent<TerrainModifier>();
				TerrainOp component4 = selectedPrefab.GetComponent<TerrainOp>();
				if ((component3 || component4) && component.m_allowAltGroundPlacement && ((ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive()) ? (component.m_groundPiece && !this.m_altPlace) : (component.m_groundPiece && !ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltPlace"))))
				{
					float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
					vector.y = groundHeight;
				}
				this.m_placementGhost.transform.position = vector;
				this.m_placementGhost.transform.rotation = quaternion;
			}
			else
			{
				Collider[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Collider>();
				if (componentsInChildren.Length != 0)
				{
					this.m_placementGhost.transform.position = vector + up * 50f;
					this.m_placementGhost.transform.rotation = quaternion;
					Vector3 vector2 = Vector3.zero;
					float num = 999999f;
					foreach (Collider collider2 in componentsInChildren)
					{
						if (!collider2.isTrigger && collider2.enabled)
						{
							MeshCollider meshCollider = collider2 as MeshCollider;
							if (!(meshCollider != null) || meshCollider.convex)
							{
								Vector3 vector3 = collider2.ClosestPoint(vector);
								float num2 = Vector3.Distance(vector3, vector);
								if (num2 < num)
								{
									vector2 = vector3;
									num = num2;
								}
							}
						}
					}
					Vector3 vector4 = this.m_placementGhost.transform.position - vector2;
					if (component.m_waterPiece)
					{
						vector4.y = 3f;
					}
					this.m_placementGhost.transform.position = vector + vector4;
					this.m_placementGhost.transform.rotation = quaternion;
				}
			}
			if (!flag)
			{
				this.m_tempPieces.Clear();
				Transform transform;
				Transform transform2;
				if (this.FindClosestSnapPoints(this.m_placementGhost.transform, 0.5f, out transform, out transform2, this.m_tempPieces))
				{
					Vector3 position = transform2.parent.position;
					Vector3 vector5 = transform2.position - (transform.position - this.m_placementGhost.transform.position);
					if (!this.IsOverlappingOtherPiece(vector5, this.m_placementGhost.transform.rotation, this.m_placementGhost.name, this.m_tempPieces, component.m_allowRotatedOverlap))
					{
						this.m_placementGhost.transform.position = vector5;
					}
				}
			}
			if (Location.IsInsideNoBuildLocation(this.m_placementGhost.transform.position))
			{
				this.m_placementStatus = Player.PlacementStatus.NoBuildZone;
			}
			PrivateArea component5 = component.GetComponent<PrivateArea>();
			float num3 = (component5 ? component5.m_radius : 0f);
			bool flag3 = component5 != null;
			if (!PrivateArea.CheckAccess(this.m_placementGhost.transform.position, num3, flashGuardStone, flag3))
			{
				this.m_placementStatus = Player.PlacementStatus.PrivateZone;
			}
			if (this.CheckPlacementGhostVSPlayers())
			{
				this.m_placementStatus = Player.PlacementStatus.BlockedbyPlayer;
			}
			if (component.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(this.m_placementGhost.transform.position) & component.m_onlyInBiome) == Heightmap.Biome.None)
			{
				this.m_placementStatus = Player.PlacementStatus.WrongBiome;
			}
			if (component.m_noClipping && this.TestGhostClipping(this.m_placementGhost, 0.2f))
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
		}
		else
		{
			if (this.m_placementMarkerInstance)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			this.m_placementGhost.SetActive(false);
			this.m_placementStatus = Player.PlacementStatus.Invalid;
		}
		this.SetPlacementGhostValid(this.m_placementStatus == Player.PlacementStatus.Valid);
	}

	private bool IsOverlappingOtherPiece(Vector3 p, Quaternion rotation, string pieceName, List<Piece> pieces, bool allowRotatedOverlap)
	{
		foreach (Piece piece in this.m_tempPieces)
		{
			if (Vector3.Distance(p, piece.transform.position) < 0.05f && (!allowRotatedOverlap || Quaternion.Angle(piece.transform.rotation, rotation) <= 10f) && piece.gameObject.name.CustomStartsWith(pieceName))
			{
				return true;
			}
		}
		return false;
	}

	private bool FindClosestSnapPoints(Transform ghost, float maxSnapDistance, out Transform a, out Transform b, List<Piece> pieces)
	{
		this.m_tempSnapPoints1.Clear();
		ghost.GetComponent<Piece>().GetSnapPoints(this.m_tempSnapPoints1);
		this.m_tempSnapPoints2.Clear();
		this.m_tempPieces.Clear();
		Piece.GetSnapPoints(ghost.transform.position, 10f, this.m_tempSnapPoints2, this.m_tempPieces);
		float num = 9999999f;
		a = null;
		b = null;
		foreach (Transform transform in this.m_tempSnapPoints1)
		{
			Transform transform2;
			float num2;
			if (this.FindClosestSnappoint(transform.position, this.m_tempSnapPoints2, maxSnapDistance, out transform2, out num2) && num2 < num)
			{
				num = num2;
				a = transform;
				b = transform2;
			}
		}
		return a != null;
	}

	private bool FindClosestSnappoint(Vector3 p, List<Transform> snapPoints, float maxDistance, out Transform closest, out float distance)
	{
		closest = null;
		distance = 999999f;
		foreach (Transform transform in snapPoints)
		{
			float num = Vector3.Distance(transform.position, p);
			if (num <= maxDistance && num < distance)
			{
				closest = transform;
				distance = num;
			}
		}
		return closest != null;
	}

	private bool TestGhostClipping(GameObject ghost, float maxPenetration)
	{
		Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
		Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, this.m_placeRayMask);
		foreach (Collider collider in componentsInChildren)
		{
			foreach (Collider collider2 in array)
			{
				Vector3 vector;
				float num;
				if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num) && num > maxPenetration)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CheckPlacementGhostVSPlayers()
	{
		if (this.m_placementGhost == null)
		{
			return false;
		}
		List<Character> list = new List<Character>();
		Character.GetCharactersInRange(base.transform.position, 30f, list);
		foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
		{
			if (!collider.isTrigger && collider.enabled)
			{
				MeshCollider meshCollider = collider as MeshCollider;
				if (!(meshCollider != null) || meshCollider.convex)
				{
					foreach (Character character in list)
					{
						CapsuleCollider collider2 = character.GetCollider();
						Vector3 vector;
						float num;
						if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private bool PieceRayTest(out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
	{
		int num = this.m_placeRayMask;
		if (water)
		{
			num = this.m_placeWaterRayMask;
		}
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, num) && raycastHit.collider && !raycastHit.collider.attachedRigidbody && Vector3.Distance(this.m_eye.position, raycastHit.point) < this.m_maxPlaceDistance)
		{
			point = raycastHit.point;
			normal = raycastHit.normal;
			piece = raycastHit.collider.GetComponentInParent<Piece>();
			heightmap = raycastHit.collider.GetComponent<Heightmap>();
			if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
			{
				waterSurface = raycastHit.collider;
			}
			else
			{
				waterSurface = null;
			}
			return true;
		}
		point = Vector3.zero;
		normal = Vector3.zero;
		piece = null;
		heightmap = null;
		waterSurface = null;
		return false;
	}

	private void FindHoverObject(out GameObject hover, out Character hoverCreature)
	{
		hover = null;
		hoverCreature = null;
		RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, this.m_interactMask);
		Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
		RaycastHit[] array2 = array;
		int i = 0;
		while (i < array2.Length)
		{
			RaycastHit raycastHit = array2[i];
			if (!raycastHit.collider.attachedRigidbody || !(raycastHit.collider.attachedRigidbody.gameObject == base.gameObject))
			{
				if (hoverCreature == null)
				{
					Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
					if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()) && !ParticleMist.IsMistBlocked(base.GetCenterPoint(), character.GetCenterPoint()))
					{
						hoverCreature = character;
					}
				}
				if (Vector3.Distance(this.m_eye.position, raycastHit.point) >= this.m_maxInteractDistance)
				{
					break;
				}
				if (raycastHit.collider.GetComponent<Hoverable>() != null)
				{
					hover = raycastHit.collider.gameObject;
					return;
				}
				if (raycastHit.collider.attachedRigidbody)
				{
					hover = raycastHit.collider.attachedRigidbody.gameObject;
					return;
				}
				hover = raycastHit.collider.gameObject;
				return;
			}
			else
			{
				i++;
			}
		}
	}

	private void Interact(GameObject go, bool hold, bool alt)
	{
		if (this.InAttack() || this.InDodge())
		{
			return;
		}
		if (hold && Time.time - this.m_lastHoverInteractTime < 0.2f)
		{
			return;
		}
		Interactable componentInParent = go.GetComponentInParent<Interactable>();
		if (componentInParent != null)
		{
			this.m_lastHoverInteractTime = Time.time;
			if (componentInParent.Interact(this, hold, alt))
			{
				base.DoInteractAnimation(go.transform.position);
			}
		}
	}

	private void UpdateStations(float dt)
	{
		this.m_stationDiscoverTimer += dt;
		if (this.m_stationDiscoverTimer > 1f)
		{
			this.m_stationDiscoverTimer = 0f;
			CraftingStation.UpdateKnownStationsInRange(this);
		}
		if (!(this.m_currentStation != null))
		{
			if (this.m_inCraftingStation)
			{
				this.m_zanim.SetInt("crafting", 0);
				this.m_inCraftingStation = false;
				if (InventoryGui.IsVisible())
				{
					InventoryGui.instance.Hide();
				}
			}
			return;
		}
		if (!this.m_currentStation.InUseDistance(this))
		{
			InventoryGui.instance.Hide();
			this.SetCraftingStation(null);
			return;
		}
		if (!InventoryGui.IsVisible())
		{
			this.SetCraftingStation(null);
			return;
		}
		this.m_currentStation.PokeInUse();
		if (!this.AlwaysRotateCamera())
		{
			Vector3 normalized = (this.m_currentStation.transform.position - base.transform.position).normalized;
			normalized.y = 0f;
			normalized.Normalize();
			Quaternion quaternion = Quaternion.LookRotation(normalized);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, this.m_turnSpeed * dt);
		}
		this.m_zanim.SetInt("crafting", this.m_currentStation.m_useAnimation);
		this.m_inCraftingStation = true;
	}

	public void SetCraftingStation(CraftingStation station)
	{
		if (this.m_currentStation == station)
		{
			return;
		}
		if (station)
		{
			this.AddKnownStation(station);
			station.PokeInUse();
			base.HideHandItems();
		}
		this.m_currentStation = station;
	}

	public CraftingStation GetCurrentCraftingStation()
	{
		return this.m_currentStation;
	}

	private void UpdateCover(float dt)
	{
		this.m_updateCoverTimer += dt;
		if (this.m_updateCoverTimer > 1f)
		{
			this.m_updateCoverTimer = 0f;
			Cover.GetCoverForPoint(base.GetCenterPoint(), out this.m_coverPercentage, out this.m_underRoof, 0.5f);
		}
	}

	public Character GetHoverCreature()
	{
		return this.m_hoveringCreature;
	}

	public override GameObject GetHoverObject()
	{
		return this.m_hovering;
	}

	public override void OnNearFire(Vector3 point)
	{
		this.m_nearFireTimer = 0f;
	}

	public bool InShelter()
	{
		return this.m_coverPercentage >= 0.8f && this.m_underRoof;
	}

	public float GetStamina()
	{
		return this.m_stamina;
	}

	public override float GetMaxStamina()
	{
		return this.m_maxStamina;
	}

	public float GetEitr()
	{
		return this.m_eitr;
	}

	public override float GetMaxEitr()
	{
		return this.m_maxEitr;
	}

	public override float GetEitrPercentage()
	{
		return this.m_eitr / this.m_maxEitr;
	}

	public override float GetStaminaPercentage()
	{
		return this.m_stamina / this.m_maxStamina;
	}

	public void SetGodMode(bool godMode)
	{
		this.m_godMode = godMode;
	}

	public override bool InGodMode()
	{
		return this.m_godMode;
	}

	public void SetGhostMode(bool ghostmode)
	{
		this.m_ghostMode = ghostmode;
	}

	public override bool InGhostMode()
	{
		return this.m_ghostMode;
	}

	public override bool IsDebugFlying()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_debugFly;
		}
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_debugFly, false);
	}

	public override void AddEitr(float v)
	{
		this.m_eitr += v;
		if (this.m_eitr > this.m_maxEitr)
		{
			this.m_eitr = this.m_maxEitr;
		}
	}

	public override void AddStamina(float v)
	{
		this.m_stamina += v;
		if (this.m_stamina > this.m_maxStamina)
		{
			this.m_stamina = this.m_maxStamina;
		}
	}

	public override void UseEitr(float v)
	{
		if (v == 0f)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_UseEitr(0L, v);
			return;
		}
		this.m_nview.InvokeRPC("UseEitr", new object[] { v });
	}

	private void RPC_UseEitr(long sender, float v)
	{
		if (v == 0f)
		{
			return;
		}
		this.m_eitr -= v;
		if (this.m_eitr < 0f)
		{
			this.m_eitr = 0f;
		}
		this.m_eitrRegenTimer = this.m_eitrRegenDelay;
	}

	public override bool HaveEitr(float amount = 0f)
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetFloat(ZDOVars.s_eitr, this.m_maxEitr) > amount;
		}
		return this.m_eitr > amount;
	}

	public override void UseStamina(float v)
	{
		if (v == 0f)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_UseStamina(0L, v);
			return;
		}
		this.m_nview.InvokeRPC("UseStamina", new object[] { v });
	}

	private void RPC_UseStamina(long sender, float v)
	{
		if (v == 0f)
		{
			return;
		}
		this.m_stamina -= v;
		if (this.m_stamina < 0f)
		{
			this.m_stamina = 0f;
		}
		this.m_staminaRegenTimer = this.m_staminaRegenDelay;
	}

	public override bool HaveStamina(float amount = 0f)
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, this.m_maxStamina) > amount;
		}
		return this.m_stamina > amount;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(26);
		pkg.Write(base.GetMaxHealth());
		pkg.Write(base.GetHealth());
		pkg.Write(this.GetMaxStamina());
		pkg.Write(this.m_firstSpawn);
		pkg.Write(this.m_timeSinceDeath);
		pkg.Write(this.m_guardianPower);
		pkg.Write(this.m_guardianPowerCooldown);
		this.m_inventory.Save(pkg);
		pkg.Write(this.m_knownRecipes.Count);
		foreach (string text in this.m_knownRecipes)
		{
			pkg.Write(text);
		}
		pkg.Write(this.m_knownStations.Count);
		foreach (KeyValuePair<string, int> keyValuePair in this.m_knownStations)
		{
			pkg.Write(keyValuePair.Key);
			pkg.Write(keyValuePair.Value);
		}
		pkg.Write(this.m_knownMaterial.Count);
		foreach (string text2 in this.m_knownMaterial)
		{
			pkg.Write(text2);
		}
		pkg.Write(this.m_shownTutorials.Count);
		foreach (string text3 in this.m_shownTutorials)
		{
			pkg.Write(text3);
		}
		pkg.Write(this.m_uniques.Count);
		foreach (string text4 in this.m_uniques)
		{
			pkg.Write(text4);
		}
		pkg.Write(this.m_trophies.Count);
		foreach (string text5 in this.m_trophies)
		{
			pkg.Write(text5);
		}
		pkg.Write(this.m_knownBiome.Count);
		foreach (Heightmap.Biome biome in this.m_knownBiome)
		{
			pkg.Write((int)biome);
		}
		pkg.Write(this.m_knownTexts.Count);
		foreach (KeyValuePair<string, string> keyValuePair2 in this.m_knownTexts)
		{
			pkg.Write(keyValuePair2.Key);
			pkg.Write(keyValuePair2.Value);
		}
		pkg.Write(this.m_beardItem);
		pkg.Write(this.m_hairItem);
		pkg.Write(this.m_skinColor);
		pkg.Write(this.m_hairColor);
		pkg.Write(this.m_modelIndex);
		pkg.Write(this.m_foods.Count);
		foreach (Player.Food food in this.m_foods)
		{
			pkg.Write(food.m_name);
			pkg.Write(food.m_time);
		}
		this.m_skills.Save(pkg);
		pkg.Write(this.m_customData.Count);
		foreach (KeyValuePair<string, string> keyValuePair3 in this.m_customData)
		{
			pkg.Write(keyValuePair3.Key);
			pkg.Write(keyValuePair3.Value);
		}
		pkg.Write(this.GetStamina());
		pkg.Write(this.GetMaxEitr());
		pkg.Write(this.GetEitr());
	}

	public void Load(ZPackage pkg)
	{
		this.m_isLoading = true;
		base.UnequipAllItems();
		int num = pkg.ReadInt();
		if (num >= 7)
		{
			this.SetMaxHealth(pkg.ReadSingle(), false);
		}
		float num2 = pkg.ReadSingle();
		float maxHealth = base.GetMaxHealth();
		if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
		{
			num2 = maxHealth;
		}
		base.SetHealth(num2);
		if (num >= 10)
		{
			float num3 = pkg.ReadSingle();
			this.SetMaxStamina(num3, false);
			this.m_stamina = num3;
		}
		if (num >= 8)
		{
			this.m_firstSpawn = pkg.ReadBool();
		}
		if (num >= 20)
		{
			this.m_timeSinceDeath = pkg.ReadSingle();
		}
		if (num >= 23)
		{
			string text = pkg.ReadString();
			this.SetGuardianPower(text);
		}
		if (num >= 24)
		{
			this.m_guardianPowerCooldown = pkg.ReadSingle();
		}
		if (num == 2)
		{
			pkg.ReadZDOID();
		}
		this.m_inventory.Load(pkg);
		int num4 = pkg.ReadInt();
		for (int i = 0; i < num4; i++)
		{
			string text2 = pkg.ReadString();
			this.m_knownRecipes.Add(text2);
		}
		if (num < 15)
		{
			int num5 = pkg.ReadInt();
			for (int j = 0; j < num5; j++)
			{
				pkg.ReadString();
			}
		}
		else
		{
			int num6 = pkg.ReadInt();
			for (int k = 0; k < num6; k++)
			{
				string text3 = pkg.ReadString();
				int num7 = pkg.ReadInt();
				this.m_knownStations.Add(text3, num7);
			}
		}
		int num8 = pkg.ReadInt();
		for (int l = 0; l < num8; l++)
		{
			string text4 = pkg.ReadString();
			this.m_knownMaterial.Add(text4);
		}
		if (num < 19 || num >= 21)
		{
			int num9 = pkg.ReadInt();
			for (int m = 0; m < num9; m++)
			{
				string text5 = pkg.ReadString();
				this.m_shownTutorials.Add(text5);
			}
		}
		if (num >= 6)
		{
			int num10 = pkg.ReadInt();
			for (int n = 0; n < num10; n++)
			{
				string text6 = pkg.ReadString();
				this.m_uniques.Add(text6);
			}
		}
		if (num >= 9)
		{
			int num11 = pkg.ReadInt();
			for (int num12 = 0; num12 < num11; num12++)
			{
				string text7 = pkg.ReadString();
				this.m_trophies.Add(text7);
			}
		}
		if (num >= 18)
		{
			int num13 = pkg.ReadInt();
			for (int num14 = 0; num14 < num13; num14++)
			{
				Heightmap.Biome biome = (Heightmap.Biome)pkg.ReadInt();
				this.m_knownBiome.Add(biome);
			}
		}
		if (num >= 22)
		{
			int num15 = pkg.ReadInt();
			for (int num16 = 0; num16 < num15; num16++)
			{
				string text8 = pkg.ReadString();
				string text9 = pkg.ReadString();
				this.m_knownTexts.Add(text8, text9);
			}
		}
		if (num >= 4)
		{
			string text10 = pkg.ReadString();
			string text11 = pkg.ReadString();
			base.SetBeard(text10);
			base.SetHair(text11);
		}
		if (num >= 5)
		{
			Vector3 vector = pkg.ReadVector3();
			Vector3 vector2 = pkg.ReadVector3();
			this.SetSkinColor(vector);
			this.SetHairColor(vector2);
		}
		if (num >= 11)
		{
			int num17 = pkg.ReadInt();
			this.SetPlayerModel(num17);
		}
		if (num >= 12)
		{
			this.m_foods.Clear();
			int num18 = pkg.ReadInt();
			for (int num19 = 0; num19 < num18; num19++)
			{
				if (num >= 14)
				{
					Player.Food food = new Player.Food();
					food.m_name = pkg.ReadString();
					if (num >= 25)
					{
						food.m_time = pkg.ReadSingle();
					}
					else
					{
						food.m_health = pkg.ReadSingle();
						if (num >= 16)
						{
							food.m_stamina = pkg.ReadSingle();
						}
					}
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
					if (itemPrefab == null)
					{
						ZLog.LogWarning("Failed to find food item " + food.m_name);
					}
					else
					{
						food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
						this.m_foods.Add(food);
					}
				}
				else
				{
					pkg.ReadString();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					if (num >= 13)
					{
						pkg.ReadSingle();
					}
				}
			}
		}
		if (num >= 17)
		{
			this.m_skills.Load(pkg);
		}
		if (num >= 26)
		{
			int num20 = pkg.ReadInt();
			for (int num21 = 0; num21 < num20; num21++)
			{
				string text12 = pkg.ReadString();
				string text13 = pkg.ReadString();
				this.m_customData[text12] = text13;
			}
			this.m_stamina = Mathf.Clamp(pkg.ReadSingle(), 0f, this.m_maxStamina);
			this.SetMaxEitr(pkg.ReadSingle(), false);
			this.m_eitr = Mathf.Clamp(pkg.ReadSingle(), 0f, this.m_maxEitr);
		}
		this.m_isLoading = false;
		this.UpdateAvailablePiecesList();
		this.EquipInventoryItems();
	}

	private void EquipInventoryItems()
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetEquippedItems())
		{
			if (!base.EquipItem(itemData, false))
			{
				itemData.m_equipped = false;
			}
		}
	}

	public override bool CanMove()
	{
		return !this.m_teleporting && !this.InCutscene() && (!this.IsEncumbered() || this.HaveStamina(0f)) && base.CanMove();
	}

	public override bool IsEncumbered()
	{
		return this.m_inventory.GetTotalWeight() > this.GetMaxCarryWeight();
	}

	public float GetMaxCarryWeight()
	{
		float maxCarryWeight = this.m_maxCarryWeight;
		this.m_seman.ModifyMaxCarryWeight(maxCarryWeight, ref maxCarryWeight);
		return maxCarryWeight;
	}

	public override bool HaveUniqueKey(string name)
	{
		return this.m_uniques.Contains(name);
	}

	protected override void AddUniqueKey(string name)
	{
		if (!this.m_uniques.Contains(name))
		{
			this.m_uniques.Add(name);
		}
	}

	public bool IsBiomeKnown(Heightmap.Biome biome)
	{
		return this.m_knownBiome.Contains(biome);
	}

	private void AddKnownBiome(Heightmap.Biome biome)
	{
		if (!this.m_knownBiome.Contains(biome))
		{
			this.m_knownBiome.Add(biome);
			if (biome != Heightmap.Biome.Meadows && biome != Heightmap.Biome.None)
			{
				string text = "$biome_" + biome.ToString().ToLower();
				MessageHud.instance.ShowBiomeFoundMsg(text, true);
			}
			if (biome == Heightmap.Biome.BlackForest && !ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
			{
				this.ShowTutorial("blackforest", false);
			}
			Gogan.LogEvent("Game", "BiomeFound", biome.ToString(), 0L);
		}
	}

	public bool IsRecipeKnown(string name)
	{
		return this.m_knownRecipes.Contains(name);
	}

	private void AddKnownRecipe(Recipe recipe)
	{
		if (!this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name))
		{
			this.m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
			MessageHud.instance.QueueUnlockMsg(recipe.m_item.m_itemData.GetIcon(), "$msg_newrecipe", recipe.m_item.m_itemData.m_shared.m_name);
			Gogan.LogEvent("Game", "RecipeFound", recipe.m_item.m_itemData.m_shared.m_name, 0L);
		}
	}

	private void AddKnownPiece(Piece piece)
	{
		if (this.m_knownRecipes.Contains(piece.m_name))
		{
			return;
		}
		this.m_knownRecipes.Add(piece.m_name);
		MessageHud.instance.QueueUnlockMsg(piece.m_icon, "$msg_newpiece", piece.m_name);
		Gogan.LogEvent("Game", "PieceFound", piece.m_name, 0L);
	}

	public void AddKnownStation(CraftingStation station)
	{
		int level = station.GetLevel();
		int num;
		if (this.m_knownStations.TryGetValue(station.m_name, out num))
		{
			if (num < level)
			{
				this.m_knownStations[station.m_name] = level;
				MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation_level", station.m_name + " $msg_level " + level.ToString());
				this.UpdateKnownRecipesList();
			}
			return;
		}
		this.m_knownStations.Add(station.m_name, level);
		MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation", station.m_name);
		Gogan.LogEvent("Game", "StationFound", station.m_name, 0L);
		this.UpdateKnownRecipesList();
	}

	private bool KnowStationLevel(string name, int level)
	{
		int num;
		return this.m_knownStations.TryGetValue(name, out num) && num >= level;
	}

	public void AddKnownText(string label, string text)
	{
		if (label.Length == 0)
		{
			ZLog.LogWarning("Text " + text + " Is missing label");
			return;
		}
		if (!this.m_knownTexts.ContainsKey(label))
		{
			this.m_knownTexts.Add(label, text);
			this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_newtext", new string[] { label }), 0, this.m_textIcon);
		}
	}

	public List<KeyValuePair<string, string>> GetKnownTexts()
	{
		return this.m_knownTexts.ToList<KeyValuePair<string, string>>();
	}

	public void AddKnownItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
		{
			this.AddTrophy(item);
		}
		if (!this.m_knownMaterial.Contains(item.m_shared.m_name))
		{
			this.m_knownMaterial.Add(item.m_shared.m_name);
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newmaterial", item.m_shared.m_name);
			}
			else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newtrophy", item.m_shared.m_name);
			}
			else
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newitem", item.m_shared.m_name);
			}
			Gogan.LogEvent("Game", "ItemFound", item.m_shared.m_name, 0L);
			this.UpdateKnownRecipesList();
		}
	}

	private void AddTrophy(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
		{
			return;
		}
		if (!this.m_trophies.Contains(item.m_dropPrefab.name))
		{
			this.m_trophies.Add(item.m_dropPrefab.name);
		}
	}

	public List<string> GetTrophies()
	{
		List<string> list = new List<string>();
		list.AddRange(this.m_trophies);
		return list;
	}

	private void UpdateKnownRecipesList()
	{
		if (Game.instance == null)
		{
			return;
		}
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			if (recipe.m_enabled && !this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && this.HaveRequirements(recipe, true, 0))
			{
				this.AddKnownRecipe(recipe);
			}
		}
		this.m_tempOwnedPieceTables.Clear();
		this.m_inventory.GetAllPieceTables(this.m_tempOwnedPieceTables);
		bool flag = false;
		foreach (PieceTable pieceTable in this.m_tempOwnedPieceTables)
		{
			foreach (GameObject gameObject in pieceTable.m_pieces)
			{
				Piece component = gameObject.GetComponent<Piece>();
				if (component.m_enabled && !this.m_knownRecipes.Contains(component.m_name) && this.HaveRequirements(component, Player.RequirementMode.IsKnown))
				{
					this.AddKnownPiece(component);
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.UpdateAvailablePiecesList();
		}
	}

	private void UpdateAvailablePiecesList()
	{
		if (this.m_buildPieces != null)
		{
			this.m_buildPieces.UpdateAvailable(this.m_knownRecipes, this, false, this.m_noPlacementCost);
		}
		this.SetupPlacementGhost();
	}

	public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (MessageHud.instance)
			{
				MessageHud.instance.ShowMessage(type, msg, amount, icon);
				return;
			}
		}
		else
		{
			this.m_nview.InvokeRPC("Message", new object[]
			{
				(int)type,
				msg,
				amount
			});
		}
	}

	private void RPC_Message(long sender, int type, string msg, int amount)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (MessageHud.instance)
		{
			MessageHud.instance.ShowMessage((MessageHud.MessageType)type, msg, amount, null);
		}
	}

	public static Player GetPlayer(long playerID)
	{
		foreach (Player player in Player.s_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return player;
			}
		}
		return null;
	}

	public static Player GetClosestPlayer(Vector3 point, float maxRange)
	{
		Player player = null;
		float num = 999999f;
		foreach (Player player2 in Player.s_players)
		{
			float num2 = Vector3.Distance(player2.transform.position, point);
			if (num2 < num && num2 < maxRange)
			{
				num = num2;
				player = player2;
			}
		}
		return player;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, long playerID)
	{
		foreach (Player player in Player.s_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return Utils.DistanceXZ(player.transform.position, point) < range;
			}
		}
		return false;
	}

	public static void MessageAllInRange(Vector3 point, float range, MessageHud.MessageType type, string msg, Sprite icon = null)
	{
		foreach (Player player in Player.s_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				player.Message(type, msg, 0, icon);
			}
		}
	}

	public static int GetPlayersInRangeXZ(Vector3 point, float range)
	{
		int num = 0;
		using (List<Player>.Enumerator enumerator = Player.s_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static void GetPlayersInRange(Vector3 point, float range, List<Player> players)
	{
		foreach (Player player in Player.s_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				players.Add(player);
			}
		}
	}

	public static bool IsPlayerInRange(Vector3 point, float range)
	{
		using (List<Player>.Enumerator enumerator = Player.s_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, float minNoise)
	{
		foreach (Player player in Player.s_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				float noiseRange = player.GetNoiseRange();
				if (range <= noiseRange && noiseRange >= minNoise)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Player GetPlayerNoiseRange(Vector3 point, float maxNoiseRange = 100f)
	{
		foreach (Player player in Player.s_players)
		{
			float num = Vector3.Distance(player.transform.position, point);
			float num2 = Mathf.Min(player.GetNoiseRange(), maxNoiseRange);
			if (num < num2)
			{
				return player;
			}
		}
		return null;
	}

	public static List<Player> GetAllPlayers()
	{
		return Player.s_players;
	}

	public static Player GetRandomPlayer()
	{
		if (Player.s_players.Count == 0)
		{
			return null;
		}
		return Player.s_players[UnityEngine.Random.Range(0, Player.s_players.Count)];
	}

	public void GetAvailableRecipes(ref List<Recipe> available)
	{
		available.Clear();
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			if (recipe.m_enabled && (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && (this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) || this.m_noPlacementCost) && (this.RequiredCraftingStation(recipe, 1, false) || this.m_noPlacementCost))
			{
				available.Add(recipe);
			}
		}
	}

	private void OnInventoryChanged()
	{
		if (this.m_isLoading)
		{
			return;
		}
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetAllItems())
		{
			this.AddKnownItem(itemData);
			if (itemData.m_shared.m_name == "$item_hammer")
			{
				this.ShowTutorial("hammer", false);
			}
			else if (itemData.m_shared.m_name == "$item_hoe")
			{
				this.ShowTutorial("hoe", false);
			}
			else if (itemData.m_shared.m_name == "$item_pickaxe_antler")
			{
				this.ShowTutorial("pickaxe", false);
			}
			else if (itemData.m_shared.m_name.CustomStartsWith("$item_shield"))
			{
				this.ShowTutorial("shield", false);
			}
			if (itemData.m_shared.m_name == "$item_trophy_eikthyr")
			{
				this.ShowTutorial("boss_trophy", false);
			}
			if (itemData.m_shared.m_name == "$item_wishbone")
			{
				this.ShowTutorial("wishbone", false);
			}
			else if (itemData.m_shared.m_name == "$item_copperore" || itemData.m_shared.m_name == "$item_tinore")
			{
				this.ShowTutorial("ore", false);
			}
			else if (itemData.m_shared.m_food > 0f || itemData.m_shared.m_foodStamina > 0f)
			{
				this.ShowTutorial("food", false);
			}
		}
		this.UpdateKnownRecipesList();
		this.UpdateAvailablePiecesList();
	}

	public bool InDebugFlyMode()
	{
		return this.m_debugFly;
	}

	public void ShowTutorial(string name, bool force = false)
	{
		if (this.HaveSeenTutorial(name))
		{
			return;
		}
		Tutorial.instance.ShowText(name, force);
	}

	public void SetSeenTutorial(string name)
	{
		if (name.Length == 0)
		{
			return;
		}
		if (this.m_shownTutorials.Contains(name))
		{
			return;
		}
		this.m_shownTutorials.Add(name);
	}

	public bool HaveSeenTutorial(string name)
	{
		return name.Length != 0 && this.m_shownTutorials.Contains(name);
	}

	public static bool IsSeenTutorialsCleared()
	{
		return !Player.m_localPlayer || Player.m_localPlayer.m_shownTutorials.Count == 0;
	}

	public static void ResetSeenTutorials()
	{
		if (Player.m_localPlayer)
		{
			Player.m_localPlayer.m_shownTutorials.Clear();
		}
	}

	public void SetMouseLook(Vector2 mouseLook)
	{
		this.m_lookYaw *= Quaternion.Euler(0f, mouseLook.x, 0f);
		this.m_lookPitch = Mathf.Clamp(this.m_lookPitch - mouseLook.y, -89f, 89f);
		this.UpdateEyeRotation();
		this.m_lookDir = this.m_eye.forward;
		if (this.m_lookTransitionTime > 0f && mouseLook != Vector2.zero)
		{
			this.m_lookTransitionTime = 0f;
		}
	}

	protected override void UpdateEyeRotation()
	{
		this.m_eye.rotation = this.m_lookYaw * Quaternion.Euler(this.m_lookPitch, 0f, 0f);
	}

	public Ragdoll GetRagdoll()
	{
		return this.m_ragdoll;
	}

	public void OnDodgeMortal()
	{
		this.m_dodgeInvincible = false;
	}

	private void UpdateDodge(float dt)
	{
		this.m_queuedDodgeTimer -= dt;
		if (this.m_queuedDodgeTimer > 0f && base.IsOnGround() && !this.IsDead() && !this.InAttack() && !this.IsEncumbered() && !this.InDodge() && !base.IsStaggering())
		{
			float num = this.m_dodgeStaminaUsage - this.m_dodgeStaminaUsage * this.m_equipmentMovementModifier;
			if (this.HaveStamina(num))
			{
				this.ClearActionQueue();
				this.m_queuedDodgeTimer = 0f;
				this.m_dodgeInvincible = true;
				base.transform.rotation = Quaternion.LookRotation(this.m_queuedDodgeDir);
				this.m_body.rotation = base.transform.rotation;
				this.m_zanim.SetTrigger("dodge");
				base.AddNoise(5f);
				this.UseStamina(num);
				this.m_dodgeEffects.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
			}
			else
			{
				Hud.instance.StaminaBarEmptyFlash();
			}
		}
		bool flag = this.m_animator.GetBool(Player.s_animatorTagDodge) || base.GetNextOrCurrentAnimHash() == Player.s_animatorTagDodge;
		bool flag2 = flag && this.m_dodgeInvincible;
		this.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, flag2);
		this.m_inDodge = flag;
	}

	public override bool IsDodgeInvincible()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_dodgeinv, false);
	}

	public override bool InDodge()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_inDodge;
	}

	public override bool IsDead()
	{
		ZDO zdo = this.m_nview.GetZDO();
		return zdo != null && zdo.GetBool(ZDOVars.s_dead, false);
	}

	private void Dodge(Vector3 dodgeDir)
	{
		this.m_queuedDodgeTimer = 0.5f;
		this.m_queuedDodgeDir = dodgeDir;
	}

	protected override bool AlwaysRotateCamera()
	{
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		if ((currentWeapon != null && this.m_currentAttack != null && this.m_lastCombatTimer < 1f && this.m_currentAttack.m_attackType != Attack.AttackType.None && ZInput.IsMouseActive()) || this.IsDrawingBow() || this.m_blocking)
		{
			return true;
		}
		if (currentWeapon != null && currentWeapon.m_shared.m_alwaysRotate && this.m_moveDir.magnitude < 0.01f)
		{
			return true;
		}
		if (this.m_currentAttack != null && this.m_currentAttack.m_loopingAttack && this.InAttack())
		{
			return true;
		}
		if (this.InPlaceMode())
		{
			Vector3 vector = base.GetLookYaw() * Vector3.forward;
			Vector3 forward = base.transform.forward;
			if (Vector3.Angle(vector, forward) > 95f)
			{
				return true;
			}
		}
		return false;
	}

	public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RPC_TeleportTo", new object[] { pos, rot, distantTeleport });
			return false;
		}
		if (this.IsTeleporting())
		{
			return false;
		}
		if (this.m_teleportCooldown < 2f)
		{
			return false;
		}
		this.m_teleporting = true;
		this.m_distantTeleport = distantTeleport;
		this.m_teleportTimer = 0f;
		this.m_teleportCooldown = 0f;
		this.m_teleportFromPos = base.transform.position;
		this.m_teleportFromRot = base.transform.rotation;
		this.m_teleportTargetPos = pos;
		this.m_teleportTargetRot = rot;
		return true;
	}

	private void UpdateTeleport(float dt)
	{
		if (!this.m_teleporting)
		{
			this.m_teleportCooldown += dt;
			return;
		}
		this.m_teleportCooldown = 0f;
		this.m_teleportTimer += dt;
		if (this.m_teleportTimer > 2f)
		{
			Vector3 vector = this.m_teleportTargetRot * Vector3.forward;
			base.transform.position = this.m_teleportTargetPos;
			base.transform.rotation = this.m_teleportTargetRot;
			this.m_body.velocity = Vector3.zero;
			this.m_maxAirAltitude = base.transform.position.y;
			base.SetLookDir(vector, 0f);
			if ((this.m_teleportTimer > 8f || !this.m_distantTeleport) && ZNetScene.instance.IsAreaReady(this.m_teleportTargetPos))
			{
				float num = 0f;
				if (ZoneSystem.instance.FindFloor(this.m_teleportTargetPos, out num))
				{
					this.m_teleportTimer = 0f;
					this.m_teleporting = false;
					base.ResetCloth();
					return;
				}
				if (this.m_teleportTimer > 15f || !this.m_distantTeleport)
				{
					if (this.m_distantTeleport)
					{
						Vector3 position = base.transform.position;
						position.y = ZoneSystem.instance.GetSolidHeight(this.m_teleportTargetPos) + 0.5f;
						base.transform.position = position;
					}
					else
					{
						base.transform.rotation = this.m_teleportFromRot;
						base.transform.position = this.m_teleportFromPos;
						this.m_maxAirAltitude = base.transform.position.y;
						this.Message(MessageHud.MessageType.Center, "$msg_portal_blocked", 0, null);
					}
					this.m_teleportTimer = 0f;
					this.m_teleporting = false;
					base.ResetCloth();
				}
			}
		}
	}

	public override bool IsTeleporting()
	{
		return this.m_teleporting;
	}

	public bool ShowTeleportAnimation()
	{
		return this.m_teleporting && this.m_distantTeleport;
	}

	public void SetPlayerModel(int index)
	{
		if (this.m_modelIndex == index)
		{
			return;
		}
		this.m_modelIndex = index;
		this.m_visEquipment.SetModel(index);
	}

	public int GetPlayerModel()
	{
		return this.m_modelIndex;
	}

	public void SetSkinColor(Vector3 color)
	{
		if (color == this.m_skinColor)
		{
			return;
		}
		this.m_skinColor = color;
		this.m_visEquipment.SetSkinColor(this.m_skinColor);
	}

	public void SetHairColor(Vector3 color)
	{
		if (this.m_hairColor == color)
		{
			return;
		}
		this.m_hairColor = color;
		this.m_visEquipment.SetHairColor(this.m_hairColor);
	}

	protected override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		base.SetupVisEquipment(visEq, isRagdoll);
		visEq.SetModel(this.m_modelIndex);
		visEq.SetSkinColor(this.m_skinColor);
		visEq.SetHairColor(this.m_hairColor);
	}

	public override bool CanConsumeItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
		{
			return false;
		}
		if (item.m_shared.m_food > 0f && !this.CanEat(item, true))
		{
			return false;
		}
		if (item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			if (this.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.name) || this.m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
			{
				this.Message(MessageHud.MessageType.Center, "$msg_cantconsume", 0, null);
				return false;
			}
		}
		return true;
	}

	public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
	{
		if (!this.CanConsumeItem(item))
		{
			return false;
		}
		if (item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			this.m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, true, 0, 0f);
		}
		if (item.m_shared.m_food > 0f)
		{
			this.EatFood(item);
		}
		inventory.RemoveOneItem(item);
		return true;
	}

	public void SetIntro(bool intro)
	{
		if (this.m_intro == intro)
		{
			return;
		}
		this.m_intro = intro;
		this.m_zanim.SetBool("intro", intro);
	}

	public override bool InIntro()
	{
		return this.m_intro;
	}

	public override bool InCutscene()
	{
		return base.GetCurrentAnimHash() == Player.s_animatorTagCutscene || this.InIntro() || this.m_sleeping || base.InCutscene();
	}

	public void SetMaxStamina(float stamina, bool flashBar)
	{
		if (flashBar && Hud.instance != null && stamina > this.m_maxStamina)
		{
			Hud.instance.StaminaBarUppgradeFlash();
		}
		this.m_maxStamina = stamina;
		this.m_stamina = Mathf.Clamp(this.m_stamina, 0f, this.m_maxStamina);
	}

	private void SetMaxEitr(float eitr, bool flashBar)
	{
		if (flashBar && Hud.instance != null && eitr > this.m_maxEitr)
		{
			Hud.instance.EitrBarUppgradeFlash();
		}
		this.m_maxEitr = eitr;
		this.m_eitr = Mathf.Clamp(this.m_eitr, 0f, this.m_maxEitr);
	}

	public void SetMaxHealth(float health, bool flashBar)
	{
		if (flashBar && Hud.instance != null && health > base.GetMaxHealth())
		{
			Hud.instance.FlashHealthBar();
		}
		base.SetMaxHealth(health);
	}

	public override bool IsPVPEnabled()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_pvp;
		}
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_pvp, false);
	}

	public void SetPVP(bool enabled)
	{
		if (this.m_pvp == enabled)
		{
			return;
		}
		this.m_pvp = enabled;
		this.m_nview.GetZDO().Set(ZDOVars.s_pvp, this.m_pvp);
		if (this.m_pvp)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_pvpon", 0, null);
			return;
		}
		this.Message(MessageHud.MessageType.Center, "$msg_pvpoff", 0, null);
	}

	public bool CanSwitchPVP()
	{
		return this.m_lastCombatTimer > 10f;
	}

	public bool NoCostCheat()
	{
		return this.m_noPlacementCost;
	}

	public bool StartEmote(string emote, bool oneshot = true)
	{
		if (!this.CanMove() || this.InAttack() || this.IsDrawingBow() || this.IsAttached() || this.IsAttachedToShip())
		{
			return false;
		}
		this.SetCrouch(false);
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_emoteID, 0);
		this.m_nview.GetZDO().Set(ZDOVars.s_emoteID, @int + 1, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_emote, emote);
		this.m_nview.GetZDO().Set(ZDOVars.s_emoteOneshot, oneshot);
		return true;
	}

	protected override void StopEmote()
	{
		if (this.m_nview.GetZDO().GetString(ZDOVars.s_emote, "") != "")
		{
			int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_emoteID, 0);
			this.m_nview.GetZDO().Set(ZDOVars.s_emoteID, @int + 1, false);
			this.m_nview.GetZDO().Set(ZDOVars.s_emote, "");
		}
	}

	private void UpdateEmote()
	{
		if (this.m_nview.IsOwner() && this.InEmote() && this.m_moveDir != Vector3.zero)
		{
			this.StopEmote();
		}
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_emoteID, 0);
		if (@int != this.m_emoteID)
		{
			this.m_emoteID = @int;
			if (!string.IsNullOrEmpty(this.m_emoteState))
			{
				this.m_animator.SetBool("emote_" + this.m_emoteState, false);
			}
			this.m_emoteState = "";
			this.m_animator.SetTrigger("emote_stop");
			string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_emote, "");
			if (!string.IsNullOrEmpty(@string))
			{
				bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_emoteOneshot, false);
				this.m_animator.ResetTrigger("emote_stop");
				if (@bool)
				{
					this.m_animator.SetTrigger("emote_" + @string);
					return;
				}
				this.m_emoteState = @string;
				this.m_animator.SetBool("emote_" + @string, true);
			}
		}
	}

	public override bool InEmote()
	{
		return !string.IsNullOrEmpty(this.m_emoteState) || base.GetCurrentAnimHash() == Player.s_animatorTagEmote;
	}

	public override bool IsCrouching()
	{
		return base.GetCurrentAnimHash() == Player.s_animatorTagCrouch;
	}

	private void UpdateCrouch(float dt)
	{
		if (this.m_crouchToggled)
		{
			if (!this.HaveStamina(0f) || base.IsSwimming() || this.InBed() || this.InPlaceMode() || this.m_run || this.IsBlocking() || base.IsFlying())
			{
				this.SetCrouch(false);
			}
			bool flag = this.InAttack() || this.IsDrawingBow();
			this.m_zanim.SetBool(Player.s_crouching, this.m_crouchToggled && !flag);
			return;
		}
		this.m_zanim.SetBool(Player.s_crouching, false);
	}

	protected override void SetCrouch(bool crouch)
	{
		this.m_crouchToggled = crouch;
	}

	public void SetGuardianPower(string name)
	{
		this.m_guardianPower = name;
		this.m_guardianSE = ObjectDB.instance.GetStatusEffect(this.m_guardianPower);
	}

	public string GetGuardianPowerName()
	{
		return this.m_guardianPower;
	}

	public void GetGuardianPowerHUD(out StatusEffect se, out float cooldown)
	{
		se = this.m_guardianSE;
		cooldown = this.m_guardianPowerCooldown;
	}

	public bool StartGuardianPower()
	{
		if (this.m_guardianSE == null)
		{
			return false;
		}
		if ((this.InAttack() && !this.HaveQueuedChain()) || this.InDodge() || !this.CanMove() || base.IsKnockedBack() || base.IsStaggering() || this.InMinorAction())
		{
			return false;
		}
		if (this.m_guardianPowerCooldown > 0f)
		{
			this.Message(MessageHud.MessageType.Center, "$hud_powernotready", 0, null);
			return false;
		}
		this.m_zanim.SetTrigger("gpower");
		return true;
	}

	public bool ActivateGuardianPower()
	{
		if (this.m_guardianPowerCooldown > 0f)
		{
			return false;
		}
		if (this.m_guardianSE == null)
		{
			return false;
		}
		List<Player> list = new List<Player>();
		Player.GetPlayersInRange(base.transform.position, 10f, list);
		foreach (Player player in list)
		{
			player.GetSEMan().AddStatusEffect(this.m_guardianSE.NameHash(), true, 0, 0f);
		}
		this.m_guardianPowerCooldown = this.m_guardianSE.m_cooldown;
		return false;
	}

	private void UpdateGuardianPower(float dt)
	{
		this.m_guardianPowerCooldown -= dt;
		if (this.m_guardianPowerCooldown < 0f)
		{
			this.m_guardianPowerCooldown = 0f;
		}
	}

	public override void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset)
	{
		if (this.m_attached)
		{
			return;
		}
		this.m_attached = true;
		this.m_attachedToShip = onShip;
		this.m_attachPoint = attachPoint;
		this.m_detachOffset = detachOffset;
		this.m_attachAnimation = attachAnimation;
		this.m_zanim.SetBool(attachAnimation, true);
		this.m_nview.GetZDO().Set(ZDOVars.s_inBed, isBed);
		if (colliderRoot != null)
		{
			this.m_attachColliders = colliderRoot.GetComponentsInChildren<Collider>();
			ZLog.Log("Ignoring " + this.m_attachColliders.Length.ToString() + " colliders");
			foreach (Collider collider in this.m_attachColliders)
			{
				Physics.IgnoreCollision(this.m_collider, collider, true);
			}
		}
		if (hideWeapons)
		{
			base.HideHandItems();
		}
		this.UpdateAttach();
		base.ResetCloth();
	}

	private void UpdateAttach()
	{
		if (this.m_attached)
		{
			if (this.m_attachPoint != null)
			{
				base.transform.position = this.m_attachPoint.position;
				base.transform.rotation = this.m_attachPoint.rotation;
				Rigidbody componentInParent = this.m_attachPoint.GetComponentInParent<Rigidbody>();
				this.m_body.useGravity = false;
				this.m_body.velocity = (componentInParent ? componentInParent.GetPointVelocity(base.transform.position) : Vector3.zero);
				this.m_body.angularVelocity = Vector3.zero;
				this.m_maxAirAltitude = base.transform.position.y;
				return;
			}
			this.AttachStop();
		}
	}

	public override bool IsAttached()
	{
		return this.m_attached || base.IsAttached();
	}

	public override bool IsAttachedToShip()
	{
		return this.m_attached && this.m_attachedToShip;
	}

	public override bool IsRiding()
	{
		return this.m_doodadController != null && this.m_doodadController.IsValid() && this.m_doodadController is Sadle;
	}

	public override bool InBed()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_inBed, false);
	}

	public override void AttachStop()
	{
		if (this.m_sleeping)
		{
			return;
		}
		if (this.m_attached)
		{
			if (this.m_attachPoint != null)
			{
				base.transform.position = this.m_attachPoint.TransformPoint(this.m_detachOffset);
			}
			if (this.m_attachColliders != null)
			{
				foreach (Collider collider in this.m_attachColliders)
				{
					if (collider)
					{
						Physics.IgnoreCollision(this.m_collider, collider, false);
					}
				}
				this.m_attachColliders = null;
			}
			this.m_body.useGravity = true;
			this.m_attached = false;
			this.m_attachPoint = null;
			this.m_zanim.SetBool(this.m_attachAnimation, false);
			this.m_nview.GetZDO().Set(ZDOVars.s_inBed, false);
			base.ResetCloth();
		}
	}

	public void StartDoodadControl(IDoodadController shipControl)
	{
		this.m_doodadController = shipControl;
		ZLog.Log("Doodad controlls set " + shipControl.GetControlledComponent().gameObject.name);
	}

	public void StopDoodadControl()
	{
		if (this.m_doodadController != null)
		{
			if (this.m_doodadController.IsValid())
			{
				this.m_doodadController.OnUseStop(this);
			}
			ZLog.Log("Stop doodad controlls");
			this.m_doodadController = null;
		}
	}

	private void SetDoodadControlls(ref Vector3 moveDir, ref Vector3 lookDir, ref bool run, ref bool autoRun, bool block)
	{
		if (this.m_doodadController.IsValid())
		{
			this.m_doodadController.ApplyControlls(moveDir, lookDir, run, autoRun, block);
		}
		moveDir = Vector3.zero;
		autoRun = false;
		run = false;
	}

	public Ship GetControlledShip()
	{
		if (this.m_doodadController != null && this.m_doodadController.IsValid())
		{
			return this.m_doodadController.GetControlledComponent() as Ship;
		}
		return null;
	}

	public IDoodadController GetDoodadController()
	{
		return this.m_doodadController;
	}

	private void UpdateDoodadControls(float dt)
	{
		if (this.m_doodadController == null)
		{
			return;
		}
		if (!this.m_doodadController.IsValid())
		{
			this.StopDoodadControl();
			return;
		}
		Vector3 forward = this.m_doodadController.GetControlledComponent().transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion quaternion = Quaternion.LookRotation(forward);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, 100f * dt);
		if (Vector3.Distance(this.m_doodadController.GetPosition(), base.transform.position) > this.m_maxInteractDistance)
		{
			this.StopDoodadControl();
		}
	}

	public bool IsSleeping()
	{
		return this.m_sleeping;
	}

	public void SetSleeping(bool sleep)
	{
		if (this.m_sleeping == sleep)
		{
			return;
		}
		this.m_sleeping = sleep;
		if (!sleep)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_goodmorning", 0, null);
			this.m_seman.AddStatusEffect(Player.s_statusEffectRested, true, 0, 0f);
			this.m_wakeupTime = DateTime.Now;
		}
	}

	public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool secondaryAttackHold, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun, bool dodge = false)
	{
		if ((this.IsAttached() || this.InEmote()) && (movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && this.GetDoodadController() == null)
		{
			attack = false;
			attackHold = false;
			secondaryAttack = false;
			secondaryAttackHold = false;
			this.StopEmote();
			this.AttachStop();
		}
		if (this.m_doodadController != null)
		{
			this.SetDoodadControlls(ref movedir, ref this.m_lookDir, ref run, ref autoRun, blockHold);
			if (jump || attack || secondaryAttack)
			{
				attack = false;
				attackHold = false;
				secondaryAttack = false;
				secondaryAttackHold = false;
				this.StopDoodadControl();
			}
		}
		if (run)
		{
			this.m_walk = false;
		}
		if (!this.m_autoRun)
		{
			Vector3 lookDir = this.m_lookDir;
			lookDir.y = 0f;
			lookDir.Normalize();
			this.m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
		}
		if (!this.m_autoRun && autoRun && !this.InPlaceMode())
		{
			this.m_autoRun = true;
			this.SetCrouch(false);
			this.m_moveDir = this.m_lookDir;
			this.m_moveDir.y = 0f;
			this.m_moveDir.Normalize();
		}
		else if (this.m_autoRun)
		{
			if (attack || jump || crouch || movedir != Vector3.zero || this.InPlaceMode() || attackHold || secondaryAttackHold)
			{
				this.m_autoRun = false;
			}
			else if (autoRun || blockHold)
			{
				this.m_moveDir = this.m_lookDir;
				this.m_moveDir.y = 0f;
				this.m_moveDir.Normalize();
				blockHold = false;
				block = false;
			}
		}
		this.m_attack = attack;
		this.m_attackHold = attackHold;
		this.m_secondaryAttack = secondaryAttack;
		this.m_secondaryAttackHold = secondaryAttackHold;
		this.m_blocking = blockHold;
		this.m_run = run;
		if (crouch)
		{
			this.SetCrouch(!this.m_crouchToggled);
		}
		if (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive())
		{
			if (jump)
			{
				if (this.m_blocking)
				{
					Vector3 vector = this.m_moveDir;
					if (vector.magnitude < 0.1f)
					{
						vector = -this.m_lookDir;
						vector.y = 0f;
						vector.Normalize();
					}
					this.Dodge(vector);
					return;
				}
				if (this.IsCrouching() || this.m_crouchToggled)
				{
					Vector3 vector2 = this.m_moveDir;
					if (vector2.magnitude < 0.1f)
					{
						vector2 = this.m_lookDir;
						vector2.y = 0f;
						vector2.Normalize();
					}
					this.Dodge(vector2);
					return;
				}
				base.Jump(false);
				return;
			}
		}
		else if (ZInput.InputLayout == InputLayout.Alternative1)
		{
			if (dodge)
			{
				if (this.m_blocking)
				{
					Vector3 vector3 = this.m_moveDir;
					if (vector3.magnitude < 0.1f)
					{
						vector3 = -this.m_lookDir;
						vector3.y = 0f;
						vector3.Normalize();
					}
					this.Dodge(vector3);
				}
				else if (this.IsCrouching() || this.m_crouchToggled)
				{
					Vector3 vector4 = this.m_moveDir;
					if (vector4.magnitude < 0.1f)
					{
						vector4 = this.m_lookDir;
						vector4.y = 0f;
						vector4.Normalize();
					}
					this.Dodge(vector4);
				}
			}
			if (jump)
			{
				base.Jump(false);
			}
		}
	}

	private void UpdateTargeted(float dt)
	{
		this.m_timeSinceTargeted += dt;
		this.m_timeSinceSensed += dt;
	}

	public override void OnTargeted(bool sensed, bool alerted)
	{
		if (sensed)
		{
			if (this.m_timeSinceSensed > 0.5f)
			{
				this.m_timeSinceSensed = 0f;
				this.m_nview.InvokeRPC("OnTargeted", new object[] { sensed, alerted });
				return;
			}
		}
		else if (this.m_timeSinceTargeted > 0.5f)
		{
			this.m_timeSinceTargeted = 0f;
			this.m_nview.InvokeRPC("OnTargeted", new object[] { sensed, alerted });
		}
	}

	private void RPC_OnTargeted(long sender, bool sensed, bool alerted)
	{
		this.m_timeSinceTargeted = 0f;
		if (sensed)
		{
			this.m_timeSinceSensed = 0f;
		}
		if (alerted)
		{
			MusicMan.instance.ResetCombatTimer();
		}
	}

	protected override void OnDamaged(HitData hit)
	{
		base.OnDamaged(hit);
		if (hit.GetTotalDamage() > base.GetMaxHealth() / 10f)
		{
			Hud.instance.DamageFlash();
		}
	}

	public bool IsTargeted()
	{
		return this.m_timeSinceTargeted < 1f;
	}

	public bool IsSensed()
	{
		return this.m_timeSinceSensed < 1f;
	}

	protected override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
		if (this.m_chestItem != null)
		{
			mods.Apply(this.m_chestItem.m_shared.m_damageModifiers);
		}
		if (this.m_legItem != null)
		{
			mods.Apply(this.m_legItem.m_shared.m_damageModifiers);
		}
		if (this.m_helmetItem != null)
		{
			mods.Apply(this.m_helmetItem.m_shared.m_damageModifiers);
		}
		if (this.m_shoulderItem != null)
		{
			mods.Apply(this.m_shoulderItem.m_shared.m_damageModifiers);
		}
	}

	public override float GetBodyArmor()
	{
		float num = 0f;
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.GetArmor();
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.GetArmor();
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.GetArmor();
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.GetArmor();
		}
		return num;
	}

	protected override void OnSneaking(float dt)
	{
		float num = Mathf.Pow(this.m_skills.GetSkillFactor(Skills.SkillType.Sneak), 0.5f);
		float num2 = Mathf.Lerp(1f, 0.25f, num);
		this.UseStamina(dt * this.m_sneakStaminaDrain * num2);
		if (!this.HaveStamina(0f))
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		this.m_sneakSkillImproveTimer += dt;
		if (this.m_sneakSkillImproveTimer > 1f)
		{
			this.m_sneakSkillImproveTimer = 0f;
			if (BaseAI.InStealthRange(this))
			{
				this.RaiseSkill(Skills.SkillType.Sneak, 1f);
				return;
			}
			this.RaiseSkill(Skills.SkillType.Sneak, 0.1f);
		}
	}

	private void UpdateStealth(float dt)
	{
		this.m_stealthFactorUpdateTimer += dt;
		if (this.m_stealthFactorUpdateTimer > 0.5f)
		{
			this.m_stealthFactorUpdateTimer = 0f;
			this.m_stealthFactorTarget = 0f;
			if (this.IsCrouching())
			{
				float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Sneak);
				float lightFactor = StealthSystem.instance.GetLightFactor(base.GetCenterPoint());
				this.m_stealthFactorTarget = Mathf.Lerp(0.5f + lightFactor * 0.5f, 0.2f + lightFactor * 0.4f, skillFactor);
				this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
				this.m_seman.ModifyStealth(this.m_stealthFactorTarget, ref this.m_stealthFactorTarget);
				this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
			}
			else
			{
				this.m_stealthFactorTarget = 1f;
			}
		}
		this.m_stealthFactor = Mathf.MoveTowards(this.m_stealthFactor, this.m_stealthFactorTarget, dt / 4f);
		this.m_nview.GetZDO().Set(ZDOVars.s_stealth, this.m_stealthFactor);
	}

	public override float GetStealthFactor()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_stealthFactor;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_stealth, 0f);
	}

	public override bool InAttack()
	{
		if (MonoUpdaters.UpdateCount == this.m_cachedFrame)
		{
			return this.m_cachedAttack;
		}
		this.m_cachedFrame = MonoUpdaters.UpdateCount;
		if (base.GetNextOrCurrentAnimHash() == Humanoid.s_animatorTagAttack)
		{
			this.m_cachedAttack = true;
			return true;
		}
		for (int i = 1; i < this.m_animator.layerCount; i++)
		{
			if ((this.m_animator.IsInTransition(i) ? this.m_animator.GetNextAnimatorStateInfo(i).tagHash : this.m_animator.GetCurrentAnimatorStateInfo(i).tagHash) == Humanoid.s_animatorTagAttack)
			{
				this.m_cachedAttack = true;
				return true;
			}
		}
		this.m_cachedAttack = false;
		return false;
	}

	public override float GetEquipmentMovementModifier()
	{
		return this.m_equipmentMovementModifier;
	}

	protected override float GetJogSpeedFactor()
	{
		return 1f + this.m_equipmentMovementModifier;
	}

	protected override float GetRunSpeedFactor()
	{
		float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		return (1f + skillFactor * 0.25f) * (1f + this.m_equipmentMovementModifier * 1.5f);
	}

	public override bool InMinorAction()
	{
		int tagHash = this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash;
		if (tagHash == Player.s_animatorTagMinorAction || tagHash == Player.s_animatorTagMinorActionFast)
		{
			return true;
		}
		if (this.m_animator.IsInTransition(1))
		{
			int tagHash2 = this.m_animator.GetNextAnimatorStateInfo(1).tagHash;
			return tagHash2 == Player.s_animatorTagMinorAction || tagHash2 == Player.s_animatorTagMinorActionFast;
		}
		return false;
	}

	public override bool InMinorActionSlowdown()
	{
		return this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash == Player.s_animatorTagMinorAction || (this.m_animator.IsInTransition(1) && this.m_animator.GetNextAnimatorStateInfo(1).tagHash == Player.s_animatorTagMinorAction);
	}

	public override bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if (this.m_attached && this.m_attachPoint)
		{
			ZNetView componentInParent = this.m_attachPoint.GetComponentInParent<ZNetView>();
			if (componentInParent && componentInParent.IsValid())
			{
				parent = componentInParent.GetZDO().m_uid;
				if (componentInParent.GetComponent<Character>() != null)
				{
					attachJoint = this.m_attachPoint.name;
					relativePos = Vector3.zero;
					relativeRot = Quaternion.identity;
				}
				else
				{
					attachJoint = "";
					relativePos = componentInParent.transform.InverseTransformPoint(base.transform.position);
					relativeRot = Quaternion.Inverse(componentInParent.transform.rotation) * base.transform.rotation;
				}
				relativeVel = Vector3.zero;
				return true;
			}
		}
		return base.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
	}

	public override Skills GetSkills()
	{
		return this.m_skills;
	}

	public override float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return this.m_skills.GetRandomSkillFactor(skill);
	}

	public override float GetSkillFactor(Skills.SkillType skill)
	{
		return this.m_skills.GetSkillFactor(skill);
	}

	protected override void DoDamageCameraShake(HitData hit)
	{
		float totalStaggerDamage = hit.m_damage.GetTotalStaggerDamage();
		if (GameCamera.instance && totalStaggerDamage > 0f)
		{
			float num = Mathf.Clamp01(totalStaggerDamage / base.GetMaxHealth());
			GameCamera.instance.AddShake(base.transform.position, 50f, this.m_baseCameraShake * num, false);
		}
	}

	protected override void DamageArmorDurability(HitData hit)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		if (this.m_chestItem != null)
		{
			list.Add(this.m_chestItem);
		}
		if (this.m_legItem != null)
		{
			list.Add(this.m_legItem);
		}
		if (this.m_helmetItem != null)
		{
			list.Add(this.m_helmetItem);
		}
		if (this.m_shoulderItem != null)
		{
			list.Add(this.m_shoulderItem);
		}
		if (list.Count == 0)
		{
			return;
		}
		float num = hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage();
		if (num <= 0f)
		{
			return;
		}
		int num2 = UnityEngine.Random.Range(0, list.Count);
		ItemDrop.ItemData itemData = list[num2];
		itemData.m_durability = Mathf.Max(0f, itemData.m_durability - num);
	}

	protected override bool ToggleEquipped(ItemDrop.ItemData item)
	{
		if (!item.IsEquipable())
		{
			return false;
		}
		if (this.InAttack())
		{
			return true;
		}
		if (item.m_shared.m_equipDuration <= 0f)
		{
			if (base.IsItemEquiped(item))
			{
				base.UnequipItem(item, true);
			}
			else
			{
				base.EquipItem(item, true);
			}
		}
		else if (base.IsItemEquiped(item))
		{
			this.QueueUnequipAction(item);
		}
		else
		{
			this.QueueEquipAction(item);
		}
		return true;
	}

	public void GetActionProgress(out string name, out float progress)
	{
		if (this.m_actionQueue.Count > 0)
		{
			Player.MinorActionData minorActionData = this.m_actionQueue[0];
			if (minorActionData.m_duration > 0.5f)
			{
				float num = Mathf.Clamp01(minorActionData.m_time / minorActionData.m_duration);
				if (num > 0f)
				{
					name = minorActionData.m_progressText;
					progress = num;
					return;
				}
			}
		}
		name = null;
		progress = 0f;
	}

	private void UpdateActionQueue(float dt)
	{
		if (this.m_actionQueuePause > 0f)
		{
			this.m_actionQueuePause -= dt;
			if (this.m_actionAnimation != null)
			{
				this.m_zanim.SetBool(this.m_actionAnimation, false);
				this.m_actionAnimation = null;
			}
			return;
		}
		if (this.InAttack())
		{
			if (this.m_actionAnimation != null)
			{
				this.m_zanim.SetBool(this.m_actionAnimation, false);
				this.m_actionAnimation = null;
			}
			return;
		}
		if (this.m_actionQueue.Count == 0)
		{
			if (this.m_actionAnimation != null)
			{
				this.m_zanim.SetBool(this.m_actionAnimation, false);
				this.m_actionAnimation = null;
			}
			return;
		}
		Player.MinorActionData minorActionData = this.m_actionQueue[0];
		if (this.m_actionAnimation != null && this.m_actionAnimation != minorActionData.m_animation)
		{
			this.m_zanim.SetBool(this.m_actionAnimation, false);
			this.m_actionAnimation = null;
		}
		this.m_zanim.SetBool(minorActionData.m_animation, true);
		this.m_actionAnimation = minorActionData.m_animation;
		if (minorActionData.m_time == 0f && minorActionData.m_startEffect != null)
		{
			minorActionData.m_startEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		if (minorActionData.m_staminaDrain > 0f)
		{
			this.UseStamina(minorActionData.m_staminaDrain * dt);
		}
		minorActionData.m_time += dt;
		if (minorActionData.m_time > minorActionData.m_duration)
		{
			this.m_actionQueue.RemoveAt(0);
			this.m_zanim.SetBool(this.m_actionAnimation, false);
			this.m_actionAnimation = null;
			if (!string.IsNullOrEmpty(minorActionData.m_doneAnimation))
			{
				this.m_zanim.SetTrigger(minorActionData.m_doneAnimation);
			}
			switch (minorActionData.m_type)
			{
			case Player.MinorActionData.ActionType.Equip:
				base.EquipItem(minorActionData.m_item, true);
				break;
			case Player.MinorActionData.ActionType.Unequip:
				base.UnequipItem(minorActionData.m_item, true);
				break;
			case Player.MinorActionData.ActionType.Reload:
				this.SetWeaponLoaded(minorActionData.m_item);
				break;
			}
			this.m_actionQueuePause = 0.3f;
		}
	}

	private void QueueEquipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (this.IsEquipActionQueued(item))
		{
			this.RemoveEquipAction(item);
			return;
		}
		this.CancelReloadAction();
		Player.MinorActionData minorActionData = new Player.MinorActionData();
		minorActionData.m_item = item;
		minorActionData.m_type = Player.MinorActionData.ActionType.Equip;
		minorActionData.m_duration = item.m_shared.m_equipDuration;
		minorActionData.m_progressText = "$hud_equipping " + item.m_shared.m_name;
		minorActionData.m_animation = "equipping";
		if (minorActionData.m_duration >= 1f)
		{
			minorActionData.m_startEffect = this.m_equipStartEffects;
		}
		this.m_actionQueue.Add(minorActionData);
	}

	private void QueueUnequipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (this.IsEquipActionQueued(item))
		{
			this.RemoveEquipAction(item);
			return;
		}
		this.CancelReloadAction();
		Player.MinorActionData minorActionData = new Player.MinorActionData();
		minorActionData.m_item = item;
		minorActionData.m_type = Player.MinorActionData.ActionType.Unequip;
		minorActionData.m_duration = item.m_shared.m_equipDuration;
		minorActionData.m_progressText = "$hud_unequipping " + item.m_shared.m_name;
		minorActionData.m_animation = "equipping";
		this.m_actionQueue.Add(minorActionData);
	}

	private void QueueReloadAction()
	{
		if (this.IsReloadActionQueued())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		if (currentWeapon == null || !currentWeapon.m_shared.m_attack.m_requiresReload)
		{
			return;
		}
		Player.MinorActionData minorActionData = new Player.MinorActionData();
		minorActionData.m_item = currentWeapon;
		minorActionData.m_type = Player.MinorActionData.ActionType.Reload;
		minorActionData.m_duration = currentWeapon.GetWeaponLoadingTime();
		minorActionData.m_progressText = "$hud_reloading " + currentWeapon.m_shared.m_name;
		minorActionData.m_animation = currentWeapon.m_shared.m_attack.m_reloadAnimation;
		minorActionData.m_doneAnimation = currentWeapon.m_shared.m_attack.m_reloadAnimation + "_done";
		minorActionData.m_staminaDrain = currentWeapon.m_shared.m_attack.m_reloadStaminaDrain;
		this.m_actionQueue.Add(minorActionData);
	}

	protected override void ClearActionQueue()
	{
		this.m_actionQueue.Clear();
	}

	public override void RemoveEquipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if (minorActionData.m_item == item)
			{
				this.m_actionQueue.Remove(minorActionData);
				break;
			}
		}
	}

	public bool IsEquipActionQueued(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return false;
		}
		foreach (Player.MinorActionData minorActionData in this.m_actionQueue)
		{
			if ((minorActionData.m_type == Player.MinorActionData.ActionType.Equip || minorActionData.m_type == Player.MinorActionData.ActionType.Unequip) && minorActionData.m_item == item)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReloadActionQueued()
	{
		using (List<Player.MinorActionData>.Enumerator enumerator = this.m_actionQueue.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_type == Player.MinorActionData.ActionType.Reload)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void ResetCharacter()
	{
		this.m_guardianPowerCooldown = 0f;
		Player.ResetSeenTutorials();
		this.m_knownRecipes.Clear();
		this.m_knownStations.Clear();
		this.m_knownMaterial.Clear();
		this.m_uniques.Clear();
		this.m_trophies.Clear();
		this.m_skills.Clear();
		this.m_knownBiome.Clear();
		this.m_knownTexts.Clear();
	}

	public bool ToggleDebugFly()
	{
		this.m_debugFly = !this.m_debugFly;
		this.m_nview.GetZDO().Set(ZDOVars.s_debugFly, this.m_debugFly);
		this.Message(MessageHud.MessageType.TopLeft, "Debug fly:" + this.m_debugFly.ToString(), 0, null);
		return this.m_debugFly;
	}

	public void SetNoPlacementCost(bool value)
	{
		if (value != this.m_noPlacementCost)
		{
			this.ToggleNoPlacementCost();
		}
	}

	public bool ToggleNoPlacementCost()
	{
		this.m_noPlacementCost = !this.m_noPlacementCost;
		this.Message(MessageHud.MessageType.TopLeft, "No placement cost:" + this.m_noPlacementCost.ToString(), 0, null);
		this.UpdateAvailablePiecesList();
		return this.m_noPlacementCost;
	}

	public bool IsKnownMaterial(string name)
	{
		return this.m_knownMaterial.Contains(name);
	}

	public bool AlternativePlacementActive
	{
		get
		{
			return this.m_altPlace;
		}
	}

	public static bool m_debugMode = false;

	private const int m_playerDataSaveVersion = 26;

	private float m_baseValueUpdateTimer;

	private float m_rotatePieceTimer;

	private bool m_altPlace;

	public static Player m_localPlayer = null;

	private static readonly List<Player> s_players = new List<Player>();

	[Header("Player")]
	public float m_maxPlaceDistance = 5f;

	public float m_maxInteractDistance = 5f;

	public float m_staminaRegen = 5f;

	public float m_staminaRegenTimeMultiplier = 1f;

	public float m_staminaRegenDelay = 1f;

	public float m_runStaminaDrain = 10f;

	public float m_sneakStaminaDrain = 5f;

	public float m_swimStaminaDrainMinSkill = 5f;

	public float m_swimStaminaDrainMaxSkill = 2f;

	public float m_dodgeStaminaUsage = 10f;

	public float m_eiterRegen = 5f;

	public float m_eitrRegenDelay = 1f;

	public float m_autoPickupRange = 2f;

	public float m_maxCarryWeight = 300f;

	public float m_encumberedStaminaDrain = 10f;

	public float m_hardDeathCooldown = 10f;

	public float m_baseCameraShake = 4f;

	public float m_placeDelay = 0.4f;

	public float m_removeDelay = 0.25f;

	public EffectList m_drownEffects = new EffectList();

	public EffectList m_spawnEffects = new EffectList();

	public EffectList m_removeEffects = new EffectList();

	public EffectList m_dodgeEffects = new EffectList();

	public EffectList m_skillLevelupEffects = new EffectList();

	public EffectList m_equipStartEffects = new EffectList();

	public GameObject m_placeMarker;

	public GameObject m_tombstone;

	public GameObject m_valkyrie;

	public Sprite m_textIcon;

	public DateTime m_wakeupTime;

	public float m_baseHP = 25f;

	public float m_baseStamina = 75f;

	private Skills m_skills;

	private PieceTable m_buildPieces;

	private bool m_noPlacementCost;

	private const bool m_hideUnavailable = false;

	private bool m_enableAutoPickup = true;

	private readonly HashSet<string> m_knownRecipes = new HashSet<string>();

	private readonly Dictionary<string, int> m_knownStations = new Dictionary<string, int>();

	private readonly HashSet<string> m_knownMaterial = new HashSet<string>();

	private readonly HashSet<string> m_shownTutorials = new HashSet<string>();

	private readonly HashSet<string> m_uniques = new HashSet<string>();

	private readonly HashSet<string> m_trophies = new HashSet<string>();

	private readonly HashSet<Heightmap.Biome> m_knownBiome = new HashSet<Heightmap.Biome>();

	private readonly Dictionary<string, string> m_knownTexts = new Dictionary<string, string>();

	private float m_stationDiscoverTimer;

	private bool m_debugFly;

	private bool m_godMode;

	private bool m_ghostMode;

	private float m_lookPitch;

	private const int m_maxFoods = 3;

	private const float m_foodDrainPerSec = 0.1f;

	private float m_foodUpdateTimer;

	private float m_foodRegenTimer;

	private readonly List<Player.Food> m_foods = new List<Player.Food>();

	private float m_stamina = 100f;

	private float m_maxStamina = 100f;

	private float m_staminaRegenTimer;

	private float m_eitr;

	private float m_maxEitr;

	private float m_eitrRegenTimer;

	private string m_guardianPower = "";

	public float m_guardianPowerCooldown;

	private StatusEffect m_guardianSE;

	private float m_placePressedTime = -1000f;

	private float m_removePressedTime = -1000f;

	private float m_lastToolUseTime;

	private GameObject m_placementMarkerInstance;

	private GameObject m_placementGhost;

	private Player.PlacementStatus m_placementStatus = Player.PlacementStatus.Invalid;

	private int m_placeRotation;

	private int m_placeRayMask;

	private int m_placeGroundRayMask;

	private int m_placeWaterRayMask;

	private int m_removeRayMask;

	private int m_interactMask;

	private int m_autoPickupMask;

	private readonly List<Player.MinorActionData> m_actionQueue = new List<Player.MinorActionData>();

	private float m_actionQueuePause;

	private string m_actionAnimation;

	private GameObject m_hovering;

	private Character m_hoveringCreature;

	private float m_lastHoverInteractTime;

	private bool m_pvp;

	private float m_updateCoverTimer;

	private float m_coverPercentage;

	private bool m_underRoof = true;

	private float m_nearFireTimer;

	private bool m_isLoading;

	private ItemDrop.ItemData m_weaponLoaded;

	private float m_queuedAttackTimer;

	private float m_queuedSecondAttackTimer;

	private float m_queuedDodgeTimer;

	private Vector3 m_queuedDodgeDir = Vector3.zero;

	private bool m_inDodge;

	private bool m_dodgeInvincible;

	private CraftingStation m_currentStation;

	private bool m_inCraftingStation;

	private Ragdoll m_ragdoll;

	private Piece m_hoveringPiece;

	private string m_emoteState = "";

	private int m_emoteID;

	private bool m_intro;

	private bool m_firstSpawn = true;

	private bool m_crouchToggled;

	private bool m_autoRun;

	private bool m_safeInHome;

	private IDoodadController m_doodadController;

	private bool m_attached;

	private string m_attachAnimation = "";

	private bool m_sleeping;

	private bool m_attachedToShip;

	private Transform m_attachPoint;

	private Vector3 m_detachOffset = Vector3.zero;

	private Collider[] m_attachColliders;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private bool m_teleporting;

	private bool m_distantTeleport;

	private float m_teleportTimer;

	private float m_teleportCooldown;

	private Vector3 m_teleportFromPos;

	private Quaternion m_teleportFromRot;

	private Vector3 m_teleportTargetPos;

	private Quaternion m_teleportTargetRot;

	private Heightmap.Biome m_currentBiome;

	private float m_biomeTimer;

	private int m_baseValue;

	private int m_comfortLevel;

	private float m_drownDamageTimer;

	private float m_timeSinceTargeted;

	private float m_timeSinceSensed;

	private float m_stealthFactorUpdateTimer;

	private float m_stealthFactor;

	private float m_stealthFactorTarget;

	private float m_wakeupTimer = -1f;

	private float m_timeSinceDeath = 999999f;

	private float m_runSkillImproveTimer;

	private float m_swimSkillImproveTimer;

	private float m_sneakSkillImproveTimer;

	private float m_equipmentMovementModifier;

	private readonly List<PieceTable> m_tempOwnedPieceTables = new List<PieceTable>();

	private readonly List<Transform> m_tempSnapPoints1 = new List<Transform>();

	private readonly List<Transform> m_tempSnapPoints2 = new List<Transform>();

	private readonly List<Piece> m_tempPieces = new List<Piece>();

	[HideInInspector]
	public Dictionary<string, string> m_customData = new Dictionary<string, string>();

	private static int s_attackMask = 0;

	private static readonly int s_crouching = ZSyncAnimation.GetHash("crouching");

	private static readonly int s_animatorTagDodge = ZSyncAnimation.GetHash("dodge");

	private static readonly int s_animatorTagCutscene = ZSyncAnimation.GetHash("cutscene");

	private static readonly int s_animatorTagCrouch = ZSyncAnimation.GetHash("crouch");

	private static readonly int s_animatorTagMinorAction = ZSyncAnimation.GetHash("minoraction");

	private static readonly int s_animatorTagMinorActionFast = ZSyncAnimation.GetHash("minoraction_fast");

	private static readonly int s_animatorTagEmote = ZSyncAnimation.GetHash("emote");

	private static readonly int s_statusEffectRested = "Rested".GetStableHashCode();

	private static readonly int s_statusEffectEncumbered = "Encumbered".GetStableHashCode();

	private static readonly int s_statusEffectSoftDeath = "SoftDeath".GetStableHashCode();

	private static readonly int s_statusEffectWet = "Wet".GetStableHashCode();

	private static readonly int s_statusEffectShelter = "Shelter".GetStableHashCode();

	private static readonly int s_statusEffectCampFire = "CampFire".GetStableHashCode();

	private static readonly int s_statusEffectResting = "Resting".GetStableHashCode();

	private static readonly int s_statusEffectCold = "Cold".GetStableHashCode();

	private static readonly int s_statusEffectFreezing = "Freezing".GetStableHashCode();

	private int m_cachedFrame;

	private bool m_cachedAttack;

	public enum RequirementMode
	{

		CanBuild,

		IsKnown,

		CanAlmostBuild
	}

	public class Food
	{

		public bool CanEatAgain()
		{
			return this.m_time < this.m_item.m_shared.m_foodBurnTime / 2f;
		}

		public string m_name = "";

		public ItemDrop.ItemData m_item;

		public float m_time;

		public float m_health;

		public float m_stamina;

		public float m_eitr;
	}

	public class MinorActionData
	{

		public Player.MinorActionData.ActionType m_type;

		public ItemDrop.ItemData m_item;

		public string m_progressText = "";

		public float m_time;

		public float m_duration;

		public string m_animation = "";

		public string m_doneAnimation = "";

		public float m_staminaDrain;

		public EffectList m_startEffect;

		public enum ActionType
		{

			Equip,

			Unequip,

			Reload
		}
	}

	private enum PlacementStatus
	{

		Valid,

		Invalid,

		BlockedbyPlayer,

		NoBuildZone,

		PrivateZone,

		MoreSpace,

		NoTeleportArea,

		ExtensionMissingStation,

		WrongBiome,

		NeedCultivated,

		NeedDirt,

		NotInDungeon
	}
}
