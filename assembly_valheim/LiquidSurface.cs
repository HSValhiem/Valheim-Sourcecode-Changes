using System;
using System.Collections.Generic;
using UnityEngine;

public class LiquidSurface : MonoBehaviour
{

	private void Awake()
	{
		this.m_liquid = base.GetComponentInParent<LiquidVolume>();
	}

	private void FixedUpdate()
	{
		this.UpdateFloaters();
	}

	public LiquidType GetLiquidType()
	{
		return this.m_liquid.m_liquidType;
	}

	public float GetSurface(Vector3 p)
	{
		return this.m_liquid.GetSurface(p);
	}

	private void OnTriggerEnter(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			component.Increment(this.m_liquid.m_liquidType);
			if (!this.m_inWater.Contains(component))
			{
				this.m_inWater.Add(component);
			}
		}
	}

	private void UpdateFloaters()
	{
		if (this.m_inWater.Count == 0)
		{
			return;
		}
		LiquidSurface.s_inWaterRemoveIndices.Clear();
		for (int i = 0; i < this.m_inWater.Count; i++)
		{
			IWaterInteractable waterInteractable = this.m_inWater[i];
			if (waterInteractable == null)
			{
				LiquidSurface.s_inWaterRemoveIndices.Add(i);
			}
			else
			{
				Transform transform = waterInteractable.GetTransform();
				if (transform)
				{
					float surface = this.m_liquid.GetSurface(transform.position);
					waterInteractable.SetLiquidLevel(surface, this.m_liquid.m_liquidType, this);
				}
				else
				{
					LiquidSurface.s_inWaterRemoveIndices.Add(i);
				}
			}
		}
		for (int j = LiquidSurface.s_inWaterRemoveIndices.Count - 1; j >= 0; j--)
		{
			this.m_inWater.RemoveAt(LiquidSurface.s_inWaterRemoveIndices[j]);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			if (component.Decrement(this.m_liquid.m_liquidType) == 0)
			{
				component.SetLiquidLevel(-10000f, this.m_liquid.m_liquidType, this);
			}
			this.m_inWater.Remove(component);
		}
	}

	private void OnDestroy()
	{
		foreach (IWaterInteractable waterInteractable in this.m_inWater)
		{
			if (waterInteractable != null && waterInteractable.Decrement(this.m_liquid.m_liquidType) == 0)
			{
				waterInteractable.SetLiquidLevel(-10000f, this.m_liquid.m_liquidType, this);
			}
		}
		this.m_inWater.Clear();
	}

	private LiquidVolume m_liquid;

	private readonly List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	private static readonly List<int> s_inWaterRemoveIndices = new List<int>();
}
