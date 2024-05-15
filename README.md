# Valheim Source Code Changes

## Info
- Valheim Latest Version: 0.218.15
- Valheim Previous Version: 0.217.46

## Steps to Update
### Extract Source
- Open assembly_valheim in "Valheim\valheim_Data\Managed" in DNSpy
- Make sure "Show tokens, RVAs and file offsets" is unchecked
  
![image](https://github.com/HSValhiem/Valheim-Sourcecode-Changes/assets/18600015/73f23140-a317-4b83-b29c-0fa5f968d842)

- Export to Project with DNSpy
  
![image](https://github.com/HSValhiem/Valheim-Sourcecode-Changes/assets/18600015/28304d8b-9c0e-4f2c-97fa-817a0fb2ad21)

### Use Notepad++ to Remove Token Tags with Regex
- ^\s*\/\/\s*\((get|add|remove|[^\)]+)\). and ^\s*\/\/\s*Token.*$ and ^\s*Token.*$

### Use WinMerge to Check Differerances
- Open the old version dump directory as left side and new version dump directory as right side in Minmerge and Compare

![WinMergeU_Lgv7wIQoDL](https://github.com/HSValhiem/Valheim-Sourcecode-Changes/assets/18600015/d3fe3cc1-8197-4d63-b9bb-a742a29b4e39)

- Genererate an HTML Report for each folder of changes and export as index.html
