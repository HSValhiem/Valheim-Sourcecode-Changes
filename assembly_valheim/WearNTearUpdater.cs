using System;
using System.Collections.Generic;
using UnityEngine;

public class WearNTearUpdater : MonoBehaviour
{

	private void Update()
	{
		float time = Time.time;
		if (time < this.m_sleepUntil)
		{
			return;
		}
		List<WearNTear> allInstances = WearNTear.GetAllInstances();
		float deltaTime = Time.deltaTime;
		foreach (WearNTear wearNTear in allInstances)
		{
			wearNTear.UpdateCover(deltaTime);
		}
		int num = this.m_index;
		int num2 = 0;
		while (num2 < 50 && allInstances.Count != 0 && num < allInstances.Count)
		{
			allInstances[num].UpdateWear(time);
			num++;
			num2++;
		}
		this.m_index = ((num < allInstances.Count) ? num : 0);
		if (this.m_index == 0)
		{
			this.m_sleepUntil = time + 0.5f;
		}
	}

	private int m_index;

	private float m_sleepUntil;

	private const int c_UpdatesPerFrame = 50;

	private const float c_SleepTime = 0.5f;
}
