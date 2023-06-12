using System;
using UnityEngine;

public class EggHatch : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (UnityEngine.Random.value <= this.m_chanceToHatch)
		{
			base.InvokeRepeating("CheckSpawn", UnityEngine.Random.Range(1f, 2f), 1f);
		}
	}

	private void CheckSpawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_triggerDistance);
		if (closestPlayer && !closestPlayer.InGhostMode())
		{
			this.Hatch();
		}
	}

	private void Hatch()
	{
		this.m_hatchEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		UnityEngine.Object.Instantiate<GameObject>(this.m_spawnPrefab, base.transform.TransformPoint(this.m_spawnOffset), Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f));
		this.m_nview.Destroy();
	}

	public float m_triggerDistance = 5f;

	[Range(0f, 1f)]
	public float m_chanceToHatch = 1f;

	public Vector3 m_spawnOffset = new Vector3(0f, 0.5f, 0f);

	public GameObject m_spawnPrefab;

	public EffectList m_hatchEffect;

	private ZNetView m_nview;
}
