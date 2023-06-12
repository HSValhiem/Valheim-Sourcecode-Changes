using System;
using UnityEngine;

public class SE_Puke : SE_Stats
{

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_removeTimer += dt;
		if (this.m_removeTimer > this.m_removeInterval)
		{
			this.m_removeTimer = 0f;
			if ((this.m_character as Player).RemoveOneFood())
			{
				Hud.instance.DamageFlash();
			}
		}
	}

	[Header("__SE_Puke__")]
	public float m_removeInterval = 1f;

	private float m_removeTimer;
}
