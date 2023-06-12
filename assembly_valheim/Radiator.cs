using System;
using System.Collections;
using UnityEngine;

public class Radiator : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
	}

	private void OnEnable()
	{
		base.StartCoroutine("UpdateLoop");
	}

	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(this.m_rateMin, this.m_rateMax));
			if (this.m_nview.IsValid() && this.m_nview.IsOwner())
			{
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				Vector3 vector = base.transform.position;
				if (onUnitSphere.y < 0f)
				{
					onUnitSphere.y = -onUnitSphere.y;
				}
				if (this.m_emitFrom)
				{
					vector = this.m_emitFrom.ClosestPoint(this.m_emitFrom.transform.position + onUnitSphere * 1000f) + onUnitSphere * this.m_offset;
				}
				UnityEngine.Object.Instantiate<GameObject>(this.m_projectile, vector, Quaternion.LookRotation(onUnitSphere, Vector3.up)).GetComponent<Projectile>().Setup(null, onUnitSphere * this.m_velocity, 0f, null, null, null);
			}
		}
		yield break;
	}

	public GameObject m_projectile;

	public Collider m_emitFrom;

	public float m_rateMin = 2f;

	public float m_rateMax = 5f;

	public float m_velocity = 10f;

	public float m_offset = 0.1f;

	private ZNetView m_nview;
}
