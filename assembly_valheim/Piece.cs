using System;
using System.Collections.Generic;
using UnityEngine;

public class Piece : StaticTarget
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		Piece.s_allPieces.Add(this);
		this.m_myListIndex = Piece.s_allPieces.Count - 1;
		if (this.m_comfort > 0)
		{
			Piece.s_allComfortPieces.Add(this);
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_creator = this.m_nview.GetZDO().GetLong(ZDOVars.s_creator, 0L);
		}
		if (Piece.s_ghostLayer == 0)
		{
			Piece.s_ghostLayer = LayerMask.NameToLayer("ghost");
		}
		if (Piece.s_pieceRayMask == 0)
		{
			Piece.s_pieceRayMask = LayerMask.GetMask(new string[] { "piece", "piece_nonsolid" });
		}
	}

	private void OnDestroy()
	{
		if (this.m_myListIndex >= 0)
		{
			Piece.s_allPieces[this.m_myListIndex] = Piece.s_allPieces[Piece.s_allPieces.Count - 1];
			Piece.s_allPieces[this.m_myListIndex].m_myListIndex = this.m_myListIndex;
			Piece.s_allPieces.RemoveAt(Piece.s_allPieces.Count - 1);
			this.m_myListIndex = -1;
		}
		if (this.m_comfort > 0)
		{
			Piece.s_allComfortPieces.Remove(this);
		}
	}

	public bool CanBeRemoved()
	{
		Container componentInChildren = base.GetComponentInChildren<Container>();
		if (componentInChildren != null)
		{
			return componentInChildren.CanBeRemoved();
		}
		Ship componentInChildren2 = base.GetComponentInChildren<Ship>();
		return !(componentInChildren2 != null) || componentInChildren2.CanBeRemoved();
	}

	public void DropResources()
	{
		Container container = null;
		foreach (Piece.Requirement requirement in this.m_resources)
		{
			if (!(requirement.m_resItem == null) && requirement.m_recover)
			{
				GameObject gameObject = requirement.m_resItem.gameObject;
				int j = requirement.m_amount;
				if (!this.IsPlacedByPlayer())
				{
					j = Mathf.Max(1, j / 3);
				}
				if (this.m_destroyedLootPrefab)
				{
					while (j > 0)
					{
						ItemDrop.ItemData itemData = gameObject.GetComponent<ItemDrop>().m_itemData.Clone();
						itemData.m_dropPrefab = gameObject;
						itemData.m_stack = Mathf.Min(j, itemData.m_shared.m_maxStackSize);
						j -= itemData.m_stack;
						if (container == null || !container.GetInventory().HaveEmptySlot())
						{
							container = UnityEngine.Object.Instantiate<GameObject>(this.m_destroyedLootPrefab, base.transform.position + Vector3.up, Quaternion.identity).GetComponent<Container>();
						}
						container.GetInventory().AddItem(itemData);
					}
				}
				else
				{
					while (j > 0)
					{
						ItemDrop component = UnityEngine.Object.Instantiate<GameObject>(gameObject, base.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
						component.SetStack(Mathf.Min(j, component.m_itemData.m_shared.m_maxStackSize));
						j -= component.m_itemData.m_stack;
					}
				}
			}
		}
	}

	public override bool IsPriorityTarget()
	{
		return base.IsPriorityTarget() && (this.m_targetNonPlayerBuilt || this.IsPlacedByPlayer());
	}

	public override bool IsRandomTarget()
	{
		return base.IsRandomTarget() && (this.m_targetNonPlayerBuilt || this.IsPlacedByPlayer());
	}

	public void SetCreator(long uid)
	{
		if (this.m_nview == null)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (this.GetCreator() != 0L)
			{
				return;
			}
			this.m_creator = uid;
			this.m_nview.GetZDO().Set(ZDOVars.s_creator, uid);
		}
	}

	public long GetCreator()
	{
		return this.m_creator;
	}

	public bool IsCreator()
	{
		long creator = this.GetCreator();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return creator == playerID;
	}

	public bool IsPlacedByPlayer()
	{
		return this.GetCreator() != 0L;
	}

	public void SetInvalidPlacementHeightlight(bool enabled)
	{
		if ((enabled && this.m_invalidPlacementMaterials != null) || (!enabled && this.m_invalidPlacementMaterials == null))
		{
			return;
		}
		Renderer[] componentsInChildren = base.GetComponentsInChildren<Renderer>();
		if (enabled)
		{
			this.m_invalidPlacementMaterials = new List<KeyValuePair<Renderer, Material[]>>();
			foreach (Renderer renderer in componentsInChildren)
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				this.m_invalidPlacementMaterials.Add(new KeyValuePair<Renderer, Material[]>(renderer, sharedMaterials));
			}
			Renderer[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				foreach (Material material in array[i].materials)
				{
					if (material.HasProperty("_EmissionColor"))
					{
						material.SetColor("_EmissionColor", Color.red * 0.7f);
					}
					material.color = Color.red;
				}
			}
			return;
		}
		foreach (KeyValuePair<Renderer, Material[]> keyValuePair in this.m_invalidPlacementMaterials)
		{
			if (keyValuePair.Key)
			{
				keyValuePair.Key.materials = keyValuePair.Value;
			}
		}
		this.m_invalidPlacementMaterials = null;
	}

	public static void GetSnapPoints(Vector3 point, float radius, List<Transform> points, List<Piece> pieces)
	{
		int num = Physics.OverlapSphereNonAlloc(point, radius, Piece.s_pieceColliders, Piece.s_pieceRayMask);
		for (int i = 0; i < num; i++)
		{
			Piece componentInParent = Piece.s_pieceColliders[i].GetComponentInParent<Piece>();
			if (componentInParent != null)
			{
				componentInParent.GetSnapPoints(points);
				pieces.Add(componentInParent);
			}
		}
	}

	public static void GetAllComfortPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		foreach (Piece piece in Piece.s_allComfortPieces)
		{
			if (piece.gameObject.layer != Piece.s_ghostLayer && Vector3.Distance(p, piece.transform.position) < radius)
			{
				pieces.Add(piece);
			}
		}
	}

	public void GetSnapPoints(List<Transform> points)
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (child.CompareTag("snappoint"))
			{
				points.Add(child);
			}
		}
	}

	public int GetComfort()
	{
		if (this.m_comfortObject != null && !this.m_comfortObject.activeInHierarchy)
		{
			return 0;
		}
		return this.m_comfort;
	}

	public bool m_targetNonPlayerBuilt = true;

	[Header("Basic stuffs")]
	public Sprite m_icon;

	public string m_name = "";

	public string m_description = "";

	public bool m_enabled = true;

	public Piece.PieceCategory m_category;

	public bool m_isUpgrade;

	[Header("Comfort")]
	public int m_comfort;

	public Piece.ComfortGroup m_comfortGroup;

	public GameObject m_comfortObject;

	[Header("Placement rules")]
	public bool m_groundPiece;

	public bool m_allowAltGroundPlacement;

	public bool m_groundOnly;

	public bool m_cultivatedGroundOnly;

	public bool m_waterPiece;

	public bool m_clipGround;

	public bool m_clipEverything;

	public bool m_noInWater;

	public bool m_notOnWood;

	public bool m_notOnTiltingSurface;

	public bool m_inCeilingOnly;

	public bool m_notOnFloor;

	public bool m_noClipping;

	public bool m_onlyInTeleportArea;

	public bool m_allowedInDungeons;

	public float m_spaceRequirement;

	public bool m_repairPiece;

	public bool m_canBeRemoved = true;

	public bool m_allowRotatedOverlap;

	public bool m_vegetationGroundOnly;

	public List<Piece> m_blockingPieces = new List<Piece>();

	public float m_blockRadius;

	public ZNetView m_mustConnectTo;

	public float m_connectRadius;

	public bool m_mustBeAboveConnected;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_onlyInBiome;

	[Header("Effects")]
	public EffectList m_placeEffect = new EffectList();

	[Header("Requirements")]
	public string m_dlc = "";

	public CraftingStation m_craftingStation;

	public Piece.Requirement[] m_resources = Array.Empty<Piece.Requirement>();

	public GameObject m_destroyedLootPrefab;

	private ZNetView m_nview;

	private List<KeyValuePair<Renderer, Material[]>> m_invalidPlacementMaterials;

	private long m_creator;

	private int m_myListIndex = -1;

	private static int s_ghostLayer = 0;

	private static int s_pieceRayMask = 0;

	private static readonly Collider[] s_pieceColliders = new Collider[2000];

	private static readonly List<Piece> s_allPieces = new List<Piece>();

	private static readonly HashSet<Piece> s_allComfortPieces = new HashSet<Piece>();

	public enum PieceCategory
	{

		Misc,

		Crafting,

		Building,

		Furniture,

		Max,

		All = 100
	}

	public enum ComfortGroup
	{

		None,

		Fire,

		Bed,

		Banner,

		Chair,

		Table,

		Carpet
	}

	[Serializable]
	public class Requirement
	{

		public int GetAmount(int qualityLevel)
		{
			if (qualityLevel <= 1)
			{
				return this.m_amount;
			}
			return (qualityLevel - 1) * this.m_amountPerLevel;
		}

		[Header("Resource")]
		public ItemDrop m_resItem;

		public int m_amount = 1;

		public int m_extraAmountOnlyOneIngredient;

		[Header("Item")]
		public int m_amountPerLevel = 1;

		[Header("Piece")]
		public bool m_recover = true;
	}
}
