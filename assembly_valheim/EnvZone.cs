using System;
using UnityEngine;

public class EnvZone : MonoBehaviour
{

	private void Awake()
	{
		if (this.m_exteriorMesh)
		{
			this.m_exteriorMesh.forceRenderingOff = true;
		}
	}

	private void OnTriggerStay(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		if (this.m_force && string.IsNullOrEmpty(EnvMan.instance.m_debugEnv))
		{
			EnvMan.instance.SetForceEnvironment(this.m_environment);
		}
		EnvZone.s_triggered = this;
		if (this.m_exteriorMesh)
		{
			this.m_exteriorMesh.forceRenderingOff = false;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (EnvZone.s_triggered != this)
		{
			return;
		}
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		if (this.m_force)
		{
			EnvMan.instance.SetForceEnvironment("");
		}
		EnvZone.s_triggered = null;
	}

	public static string GetEnvironment()
	{
		if (EnvZone.s_triggered && !EnvZone.s_triggered.m_force)
		{
			return EnvZone.s_triggered.m_environment;
		}
		return null;
	}

	private void Update()
	{
		if (this.m_exteriorMesh)
		{
			this.m_exteriorMesh.forceRenderingOff = EnvZone.s_triggered != this;
		}
	}

	public string m_environment = "";

	public bool m_force = true;

	public MeshRenderer m_exteriorMesh;

	private static EnvZone s_triggered;
}
