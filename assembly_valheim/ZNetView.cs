﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ZNetView : MonoBehaviour
{

	private void Awake()
	{
		if (ZNetView.m_forceDisableInit || ZDOMan.instance == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		this.m_body = base.GetComponent<Rigidbody>();
		if (ZNetView.m_useInitZDO && ZNetView.m_initZDO == null)
		{
			ZLog.LogWarning("Double ZNetview when initializing object " + base.gameObject.name);
		}
		if (ZNetView.m_initZDO != null)
		{
			this.m_zdo = ZNetView.m_initZDO;
			ZNetView.m_initZDO = null;
			if (this.m_zdo.Type != this.m_type && this.m_zdo.IsOwner())
			{
				this.m_zdo.SetType(this.m_type);
			}
			if (this.m_zdo.Distant != this.m_distant && this.m_zdo.IsOwner())
			{
				this.m_zdo.SetDistant(this.m_distant);
			}
			if (this.m_syncInitialScale)
			{
				Vector3 vec = this.m_zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
				if (vec != Vector3.zero)
				{
					base.transform.localScale = vec;
				}
				else
				{
					float @float = this.m_zdo.GetFloat(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
					if (!base.transform.localScale.x.Equals(@float))
					{
						base.transform.localScale = new Vector3(@float, @float, @float);
					}
				}
			}
			if (this.m_body)
			{
				this.m_body.Sleep();
			}
		}
		else
		{
			string prefabName = this.GetPrefabName();
			this.m_zdo = ZDOMan.instance.CreateNewZDO(base.transform.position, prefabName.GetStableHashCode());
			this.m_zdo.Persistent = this.m_persistent;
			this.m_zdo.Type = this.m_type;
			this.m_zdo.Distant = this.m_distant;
			this.m_zdo.SetPrefab(prefabName.GetStableHashCode());
			this.m_zdo.SetRotation(base.transform.rotation);
			if (this.m_syncInitialScale)
			{
				this.SyncScale(true);
			}
			if (ZNetView.m_ghostInit)
			{
				this.m_ghost = true;
				return;
			}
		}
		ZNetScene.instance.AddInstance(this.m_zdo, this);
	}

	public void SetLocalScale(Vector3 scale)
	{
		if (base.transform.localScale == scale)
		{
			return;
		}
		base.transform.localScale = scale;
		if (this.m_zdo != null && this.m_syncInitialScale && this.IsOwner())
		{
			this.SyncScale(false);
		}
	}

	private void SyncScale(bool skipOne = false)
	{
		if (!Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.y) || !Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.z))
		{
			this.m_zdo.Set(ZDOVars.s_scaleHash, base.transform.localScale);
			return;
		}
		if (skipOne && Mathf.Approximately(base.transform.localScale.x, 1f))
		{
			return;
		}
		this.m_zdo.Set(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
	}

	private void OnDestroy()
	{
		ZNetScene.instance;
	}

	private string GetPrefabName()
	{
		return Utils.GetPrefabName(base.gameObject);
	}

	public void Destroy()
	{
		ZNetScene.instance.Destroy(base.gameObject);
	}

	public bool IsOwner()
	{
		return this.IsValid() && this.m_zdo.IsOwner();
	}

	public bool HasOwner()
	{
		return this.IsValid() && this.m_zdo.HasOwner();
	}

	public void ClaimOwnership()
	{
		if (this.IsOwner())
		{
			return;
		}
		this.m_zdo.SetOwner(ZDOMan.GetSessionID());
	}

	public ZDO GetZDO()
	{
		return this.m_zdo;
	}

	public bool IsValid()
	{
		return this.m_zdo != null && this.m_zdo.IsValid();
	}

	public void ResetZDO()
	{
		this.m_zdo = null;
	}

	public void Register(string name, Action<long> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod(f));
	}

	public void Register<T>(string name, Action<long, T> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T>(f));
	}

	public void Register<T, U>(string name, Action<long, T, U> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U>(f));
	}

	public void Register<T, U, V>(string name, Action<long, T, U, V> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V>(f));
	}

	public void Register<T, U, V, B>(string name, RoutedMethod<T, U, V, B>.Method f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B>(f));
	}

	public void Unregister(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
	}

	public void HandleRoutedRPC(ZRoutedRpc.RoutedRPCData rpcData)
	{
		RoutedMethodBase routedMethodBase;
		if (this.m_functions.TryGetValue(rpcData.m_methodHash, out routedMethodBase))
		{
			routedMethodBase.Invoke(rpcData.m_senderPeerID, rpcData.m_parameters);
			return;
		}
		ZLog.LogWarning("Failed to find rpc method " + rpcData.m_methodHash.ToString());
	}

	public void InvokeRPC(long targetID, string method, params object[] parameters)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(targetID, this.m_zdo.m_uid, method, parameters);
	}

	public void InvokeRPC(string method, params object[] parameters)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(this.m_zdo.GetOwner(), this.m_zdo.m_uid, method, parameters);
	}

	public static object[] Deserialize(long callerID, ParameterInfo[] paramInfo, ZPackage pkg)
	{
		List<object> list = new List<object>();
		list.Add(callerID);
		ZRpc.Deserialize(paramInfo, pkg, ref list);
		return list.ToArray();
	}

	public static void StartGhostInit()
	{
		ZNetView.m_ghostInit = true;
	}

	public static void FinishGhostInit()
	{
		ZNetView.m_ghostInit = false;
	}

	public static long Everybody;

	public bool m_persistent;

	public bool m_distant;

	public ZDO.ObjectType m_type;

	public bool m_syncInitialScale;

	public static bool m_useInitZDO;

	public static ZDO m_initZDO;

	public static bool m_forceDisableInit;

	private ZDO m_zdo;

	private Rigidbody m_body;

	private Dictionary<int, RoutedMethodBase> m_functions = new Dictionary<int, RoutedMethodBase>();

	private bool m_ghost;

	private static bool m_ghostInit;
}
