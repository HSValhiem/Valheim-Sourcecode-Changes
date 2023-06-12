using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ManageSavesMenu : MonoBehaviour
{

	private void Update()
	{
		bool flag = false;
		if (!this.blockerInfo.IsBlocked())
		{
			bool flag2 = true;
			if (Input.GetKeyDown(KeyCode.LeftArrow) && this.IsSelectedExpanded())
			{
				this.CollapseSelected();
				flag = true;
				flag2 = false;
			}
			if (Input.GetKeyDown(KeyCode.RightArrow) && !this.IsSelectedExpanded())
			{
				this.ExpandSelected();
				flag = true;
			}
			if (flag2)
			{
				if (Input.GetKeyDown(KeyCode.DownArrow))
				{
					this.SelectRelative(1);
					flag = true;
				}
				if (Input.GetKeyDown(KeyCode.UpArrow))
				{
					this.SelectRelative(-1);
					flag = true;
				}
			}
			if (ZInput.IsGamepadActive())
			{
				if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
				{
					this.SelectRelative(1);
					flag = true;
				}
				if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
				{
					this.SelectRelative(-1);
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.UpdateButtons();
			this.CenterSelected();
			return;
		}
		this.UpdateButtonsInteractable();
	}

	private void LateUpdate()
	{
		if (this.elementHeightChanged)
		{
			this.elementHeightChanged = false;
			this.UpdateElementPositions();
		}
	}

	private void UpdateButtons()
	{
		this.moveButton.gameObject.SetActive(FileHelpers.m_cloudEnabled && !FileHelpers.m_cloudOnly);
		if (this.selectedSaveIndex < 0)
		{
			this.actionButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$menu_expand");
		}
		else
		{
			if (this.selectedBackupIndex < 0)
			{
				if (this.listElements[this.selectedSaveIndex].BackupCount > 0)
				{
					this.actionButton.GetComponentInChildren<Text>().text = Localization.instance.Localize(this.listElements[this.selectedSaveIndex].IsExpanded ? "$menu_collapse" : "$menu_expand");
				}
			}
			else
			{
				this.actionButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$menu_restorebackup");
			}
			if (this.selectedBackupIndex < 0)
			{
				if (!this.currentList[this.selectedSaveIndex].IsDeleted)
				{
					this.moveButton.GetComponentInChildren<Text>().text = Localization.instance.Localize((this.currentList[this.selectedSaveIndex].PrimaryFile.m_source != FileHelpers.FileSource.Cloud) ? "$menu_movetocloud" : "$menu_movetolocal");
				}
			}
			else
			{
				this.moveButton.GetComponentInChildren<Text>().text = Localization.instance.Localize((this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex].m_source != FileHelpers.FileSource.Cloud) ? "$menu_movetocloud" : "$menu_movetolocal");
			}
		}
		this.UpdateButtonsInteractable();
	}

	private void UpdateButtonsInteractable()
	{
		bool flag = (DateTime.Now - this.mostRecentBackupCreatedTime).TotalSeconds >= 1.0;
		bool flag2 = this.selectedSaveIndex >= 0 && this.selectedSaveIndex < this.listElements.Count;
		bool flag3 = flag2 && this.selectedBackupIndex >= 0;
		bool flag4 = flag2 && this.listElements[this.selectedSaveIndex].BackupCount > 0 && this.selectedBackupIndex < 0;
		this.actionButton.interactable = flag4 || (flag3 && flag);
		bool flag5 = flag2 && (this.selectedBackupIndex >= 0 || !this.currentList[this.selectedSaveIndex].IsDeleted);
		this.removeButton.interactable = flag5;
		this.moveButton.interactable = flag5 && flag;
	}

	private void OnSaveElementHeighChanged()
	{
		this.elementHeightChanged = true;
	}

	private void UpdateCloudUsageAsync(ManageSavesMenu.UpdateCloudUsageFinishedCallback callback = null)
	{
		if (FileHelpers.m_cloudEnabled)
		{
			this.PushPleaseWait();
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			ulong usedBytes = 0UL;
			ulong capacityBytes = 0UL;
			backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
			{
				usedBytes = FileHelpers.GetTotalCloudUsage();
				capacityBytes = FileHelpers.GetTotalCloudCapacity();
			};
			backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
			{
				this.storageUsed.gameObject.SetActive(true);
				this.storageBar.parent.gameObject.SetActive(true);
				this.storageUsed.text = Localization.instance.Localize("$menu_cloudstorageused", new string[]
				{
					FileHelpers.BytesAsNumberString(usedBytes, 1U),
					FileHelpers.BytesAsNumberString(capacityBytes, 1U)
				});
				this.storageBar.localScale = new Vector3(usedBytes / capacityBytes, this.storageBar.localScale.y, this.storageBar.localScale.z);
				this.PopPleaseWait();
				ManageSavesMenu.UpdateCloudUsageFinishedCallback callback3 = callback;
				if (callback3 == null)
				{
					return;
				}
				callback3();
			};
			backgroundWorker.RunWorkerAsync();
			return;
		}
		this.storageUsed.gameObject.SetActive(false);
		this.storageBar.parent.gameObject.SetActive(false);
		ManageSavesMenu.UpdateCloudUsageFinishedCallback callback2 = callback;
		if (callback2 == null)
		{
			return;
		}
		callback2();
	}

	private void OnBackButton()
	{
		this.Close();
	}

	private void OnRemoveButton()
	{
		if (this.selectedSaveIndex < 0)
		{
			return;
		}
		bool isBackup = this.selectedBackupIndex >= 0;
		string text;
		if (isBackup)
		{
			text = "$menu_removebackup";
		}
		else
		{
			int activeTab = this.tabHandler.GetActiveTab();
			if (activeTab != 0)
			{
				if (activeTab != 1)
				{
					text = "Remove?";
				}
				else
				{
					text = "$menu_removecharacter";
				}
			}
			else
			{
				text = "$menu_removeworld";
			}
		}
		SaveFile toDelete = (isBackup ? this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex] : this.currentList[this.selectedSaveIndex].PrimaryFile);
		UnifiedPopup.Push(new YesNoPopup(Localization.instance.Localize(text), isBackup ? toDelete.FileName : this.currentList[this.selectedSaveIndex].m_name, delegate
		{
			UnifiedPopup.Pop();
			this.DeleteSaveFile(toDelete, isBackup);
		}, delegate
		{
			UnifiedPopup.Pop();
		}, false));
	}

	private void OnMoveButton()
	{
		if (this.selectedSaveIndex < 0)
		{
			return;
		}
		bool flag = this.selectedBackupIndex >= 0;
		SaveFile saveFile = (flag ? this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex] : this.currentList[this.selectedSaveIndex].PrimaryFile);
		FileHelpers.FileSource fileSource = ((saveFile.m_source != FileHelpers.FileSource.Cloud) ? FileHelpers.FileSource.Cloud : FileHelpers.FileSource.Local);
		SaveFile saveFile2 = null;
		for (int i = 0; i < this.currentList[this.selectedSaveIndex].BackupFiles.Length; i++)
		{
			if (i != this.selectedBackupIndex && this.currentList[this.selectedSaveIndex].BackupFiles[i].m_source == fileSource && this.currentList[this.selectedSaveIndex].BackupFiles[i].FileName == saveFile.FileName)
			{
				saveFile2 = this.currentList[this.selectedSaveIndex].BackupFiles[i];
				break;
			}
		}
		if (saveFile2 == null && flag && !this.currentList[this.selectedSaveIndex].IsDeleted && this.currentList[this.selectedSaveIndex].PrimaryFile.m_source == fileSource && this.currentList[this.selectedSaveIndex].PrimaryFile.FileName == saveFile.FileName)
		{
			saveFile2 = this.currentList[this.selectedSaveIndex].PrimaryFile;
		}
		if (saveFile2 != null)
		{
			UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$menu_cantmovesave"), Localization.instance.Localize("$menu_duplicatefileprompttext", new string[] { saveFile.FileName }), delegate
			{
				UnifiedPopup.Pop();
			}, false));
			return;
		}
		if (SaveSystem.IsCorrupt(saveFile))
		{
			UnifiedPopup.Push(new WarningPopup("$menu_cantmovesave", "$menu_savefilecorrupt", delegate
			{
				UnifiedPopup.Pop();
			}, true));
			return;
		}
		this.MoveSource(saveFile, flag, fileSource);
	}

	private void OnPrimaryActionButton()
	{
		if (this.selectedSaveIndex < 0)
		{
			return;
		}
		if (this.selectedBackupIndex >= 0)
		{
			this.RestoreBackup();
			return;
		}
		if (this.listElements[this.selectedSaveIndex].BackupCount > 0)
		{
			this.listElements[this.selectedSaveIndex].SetExpanded(!this.listElements[this.selectedSaveIndex].IsExpanded, true);
			this.UpdateButtons();
		}
	}

	private void RestoreBackup()
	{
		SaveWithBackups saveWithBackups = this.currentList[this.selectedSaveIndex];
		SaveFile backup = this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex];
		UnifiedPopup.Push(new YesNoPopup(Localization.instance.Localize("$menu_backuprestorepromptheader"), saveWithBackups.IsDeleted ? Localization.instance.Localize("$menu_backuprestorepromptrecover", new string[] { saveWithBackups.m_name, backup.FileName }) : Localization.instance.Localize("$menu_backuprestorepromptreplace", new string[] { saveWithBackups.m_name, backup.FileName }), delegate
		{
			UnifiedPopup.Pop();
			base.<RestoreBackup>g__RestoreBackupAsync|2();
		}, delegate
		{
			UnifiedPopup.Pop();
		}, false));
	}

	private void UpdateGuiAfterFileModification(bool alwaysSelectSave = false)
	{
		string saveName = ((this.selectedSaveIndex >= 0) ? this.listElements[this.selectedSaveIndex].Save.m_name : "");
		string backupName = ((this.selectedSaveIndex >= 0 && this.selectedBackupIndex >= 0 && this.selectedBackupIndex < this.listElements[this.selectedSaveIndex].Save.BackupFiles.Length) ? this.listElements[this.selectedSaveIndex].Save.BackupFiles[this.selectedBackupIndex].FileName : "");
		int saveIndex = this.selectedSaveIndex;
		int backupIndex = this.selectedBackupIndex;
		this.DeselectCurrent();
		this.UpdateCloudUsageAsync(null);
		this.ReloadSavesAsync(delegate(bool success)
		{
			if (success)
			{
				base.<UpdateGuiAfterFileModification>g__UpdateGuiAsync|1();
				return;
			}
			this.ShowReloadError();
		});
	}

	public void OnWorldTab()
	{
		if (this.pleaseWaitCount > 0)
		{
			return;
		}
		this.ChangeList(SaveDataType.World);
	}

	public void OnCharacterTab()
	{
		if (this.pleaseWaitCount > 0)
		{
			return;
		}
		this.ChangeList(SaveDataType.Character);
	}

	private void ChangeList(SaveDataType dataType)
	{
		this.DeselectCurrent();
		this.currentList = SaveSystem.GetSavesByType(dataType);
		this.currentListType = dataType;
		this.UpdateSavesListGuiAsync(delegate
		{
			bool flag = false;
			if (!string.IsNullOrEmpty(this.m_queuedNameToSelect))
			{
				for (int i = 0; i < this.currentList.Length; i++)
				{
					if (!this.currentList[i].IsDeleted && this.currentList[i].PrimaryFile.FileName == this.m_queuedNameToSelect)
					{
						this.SelectByIndex(i, -1);
						flag = true;
						break;
					}
				}
				this.m_queuedNameToSelect = null;
			}
			if (!flag || this.listElements.Count <= 0)
			{
				this.SelectByIndex(0, -1);
			}
			if (this.selectedSaveIndex >= 0)
			{
				this.CenterSelected();
			}
			this.UpdateButtons();
		});
	}

	private void DeleteSaveFile(SaveFile file, bool isBackup)
	{
		this.PushPleaseWait();
		bool success = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			success = SaveSystem.Delete(file);
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			this.PopPleaseWait();
			if (!success)
			{
				ManageSavesMenu.<DeleteSaveFile>g__DeleteSaveFailed|37_2();
				ZLog.LogError("Failed to delete save " + file.FileName);
			}
			this.mostRecentBackupCreatedTime = DateTime.Now;
			ManageSavesMenu.SavesModifiedCallback savesModifiedCallback = this.savesModifiedCallback;
			if (savesModifiedCallback != null)
			{
				savesModifiedCallback(this.GetCurrentListType());
			}
			this.UpdateGuiAfterFileModification(false);
		};
		backgroundWorker.RunWorkerAsync();
	}

	private void MoveSource(SaveFile file, bool isBackup, FileHelpers.FileSource destinationSource)
	{
		this.PushPleaseWait();
		bool cloudQuotaExceeded = false;
		bool success = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			success = SaveSystem.MoveSource(file, isBackup, destinationSource, out cloudQuotaExceeded);
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			this.PopPleaseWait();
			if (cloudQuotaExceeded)
			{
				this.ShowCloudQuotaWarning();
			}
			else if (!success)
			{
				ManageSavesMenu.<MoveSource>g__MoveSourceFailed|38_2();
			}
			this.mostRecentBackupCreatedTime = DateTime.Now;
			ManageSavesMenu.SavesModifiedCallback savesModifiedCallback = this.savesModifiedCallback;
			if (savesModifiedCallback != null)
			{
				savesModifiedCallback(this.GetCurrentListType());
			}
			this.UpdateGuiAfterFileModification(false);
		};
		backgroundWorker.RunWorkerAsync();
	}

	private SaveDataType GetCurrentListType()
	{
		return this.currentListType;
	}

	private void ReloadSavesAsync(ManageSavesMenu.ReloadSavesFinishedCallback callback)
	{
		this.PushPleaseWait();
		Exception reloadException = null;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			try
			{
				SaveSystem.ForceRefreshCache();
			}
			catch (Exception ex)
			{
				reloadException = ex;
			}
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			this.currentList = SaveSystem.GetSavesByType(this.currentListType);
			this.PopPleaseWait();
			if (reloadException != null)
			{
				ZLog.LogError(reloadException.ToString());
			}
			ManageSavesMenu.ReloadSavesFinishedCallback callback2 = callback;
			if (callback2 == null)
			{
				return;
			}
			callback2(reloadException == null);
		};
		backgroundWorker.RunWorkerAsync();
	}

	private void UpdateElementPositions()
	{
		float num = 0f;
		for (int i = 0; i < this.listElements.Count; i++)
		{
			this.listElements[i].rectTransform.anchoredPosition = new Vector2(this.listElements[i].rectTransform.anchoredPosition.x, -num);
			num += this.listElements[i].rectTransform.sizeDelta.y;
		}
		this.listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
	}

	private IEnumerator UpdateElementPositionsEnumerator()
	{
		float pos = 0f;
		int num;
		for (int i = 0; i < this.listElements.Count; i = num + 1)
		{
			this.listElements[i].rectTransform.anchoredPosition = new Vector2(this.listElements[i].rectTransform.anchoredPosition.x, -pos);
			pos += this.listElements[i].rectTransform.sizeDelta.y;
			yield return null;
			num = i;
		}
		this.listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pos);
		yield break;
	}

	private ManageSavesMenuElement CreateElement()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.saveElement, this.listRoot);
		ManageSavesMenuElement component = gameObject.GetComponent<ManageSavesMenuElement>();
		gameObject.SetActive(true);
		component.HeightChanged += this.OnSaveElementHeighChanged;
		component.ElementClicked += this.OnElementClicked;
		component.ElementExpandedChanged += this.OnElementExpandedChanged;
		return component;
	}

	private void UpdateSavesListGui()
	{
		List<ManageSavesMenuElement> list = new List<ManageSavesMenuElement>();
		Dictionary<string, ManageSavesMenuElement> dictionary = new Dictionary<string, ManageSavesMenuElement>();
		for (int i = 0; i < this.listElements.Count; i++)
		{
			dictionary.Add(this.listElements[i].Save.m_name, this.listElements[i]);
		}
		for (int j = 0; j < this.currentList.Length; j++)
		{
			if (dictionary.ContainsKey(this.currentList[j].m_name))
			{
				dictionary[this.currentList[j].m_name].UpdateElement(this.currentList[j]);
				list.Add(dictionary[this.currentList[j].m_name]);
				dictionary.Remove(this.currentList[j].m_name);
			}
			else
			{
				ManageSavesMenuElement manageSavesMenuElement = this.CreateElement();
				manageSavesMenuElement.SetUp(manageSavesMenuElement.Save);
				list.Add(manageSavesMenuElement);
			}
		}
		foreach (KeyValuePair<string, ManageSavesMenuElement> keyValuePair in dictionary)
		{
			UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
		}
		this.listElements = list;
		this.UpdateElementPositions();
	}

	private IEnumerator UpdateSaveListGuiAsyncCoroutine(ManageSavesMenu.UpdateGuiListFinishedCallback callback)
	{
		this.PushPleaseWait();
		float timeBudget = 0.25f / (float)Application.targetFrameRate;
		DateTime dateTime = DateTime.Now;
		int num;
		for (int i = this.listElements.Count - 1; i >= 0; i = num - 1)
		{
			this.listElements[i].rectTransform.anchoredPosition = new Vector2(this.listElements[i].rectTransform.anchoredPosition.x, 1000000f);
			if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				dateTime = DateTime.Now;
			}
			num = i;
		}
		if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
		{
			yield return null;
			dateTime = DateTime.Now;
		}
		List<ManageSavesMenuElement> newSaveElementsList = new List<ManageSavesMenuElement>();
		Dictionary<string, ManageSavesMenuElement> saveNameToElementMap = new Dictionary<string, ManageSavesMenuElement>();
		for (int i = 0; i < this.listElements.Count; i = num + 1)
		{
			saveNameToElementMap.Add(this.listElements[i].Save.m_name, this.listElements[i]);
			if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				dateTime = DateTime.Now;
			}
			num = i;
		}
		for (int i = 0; i < this.currentList.Length; i = num + 1)
		{
			if (saveNameToElementMap.ContainsKey(this.currentList[i].m_name))
			{
				IEnumerator updateElementEnumerator = saveNameToElementMap[this.currentList[i].m_name].UpdateElementEnumerator(this.currentList[i]);
				while (updateElementEnumerator.MoveNext())
				{
					if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
					{
						yield return null;
						dateTime = DateTime.Now;
					}
				}
				newSaveElementsList.Add(saveNameToElementMap[this.currentList[i].m_name]);
				saveNameToElementMap.Remove(this.currentList[i].m_name);
				updateElementEnumerator = null;
			}
			else
			{
				ManageSavesMenuElement manageSavesMenuElement = this.CreateElement();
				newSaveElementsList.Add(manageSavesMenuElement);
				newSaveElementsList[newSaveElementsList.Count - 1].rectTransform.anchoredPosition = new Vector2(newSaveElementsList[newSaveElementsList.Count - 1].rectTransform.anchoredPosition.x, 1000000f);
				IEnumerator updateElementEnumerator = manageSavesMenuElement.SetUpEnumerator(this.currentList[i]);
				while (updateElementEnumerator.MoveNext())
				{
					if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
					{
						yield return null;
						dateTime = DateTime.Now;
					}
				}
				updateElementEnumerator = null;
			}
			if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				dateTime = DateTime.Now;
			}
			num = i;
		}
		foreach (KeyValuePair<string, ManageSavesMenuElement> keyValuePair in saveNameToElementMap)
		{
			UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
			if ((DateTime.Now - dateTime).TotalSeconds > (double)timeBudget)
			{
				yield return null;
				dateTime = DateTime.Now;
			}
		}
		Dictionary<string, ManageSavesMenuElement>.Enumerator enumerator = default(Dictionary<string, ManageSavesMenuElement>.Enumerator);
		this.listElements = newSaveElementsList;
		if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
		{
			yield return null;
			dateTime = DateTime.Now;
		}
		IEnumerator updateElementPositionsEnumerator = this.UpdateElementPositionsEnumerator();
		while (updateElementPositionsEnumerator.MoveNext())
		{
			if ((DateTime.Now - dateTime).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				dateTime = DateTime.Now;
			}
		}
		this.PopPleaseWait();
		if (callback != null)
		{
			callback();
		}
		yield break;
		yield break;
	}

	private void UpdateSavesListGuiAsync(ManageSavesMenu.UpdateGuiListFinishedCallback callback)
	{
		base.StartCoroutine(this.UpdateSaveListGuiAsyncCoroutine(callback));
	}

	private void DestroyGui()
	{
		for (int i = 0; i < this.listElements.Count; i++)
		{
			UnityEngine.Object.Destroy(this.listElements[i].gameObject);
		}
		this.listElements.Clear();
	}

	public void Open(SaveDataType dataType, string selectedSaveName, ManageSavesMenu.ClosedCallback closedCallback, ManageSavesMenu.SavesModifiedCallback savesModifiedCallback)
	{
		this.QueueSelectByName(selectedSaveName);
		this.Open(dataType, closedCallback, savesModifiedCallback);
	}

	public void Open(SaveDataType dataType, ManageSavesMenu.ClosedCallback closedCallback, ManageSavesMenu.SavesModifiedCallback savesModifiedCallback)
	{
		this.closedCallback = closedCallback;
		this.savesModifiedCallback = savesModifiedCallback;
		if (base.gameObject.activeSelf && this.tabHandler.GetActiveTab() == this.GetTabIndexFromSaveDataType(dataType))
		{
			return;
		}
		this.backButton.onClick.AddListener(new UnityAction(this.OnBackButton));
		this.removeButton.onClick.AddListener(new UnityAction(this.OnRemoveButton));
		this.moveButton.onClick.AddListener(new UnityAction(this.OnMoveButton));
		this.actionButton.onClick.AddListener(new UnityAction(this.OnPrimaryActionButton));
		this.storageUsed.gameObject.SetActive(false);
		this.storageBar.parent.gameObject.SetActive(false);
		base.gameObject.SetActive(true);
		this.UpdateCloudUsageAsync(null);
		this.ReloadSavesAsync(delegate(bool success)
		{
			if (!success)
			{
				this.ShowReloadError();
			}
			this.tabHandler.SetActiveTabWithoutInvokingOnClick(this.GetTabIndexFromSaveDataType(dataType));
			this.ChangeList(dataType);
		});
	}

	private void QueueSelectByName(string name)
	{
		this.m_queuedNameToSelect = name;
	}

	private int GetTabIndexFromSaveDataType(SaveDataType dataType)
	{
		if (dataType == SaveDataType.World)
		{
			return 0;
		}
		if (dataType != SaveDataType.Character)
		{
			throw new ArgumentException(string.Format("{0} does not have a tab!", dataType));
		}
		return 1;
	}

	public void Close()
	{
		this.DestroyGui();
		this.backButton.onClick.RemoveListener(new UnityAction(this.OnBackButton));
		this.removeButton.onClick.RemoveListener(new UnityAction(this.OnRemoveButton));
		this.moveButton.onClick.RemoveListener(new UnityAction(this.OnMoveButton));
		this.actionButton.onClick.RemoveListener(new UnityAction(this.OnPrimaryActionButton));
		base.gameObject.SetActive(false);
		ManageSavesMenu.ClosedCallback closedCallback = this.closedCallback;
		if (closedCallback == null)
		{
			return;
		}
		closedCallback();
	}

	public bool IsVisible()
	{
		return base.gameObject.activeInHierarchy;
	}

	private void SelectByIndex(int saveIndex, int backupIndex = -1)
	{
		this.DeselectCurrent();
		this.selectedSaveIndex = saveIndex;
		this.selectedBackupIndex = backupIndex;
		if (this.listElements.Count <= 0)
		{
			this.selectedSaveIndex = -1;
			this.selectedBackupIndex = -1;
			return;
		}
		this.selectedSaveIndex = Mathf.Clamp(this.selectedSaveIndex, 0, this.listElements.Count - 1);
		this.listElements[this.selectedSaveIndex].Select(ref this.selectedBackupIndex);
	}

	private void SelectRelative(int offset)
	{
		int num = this.selectedSaveIndex;
		int num2 = this.selectedBackupIndex;
		this.DeselectCurrent();
		if (this.listElements.Count <= 0)
		{
			this.selectedSaveIndex = -1;
			this.selectedBackupIndex = -1;
			return;
		}
		if (num < 0)
		{
			num = 0;
			num2 = -1;
		}
		else if (num > this.listElements.Count - 1)
		{
			num = this.listElements.Count - 1;
			num2 = (this.listElements[num].IsExpanded ? this.listElements[num].BackupCount : (-1));
		}
		int num4;
		for (int num3 = offset; num3 != 0; num3 -= num4)
		{
			num4 = Math.Sign(num3);
			if (this.listElements[num].IsExpanded)
			{
				if (num2 + num4 < -1 || num2 + num4 > this.listElements[num].BackupCount - 1)
				{
					if (num + num4 >= 0 && num + num4 <= this.listElements.Count - 1)
					{
						num += num4;
						num2 = ((num4 < 0 && this.listElements[num].IsExpanded) ? (this.listElements[num].BackupCount - 1) : (-1));
					}
				}
				else
				{
					num2 += num4;
				}
			}
			else if (num2 >= 0)
			{
				if (num + num4 >= 0 && num + num4 <= this.listElements.Count - 1 && num4 > 0)
				{
					num += num4;
				}
				num2 = -1;
			}
			else if (num + num4 >= 0 && num + num4 <= this.listElements.Count - 1)
			{
				num += num4;
				num2 = ((num4 < 0 && this.listElements[num].IsExpanded) ? (this.listElements[num].BackupCount - 1) : (-1));
			}
		}
		this.SelectByIndex(num, num2);
	}

	private void DeselectCurrent()
	{
		if (this.selectedSaveIndex >= 0 && this.selectedSaveIndex <= this.listElements.Count - 1)
		{
			this.listElements[this.selectedSaveIndex].Deselect(this.selectedBackupIndex);
		}
		this.selectedSaveIndex = -1;
		this.selectedBackupIndex = -1;
	}

	private bool IsSelectedExpanded()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogError(string.Concat(new string[]
			{
				"Failed to expand save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				"."
			}));
			return false;
		}
		return this.listElements[this.selectedSaveIndex].IsExpanded;
	}

	private void ExpandSelected()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to expand save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.listElements[this.selectedSaveIndex].SetExpanded(true, true);
	}

	private void CollapseSelected()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to collapse save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.listElements[this.selectedSaveIndex].SetExpanded(false, true);
	}

	private void CenterSelected()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to center save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.scrollRectEnsureVisible.CenterOnItem(this.listElements[this.selectedSaveIndex].GetTransform(this.selectedBackupIndex));
	}

	private void OnElementClicked(ManageSavesMenuElement element, int backupElementIndex)
	{
		int num = this.selectedSaveIndex;
		int num2 = this.selectedBackupIndex;
		int num3 = this.listElements.IndexOf(element);
		this.DeselectCurrent();
		this.SelectByIndex(num3, backupElementIndex);
		if (this.selectedSaveIndex == num && this.selectedBackupIndex == num2 && Time.time < this.timeClicked + 0.5f)
		{
			this.OnPrimaryActionButton();
			this.timeClicked = Time.time - 0.5f;
		}
		else
		{
			this.timeClicked = Time.time;
		}
		this.UpdateButtons();
	}

	private void OnElementExpandedChanged(ManageSavesMenuElement element, bool isExpanded)
	{
		int num = this.listElements.IndexOf(element);
		if (this.selectedSaveIndex == num)
		{
			if (!isExpanded && this.selectedBackupIndex >= 0)
			{
				this.DeselectCurrent();
				this.SelectByIndex(num, -1);
			}
			this.UpdateButtons();
		}
	}

	public void ShowCloudQuotaWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_cloudstoragefull", "$menu_cloudstoragefulloperationfailed", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	public void ShowReloadError()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_reloadfailed", "$menu_checklogfile", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	private void PushPleaseWait()
	{
		if (this.pleaseWaitCount == 0)
		{
			this.pleaseWait.SetActive(true);
		}
		this.pleaseWaitCount++;
	}

	private void PopPleaseWait()
	{
		this.pleaseWaitCount--;
		if (this.pleaseWaitCount == 0)
		{
			this.pleaseWait.SetActive(false);
		}
	}

	[CompilerGenerated]
	internal static void <RestoreBackup>g__RestoreBackupFailed|32_3()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_backuprestorefailedheader", "$menu_tryagainorrestart", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	[CompilerGenerated]
	internal static void <DeleteSaveFile>g__DeleteSaveFailed|37_2()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_deletefailedheader", "$menu_tryagainorrestart", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	[CompilerGenerated]
	internal static void <MoveSource>g__MoveSourceFailed|38_2()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_movefailedheader", "$menu_tryagainorrestart", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	[SerializeField]
	private Button backButton;

	[SerializeField]
	private Button removeButton;

	[SerializeField]
	private Button moveButton;

	[SerializeField]
	private Button actionButton;

	[SerializeField]
	private GameObject saveElement;

	[SerializeField]
	private Text storageUsed;

	[SerializeField]
	private TabHandler tabHandler;

	[SerializeField]
	private RectTransform storageBar;

	[SerializeField]
	private RectTransform listRoot;

	[SerializeField]
	private ScrollRectEnsureVisible scrollRectEnsureVisible;

	[SerializeField]
	private UIGamePad blockerInfo;

	[SerializeField]
	private GameObject pleaseWait;

	private SaveWithBackups[] currentList;

	private SaveDataType currentListType;

	private DateTime mostRecentBackupCreatedTime = DateTime.MinValue;

	private List<ManageSavesMenuElement> listElements = new List<ManageSavesMenuElement>();

	private bool elementHeightChanged;

	private ManageSavesMenu.ClosedCallback closedCallback;

	private ManageSavesMenu.SavesModifiedCallback savesModifiedCallback;

	private string m_queuedNameToSelect;

	private int selectedSaveIndex = -1;

	private int selectedBackupIndex = -1;

	private float timeClicked;

	private const float doubleClickTime = 0.5f;

	private int pleaseWaitCount;

	public delegate void ClosedCallback();

	public delegate void SavesModifiedCallback(SaveDataType list);

	private delegate void UpdateCloudUsageFinishedCallback();

	private delegate void ReloadSavesFinishedCallback(bool success);

	private delegate void UpdateGuiListFinishedCallback();
}
