using System;
using UnityEngine;

public class AnimSetTrigger : StateMachineBehaviour
{

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!string.IsNullOrEmpty(this.TriggerOnEnter))
		{
			if (this.TriggerOnEnterEnable)
			{
				animator.SetTrigger(this.TriggerOnEnter);
				return;
			}
			animator.ResetTrigger(this.TriggerOnEnter);
		}
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!string.IsNullOrEmpty(this.TriggerOnExit))
		{
			if (this.TriggerOnExitEnable)
			{
				animator.SetTrigger(this.TriggerOnExit);
				return;
			}
			animator.ResetTrigger(this.TriggerOnExit);
		}
	}

	public string TriggerOnEnter;

	public bool TriggerOnEnterEnable = true;

	public string TriggerOnExit;

	public bool TriggerOnExitEnable = true;
}
