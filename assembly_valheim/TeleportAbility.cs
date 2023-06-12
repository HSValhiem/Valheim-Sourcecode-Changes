using System;
using System.Collections.Generic;
using UnityEngine;

public class TeleportAbility : MonoBehaviour, IProjectile
{

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		GameObject gameObject = this.FindTarget();
		if (gameObject)
		{
			Vector3 position = gameObject.transform.position;
			if (ZoneSystem.instance.FindFloor(position, out position.y))
			{
				this.m_owner.transform.position = position;
				this.m_owner.transform.rotation = gameObject.transform.rotation;
				if (this.m_message.Length > 0)
				{
					Player.MessageAllInRange(base.transform.position, 100f, MessageHud.MessageType.Center, this.m_message, null);
				}
			}
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private GameObject FindTarget()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag(this.m_targetTag);
		List<GameObject> list = new List<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (Vector3.Distance(gameObject.transform.position, this.m_owner.transform.position) <= this.m_maxTeleportRange)
			{
				list.Add(gameObject);
			}
		}
		if (list.Count == 0)
		{
			ZLog.Log("No valid telport target in range");
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	public string m_targetTag = "";

	public string m_message = "";

	public float m_maxTeleportRange = 100f;

	private Character m_owner;
}
