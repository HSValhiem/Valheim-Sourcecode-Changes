using System;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldSubmit : MonoBehaviour
{

	private void Awake()
	{
		this.m_field = base.GetComponent<InputField>();
	}

	private void Update()
	{
		if (this.m_field.text != "" && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || ZInput.GetButtonDown("JoyButtonA")))
		{
			this.m_onSubmit(this.m_field.text);
			this.m_field.text = "";
		}
	}

	public Action<string> m_onSubmit;

	private InputField m_field;
}
