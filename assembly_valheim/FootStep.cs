using System;
using System.Collections.Generic;
using UnityEngine;

public class FootStep : MonoBehaviour
{

	private void Start()
	{
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_character = base.GetComponent<Character>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_footstep = this.m_animator.GetFloat(FootStep.s_footstepID);
		if (this.m_pieceLayer == 0)
		{
			this.m_pieceLayer = LayerMask.NameToLayer("piece");
		}
		Character character = this.m_character;
		character.m_onLand = (Action<Vector3>)Delegate.Combine(character.m_onLand, new Action<Vector3>(this.OnLand));
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register<int, Vector3>("Step", new Action<long, int, Vector3>(this.RPC_Step));
		}
	}

	private void OnEnable()
	{
		FootStep.Instances.Add(this);
	}

	private void OnDisable()
	{
		FootStep.Instances.Remove(this);
	}

	public void CustomUpdate(float dt)
	{
		if (this.m_nview == null || !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateFootstep(dt);
	}

	private void UpdateFootstep(float dt)
	{
		if (this.m_feet.Length == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(base.transform.position, mainCamera.transform.position) > this.m_footstepCullDistance)
		{
			return;
		}
		this.UpdateFootstepCurveTrigger(dt);
	}

	private void UpdateFootstepCurveTrigger(float dt)
	{
		this.m_footstepTimer += dt;
		float @float = this.m_animator.GetFloat(FootStep.s_footstepID);
		if (Utils.SignDiffers(@float, this.m_footstep) && Mathf.Max(Mathf.Abs(this.m_animator.GetFloat(FootStep.s_forwardSpeedID)), Mathf.Abs(this.m_animator.GetFloat(FootStep.s_sidewaySpeedID))) > 0.2f && this.m_footstepTimer > 0.2f)
		{
			this.m_footstepTimer = 0f;
			this.OnFoot();
		}
		this.m_footstep = @float;
	}

	private Transform FindActiveFoot()
	{
		Transform transform = null;
		float num = 9999f;
		Vector3 forward = base.transform.forward;
		foreach (Transform transform2 in this.m_feet)
		{
			if (!(transform2 == null))
			{
				Vector3 vector = transform2.position - base.transform.position;
				float num2 = Vector3.Dot(forward, vector);
				if (num2 > num || transform == null)
				{
					transform = transform2;
					num = num2;
				}
			}
		}
		return transform;
	}

	private Transform FindFoot(string name)
	{
		foreach (Transform transform in this.m_feet)
		{
			if (transform.gameObject.name == name)
			{
				return transform;
			}
		}
		return null;
	}

	public void OnFoot()
	{
		Transform transform = this.FindActiveFoot();
		this.OnFoot(transform);
	}

	public void OnFoot(string name)
	{
		Transform transform = this.FindFoot(name);
		if (transform == null)
		{
			ZLog.LogWarning("FAiled to find foot:" + name);
			return;
		}
		this.OnFoot(transform);
	}

	private void OnLand(Vector3 point)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, point);
		int num = this.FindBestStepEffect(groundMaterial, FootStep.MotionType.Land);
		if (num != -1)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[] { num, point });
		}
	}

	private void OnFoot(Transform foot)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		Vector3 vector = ((foot != null) ? foot.position : base.transform.position);
		FootStep.MotionType motionType = FootStep.GetMotionType(this.m_character);
		FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, vector);
		int num = this.FindBestStepEffect(groundMaterial, motionType);
		if (num != -1)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[] { num, vector });
		}
	}

	private static void PurgeOldEffects()
	{
		while (FootStep.s_stepInstances.Count > 30)
		{
			GameObject gameObject = FootStep.s_stepInstances.Dequeue();
			if (gameObject)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	private void DoEffect(FootStep.StepEffect effect, Vector3 point)
	{
		foreach (GameObject gameObject in effect.m_effectPrefabs)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, point, base.transform.rotation);
			FootStep.s_stepInstances.Enqueue(gameObject2);
			if (gameObject2.GetComponent<ZNetView>() != null)
			{
				ZLog.LogWarning(string.Concat(new string[]
				{
					"Foot step effect ",
					effect.m_name,
					" prefab ",
					gameObject.name,
					" in ",
					this.m_character.gameObject.name,
					" should not contain a ZNetView component"
				}));
			}
		}
		FootStep.PurgeOldEffects();
	}

	private void RPC_Step(long sender, int effectIndex, Vector3 point)
	{
		FootStep.StepEffect stepEffect = this.m_effects[effectIndex];
		this.DoEffect(stepEffect, point);
	}

	private static FootStep.MotionType GetMotionType(Character character)
	{
		if (character.IsWalking())
		{
			return FootStep.MotionType.Walk;
		}
		if (character.IsSwimming())
		{
			return FootStep.MotionType.Swimming;
		}
		if (character.IsWallRunning())
		{
			return FootStep.MotionType.Climbing;
		}
		if (character.IsRunning())
		{
			return FootStep.MotionType.Run;
		}
		if (character.IsSneaking())
		{
			return FootStep.MotionType.Sneak;
		}
		return FootStep.MotionType.Jog;
	}

	private FootStep.GroundMaterial GetGroundMaterial(Character character, Vector3 point)
	{
		if (character.InWater())
		{
			return FootStep.GroundMaterial.Water;
		}
		if (character.InLiquid())
		{
			return FootStep.GroundMaterial.Tar;
		}
		if (!character.IsOnGround())
		{
			return FootStep.GroundMaterial.None;
		}
		Collider lastGroundCollider = character.GetLastGroundCollider();
		if (lastGroundCollider == null)
		{
			return FootStep.GroundMaterial.Default;
		}
		Heightmap component = lastGroundCollider.GetComponent<Heightmap>();
		if (component != null)
		{
			float num = Mathf.Acos(Mathf.Clamp01(character.GetLastGroundNormal().y)) * 57.29578f;
			Heightmap.Biome biome = component.GetBiome(point);
			if (biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth)
			{
				if (num < 40f && !component.IsCleared(point))
				{
					return FootStep.GroundMaterial.Snow;
				}
			}
			else if (biome == Heightmap.Biome.Swamp)
			{
				if (num < 40f)
				{
					return FootStep.GroundMaterial.Mud;
				}
			}
			else if ((biome == Heightmap.Biome.Meadows || biome == Heightmap.Biome.BlackForest) && num < 25f)
			{
				return FootStep.GroundMaterial.Grass;
			}
			return FootStep.GroundMaterial.GenericGround;
		}
		if (lastGroundCollider.gameObject.layer != this.m_pieceLayer)
		{
			return FootStep.GroundMaterial.Default;
		}
		WearNTear componentInParent = lastGroundCollider.GetComponentInParent<WearNTear>();
		if (!componentInParent)
		{
			return FootStep.GroundMaterial.Default;
		}
		switch (componentInParent.m_materialType)
		{
		case WearNTear.MaterialType.Wood:
			return FootStep.GroundMaterial.Wood;
		case WearNTear.MaterialType.Stone:
		case WearNTear.MaterialType.Marble:
			return FootStep.GroundMaterial.Stone;
		case WearNTear.MaterialType.Iron:
			return FootStep.GroundMaterial.Metal;
		case WearNTear.MaterialType.HardWood:
			return FootStep.GroundMaterial.Wood;
		default:
			return FootStep.GroundMaterial.Default;
		}
	}

	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		Transform transform = Utils.FindChild(base.transform, "LeftFootFront");
		Transform transform2 = Utils.FindChild(base.transform, "RightFootFront");
		Transform transform3 = Utils.FindChild(base.transform, "LeftFoot");
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "LeftFootBack");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "l_foot");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "Foot.l");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "foot.l");
		}
		Transform transform4 = Utils.FindChild(base.transform, "RightFoot");
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "RightFootBack");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "r_foot");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "Foot.r");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "foot.r");
		}
		List<Transform> list = new List<Transform>();
		if (transform)
		{
			list.Add(transform);
		}
		if (transform2)
		{
			list.Add(transform2);
		}
		if (transform3)
		{
			list.Add(transform3);
		}
		if (transform4)
		{
			list.Add(transform4);
		}
		this.m_feet = list.ToArray();
	}

	private int FindBestStepEffect(FootStep.GroundMaterial material, FootStep.MotionType motion)
	{
		FootStep.StepEffect stepEffect = null;
		int num = -1;
		for (int i = 0; i < this.m_effects.Count; i++)
		{
			FootStep.StepEffect stepEffect2 = this.m_effects[i];
			if (((stepEffect2.m_material & material) != FootStep.GroundMaterial.None || (stepEffect == null && (stepEffect2.m_material & FootStep.GroundMaterial.Default) != FootStep.GroundMaterial.None)) && (stepEffect2.m_motionType & motion) != (FootStep.MotionType)0)
			{
				stepEffect = stepEffect2;
				num = i;
			}
		}
		return num;
	}

	public static List<FootStep> Instances { get; } = new List<FootStep>();

	public float m_footstepCullDistance = 20f;

	public List<FootStep.StepEffect> m_effects = new List<FootStep.StepEffect>();

	public Transform[] m_feet = Array.Empty<Transform>();

	private static readonly int s_footstepID = ZSyncAnimation.GetHash("footstep");

	private static readonly int s_forwardSpeedID = ZSyncAnimation.GetHash("forward_speed");

	private static readonly int s_sidewaySpeedID = ZSyncAnimation.GetHash("sideway_speed");

	private static readonly Queue<GameObject> s_stepInstances = new Queue<GameObject>();

	private float m_footstep;

	private float m_footstepTimer;

	private int m_pieceLayer;

	private const float c_MinFootstepInterval = 0.2f;

	private const int c_MaxFootstepInstances = 30;

	private Animator m_animator;

	private Character m_character;

	private ZNetView m_nview;

	[Flags]
	public enum MotionType
	{

		Jog = 1,

		Run = 2,

		Sneak = 4,

		Climbing = 8,

		Swimming = 16,

		Land = 32,

		Walk = 64
	}

	[Flags]
	public enum GroundMaterial
	{

		None = 0,

		Default = 1,

		Water = 2,

		Stone = 4,

		Wood = 8,

		Snow = 16,

		Mud = 32,

		Grass = 64,

		GenericGround = 128,

		Metal = 256,

		Tar = 512
	}

	[Serializable]
	public class StepEffect
	{

		public string m_name = "";

		[BitMask(typeof(FootStep.MotionType))]
		public FootStep.MotionType m_motionType = FootStep.MotionType.Jog;

		[BitMask(typeof(FootStep.GroundMaterial))]
		public FootStep.GroundMaterial m_material = FootStep.GroundMaterial.Default;

		public GameObject[] m_effectPrefabs = Array.Empty<GameObject>();
	}
}
