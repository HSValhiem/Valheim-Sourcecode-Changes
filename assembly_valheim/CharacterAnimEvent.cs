﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimEvent : MonoBehaviour
{

	private void Awake()
	{
		this.m_character = base.GetComponentInParent<Character>();
		this.m_nview = this.m_character.GetComponent<ZNetView>();
		this.m_animator = base.GetComponent<Animator>();
		this.m_monsterAI = this.m_character.GetComponent<MonsterAI>();
		this.m_visEquipment = this.m_character.GetComponent<VisEquipment>();
		this.m_footStep = this.m_character.GetComponent<FootStep>();
		this.m_head = this.m_animator.GetBoneTransform(HumanBodyBones.Head);
		this.m_headLookDir = this.m_character.transform.forward;
		if (CharacterAnimEvent.s_ikGroundMask == 0)
		{
			CharacterAnimEvent.s_ikGroundMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "terrain", "vehicle" });
		}
	}

	private void OnEnable()
	{
		CharacterAnimEvent.Instances.Add(this);
	}

	private void OnDisable()
	{
		CharacterAnimEvent.Instances.Remove(this);
	}

	private void OnAnimatorMove()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_character.AddRootMotion(this.m_animator.deltaPosition);
	}

	public void CustomFixedUpdate()
	{
		if (this.m_character == null)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_character.InAttack() && !this.m_character.InMinorAction() && !this.m_character.InEmote() && this.m_character.CanMove())
		{
			this.m_animator.speed = 1f;
		}
		this.UpdateFreezeFrame(Time.fixedDeltaTime);
	}

	public bool CanChain()
	{
		return this.m_chain;
	}

	public void FreezeFrame(float delay)
	{
		if (delay <= 0f)
		{
			return;
		}
		if (this.m_pauseTimer > 0f)
		{
			this.m_pauseTimer = delay;
			return;
		}
		this.m_pauseTimer = delay;
		this.m_pauseSpeed = this.m_animator.speed;
		this.m_animator.speed = 0.0001f;
		if (this.m_pauseSpeed <= 0.01f)
		{
			this.m_pauseSpeed = 1f;
		}
	}

	private void UpdateFreezeFrame(float dt)
	{
		if (this.m_pauseTimer > 0f)
		{
			this.m_pauseTimer -= dt;
			if (this.m_pauseTimer <= 0f)
			{
				this.m_animator.speed = this.m_pauseSpeed;
			}
		}
		if (this.m_animator.speed < 0.01f && this.m_pauseTimer <= 0f)
		{
			this.m_animator.speed = 1f;
		}
	}

	public void Speed(float speedScale)
	{
		this.m_animator.speed = speedScale;
	}

	public void Chain()
	{
		this.m_chain = true;
	}

	public void ResetChain()
	{
		this.m_chain = false;
	}

	public void FootStep(AnimationEvent e)
	{
		if ((double)e.animatorClipInfo.weight < 0.33)
		{
			return;
		}
		if (this.m_footStep)
		{
			if (e.stringParameter.Length > 0)
			{
				this.m_footStep.OnFoot(e.stringParameter);
				return;
			}
			this.m_footStep.OnFoot();
		}
	}

	public void Hit()
	{
		this.m_character.OnAttackTrigger();
	}

	public void OnAttackTrigger()
	{
		this.m_character.OnAttackTrigger();
	}

	public void Jump()
	{
		this.m_character.Jump(true);
	}

	public void Land()
	{
		if (this.m_character.IsFlying())
		{
			this.m_character.Land();
		}
	}

	public void TakeOff()
	{
		if (!this.m_character.IsFlying())
		{
			this.m_character.TakeOff();
		}
	}

	public void Stop(AnimationEvent e)
	{
		this.m_character.OnStopMoving();
	}

	public void DodgeMortal()
	{
		Player player = this.m_character as Player;
		if (player)
		{
			player.OnDodgeMortal();
		}
	}

	public void TrailOn()
	{
		if (this.m_visEquipment)
		{
			this.m_visEquipment.SetWeaponTrails(true);
		}
		this.m_character.OnWeaponTrailStart();
	}

	public void TrailOff()
	{
		if (this.m_visEquipment)
		{
			this.m_visEquipment.SetWeaponTrails(false);
		}
	}

	public void GPower()
	{
		Player player = this.m_character as Player;
		if (player)
		{
			player.ActivateGuardianPower();
		}
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateLookat();
		this.UpdateFootIK();
	}

	public void CustomLateUpdate()
	{
		this.UpdateHeadRotation(Time.deltaTime);
		if (this.m_femaleHack)
		{
			Character character = this.m_character;
			float num = ((this.m_visEquipment.GetModelIndex() == 1) ? this.m_femaleOffset : this.m_maleOffset);
			Vector3 localPosition = this.m_leftShoulder.localPosition;
			localPosition.x = -num;
			this.m_leftShoulder.localPosition = localPosition;
			Vector3 localPosition2 = this.m_rightShoulder.localPosition;
			localPosition2.x = num;
			this.m_rightShoulder.localPosition = localPosition2;
		}
	}

	private void UpdateLookat()
	{
		if (this.m_headRotation && this.m_head)
		{
			float num = this.m_lookWeight;
			if (this.m_headLookDir != Vector3.zero)
			{
				this.m_animator.SetLookAtPosition(this.m_head.position + this.m_headLookDir * 10f);
			}
			if (this.m_character.InAttack() || (!this.m_character.IsPlayer() && !this.m_character.CanMove()))
			{
				num = 0f;
			}
			this.m_lookAtWeight = Mathf.MoveTowards(this.m_lookAtWeight, num, Time.deltaTime);
			float num2 = (this.m_character.IsAttached() ? 0f : this.m_bodyLookWeight);
			this.m_animator.SetLookAtWeight(this.m_lookAtWeight, num2, this.m_headLookWeight, this.m_eyeLookWeight, this.m_lookClamp);
		}
	}

	private void UpdateFootIK()
	{
		if (!this.m_footIK)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(base.transform.position, mainCamera.transform.position) > 64f)
		{
			return;
		}
		if ((this.m_character.IsFlying() && !this.m_character.IsOnGround()) || (this.m_character.IsSwimming() && !this.m_character.IsOnGround()) || this.m_character.IsSitting())
		{
			for (int i = 0; i < this.m_feets.Length; i++)
			{
				CharacterAnimEvent.Foot foot = this.m_feets[i];
				this.m_animator.SetIKPositionWeight(foot.m_ikHandle, 0f);
				this.m_animator.SetIKRotationWeight(foot.m_ikHandle, 0f);
			}
			return;
		}
		bool flag = this.m_character.IsSitting();
		float deltaTime = Time.deltaTime;
		for (int j = 0; j < this.m_feets.Length; j++)
		{
			CharacterAnimEvent.Foot foot2 = this.m_feets[j];
			Vector3 position = foot2.m_transform.position;
			AvatarIKGoal ikHandle = foot2.m_ikHandle;
			float num = (this.m_useFeetValues ? foot2.m_footDownMax : this.m_footDownMax);
			float num2 = (this.m_useFeetValues ? foot2.m_footOffset : this.m_footOffset);
			float num3 = (this.m_useFeetValues ? foot2.m_footStepHeight : this.m_footStepHeight);
			float num4 = (this.m_useFeetValues ? foot2.m_stabalizeDistance : this.m_stabalizeDistance);
			if (flag)
			{
				num3 /= 4f;
			}
			Vector3 vector = base.transform.InverseTransformPoint(position - base.transform.up * num2);
			float num5 = 1f - Mathf.Clamp01(vector.y / num);
			foot2.m_ikWeight = Mathf.MoveTowards(foot2.m_ikWeight, num5, deltaTime * 10f);
			this.m_animator.SetIKPositionWeight(ikHandle, foot2.m_ikWeight);
			this.m_animator.SetIKRotationWeight(ikHandle, foot2.m_ikWeight * 0.5f);
			if (foot2.m_ikWeight > 0f)
			{
				RaycastHit raycastHit;
				if (Physics.Raycast(position + Vector3.up * num3, Vector3.down, out raycastHit, num3 * 4f, CharacterAnimEvent.s_ikGroundMask))
				{
					Vector3 vector2 = raycastHit.point + Vector3.up * num2;
					Vector3 vector3 = raycastHit.normal;
					if (num4 > 0f)
					{
						if (foot2.m_ikWeight >= 1f)
						{
							if (!foot2.m_isPlanted)
							{
								foot2.m_plantPosition = vector2;
								foot2.m_plantNormal = vector3;
								foot2.m_isPlanted = true;
							}
							else if (Vector3.Distance(foot2.m_plantPosition, vector2) > num4)
							{
								foot2.m_isPlanted = false;
							}
							else
							{
								vector2 = foot2.m_plantPosition;
								vector3 = foot2.m_plantNormal;
							}
						}
						else
						{
							foot2.m_isPlanted = false;
						}
					}
					this.m_animator.SetIKPosition(ikHandle, vector2);
					Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(this.m_animator.GetIKRotation(ikHandle) * Vector3.right, raycastHit.normal), raycastHit.normal);
					this.m_animator.SetIKRotation(ikHandle, quaternion);
				}
				else
				{
					foot2.m_ikWeight = Mathf.MoveTowards(foot2.m_ikWeight, 0f, deltaTime * 4f);
					this.m_animator.SetIKPositionWeight(ikHandle, foot2.m_ikWeight);
					this.m_animator.SetIKRotationWeight(ikHandle, foot2.m_ikWeight * 0.5f);
				}
			}
		}
	}

	private void UpdateHeadRotation(float dt)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_headRotation && this.m_head)
		{
			Vector3 lookFromPos = this.GetLookFromPos();
			Vector3 vector = Vector3.zero;
			if (this.m_nview.IsOwner())
			{
				if (this.m_monsterAI != null)
				{
					Character targetCreature = this.m_monsterAI.GetTargetCreature();
					if (targetCreature != null)
					{
						vector = targetCreature.GetEyePoint();
					}
				}
				else
				{
					vector = lookFromPos + this.m_character.GetLookDir() * 100f;
				}
				if (this.m_lookAt != null)
				{
					vector = this.m_lookAt.position;
				}
				this.m_sendTimer += Time.deltaTime;
				if (this.m_sendTimer > 0.2f)
				{
					this.m_sendTimer = 0f;
					this.m_nview.GetZDO().Set(ZDOVars.s_lookTarget, vector);
				}
			}
			else
			{
				vector = this.m_nview.GetZDO().GetVec3(ZDOVars.s_lookTarget, Vector3.zero);
			}
			if (vector != Vector3.zero)
			{
				Vector3 vector2 = Vector3.Normalize(vector - lookFromPos);
				this.m_headLookDir = Vector3.Lerp(this.m_headLookDir, vector2, 0.1f);
				return;
			}
			this.m_headLookDir = this.m_character.transform.forward;
		}
	}

	private Vector3 GetLookFromPos()
	{
		if (this.m_eyes != null && this.m_eyes.Length != 0)
		{
			Vector3 vector = Vector3.zero;
			foreach (Transform transform in this.m_eyes)
			{
				vector += transform.position;
			}
			return vector / (float)this.m_eyes.Length;
		}
		return this.m_head.position;
	}

	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		List<Transform> list = new List<Transform>();
		Transform transform = Utils.FindChild(base.transform, "LeftEye");
		Transform transform2 = Utils.FindChild(base.transform, "RightEye");
		if (transform)
		{
			list.Add(transform);
		}
		if (transform2)
		{
			list.Add(transform2);
		}
		this.m_eyes = list.ToArray();
		Transform transform3 = Utils.FindChild(base.transform, "LeftFootFront");
		Transform transform4 = Utils.FindChild(base.transform, "RightFootFront");
		Transform transform5 = Utils.FindChild(base.transform, "LeftFoot");
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "LeftFootBack");
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "l_foot");
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "Foot.l");
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "foot.l");
		}
		Transform transform6 = Utils.FindChild(base.transform, "RightFoot");
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "RightFootBack");
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "r_foot");
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "Foot.r");
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "foot.r");
		}
		List<CharacterAnimEvent.Foot> list2 = new List<CharacterAnimEvent.Foot>();
		if (transform3)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform3, AvatarIKGoal.LeftHand));
		}
		if (transform4)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform4, AvatarIKGoal.RightHand));
		}
		if (transform5)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform5, AvatarIKGoal.LeftFoot));
		}
		if (transform6)
		{
			list2.Add(new CharacterAnimEvent.Foot(transform6, AvatarIKGoal.RightFoot));
		}
		this.m_feets = list2.ToArray();
	}

	private void OnDrawGizmosSelected()
	{
		if (this.m_footIK)
		{
			foreach (CharacterAnimEvent.Foot foot in this.m_feets)
			{
				float num = (this.m_useFeetValues ? foot.m_footDownMax : this.m_footDownMax);
				float num2 = (this.m_useFeetValues ? foot.m_footOffset : this.m_footOffset);
				float num3 = (this.m_useFeetValues ? foot.m_footStepHeight : this.m_footStepHeight);
				float num4 = (this.m_useFeetValues ? foot.m_stabalizeDistance : this.m_stabalizeDistance);
				Vector3 vector = foot.m_transform.position - base.transform.up * num2;
				Gizmos.color = ((vector.y > base.transform.position.y) ? Color.red : Color.white);
				Gizmos.DrawWireSphere(vector, 0.1f);
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireCube(new Vector3(vector.x, base.transform.position.y, vector.z) + Vector3.up * num, new Vector3(1f, 0.01f, 1f));
				Gizmos.color = Color.red;
				Gizmos.DrawLine(vector, vector + Vector3.up * num3);
				if (num4 > 0f)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawWireSphere(vector, num4);
					Gizmos.matrix = Matrix4x4.identity;
				}
				if (foot.m_isPlanted)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireCube(vector, new Vector3(0.4f, 0.3f, 0.4f));
				}
			}
		}
	}

	public static List<CharacterAnimEvent> Instances { get; } = new List<CharacterAnimEvent>();

	[Header("Foot IK")]
	public bool m_footIK;

	public float m_footDownMax = 0.4f;

	public float m_footOffset = 0.1f;

	public float m_footStepHeight = 1f;

	public float m_stabalizeDistance;

	public bool m_useFeetValues;

	public CharacterAnimEvent.Foot[] m_feets = Array.Empty<CharacterAnimEvent.Foot>();

	[Header("Head/eye rotation")]
	public bool m_headRotation = true;

	public Transform[] m_eyes;

	public float m_lookWeight = 0.5f;

	public float m_bodyLookWeight = 0.1f;

	public float m_headLookWeight = 1f;

	public float m_eyeLookWeight;

	public float m_lookClamp = 0.5f;

	private const float m_headRotationSmoothness = 0.1f;

	public Transform m_lookAt;

	[Header("Player Female hack")]
	public bool m_femaleHack;

	public Transform m_leftShoulder;

	public Transform m_rightShoulder;

	public float m_femaleOffset = 0.0004f;

	public float m_maleOffset = 0.0007651657f;

	private Character m_character;

	private Animator m_animator;

	private ZNetView m_nview;

	private MonsterAI m_monsterAI;

	private VisEquipment m_visEquipment;

	private FootStep m_footStep;

	private float m_pauseTimer;

	private float m_pauseSpeed = 1f;

	private float m_sendTimer;

	private Vector3 m_headLookDir;

	private float m_lookAtWeight;

	private Transform m_head;

	private bool m_chain;

	private static int s_ikGroundMask = 0;

	[Serializable]
	public class Foot
	{

		public Foot(Transform t, AvatarIKGoal handle)
		{
			this.m_transform = t;
			this.m_ikHandle = handle;
			this.m_ikWeight = 0f;
		}

		public Transform m_transform;

		public AvatarIKGoal m_ikHandle;

		public float m_footDownMax = 0.4f;

		public float m_footOffset = 0.1f;

		public float m_footStepHeight = 1f;

		public float m_stabalizeDistance;

		[NonSerialized]
		public float m_ikWeight;

		[NonSerialized]
		public Vector3 m_plantPosition = Vector3.zero;

		[NonSerialized]
		public Vector3 m_plantNormal = Vector3.up;

		[NonSerialized]
		public bool m_isPlanted;
	}
}
