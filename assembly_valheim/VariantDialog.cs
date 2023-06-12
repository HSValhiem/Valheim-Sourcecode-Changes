using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VariantDialog : MonoBehaviour
{

	public void Setup(ItemDrop.ItemData item)
	{
		base.gameObject.SetActive(true);
		foreach (GameObject gameObject in this.m_elements)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		this.m_elements.Clear();
		for (int i = 0; i < item.m_shared.m_variants; i++)
		{
			Sprite sprite = item.m_shared.m_icons[i];
			int num = i / this.m_gridWidth;
			int num2 = i % this.m_gridWidth;
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			gameObject2.SetActive(true);
			(gameObject2.transform as RectTransform).anchoredPosition = new Vector2((float)num2 * this.m_spacing, (float)(-(float)num) * this.m_spacing);
			Button component = gameObject2.transform.Find("Button").GetComponent<Button>();
			int buttonIndex = i;
			component.onClick.AddListener(delegate
			{
				this.OnClicked(buttonIndex);
			});
			component.GetComponent<Image>().sprite = sprite;
			this.m_elements.Add(gameObject2);
		}
	}

	public void OnClose()
	{
		base.gameObject.SetActive(false);
	}

	private void OnClicked(int index)
	{
		ZLog.Log("Clicked button " + index.ToString());
		base.gameObject.SetActive(false);
		this.m_selected(index);
	}

	public Transform m_listRoot;

	public GameObject m_elementPrefab;

	public float m_spacing = 70f;

	public int m_gridWidth = 5;

	private List<GameObject> m_elements = new List<GameObject>();

	public Action<int> m_selected;
}
