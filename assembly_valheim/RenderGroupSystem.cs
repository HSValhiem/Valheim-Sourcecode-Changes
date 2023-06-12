using System;
using System.Collections.Generic;
using UnityEngine;

public class RenderGroupSystem : MonoBehaviour
{

	private void Awake()
	{
		if (RenderGroupSystem.s_instance != null)
		{
			ZLog.LogError("Instance already set!");
			return;
		}
		RenderGroupSystem.s_instance = this;
		foreach (object obj in Enum.GetValues(typeof(RenderGroup)))
		{
			RenderGroup renderGroup = (RenderGroup)obj;
			this.m_renderGroups.Add(renderGroup, new RenderGroupSystem.RenderGroupState());
		}
	}

	private void OnDestroy()
	{
		if (RenderGroupSystem.s_instance == this)
		{
			RenderGroupSystem.s_instance = null;
		}
	}

	private void LateUpdate()
	{
		bool flag = Player.m_localPlayer != null && Player.m_localPlayer.InInterior();
		this.m_renderGroups[RenderGroup.Always].Active = true;
		this.m_renderGroups[RenderGroup.Overworld].Active = !flag;
		this.m_renderGroups[RenderGroup.Interior].Active = flag;
	}

	public static void Register(RenderGroup group, RenderGroupSystem.GroupChangedHandler subscriber)
	{
		RenderGroupSystem.RenderGroupState renderGroupState = RenderGroupSystem.s_instance.m_renderGroups[group];
		renderGroupState.GroupChanged += subscriber;
		subscriber(renderGroupState.Active);
	}

	public static void Unregister(RenderGroup group, RenderGroupSystem.GroupChangedHandler subscriber)
	{
		if (RenderGroupSystem.s_instance == null)
		{
			return;
		}
		RenderGroupSystem.s_instance.m_renderGroups[group].GroupChanged -= subscriber;
	}

	public static bool IsGroupActive(RenderGroup group)
	{
		return RenderGroupSystem.s_instance == null || RenderGroupSystem.s_instance.m_renderGroups[group].Active;
	}

	private static RenderGroupSystem s_instance;

	private Dictionary<RenderGroup, RenderGroupSystem.RenderGroupState> m_renderGroups = new Dictionary<RenderGroup, RenderGroupSystem.RenderGroupState>();

	public delegate void GroupChangedHandler(bool shouldRender);

	private class RenderGroupState
	{

		public bool Active
		{
			get
			{
				return this.active;
			}
			set
			{
				if (this.active == value)
				{
					return;
				}
				this.active = value;
				RenderGroupSystem.GroupChangedHandler groupChanged = this.GroupChanged;
				if (groupChanged == null)
				{
					return;
				}
				groupChanged(this.active);
			}
		}

		public event RenderGroupSystem.GroupChangedHandler GroupChanged;

		private bool active;
	}
}
