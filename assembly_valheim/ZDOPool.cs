using System;
using System.Collections.Generic;
using UnityEngine;

public static class ZDOPool
{

	public static ZDO Create(ZDOID id, Vector3 position)
	{
		ZDO zdo = ZDOPool.Get();
		zdo.Initialize(id, position);
		return zdo;
	}

	public static ZDO Create()
	{
		return ZDOPool.Get();
	}

	public static void Release(Dictionary<ZDOID, ZDO> objects)
	{
		foreach (ZDO zdo in objects.Values)
		{
			ZDOPool.Release(zdo);
		}
	}

	public static void Release(ZDO zdo)
	{
		zdo.Reset();
		ZDOPool.s_free.Push(zdo);
		ZDOPool.s_active--;
	}

	private static ZDO Get()
	{
		if (ZDOPool.s_free.Count <= 0)
		{
			for (int i = 0; i < 64; i++)
			{
				ZDO zdo = new ZDO();
				ZDOPool.s_free.Push(zdo);
			}
		}
		ZDOPool.s_active++;
		ZDO zdo2 = ZDOPool.s_free.Pop();
		zdo2.Init();
		return zdo2;
	}

	public static int GetPoolSize()
	{
		return ZDOPool.s_free.Count;
	}

	public static int GetPoolActive()
	{
		return ZDOPool.s_active;
	}

	public static int GetPoolTotal()
	{
		return ZDOPool.s_active + ZDOPool.s_free.Count;
	}

	private const int c_BatchSize = 64;

	private static readonly Stack<ZDO> s_free = new Stack<ZDO>();

	private static int s_active;
}
