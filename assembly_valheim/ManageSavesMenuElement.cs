using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ManageSavesMenuElement : MonoBehaviour
{

	public RectTransform rectTransform
	{
		get
		{
			return base.transform as RectTransform;
		}
	}

	private RectTransform arrowRectTransform
	{
		get
		{
			return this.arrow.transform as RectTransform;
		}
	}

	public event ManageSavesMenuElement.HeightChangedHandler HeightChanged;

	public event ManageSavesMenuElement.ElementClickedHandler ElementClicked;

	public event ManageSavesMenuElement.ElementExpandedChangedHandler ElementExpandedChanged;

	public bool IsExpanded { get; private set; }

	public int BackupCount
	{
		get
		{
			return this.backupElements.Count;
		}
	}

	public SaveWithBackups Save { get; private set; }

	public void SetUp(SaveWithBackups save)
	{
		this.UpdatePrimaryElement();
		for (int i = 0; i < this.Save.BackupFiles.Length; i++)
		{
			ManageSavesMenuElement.BackupElement backupElement = this.CreateBackupElement(this.Save.BackupFiles[i], i);
			this.backupElements.Add(backupElement);
		}
		this.UpdateElementPositions();
	}

	public IEnumerator SetUpEnumerator(SaveWithBackups save)
	{
		this.Save = save;
		this.UpdatePrimaryElement();
		yield return null;
		int num;
		for (int i = 0; i < this.Save.BackupFiles.Length; i = num + 1)
		{
			ManageSavesMenuElement.BackupElement backupElement = this.CreateBackupElement(this.Save.BackupFiles[i], i);
			this.backupElements.Add(backupElement);
			yield return null;
			num = i;
		}
		IEnumerator updateElementPositions = this.UpdateElementPositionsEnumerator();
		while (updateElementPositions.MoveNext())
		{
			yield return null;
		}
		yield break;
	}

	public void UpdateElement(SaveWithBackups save)
	{
		this.Save = save;
		this.UpdatePrimaryElement();
		List<ManageSavesMenuElement.BackupElement> list = new List<ManageSavesMenuElement.BackupElement>();
		Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> dictionary = new Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>();
		for (int i = 0; i < this.backupElements.Count; i++)
		{
			if (!dictionary.ContainsKey(this.backupElements[i].File.FileName))
			{
				dictionary.Add(this.backupElements[i].File.FileName, new Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>());
			}
			dictionary[this.backupElements[i].File.FileName].Add(this.backupElements[i].File.m_source, this.backupElements[i]);
		}
		for (int j = 0; j < this.Save.BackupFiles.Length; j++)
		{
			SaveFile saveFile = this.Save.BackupFiles[j];
			if (dictionary.ContainsKey(saveFile.FileName) && dictionary[saveFile.FileName].ContainsKey(saveFile.m_source))
			{
				int currentIndex = j;
				dictionary[saveFile.FileName][saveFile.m_source].UpdateElement(saveFile, delegate
				{
					this.OnBackupElementClicked(currentIndex);
				});
				list.Add(dictionary[saveFile.FileName][saveFile.m_source]);
				dictionary[saveFile.FileName].Remove(saveFile.m_source);
				if (dictionary.Count <= 0)
				{
					dictionary.Remove(saveFile.FileName);
				}
			}
			else
			{
				ManageSavesMenuElement.BackupElement backupElement = this.CreateBackupElement(saveFile, j);
				list.Add(backupElement);
			}
		}
		foreach (KeyValuePair<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> keyValuePair in dictionary)
		{
			foreach (KeyValuePair<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement> keyValuePair2 in keyValuePair.Value)
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value.GuiInstance);
			}
		}
		this.backupElements = list;
		float num = this.UpdateElementPositions();
		this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, this.IsExpanded ? num : this.elementHeight);
	}

	public IEnumerator UpdateElementEnumerator(SaveWithBackups save)
	{
		this.Save = save;
		this.UpdatePrimaryElement();
		List<ManageSavesMenuElement.BackupElement> newBackupElementsList = new List<ManageSavesMenuElement.BackupElement>();
		Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> backupNameToElementMap = new Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>();
		int num;
		for (int i = 0; i < this.backupElements.Count; i = num + 1)
		{
			if (!backupNameToElementMap.ContainsKey(this.backupElements[i].File.FileName))
			{
				backupNameToElementMap.Add(this.backupElements[i].File.FileName, new Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>());
			}
			backupNameToElementMap[this.backupElements[i].File.FileName].Add(this.backupElements[i].File.m_source, this.backupElements[i]);
			yield return null;
			num = i;
		}
		for (int i = 0; i < this.Save.BackupFiles.Length; i = num + 1)
		{
			SaveFile saveFile = this.Save.BackupFiles[i];
			if (backupNameToElementMap.ContainsKey(saveFile.FileName) && backupNameToElementMap[saveFile.FileName].ContainsKey(saveFile.m_source))
			{
				int currentIndex = i;
				backupNameToElementMap[saveFile.FileName][saveFile.m_source].UpdateElement(saveFile, delegate
				{
					this.OnBackupElementClicked(currentIndex);
				});
				newBackupElementsList.Add(backupNameToElementMap[saveFile.FileName][saveFile.m_source]);
				backupNameToElementMap[saveFile.FileName].Remove(saveFile.m_source);
				if (backupNameToElementMap.Count <= 0)
				{
					backupNameToElementMap.Remove(saveFile.FileName);
				}
			}
			else
			{
				ManageSavesMenuElement.BackupElement backupElement = this.CreateBackupElement(saveFile, i);
				newBackupElementsList.Add(backupElement);
			}
			yield return null;
			num = i;
		}
		foreach (KeyValuePair<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> keyValuePair in backupNameToElementMap)
		{
			foreach (KeyValuePair<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement> keyValuePair2 in keyValuePair.Value)
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value.GuiInstance);
				yield return null;
			}
			Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>.Enumerator enumerator2 = default(Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>.Enumerator);
		}
		Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>.Enumerator enumerator = default(Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>.Enumerator);
		this.backupElements = newBackupElementsList;
		float num2 = this.UpdateElementPositions();
		this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, this.IsExpanded ? num2 : this.elementHeight);
		yield break;
		yield break;
	}

	private ManageSavesMenuElement.BackupElement CreateBackupElement(SaveFile backup, int index)
	{
		return new ManageSavesMenuElement.BackupElement(UnityEngine.Object.Instantiate<GameObject>(this.backupElement.gameObject, this.rectTransform), backup, delegate
		{
			this.OnBackupElementClicked(index);
		});
	}

	private float UpdateElementPositions()
	{
		float num = this.elementHeight;
		for (int i = 0; i < this.backupElements.Count; i++)
		{
			this.backupElements[i].rectTransform.anchoredPosition = new Vector2(this.backupElements[i].rectTransform.anchoredPosition.x, -num);
			num += this.backupElements[i].rectTransform.sizeDelta.y;
		}
		return num;
	}

	private IEnumerator UpdateElementPositionsEnumerator()
	{
		float pos = this.elementHeight;
		int num;
		for (int i = 0; i < this.backupElements.Count; i = num + 1)
		{
			this.backupElements[i].rectTransform.anchoredPosition = new Vector2(this.backupElements[i].rectTransform.anchoredPosition.x, -pos);
			pos += this.backupElements[i].rectTransform.sizeDelta.y;
			yield return null;
			num = i;
		}
		yield break;
	}

	private void UpdatePrimaryElement()
	{
		this.arrow.gameObject.SetActive(this.Save.BackupFiles.Length != 0);
		string text = this.Save.m_name;
		if (!this.Save.IsDeleted)
		{
			text = this.Save.PrimaryFile.FileName;
			if (SaveSystem.IsCorrupt(this.Save.PrimaryFile))
			{
				text += " [CORRUPT]";
			}
			if (SaveSystem.IsWorldWithMissingMetaFile(this.Save.PrimaryFile))
			{
				text += " [MISSING META]";
			}
		}
		this.nameText.text = text;
		this.sizeText.text = FileHelpers.BytesAsNumberString(this.Save.IsDeleted ? 0UL : this.Save.PrimaryFile.Size, 1U) + "/" + FileHelpers.BytesAsNumberString(this.Save.SizeWithBackups, 1U);
		this.backupCountText.text = Localization.instance.Localize("$menu_backupcount", new string[] { this.Save.BackupFiles.Length.ToString() });
		this.dateText.text = (this.Save.IsDeleted ? Localization.instance.Localize("$menu_deleted") : this.Save.PrimaryFile.LastModified.ToShortDateString());
		Transform transform = this.sourceParent.Find("source_cloud");
		if (transform != null)
		{
			transform.gameObject.SetActive(!this.Save.IsDeleted && this.Save.PrimaryFile.m_source == FileHelpers.FileSource.Cloud);
		}
		Transform transform2 = this.sourceParent.Find("source_local");
		if (transform2 != null)
		{
			transform2.gameObject.SetActive(!this.Save.IsDeleted && this.Save.PrimaryFile.m_source == FileHelpers.FileSource.Local);
		}
		Transform transform3 = this.sourceParent.Find("source_legacy");
		if (transform3 != null)
		{
			transform3.gameObject.SetActive(!this.Save.IsDeleted && this.Save.PrimaryFile.m_source == FileHelpers.FileSource.Legacy);
		}
		if (this.IsExpanded && this.Save.BackupFiles.Length == 0)
		{
			this.SetExpanded(false, false);
		}
	}

	private void OnDestroy()
	{
		foreach (ManageSavesMenuElement.BackupElement backupElement in this.backupElements)
		{
			UnityEngine.Object.Destroy(backupElement.GuiInstance);
		}
		this.backupElements.Clear();
	}

	private void Start()
	{
		this.elementHeight = this.rectTransform.sizeDelta.y;
	}

	private void OnEnable()
	{
		this.primaryElement.onClick.AddListener(new UnityAction(this.OnElementClicked));
		this.arrow.onClick.AddListener(new UnityAction(this.OnArrowClicked));
	}

	private void OnDisable()
	{
		this.primaryElement.onClick.RemoveListener(new UnityAction(this.OnElementClicked));
		this.arrow.onClick.RemoveListener(new UnityAction(this.OnArrowClicked));
	}

	private void OnElementClicked()
	{
		ManageSavesMenuElement.ElementClickedHandler elementClicked = this.ElementClicked;
		if (elementClicked == null)
		{
			return;
		}
		elementClicked(this, -1);
	}

	private void OnBackupElementClicked(int index)
	{
		ManageSavesMenuElement.ElementClickedHandler elementClicked = this.ElementClicked;
		if (elementClicked == null)
		{
			return;
		}
		elementClicked(this, index);
	}

	private void OnArrowClicked()
	{
		this.SetExpanded(!this.IsExpanded, true);
	}

	public void SetExpanded(bool value, bool animated = true)
	{
		if (this.IsExpanded == value)
		{
			return;
		}
		this.IsExpanded = value;
		ManageSavesMenuElement.ElementExpandedChangedHandler elementExpandedChanged = this.ElementExpandedChanged;
		if (elementExpandedChanged != null)
		{
			elementExpandedChanged(this, this.IsExpanded);
		}
		if (this.arrowAnimationCoroutine != null)
		{
			base.StopCoroutine(this.arrowAnimationCoroutine);
		}
		if (this.listAnimationCoroutine != null)
		{
			base.StopCoroutine(this.listAnimationCoroutine);
		}
		if (animated)
		{
			this.arrowAnimationCoroutine = base.StartCoroutine(this.AnimateArrow());
			this.listAnimationCoroutine = base.StartCoroutine(this.AnimateList());
			return;
		}
		float num = (float)(this.IsExpanded ? 0 : 90);
		this.arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, num);
		float num2 = (this.IsExpanded ? (this.elementHeight * (float)(this.backupElements.Count + 1)) : this.elementHeight);
		this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, num2);
		ManageSavesMenuElement.HeightChangedHandler heightChanged = this.HeightChanged;
		if (heightChanged == null)
		{
			return;
		}
		heightChanged();
	}

	public void Select(ref int backupIndex)
	{
		if (backupIndex < 0 || this.BackupCount <= 0)
		{
			this.selectedBackground.gameObject.SetActive(true);
			backupIndex = -1;
			return;
		}
		backupIndex = Mathf.Clamp(backupIndex, 0, this.BackupCount - 1);
		this.backupElements[backupIndex].rectTransform.Find("selected").gameObject.SetActive(true);
	}

	public void Deselect(int backupIndex = -1)
	{
		if (backupIndex < 0)
		{
			this.selectedBackground.gameObject.SetActive(false);
			return;
		}
		if (backupIndex > this.backupElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to deselect backup: Index ",
				backupIndex.ToString(),
				" was outside of the valid range -1-",
				(this.backupElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.backupElements[backupIndex].rectTransform.Find("selected").gameObject.SetActive(false);
	}

	public RectTransform GetTransform(int backupIndex = -1)
	{
		if (backupIndex < 0)
		{
			return this.primaryElement.transform as RectTransform;
		}
		return this.backupElements[backupIndex].rectTransform;
	}

	private IEnumerator AnimateArrow()
	{
		float currentRotation = this.arrowRectTransform.rotation.eulerAngles.z;
		float targetRotation = (float)(this.IsExpanded ? 0 : 90);
		float sign = Mathf.Sign(targetRotation - currentRotation);
		for (;;)
		{
			currentRotation += sign * 90f * 10f * Time.deltaTime;
			if (currentRotation * sign > targetRotation * sign)
			{
				currentRotation = targetRotation;
			}
			this.arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
			if (currentRotation == targetRotation)
			{
				break;
			}
			yield return null;
		}
		this.arrowAnimationCoroutine = null;
		yield break;
	}

	private IEnumerator AnimateList()
	{
		float currentSize = this.rectTransform.sizeDelta.y;
		float targetSize = (this.IsExpanded ? (this.elementHeight * (float)(this.backupElements.Count + 1)) : this.elementHeight);
		float sign = Mathf.Sign(targetSize - currentSize);
		float velocity = 0f;
		for (;;)
		{
			currentSize = Mathf.SmoothDamp(currentSize, targetSize, ref velocity, 0.06f);
			if (currentSize * sign + 0.1f > targetSize * sign)
			{
				currentSize = targetSize;
			}
			this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, currentSize);
			ManageSavesMenuElement.HeightChangedHandler heightChanged = this.HeightChanged;
			if (heightChanged != null)
			{
				heightChanged();
			}
			if (currentSize == targetSize)
			{
				break;
			}
			yield return null;
		}
		this.listAnimationCoroutine = null;
		yield break;
	}

	[SerializeField]
	private Button primaryElement;

	[SerializeField]
	private Button backupElement;

	[SerializeField]
	private GameObject selectedBackground;

	[SerializeField]
	private Button arrow;

	[SerializeField]
	private Text nameText;

	[SerializeField]
	private Text sizeText;

	[SerializeField]
	private Text backupCountText;

	[SerializeField]
	private Text dateText;

	[SerializeField]
	private RectTransform sourceParent;

	private float elementHeight = 32f;

	private List<ManageSavesMenuElement.BackupElement> backupElements = new List<ManageSavesMenuElement.BackupElement>();

	private Coroutine arrowAnimationCoroutine;

	private Coroutine listAnimationCoroutine;

	public delegate void BackupElementClickedHandler();

	private class BackupElement
	{

		public BackupElement(GameObject guiInstance, SaveFile backup, ManageSavesMenuElement.BackupElementClickedHandler clickedCallback)
		{
			this.GuiInstance = guiInstance;
			this.GuiInstance.SetActive(true);
			this.Button = this.GuiInstance.GetComponent<Button>();
			this.UpdateElement(backup, clickedCallback);
		}

		public void UpdateElement(SaveFile backup, ManageSavesMenuElement.BackupElementClickedHandler clickedCallback)
		{
			this.File = backup;
			this.Button.onClick.RemoveAllListeners();
			this.Button.onClick.AddListener(delegate
			{
				ManageSavesMenuElement.BackupElementClickedHandler clickedCallback2 = clickedCallback;
				if (clickedCallback2 == null)
				{
					return;
				}
				clickedCallback2();
			});
			string text = backup.FileName;
			if (SaveSystem.IsCorrupt(backup))
			{
				text += " [CORRUPT]";
			}
			if (SaveSystem.IsWorldWithMissingMetaFile(backup))
			{
				text += " [MISSING META FILE]";
			}
			this.rectTransform.Find("name").GetComponent<Text>().text = text;
			this.rectTransform.Find("size").GetComponent<Text>().text = FileHelpers.BytesAsNumberString(backup.Size, 1U);
			this.rectTransform.Find("date").GetComponent<Text>().text = backup.LastModified.ToShortDateString();
			Transform transform = this.rectTransform.Find("source");
			Transform transform2 = transform.Find("source_cloud");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Cloud);
			}
			Transform transform3 = transform.Find("source_local");
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Local);
			}
			Transform transform4 = transform.Find("source_legacy");
			if (transform4 == null)
			{
				return;
			}
			transform4.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Legacy);
		}

		public SaveFile File { get; private set; }

		public GameObject GuiInstance { get; private set; }

		public Button Button { get; private set; }

		public RectTransform rectTransform
		{
			get
			{
				return this.GuiInstance.transform as RectTransform;
			}
		}
	}

	public delegate void HeightChangedHandler();

	public delegate void ElementClickedHandler(ManageSavesMenuElement element, int backupElementIndex);

	public delegate void ElementExpandedChangedHandler(ManageSavesMenuElement element, bool isExpanded);
}
