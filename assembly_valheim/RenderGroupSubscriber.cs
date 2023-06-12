using System;
using UnityEngine;

public class RenderGroupSubscriber : MonoBehaviour
{

	private void OnEnable()
	{
		if (this.m_renderer == null)
		{
			this.m_renderer = base.GetComponent<MeshRenderer>();
		}
		if (this.m_renderer == null)
		{
			ZLog.LogError("RenderGroup script requires a mesh renderer!");
		}
		RenderGroupSystem.Register(this.m_group, new RenderGroupSystem.GroupChangedHandler(this.OnGroupChanged));
	}

	private void OnDisable()
	{
		RenderGroupSystem.Unregister(this.m_group, new RenderGroupSystem.GroupChangedHandler(this.OnGroupChanged));
	}

	private void OnGroupChanged(bool shouldRender)
	{
		this.m_renderer.enabled = shouldRender;
	}

	private MeshRenderer m_renderer;

	[SerializeField]
	public RenderGroup m_group;
}
