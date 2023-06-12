using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

[ExecuteAlways]
public class VortexParticles : MonoBehaviour
{

	private void Start()
	{
		this.ps = base.GetComponent<ParticleSystem>();
		if (this.ps == null)
		{
			ZLog.LogWarning("VortexParticles object '" + base.gameObject.name + "' is missing a particle system and disabled!");
			this.effectOn = false;
		}
	}

	private void Update()
	{
		if (this.ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
		{
			this.job.vortexCenter = this.centerOffset;
			this.job.upDir = new Vector3(0f, 1f, 0f);
		}
		else
		{
			this.job.vortexCenter = base.transform.position + this.centerOffset;
			this.job.upDir = base.transform.up;
		}
		this.job.pullStrength = this.pullStrength;
		this.job.vortexStrength = this.vortexStrength;
		this.job.lineAttraction = this.lineAttraction;
		this.job.useCustomData = this.useCustomData;
		this.job.deltaTime = Time.deltaTime;
	}

	private void OnParticleUpdateJobScheduled()
	{
		if (this.ps == null)
		{
			this.ps = base.GetComponent<ParticleSystem>();
			if (this.ps == null)
			{
				ZLog.LogWarning("VortexParticles object '" + base.gameObject.name + "' is missing a particle system and disabled!");
				this.effectOn = false;
			}
		}
		if (this.effectOn)
		{
			this.job.Schedule(this.ps, 1024, default(JobHandle));
		}
	}

	private ParticleSystem ps;

	private VortexParticles.VortexParticlesJob job;

	[SerializeField]
	private bool effectOn = true;

	[SerializeField]
	private Vector3 centerOffset;

	[SerializeField]
	private float pullStrength;

	[SerializeField]
	private float vortexStrength;

	[SerializeField]
	private bool lineAttraction;

	[SerializeField]
	private bool useCustomData;

	private struct VortexParticlesJob : IJobParticleSystemParallelFor
	{

		public void Execute(ParticleSystemJobData particles, int i)
		{
			ParticleSystemNativeArray3 particleSystemNativeArray = particles.velocities;
			float num = particleSystemNativeArray.x[i];
			particleSystemNativeArray = particles.velocities;
			float num2 = particleSystemNativeArray.y[i];
			particleSystemNativeArray = particles.velocities;
			Vector3 vector = new Vector3(num, num2, particleSystemNativeArray.z[i]);
			particleSystemNativeArray = particles.positions;
			float num3 = particleSystemNativeArray.x[i];
			particleSystemNativeArray = particles.positions;
			float num4 = particleSystemNativeArray.y[i];
			particleSystemNativeArray = particles.positions;
			Vector3 vector2 = new Vector3(num3, num4, particleSystemNativeArray.z[i]);
			Vector3 vector3 = this.vortexCenter;
			float num5 = (this.useCustomData ? particles.customData1.x[i] : this.vortexStrength);
			if (this.lineAttraction)
			{
				vector3.y = vector2.y;
			}
			Vector3 vector4 = vector3 - vector2;
			Vector3 vector5 = Vector3.Cross(Vector3.Normalize(vector4), this.upDir);
			Vector3 vector6 = vector + vector4 * this.pullStrength * this.deltaTime;
			vector6 += vector5 * num5 * this.deltaTime;
			NativeArray<float> x = particles.velocities.x;
			NativeArray<float> y = particles.velocities.y;
			NativeArray<float> z = particles.velocities.z;
			x[i] = vector6.x;
			y[i] = vector6.y;
			z[i] = vector6.z;
		}

		[ReadOnly]
		public Vector3 vortexCenter;

		[ReadOnly]
		public float pullStrength;

		[ReadOnly]
		public Vector3 upDir;

		[ReadOnly]
		public float vortexStrength;

		[ReadOnly]
		public bool lineAttraction;

		[ReadOnly]
		public bool useCustomData;

		[ReadOnly]
		public float deltaTime;
	}
}
