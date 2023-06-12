using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

	private void Awake()
	{
		this.m_character = base.GetComponent<Player>();
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		PlayerController.m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_gamepadSens = PlayerPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens);
		PlayerController.m_invertMouse = PlayerPrefs.GetInt("InvertMouse", 0) == 1;
	}

	private void FixedUpdate()
	{
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.TakeInput())
		{
			this.m_character.SetControls(Vector3.zero, false, false, false, false, false, false, false, false, false, false, false);
			return;
		}
		bool flag = this.InInventoryEtc();
		bool flag2 = Hud.IsPieceSelectionVisible();
		bool flag3 = (ZInput.GetButton("SecondaryAttack") || ZInput.GetButton("JoySecondaryAttack")) && !flag;
		Vector3 zero = Vector3.zero;
		if (ZInput.GetButton("Forward"))
		{
			zero.z += 1f;
		}
		if (ZInput.GetButton("Backward"))
		{
			zero.z -= 1f;
		}
		if (ZInput.GetButton("Left"))
		{
			zero.x -= 1f;
		}
		if (ZInput.GetButton("Right"))
		{
			zero.x += 1f;
		}
		if (!flag3)
		{
			zero.x += ZInput.GetJoyLeftStickX(false);
			zero.z += -ZInput.GetJoyLeftStickY(true);
		}
		if (zero.magnitude > 1f)
		{
			zero.Normalize();
		}
		bool flag4 = (ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")) && !flag;
		bool flag5 = flag4;
		bool flag6 = flag4 && !this.m_attackWasPressed;
		this.m_attackWasPressed = flag4;
		bool flag7 = flag3;
		bool flag8 = flag3 && !this.m_secondAttackWasPressed;
		this.m_secondAttackWasPressed = flag3;
		bool flag9 = (ZInput.GetButton("Block") || ZInput.GetButton("JoyBlock")) && !flag;
		bool flag10 = flag9;
		bool flag11 = flag9 && !this.m_blockWasPressed;
		this.m_blockWasPressed = flag9;
		bool button = ZInput.GetButton("Jump");
		bool flag12 = (button && !this.m_lastJump) || (ZInput.GetButtonDown("JoyJump") && !flag2 && !flag);
		this.m_lastJump = button;
		bool flag13 = ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive() && ZInput.GetButtonDown("JoyDodge") && !flag;
		bool flag14 = InventoryGui.IsVisible();
		bool flag15 = (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch")) && !flag14;
		bool flag16 = flag15 && !this.m_lastCrouch;
		this.m_lastCrouch = flag15;
		if (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive())
		{
			this.m_run = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
		}
		else
		{
			float magnitude = zero.magnitude;
			if ((this.m_run && magnitude < 0.05f && this.m_lastMagnitude < 0.05f) || this.m_character.GetStamina() <= 0f)
			{
				this.m_run = false;
			}
			bool button2 = ZInput.GetButton("JoyRun");
			if (button2 && !this.m_lastRunPressed)
			{
				this.m_run = !this.m_run;
			}
			this.m_lastRunPressed = button2;
			this.m_lastMagnitude = magnitude;
		}
		bool button3 = ZInput.GetButton("AutoRun");
		this.m_character.SetControls(zero, flag6, flag5, flag8, flag7, flag11, flag10, flag12, flag16, this.m_run, button3, flag13);
	}

	private static bool DetectTap(bool pressed, float dt, float minPressTime, bool run, ref float pressTimer, ref float releasedTimer, ref bool tapPressed)
	{
		bool flag = false;
		if (pressed)
		{
			if ((releasedTimer > 0f && releasedTimer < minPressTime) & tapPressed)
			{
				tapPressed = false;
				flag = true;
			}
			pressTimer += dt;
			releasedTimer = 0f;
		}
		else
		{
			if (pressTimer > 0f)
			{
				tapPressed = pressTimer < minPressTime;
				if (run & tapPressed)
				{
					tapPressed = false;
					flag = true;
				}
			}
			releasedTimer += dt;
			pressTimer = 0f;
		}
		return flag;
	}

	private bool TakeInput()
	{
		return !GameCamera.InFreeFly() && ((!Chat.instance || !Chat.instance.IsTakingInput()) && !Menu.IsVisible() && !global::Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && (!ZInput.IsGamepadActive() || !Minimap.IsOpen()) && (!ZInput.IsGamepadActive() || !InventoryGui.IsVisible()) && (!ZInput.IsGamepadActive() || !StoreGui.IsVisible())) && (!ZInput.IsGamepadActive() || !Hud.IsPieceSelectionVisible());
	}

	private bool InInventoryEtc()
	{
		return InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible();
	}

	private void LateUpdate()
	{
		if (!this.TakeInput() || this.InInventoryEtc())
		{
			this.m_character.SetMouseLook(Vector2.zero);
			return;
		}
		Vector2 zero = Vector2.zero;
		zero.x = Input.GetAxis("Mouse X") * PlayerController.m_mouseSens;
		zero.y = Input.GetAxis("Mouse Y") * PlayerController.m_mouseSens;
		if (!this.m_character.InPlaceMode() || !ZInput.GetButton("JoyRotate"))
		{
			zero.x += ZInput.GetJoyRightStickX() * 110f * Time.deltaTime * PlayerController.m_gamepadSens;
			zero.y += -ZInput.GetJoyRightStickY() * 110f * Time.deltaTime * PlayerController.m_gamepadSens;
		}
		if (PlayerController.m_invertMouse)
		{
			zero.y *= -1f;
		}
		this.m_character.SetMouseLook(zero);
	}

	private bool m_run;

	private bool m_lastRunPressed;

	private float m_lastMagnitude;

	private Player m_character;

	private ZNetView m_nview;

	public static float m_mouseSens = 1f;

	public static float m_gamepadSens = 1f;

	public static bool m_invertMouse = false;

	public float m_minDodgeTime = 0.2f;

	private bool m_attackWasPressed;

	private bool m_secondAttackWasPressed;

	private bool m_blockWasPressed;

	private bool m_lastJump;

	private bool m_lastCrouch;
}
