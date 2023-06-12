# Valheim Source Code Changes

## Info
- Valheim Latest Version: 0.216.9
- Valheim Previous Version: 0.215.2

### Steps to Update
1.) Extract Source
1b.) Open assembly_valheim in "Valheim\valheim_Data\Managed" in DNSpy
1c.) Export to Project with DNSpy

2.) Use Notepad++ to Remove Token Tags with Regex
2a.) ^\s*\/\/\s*\((get|add|remove|[^\)]+)\). and ^\s*\/\/\s*Token.*$ and ^\s*Token.*$

3.) Use WinMerge to Check Differerances
3a.) Open the old version dump directory and new version dump directory in Minmerge and Compare
3b.) Genererate an HTML Report for each folder of changes

