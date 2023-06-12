# Valheim Source Code Changes

## Info
- Valheim Latest Version: 0.216.9
- Valheim Previous Version: 0.215.2

## Steps to Update
### Extract Source
- Open assembly_valheim in "Valheim\valheim_Data\Managed" in DNSpy
- Export to Project with DNSpy

### Use Notepad++ to Remove Token Tags with Regex
- ^\s*\/\/\s*\((get|add|remove|[^\)]+)\). and ^\s*\/\/\s*Token.*$ and ^\s*Token.*$

### Use WinMerge to Check Differerances
- Open the old version dump directory and new version dump directory in Minmerge and Compare
- Genererate an HTML Report for each folder of changes

