using System;
using UnityEngine;

public class MovementDamage : MonoBehaviour
{

	private void Awake()
	{
		this.m_character = base.GetComponent<Character>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		Aoe component = this.m_runDamageObject.GetComponent<Aoe>();
		if (component)
		{
			component.Setup(this.m_character, Vector3.zero, 0f, null, null, null);
		}
	}

	private void Update()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			this.m_runDamageObject.SetActive(false);
			return;
		}
		bool flag = this.m_body.velocity.magnitude > this.m_speedTreshold;
		this.m_runDamageObject.SetActive(flag);
	}

	public GameObject m_runDamageObject;

	public float m_speedTreshold = 6f;

	private Character m_character;

	private ZNetView m_nview;

	private Rigidbody m_body;
}
