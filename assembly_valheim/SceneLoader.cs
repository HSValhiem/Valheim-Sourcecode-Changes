using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

	private void Start()
	{
		this.StartLoading();
	}

	private IEnumerator LoadYourAsyncScene()
	{
		ZLog.Log("Starting to load scene:" + this.m_scene);
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(this.m_scene, LoadSceneMode.Single);
		while (!asyncLoad.isDone)
		{
			yield return null;
		}
		yield break;
	}

	private void StartLoading()
	{
		base.StartCoroutine(this.LoadYourAsyncScene());
	}

	public string m_scene = "";
}
