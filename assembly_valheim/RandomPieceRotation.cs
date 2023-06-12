using System;
using UnityEngine;

public class RandomPieceRotation : MonoBehaviour
{

	private void Awake()
	{
		Vector3 position = base.transform.position;
		int num = (int)position.x * (int)(position.y * 10f) * (int)(position.z * 100f);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(num);
		float num2 = (this.m_rotateX ? ((float)UnityEngine.Random.Range(0, this.m_stepsX) * 360f / (float)this.m_stepsX) : 0f);
		float num3 = (this.m_rotateY ? ((float)UnityEngine.Random.Range(0, this.m_stepsY) * 360f / (float)this.m_stepsY) : 0f);
		float num4 = (this.m_rotateZ ? ((float)UnityEngine.Random.Range(0, this.m_stepsZ) * 360f / (float)this.m_stepsZ) : 0f);
		base.transform.localRotation = Quaternion.Euler(num2, num3, num4);
		UnityEngine.Random.state = state;
	}

	public bool m_rotateX;

	public bool m_rotateY;

	public bool m_rotateZ;

	public int m_stepsX = 4;

	public int m_stepsY = 4;

	public int m_stepsZ = 4;
}
