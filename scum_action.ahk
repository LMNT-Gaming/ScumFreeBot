#Requires AutoHotkey v2.0
#SingleInstance Force

SendMode "Event"
SetTitleMatchMode 2
SetKeyDelay 25, 25

exeName := "SCUM.exe"
chatKey := "t"
openChatWait := 900
retries := 12
retrySleep := 250

if (A_Args.Length < 2) {
    MsgBox "
    (
Usage:
scum_action.ahk cmd "#ListPlayers"
    )"
    ExitApp 1
}

action := A_Args[1]

if (action != "cmd") {
    ExitApp 9
}

cmd := A_Args[2]

; --- SCUM Prozess suchen ---
pid := 0
Loop retries {
    pid := ProcessExist(exeName)
    if pid
        break

    Sleep retrySleep
}

if !pid
    ExitApp 2

; --- SCUM Fenster suchen ---
hwnd := 0
Loop retries {
    hwnd := WinExist("ahk_pid " pid)
    if hwnd
        break

    Sleep retrySleep
}

if !hwnd
    ExitApp 3

; --- SCUM aktivieren ---
WinRestore "ahk_id " hwnd
Sleep 100

WinActivate "ahk_id " hwnd

if !WinWaitActive("ahk_id " hwnd, , 3)
    ExitApp 4

Sleep 300

; --- Fokus-Klick in Fenstermitte ---
WinGetPos &x, &y, &w, &h, "ahk_id " hwnd
cx := x + (w // 2)
cy := y + (h // 2)

MouseGetPos &mx, &my
Click cx, cy
Sleep 200
MouseMove mx, my, 0

; --- Chat öffnen ---
SendEvent "{" chatKey "}"
Sleep openChatWait

; --- Eingabe säubern ---
SendEvent "^a"
Sleep 80
SendEvent "{Backspace}"
Sleep 80

; --- Befehl/Text schreiben ---
SendText cmd
Sleep 150

; --- Absenden ---
SendEvent "{Enter}"
Sleep 150

ExitApp 0