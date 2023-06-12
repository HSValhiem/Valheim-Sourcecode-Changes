using System;
using UnityEngine;

public class TeleportWorldTrigger : MonoBehaviour
{

	private void Awake()
	{
		this.m_teleportWorld = base.GetComponentInParent<TeleportWorld>();
	}

	private void OnTriggerEnter(Collider colliderIn)
	{
		Player component = colliderIn.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		ZLog.Log("Teleportation TRIGGER");
		this.m_teleportWorld.Teleport(component);
	}

	private TeleportWorld m_teleportWorld;
}
