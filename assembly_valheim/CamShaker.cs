using System;
using System.Collections;
using UnityEngine;

public class CamShaker : MonoBehaviour
{

	private void Start()
	{
		if (this.m_continous)
		{
			if (this.m_delay <= 0f)
			{
				base.StartCoroutine("TriggerContinous");
				return;
			}
			base.Invoke("DelayedTriggerContinous", this.m_delay);
			return;
		}
		else
		{
			if (this.m_delay <= 0f)
			{
				this.Trigger();
				return;
			}
			base.Invoke("Trigger", this.m_delay);
			return;
		}
	}

	private void DelayedTriggerContinous()
	{
		base.StartCoroutine("TriggerContinous");
	}

	private IEnumerator TriggerContinous()
	{
		float t = 0f;
		for (;;)
		{
			this.Trigger();
			t += Time.deltaTime;
			if (this.m_continousDuration > 0f && t > this.m_continousDuration)
			{
				break;
			}
			yield return null;
		}
		yield break;
		yield break;
	}

	private void Trigger()
	{
		if (GameCamera.instance)
		{
			if (this.m_localOnly)
			{
				ZNetView component = base.GetComponent<ZNetView>();
				if (component && component.IsValid() && !component.IsOwner())
				{
					return;
				}
			}
			GameCamera.instance.AddShake(base.transform.position, this.m_range, this.m_strength, this.m_continous);
		}
	}

	public float m_strength = 1f;

	public float m_range = 50f;

	public float m_delay;

	public bool m_continous;

	public float m_continousDuration;

	public bool m_localOnly;
}
