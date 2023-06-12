using System;
using UnityEngine;

public class WeaponLoadState : MonoBehaviour
{

	private void Start()
	{
		this.m_owner = base.GetComponentInParent<Player>();
	}

	private void Update()
	{
		if (this.m_owner)
		{
			bool flag = this.m_owner.IsWeaponLoaded();
			this.m_unloaded.SetActive(!flag);
			this.m_loaded.SetActive(flag);
		}
	}

	public GameObject m_unloaded;

	public GameObject m_loaded;

	private Player m_owner;
}
