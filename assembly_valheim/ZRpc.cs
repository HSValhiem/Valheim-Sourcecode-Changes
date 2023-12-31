﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class ZRpc : IDisposable
{

	public ZRpc(ISocket socket)
	{
		this.m_socket = socket;
	}

	public void Dispose()
	{
		this.m_socket.Dispose();
	}

	public ISocket GetSocket()
	{
		return this.m_socket;
	}

	public ZRpc.ErrorCode Update(float dt)
	{
		if (!this.m_socket.IsConnected())
		{
			return ZRpc.ErrorCode.Disconnected;
		}
		for (ZPackage zpackage = this.m_socket.Recv(); zpackage != null; zpackage = this.m_socket.Recv())
		{
			this.m_recvPackages++;
			this.m_recvData += zpackage.Size();
			try
			{
				this.HandlePackage(zpackage);
			}
			catch (EndOfStreamException ex)
			{
				ZLog.LogError("EndOfStreamException in ZRpc::HandlePackage: Assume incompatible version: " + ex.Message);
				return ZRpc.ErrorCode.IncompatibleVersion;
			}
			catch (Exception ex2)
			{
				string text = "Exception in ZRpc::HandlePackage: ";
				Exception ex3 = ex2;
				ZLog.Log(text + ((ex3 != null) ? ex3.ToString() : null));
			}
		}
		this.UpdatePing(dt);
		return ZRpc.ErrorCode.Success;
	}

	private void UpdatePing(float dt)
	{
		this.m_pingTimer += dt;
		if (this.m_pingTimer > ZRpc.m_pingInterval)
		{
			this.m_pingTimer = 0f;
			this.m_pkg.Clear();
			this.m_pkg.Write(0);
			this.m_pkg.Write(true);
			this.SendPackage(this.m_pkg);
		}
		this.m_timeSinceLastPing += dt;
		if (this.m_timeSinceLastPing > ZRpc.m_timeout)
		{
			ZLog.LogWarning("ZRpc timeout detected");
			this.m_socket.Close();
		}
	}

	private void ReceivePing(ZPackage package)
	{
		if (package.ReadBool())
		{
			this.m_pkg.Clear();
			this.m_pkg.Write(0);
			this.m_pkg.Write(false);
			this.SendPackage(this.m_pkg);
			return;
		}
		this.m_timeSinceLastPing = 0f;
	}

	public float GetTimeSinceLastPing()
	{
		return this.m_timeSinceLastPing;
	}

	public bool IsConnected()
	{
		return this.m_socket.IsConnected();
	}

	private void HandlePackage(ZPackage package)
	{
		int num = package.ReadInt();
		if (num == 0)
		{
			this.ReceivePing(package);
			return;
		}
		ZRpc.RpcMethodBase rpcMethodBase2;
		if (ZRpc.m_DEBUG)
		{
			package.ReadString();
			ZRpc.RpcMethodBase rpcMethodBase;
			if (this.m_functions.TryGetValue(num, out rpcMethodBase))
			{
				rpcMethodBase.Invoke(this, package);
				return;
			}
		}
		else if (this.m_functions.TryGetValue(num, out rpcMethodBase2))
		{
			rpcMethodBase2.Invoke(this, package);
		}
	}

	public void Register(string name, ZRpc.RpcMethod.Method f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod(f));
	}

	public void Register<T>(string name, Action<ZRpc, T> f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T>(f));
	}

	public void Register<T, U>(string name, Action<ZRpc, T, U> f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T, U>(f));
	}

	public void Register<T, U, V>(string name, Action<ZRpc, T, U, V> f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T, U, V>(f));
	}

	public void Register<T, U, V, W>(string name, ZRpc.RpcMethod<T, U, V, W>.Method f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T, U, V, W>(f));
	}

	public void Unregister(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
	}

	public void Invoke(string method, params object[] parameters)
	{
		if (!this.IsConnected())
		{
			return;
		}
		this.m_pkg.Clear();
		int stableHashCode = method.GetStableHashCode();
		this.m_pkg.Write(stableHashCode);
		if (ZRpc.m_DEBUG)
		{
			this.m_pkg.Write(method);
		}
		ZRpc.Serialize(parameters, ref this.m_pkg);
		this.SendPackage(this.m_pkg);
	}

	private void SendPackage(ZPackage pkg)
	{
		this.m_sentPackages++;
		this.m_sentData += pkg.Size();
		this.m_socket.Send(this.m_pkg);
	}

	public static void Serialize(object[] parameters, ref ZPackage pkg)
	{
		foreach (object obj in parameters)
		{
			if (obj is int)
			{
				pkg.Write((int)obj);
			}
			else if (obj is uint)
			{
				pkg.Write((uint)obj);
			}
			else if (obj is long)
			{
				pkg.Write((long)obj);
			}
			else if (obj is float)
			{
				pkg.Write((float)obj);
			}
			else if (obj is double)
			{
				pkg.Write((double)obj);
			}
			else if (obj is bool)
			{
				pkg.Write((bool)obj);
			}
			else if (obj is string)
			{
				pkg.Write((string)obj);
			}
			else if (obj is ZPackage)
			{
				pkg.Write((ZPackage)obj);
			}
			else
			{
				if (obj is List<string>)
				{
					List<string> list = obj as List<string>;
					pkg.Write(list.Count);
					using (List<string>.Enumerator enumerator = list.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							string text = enumerator.Current;
							pkg.Write(text);
						}
						goto IL_207;
					}
				}
				if (obj is Vector3)
				{
					pkg.Write(((Vector3)obj).x);
					pkg.Write(((Vector3)obj).y);
					pkg.Write(((Vector3)obj).z);
				}
				else if (obj is Quaternion)
				{
					pkg.Write(((Quaternion)obj).x);
					pkg.Write(((Quaternion)obj).y);
					pkg.Write(((Quaternion)obj).z);
					pkg.Write(((Quaternion)obj).w);
				}
				else if (obj is ZDOID)
				{
					pkg.Write((ZDOID)obj);
				}
				else if (obj is HitData)
				{
					(obj as HitData).Serialize(ref pkg);
				}
				else if (obj is ISerializableParameter)
				{
					(obj as ISerializableParameter).Serialize(ref pkg);
				}
			}
			IL_207:;
		}
	}

	public static object[] Deserialize(ZRpc rpc, ParameterInfo[] paramInfo, ZPackage pkg)
	{
		List<object> list = new List<object>();
		list.Add(rpc);
		ZRpc.Deserialize(paramInfo, pkg, ref list);
		return list.ToArray();
	}

	public static void Deserialize(ParameterInfo[] paramInfo, ZPackage pkg, ref List<object> parameters)
	{
		for (int i = 1; i < paramInfo.Length; i++)
		{
			ParameterInfo parameterInfo = paramInfo[i];
			if (parameterInfo.ParameterType == typeof(int))
			{
				parameters.Add(pkg.ReadInt());
			}
			else if (parameterInfo.ParameterType == typeof(uint))
			{
				parameters.Add(pkg.ReadUInt());
			}
			else if (parameterInfo.ParameterType == typeof(long))
			{
				parameters.Add(pkg.ReadLong());
			}
			else if (parameterInfo.ParameterType == typeof(float))
			{
				parameters.Add(pkg.ReadSingle());
			}
			else if (parameterInfo.ParameterType == typeof(double))
			{
				parameters.Add(pkg.ReadDouble());
			}
			else if (parameterInfo.ParameterType == typeof(bool))
			{
				parameters.Add(pkg.ReadBool());
			}
			else if (parameterInfo.ParameterType == typeof(string))
			{
				parameters.Add(pkg.ReadString());
			}
			else if (parameterInfo.ParameterType == typeof(ZPackage))
			{
				parameters.Add(pkg.ReadPackage());
			}
			else if (parameterInfo.ParameterType == typeof(List<string>))
			{
				int num = pkg.ReadInt();
				List<string> list = new List<string>(num);
				for (int j = 0; j < num; j++)
				{
					list.Add(pkg.ReadString());
				}
				parameters.Add(list);
			}
			else if (parameterInfo.ParameterType == typeof(Vector3))
			{
				Vector3 vector = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
				parameters.Add(vector);
			}
			else if (parameterInfo.ParameterType == typeof(Quaternion))
			{
				Quaternion quaternion = new Quaternion(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
				parameters.Add(quaternion);
			}
			else if (parameterInfo.ParameterType == typeof(ZDOID))
			{
				parameters.Add(pkg.ReadZDOID());
			}
			else if (parameterInfo.ParameterType == typeof(HitData))
			{
				HitData hitData = new HitData();
				hitData.Deserialize(ref pkg);
				parameters.Add(hitData);
			}
			else if (typeof(ISerializableParameter).IsAssignableFrom(parameterInfo.ParameterType))
			{
				ISerializableParameter serializableParameter = (ISerializableParameter)Activator.CreateInstance(parameterInfo.ParameterType);
				serializableParameter.Deserialize(ref pkg);
				parameters.Add(serializableParameter);
			}
		}
	}

	public static void SetLongTimeout(bool enable)
	{
		if (enable)
		{
			ZRpc.m_timeout = 90f;
		}
		else
		{
			ZRpc.m_timeout = 30f;
		}
		ZLog.Log(string.Format("ZRpc timeout set to {0}s ", ZRpc.m_timeout));
	}

	private ISocket m_socket;

	private ZPackage m_pkg = new ZPackage();

	private Dictionary<int, ZRpc.RpcMethodBase> m_functions = new Dictionary<int, ZRpc.RpcMethodBase>();

	private int m_sentPackages;

	private int m_sentData;

	private int m_recvPackages;

	private int m_recvData;

	private float m_pingTimer;

	private float m_timeSinceLastPing;

	private static float m_pingInterval = 1f;

	private static float m_timeout = 30f;

	private static bool m_DEBUG = false;

	public enum ErrorCode
	{

		Success,

		Disconnected,

		IncompatibleVersion
	}

	private interface RpcMethodBase
	{

		void Invoke(ZRpc rpc, ZPackage pkg);
	}

	public class RpcMethod : ZRpc.RpcMethodBase
	{

		public RpcMethod(ZRpc.RpcMethod.Method action)
		{
			this.m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action(rpc);
		}

		private ZRpc.RpcMethod.Method m_action;

		public delegate void Method(ZRpc RPC);
	}

	private class RpcMethod<T> : ZRpc.RpcMethodBase
	{

		public RpcMethod(Action<ZRpc, T> action)
		{
			this.m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		private Action<ZRpc, T> m_action;
	}

	private class RpcMethod<T, U> : ZRpc.RpcMethodBase
	{

		public RpcMethod(Action<ZRpc, T, U> action)
		{
			this.m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		private Action<ZRpc, T, U> m_action;
	}

	private class RpcMethod<T, U, V> : ZRpc.RpcMethodBase
	{

		public RpcMethod(Action<ZRpc, T, U, V> action)
		{
			this.m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		private Action<ZRpc, T, U, V> m_action;
	}

	public class RpcMethod<T, U, V, B> : ZRpc.RpcMethodBase
	{

		public RpcMethod(ZRpc.RpcMethod<T, U, V, B>.Method action)
		{
			this.m_action = action;
		}

		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		private ZRpc.RpcMethod<T, U, V, B>.Method m_action;

		public delegate void Method(ZRpc RPC, T p0, U p1, V p2, B p3);
	}
}
