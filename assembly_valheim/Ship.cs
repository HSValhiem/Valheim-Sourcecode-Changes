using System;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
		this.m_body.maxDepenetrationVelocity = 2f;
		Heightmap.ForceGenerateAll();
		this.m_sailCloth = this.m_sailObject.GetComponentInChildren<Cloth>();
	}

	private void OnEnable()
	{
		Ship.Instances.Add(this);
	}

	private void OnDisable()
	{
		Ship.Instances.Remove(this);
	}

	public bool CanBeRemoved()
	{
		return this.m_players.Count == 0;
	}

	private void Start()
	{
		this.m_nview.Register("Stop", new Action<long>(this.RPC_Stop));
		this.m_nview.Register("Forward", new Action<long>(this.RPC_Forward));
		this.m_nview.Register("Backward", new Action<long>(this.RPC_Backward));
		this.m_nview.Register<float>("Rudder", new Action<long, float>(this.RPC_Rudder));
		base.InvokeRepeating("UpdateOwner", 2f, 2f);
	}

	private void PrintStats()
	{
		if (this.m_players.Count == 0)
		{
			return;
		}
		ZLog.Log("Vel:" + this.m_body.velocity.magnitude.ToString("0.0"));
	}

	public void ApplyControlls(Vector3 dir)
	{
		bool flag = (double)dir.z > 0.5;
		bool flag2 = (double)dir.z < -0.5;
		if (flag && !this.m_forwardPressed)
		{
			this.Forward();
		}
		if (flag2 && !this.m_backwardPressed)
		{
			this.Backward();
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = Mathf.Lerp(0.5f, 1f, Mathf.Abs(this.m_rudderValue));
		this.m_rudder = dir.x * num;
		this.m_rudderValue += this.m_rudder * this.m_rudderSpeed * fixedDeltaTime;
		this.m_rudderValue = Mathf.Clamp(this.m_rudderValue, -1f, 1f);
		if (Time.time - this.m_sendRudderTime > 0.2f)
		{
			this.m_sendRudderTime = Time.time;
			this.m_nview.InvokeRPC("Rudder", new object[] { this.m_rudderValue });
		}
		this.m_forwardPressed = flag;
		this.m_backwardPressed = flag2;
	}

	public void Forward()
	{
		this.m_nview.InvokeRPC("Forward", Array.Empty<object>());
	}

	public void Backward()
	{
		this.m_nview.InvokeRPC("Backward", Array.Empty<object>());
	}

	public void Rudder(float rudder)
	{
		this.m_nview.Invoke("Rudder", rudder);
	}

	private void RPC_Rudder(long sender, float value)
	{
		this.m_rudderValue = value;
	}

	public void Stop()
	{
		this.m_nview.InvokeRPC("Stop", Array.Empty<object>());
	}

	private void RPC_Stop(long sender)
	{
		this.m_speed = Ship.Speed.Stop;
	}

	private void RPC_Forward(long sender)
	{
		switch (this.m_speed)
		{
		case Ship.Speed.Stop:
			this.m_speed = Ship.Speed.Slow;
			return;
		case Ship.Speed.Back:
			this.m_speed = Ship.Speed.Stop;
			break;
		case Ship.Speed.Slow:
			this.m_speed = Ship.Speed.Half;
			return;
		case Ship.Speed.Half:
			this.m_speed = Ship.Speed.Full;
			return;
		case Ship.Speed.Full:
			break;
		default:
			return;
		}
	}

	private void RPC_Backward(long sender)
	{
		switch (this.m_speed)
		{
		case Ship.Speed.Stop:
			this.m_speed = Ship.Speed.Back;
			return;
		case Ship.Speed.Back:
			break;
		case Ship.Speed.Slow:
			this.m_speed = Ship.Speed.Stop;
			return;
		case Ship.Speed.Half:
			this.m_speed = Ship.Speed.Slow;
			return;
		case Ship.Speed.Full:
			this.m_speed = Ship.Speed.Half;
			break;
		default:
			return;
		}
	}

	public void CustomFixedUpdate()
	{
		bool flag = this.HaveControllingPlayer();
		this.UpdateControlls(Time.fixedDeltaTime);
		this.UpdateSail(Time.fixedDeltaTime);
		this.UpdateRudder(Time.fixedDeltaTime, flag);
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateUpsideDmg(Time.fixedDeltaTime);
		if (this.m_players.Count == 0)
		{
			this.m_speed = Ship.Speed.Stop;
			this.m_rudderValue = 0f;
		}
		if (!flag && (this.m_speed == Ship.Speed.Slow || this.m_speed == Ship.Speed.Back))
		{
			this.m_speed = Ship.Speed.Stop;
		}
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		Vector3 vector = this.m_floatCollider.transform.position + this.m_floatCollider.transform.forward * this.m_floatCollider.size.z / 2f;
		Vector3 vector2 = this.m_floatCollider.transform.position - this.m_floatCollider.transform.forward * this.m_floatCollider.size.z / 2f;
		Vector3 vector3 = this.m_floatCollider.transform.position - this.m_floatCollider.transform.right * this.m_floatCollider.size.x / 2f;
		Vector3 vector4 = this.m_floatCollider.transform.position + this.m_floatCollider.transform.right * this.m_floatCollider.size.x / 2f;
		float waterLevel = Floating.GetWaterLevel(worldCenterOfMass, ref this.m_previousCenter);
		float waterLevel2 = Floating.GetWaterLevel(vector3, ref this.m_previousLeft);
		float waterLevel3 = Floating.GetWaterLevel(vector4, ref this.m_previousRight);
		float waterLevel4 = Floating.GetWaterLevel(vector, ref this.m_previousForward);
		float waterLevel5 = Floating.GetWaterLevel(vector2, ref this.m_previousBack);
		float num = (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
		float num2 = worldCenterOfMass.y - num - this.m_waterLevelOffset;
		if (num2 > this.m_disableLevel)
		{
			return;
		}
		this.m_body.WakeUp();
		this.UpdateWaterForce(num2, Time.fixedDeltaTime);
		ref Vector3 ptr = new Vector3(vector3.x, waterLevel2, vector3.z);
		Vector3 vector5 = new Vector3(vector4.x, waterLevel3, vector4.z);
		ref Vector3 ptr2 = new Vector3(vector.x, waterLevel4, vector.z);
		Vector3 vector6 = new Vector3(vector2.x, waterLevel5, vector2.z);
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num3 = fixedDeltaTime * 50f;
		float num4 = Mathf.Clamp01(Mathf.Abs(num2) / this.m_forceDistance);
		Vector3 vector7 = Vector3.up * this.m_force * num4;
		this.m_body.AddForceAtPosition(vector7 * num3, worldCenterOfMass, ForceMode.VelocityChange);
		float num5 = Vector3.Dot(this.m_body.velocity, base.transform.forward);
		float num6 = Vector3.Dot(this.m_body.velocity, base.transform.right);
		Vector3 vector8 = this.m_body.velocity;
		float num7 = vector8.y * vector8.y * Mathf.Sign(vector8.y) * this.m_damping * num4;
		float num8 = num5 * num5 * Mathf.Sign(num5) * this.m_dampingForward * num4;
		float num9 = num6 * num6 * Mathf.Sign(num6) * this.m_dampingSideway * num4;
		vector8.y -= Mathf.Clamp(num7, -1f, 1f);
		vector8 -= base.transform.forward * Mathf.Clamp(num8, -1f, 1f);
		vector8 -= base.transform.right * Mathf.Clamp(num9, -1f, 1f);
		if (vector8.magnitude > this.m_body.velocity.magnitude)
		{
			vector8 = vector8.normalized * this.m_body.velocity.magnitude;
		}
		if (this.m_players.Count == 0)
		{
			vector8.x *= 0.1f;
			vector8.z *= 0.1f;
		}
		this.m_body.velocity = vector8;
		this.m_body.angularVelocity = this.m_body.angularVelocity - this.m_body.angularVelocity * this.m_angularDamping * num4;
		float num10 = 0.15f;
		float num11 = 0.5f;
		float num12 = Mathf.Clamp((ptr2.y - vector.y) * num10, -num11, num11);
		float num13 = Mathf.Clamp((vector6.y - vector2.y) * num10, -num11, num11);
		float num14 = Mathf.Clamp((ptr.y - vector3.y) * num10, -num11, num11);
		float num15 = Mathf.Clamp((vector5.y - vector4.y) * num10, -num11, num11);
		num12 = Mathf.Sign(num12) * Mathf.Abs(Mathf.Pow(num12, 2f));
		num13 = Mathf.Sign(num13) * Mathf.Abs(Mathf.Pow(num13, 2f));
		num14 = Mathf.Sign(num14) * Mathf.Abs(Mathf.Pow(num14, 2f));
		num15 = Mathf.Sign(num15) * Mathf.Abs(Mathf.Pow(num15, 2f));
		this.m_body.AddForceAtPosition(Vector3.up * num12 * num3, vector, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(Vector3.up * num13 * num3, vector2, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(Vector3.up * num14 * num3, vector3, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(Vector3.up * num15 * num3, vector4, ForceMode.VelocityChange);
		float num16 = 0f;
		if (this.m_speed == Ship.Speed.Full)
		{
			num16 = 1f;
		}
		else if (this.m_speed == Ship.Speed.Half)
		{
			num16 = 0.5f;
		}
		Vector3 sailForce = this.GetSailForce(num16, fixedDeltaTime);
		Vector3 vector9 = worldCenterOfMass + base.transform.up * this.m_sailForceOffset;
		this.m_body.AddForceAtPosition(sailForce, vector9, ForceMode.VelocityChange);
		Vector3 vector10 = base.transform.position + base.transform.forward * this.m_stearForceOffset;
		float num17 = num5 * this.m_stearVelForceFactor;
		this.m_body.AddForceAtPosition(base.transform.right * num17 * -this.m_rudderValue * fixedDeltaTime, vector10, ForceMode.VelocityChange);
		Vector3 vector11 = Vector3.zero;
		Ship.Speed speed = this.m_speed;
		if (speed != Ship.Speed.Back)
		{
			if (speed == Ship.Speed.Slow)
			{
				vector11 += base.transform.forward * this.m_backwardForce * (1f - Mathf.Abs(this.m_rudderValue));
			}
		}
		else
		{
			vector11 += -base.transform.forward * this.m_backwardForce * (1f - Mathf.Abs(this.m_rudderValue));
		}
		if (this.m_speed == Ship.Speed.Back || this.m_speed == Ship.Speed.Slow)
		{
			float num18 = (float)((this.m_speed == Ship.Speed.Back) ? (-1) : 1);
			vector11 += base.transform.right * this.m_stearForce * -this.m_rudderValue * num18;
		}
		this.m_body.AddForceAtPosition(vector11 * fixedDeltaTime, vector10, ForceMode.VelocityChange);
		this.ApplyEdgeForce(Time.fixedDeltaTime);
	}

	private void UpdateUpsideDmg(float dt)
	{
		if (base.transform.up.y >= 0f)
		{
			return;
		}
		this.m_upsideDownDmgTimer += dt;
		if (this.m_upsideDownDmgTimer <= this.m_upsideDownDmgInterval)
		{
			return;
		}
		this.m_upsideDownDmgTimer = 0f;
		IDestructible component = base.GetComponent<IDestructible>();
		if (component == null)
		{
			return;
		}
		HitData hitData = new HitData();
		hitData.m_damage.m_blunt = this.m_upsideDownDmg;
		hitData.m_point = base.transform.position;
		hitData.m_dir = Vector3.up;
		component.Damage(hitData);
	}

	private Vector3 GetSailForce(float sailSize, float dt)
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		float windIntensity = EnvMan.instance.GetWindIntensity();
		float num = Mathf.Lerp(0.25f, 1f, windIntensity);
		float num2 = this.GetWindAngleFactor();
		num2 *= num;
		Vector3 vector = Vector3.Normalize(windDir + base.transform.forward) * num2 * this.m_sailForceFactor * sailSize;
		this.m_sailForce = Vector3.SmoothDamp(this.m_sailForce, vector, ref this.m_windChangeVelocity, 1f, 99f);
		return this.m_sailForce;
	}

	public float GetWindAngleFactor()
	{
		float num = Vector3.Dot(EnvMan.instance.GetWindDir(), -base.transform.forward);
		float num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
		float num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
		return num2 * num3;
	}

	private void UpdateWaterForce(float depth, float dt)
	{
		if (this.m_lastDepth == -9999f)
		{
			this.m_lastDepth = depth;
			return;
		}
		float num = depth - this.m_lastDepth;
		this.m_lastDepth = depth;
		float num2 = num / dt;
		if (num2 > 0f)
		{
			return;
		}
		if (Mathf.Abs(num2) > this.m_minWaterImpactForce && Time.time - this.m_lastWaterImpactTime > this.m_minWaterImpactInterval)
		{
			this.m_lastWaterImpactTime = Time.time;
			this.m_waterImpactEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			if (this.m_players.Count > 0)
			{
				IDestructible component = base.GetComponent<IDestructible>();
				if (component != null)
				{
					HitData hitData = new HitData();
					hitData.m_damage.m_blunt = this.m_waterImpactDamage;
					hitData.m_point = base.transform.position;
					hitData.m_dir = Vector3.up;
					component.Damage(hitData);
				}
			}
		}
	}

	private void ApplyEdgeForce(float dt)
	{
		float magnitude = base.transform.position.magnitude;
		float num = 10420f;
		if (magnitude > num)
		{
			Vector3 vector = Vector3.Normalize(base.transform.position);
			float num2 = Utils.LerpStep(num, 10500f, magnitude) * 8f;
			Vector3 vector2 = vector * num2;
			this.m_body.AddForce(vector2 * dt, ForceMode.VelocityChange);
		}
	}

	private void UpdateControlls(float dt)
	{
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_forward, (int)this.m_speed, false);
			this.m_nview.GetZDO().Set(ZDOVars.s_rudder, this.m_rudderValue);
			return;
		}
		this.m_speed = (Ship.Speed)this.m_nview.GetZDO().GetInt(ZDOVars.s_forward, 0);
		if (Time.time - this.m_sendRudderTime > 1f)
		{
			this.m_rudderValue = this.m_nview.GetZDO().GetFloat(ZDOVars.s_rudder, 0f);
		}
	}

	public bool IsSailUp()
	{
		return this.m_speed == Ship.Speed.Half || this.m_speed == Ship.Speed.Full;
	}

	private void UpdateSail(float dt)
	{
		this.UpdateSailSize(dt);
		Vector3 vector = EnvMan.instance.GetWindDir();
		vector = Vector3.Cross(Vector3.Cross(vector, base.transform.up), base.transform.up);
		if (this.m_speed == Ship.Speed.Full || this.m_speed == Ship.Speed.Half)
		{
			float num = 0.5f + Vector3.Dot(base.transform.forward, vector) * 0.5f;
			Quaternion quaternion = Quaternion.LookRotation(-Vector3.Lerp(vector, Vector3.Normalize(vector - base.transform.forward), num), base.transform.up);
			this.m_mastObject.transform.rotation = Quaternion.RotateTowards(this.m_mastObject.transform.rotation, quaternion, 30f * dt);
			return;
		}
		if (this.m_speed == Ship.Speed.Back)
		{
			Quaternion quaternion2 = Quaternion.LookRotation(-base.transform.forward, base.transform.up);
			Quaternion quaternion3 = Quaternion.LookRotation(-vector, base.transform.up);
			quaternion3 = Quaternion.RotateTowards(quaternion2, quaternion3, 80f);
			this.m_mastObject.transform.rotation = Quaternion.RotateTowards(this.m_mastObject.transform.rotation, quaternion3, 30f * dt);
		}
	}

	private void UpdateRudder(float dt, bool haveControllingPlayer)
	{
		if (!this.m_rudderObject)
		{
			return;
		}
		Quaternion quaternion = Quaternion.Euler(0f, this.m_rudderRotationMax * -this.m_rudderValue, 0f);
		if (haveControllingPlayer)
		{
			if (this.m_speed == Ship.Speed.Slow)
			{
				this.m_rudderPaddleTimer += dt;
				quaternion *= Quaternion.Euler(0f, Mathf.Sin(this.m_rudderPaddleTimer * 6f) * 20f, 0f);
			}
			else if (this.m_speed == Ship.Speed.Back)
			{
				this.m_rudderPaddleTimer += dt;
				quaternion *= Quaternion.Euler(0f, Mathf.Sin(this.m_rudderPaddleTimer * -3f) * 40f, 0f);
			}
		}
		this.m_rudderObject.transform.localRotation = Quaternion.Slerp(this.m_rudderObject.transform.localRotation, quaternion, 0.5f);
	}

	private void UpdateSailSize(float dt)
	{
		float num = 0f;
		switch (this.m_speed)
		{
		case Ship.Speed.Stop:
			num = 0.1f;
			break;
		case Ship.Speed.Back:
			num = 0.1f;
			break;
		case Ship.Speed.Slow:
			num = 0.1f;
			break;
		case Ship.Speed.Half:
			num = 0.5f;
			break;
		case Ship.Speed.Full:
			num = 1f;
			break;
		}
		Vector3 localScale = this.m_sailObject.transform.localScale;
		bool flag = Mathf.Abs(localScale.y - num) < 0.01f;
		if (!flag)
		{
			localScale.y = Mathf.MoveTowards(localScale.y, num, dt);
			this.m_sailObject.transform.localScale = localScale;
		}
		if (this.m_sailCloth)
		{
			if (this.m_speed == Ship.Speed.Stop || this.m_speed == Ship.Speed.Slow || this.m_speed == Ship.Speed.Back)
			{
				if (flag && this.m_sailCloth.enabled)
				{
					this.m_sailCloth.enabled = false;
				}
			}
			else if (flag)
			{
				if (!this.m_sailWasInPosition)
				{
					this.m_sailCloth.enabled = false;
					this.m_sailCloth.enabled = true;
				}
			}
			else
			{
				this.m_sailCloth.enabled = true;
			}
		}
		this.m_sailWasInPosition = flag;
	}

	private void UpdateOwner()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (Player.m_localPlayer == null)
		{
			return;
		}
		if (this.m_players.Count > 0 && !this.IsPlayerInBoat(Player.m_localPlayer))
		{
			long owner = this.m_players[0].GetOwner();
			this.m_nview.GetZDO().SetOwner(owner);
			ZLog.Log("Changing ship owner to " + owner.ToString());
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component)
		{
			this.m_players.Add(component);
			ZLog.Log("Player onboard, total onboard " + this.m_players.Count.ToString());
			if (component == Player.m_localPlayer)
			{
				Ship.s_currentShips.Add(this);
			}
		}
		Character component2 = collider.GetComponent<Character>();
		if (component2)
		{
			Character character = component2;
			int inNumShipVolumes = character.InNumShipVolumes;
			character.InNumShipVolumes = inNumShipVolumes + 1;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component)
		{
			this.m_players.Remove(component);
			ZLog.Log("Player over board, players left " + this.m_players.Count.ToString());
			if (component == Player.m_localPlayer)
			{
				Ship.s_currentShips.Remove(this);
			}
		}
		Character component2 = collider.GetComponent<Character>();
		if (component2)
		{
			Character character = component2;
			int inNumShipVolumes = character.InNumShipVolumes;
			character.InNumShipVolumes = inNumShipVolumes - 1;
		}
	}

	public bool IsPlayerInBoat(long playerID)
	{
		using (List<Player>.Enumerator enumerator = this.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetPlayerID() == playerID)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsPlayerInBoat(Player player)
	{
		return this.m_players.Contains(player);
	}

	public bool HasPlayerOnboard()
	{
		return this.m_players.Count > 0;
	}

	private void OnDestroyed()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			Gogan.LogEvent("Game", "ShipDestroyed", base.gameObject.name, 0L);
		}
		Ship.s_currentShips.Remove(this);
	}

	public bool IsWindControllActive()
	{
		using (List<Player>.Enumerator enumerator = this.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.SailingPower))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Ship GetLocalShip()
	{
		if (Ship.s_currentShips.Count != 0)
		{
			return Ship.s_currentShips[Ship.s_currentShips.Count - 1];
		}
		return null;
	}

	private bool HaveControllingPlayer()
	{
		return this.m_players.Count != 0 && this.m_shipControlls.HaveValidUser();
	}

	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	public float GetSpeed()
	{
		return Vector3.Dot(this.m_body.velocity, base.transform.forward);
	}

	public Ship.Speed GetSpeedSetting()
	{
		return this.m_speed;
	}

	public float GetRudder()
	{
		return this.m_rudder;
	}

	public float GetRudderValue()
	{
		return this.m_rudderValue;
	}

	public float GetShipYawAngle()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return 0f;
		}
		return -Utils.YawFromDirection(mainCamera.transform.InverseTransformDirection(base.transform.forward));
	}

	public float GetWindAngle()
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		return -Utils.YawFromDirection(base.transform.InverseTransformDirection(windDir));
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.forward * this.m_stearForceOffset, 0.25f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.up * this.m_sailForceOffset, 0.25f);
	}

	public static List<Ship> Instances { get; } = new List<Ship>();

	private bool m_forwardPressed;

	private bool m_backwardPressed;

	private float m_sendRudderTime;

	[Header("Objects")]
	public GameObject m_sailObject;

	public GameObject m_mastObject;

	public GameObject m_rudderObject;

	public ShipControlls m_shipControlls;

	public Transform m_controlGuiPos;

	[Header("Misc")]
	public BoxCollider m_floatCollider;

	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_damping = 0.05f;

	public float m_dampingSideway = 0.05f;

	public float m_dampingForward = 0.01f;

	public float m_angularDamping = 0.01f;

	public float m_disableLevel = -0.5f;

	public float m_sailForceOffset;

	public float m_sailForceFactor = 0.1f;

	public float m_rudderSpeed = 0.5f;

	public float m_stearForceOffset = -10f;

	public float m_stearForce = 0.5f;

	public float m_stearVelForceFactor = 0.1f;

	public float m_backwardForce = 50f;

	public float m_rudderRotationMax = 30f;

	public float m_minWaterImpactForce = 2.5f;

	public float m_minWaterImpactInterval = 2f;

	public float m_waterImpactDamage = 10f;

	public float m_upsideDownDmgInterval = 1f;

	public float m_upsideDownDmg = 20f;

	public EffectList m_waterImpactEffect = new EffectList();

	private bool m_sailWasInPosition;

	private Vector3 m_windChangeVelocity = Vector3.zero;

	private Ship.Speed m_speed;

	private float m_rudder;

	private float m_rudderValue;

	private Vector3 m_sailForce = Vector3.zero;

	private readonly List<Player> m_players = new List<Player>();

	private WaterVolume m_previousCenter;

	private WaterVolume m_previousLeft;

	private WaterVolume m_previousRight;

	private WaterVolume m_previousForward;

	private WaterVolume m_previousBack;

	private static readonly List<Ship> s_currentShips = new List<Ship>();

	private Rigidbody m_body;

	private ZNetView m_nview;

	private Cloth m_sailCloth;

	private float m_lastDepth = -9999f;

	private float m_lastWaterImpactTime;

	private float m_upsideDownDmgTimer;

	private float m_rudderPaddleTimer;

	public enum Speed
	{

		Stop,

		Back,

		Slow,

		Half,

		Full
	}
}
