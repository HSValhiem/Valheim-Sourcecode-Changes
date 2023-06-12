using System;
using System.Collections.Generic;
using UnityEngine;

internal static class ShuffleClass
{

	public static void Shuffle<T>(this IList<T> list, bool useUnityRandom = false)
	{
		int i = list.Count;
		while (i > 1)
		{
			i--;
			int num = (useUnityRandom ? UnityEngine.Random.Range(0, i) : ShuffleClass.rng.Next(i + 1));
			T t = list[num];
			list[num] = list[i];
			list[i] = t;
		}
	}

	private static System.Random rng = new System.Random();
}
