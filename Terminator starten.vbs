' Startet Terminator (Herunterfahr-Timer) ohne schwarzes Konsolenfenster.
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
cmd = "powershell.exe -NoProfile -NonInteractive -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & dir & "\Terminator.ps1"""
CreateObject("WScript.Shell").Run cmd, 0, False
