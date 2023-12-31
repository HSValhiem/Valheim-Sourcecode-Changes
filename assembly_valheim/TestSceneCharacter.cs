﻿using System;
using System.Threading;
using UnityEngine;

public class TestSceneCharacter : MonoBehaviour
{

	private void Start()
	{
		this.m_body = base.GetComponent<Rigidbody>();
	}

	private void Update()
	{
		Thread.Sleep(30);
		this.HandleInput(Time.deltaTime);
	}

	private void HandleInput(float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector2 zero = Vector2.zero;
		zero.x = Input.GetAxis("Mouse X");
		zero.y = Input.GetAxis("Mouse Y");
		if (Input.GetKey(KeyCode.Mouse1) || Cursor.lockState != CursorLockMode.None)
		{
			this.m_lookYaw *= Quaternion.Euler(0f, zero.x, 0f);
			this.m_lookPitch = Mathf.Clamp(this.m_lookPitch - zero.y, -89f, 89f);
		}
		if (Input.GetKeyDown(KeyCode.F1))
		{
			if (Cursor.lockState == CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
			}
		}
		Vector3 vector = Vector3.zero;
		if (Input.GetKey(KeyCode.A))
		{
			vector -= base.transform.right * this.m_speed;
		}
		if (Input.GetKey(KeyCode.D))
		{
			vector += base.transform.right * this.m_speed;
		}
		if (Input.GetKey(KeyCode.W))
		{
			vector += base.transform.forward * this.m_speed;
		}
		if (Input.GetKey(KeyCode.S))
		{
			vector -= base.transform.forward * this.m_speed;
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			this.m_body.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
		}
		Vector3 vector2 = vector - this.m_body.velocity;
		vector2.y = 0f;
		this.m_body.AddForce(vector2, ForceMode.VelocityChange);
		base.transform.rotation = this.m_lookYaw;
		Quaternion quaternion = this.m_lookYaw * Quaternion.Euler(this.m_lookPitch, 0f, 0f);
		mainCamera.transform.position = base.transform.position - quaternion * Vector3.forward * this.m_cameraDistance;
		mainCamera.transform.LookAt(base.transform.position + Vector3.up);
	}

	public float m_speed = 5f;

	public float m_cameraDistance = 10f;

	private Rigidbody m_body;

	private Quaternion m_lookYaw = Quaternion.identity;

	private float m_lookPitch;
}
