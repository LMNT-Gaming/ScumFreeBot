#Requires AutoHotkey v2.0
#SingleInstance Force

exeName := "SCUM.exe"
chatKey := "{t}"
openChatWait := 900
retries := 12
retrySleep := 250

if (A_Args.Length < 2) {
    MsgBox "
    (
Usage:
scum_action.ahk cmd "#ListPlayers"
scum_action.ahk look right 600
    )"
    ExitApp 1
}


action := A_Args[1]
arg2 := A_Args[2]
amount := (A_Args.Length >= 3) ? Integer(A_Args[3]) : 300

; --- activate SCUM by PID ---
pid := 0
Loop retries {
    pid := ProcessExist(exeName)
    if pid
        break
    Sleep retrySleep
}
if !pid
    ExitApp 2

hwnd := 0
Loop retries {
    hwnd := WinExist("ahk_pid " pid)
    if hwnd
        break
    Sleep retrySleep
}
if !hwnd
    ExitApp 3

WinRestore "ahk_id " hwnd
WinActivate "ahk_id " hwnd
if !WinWaitActive("ahk_id " hwnd, , 3)
    ExitApp 4

; RDP focus unlock click center
WinGetPos &x, &y, &w, &h, "ahk_id " hwnd
cx := x + (w // 2)
cy := y + (h // 2)
MouseGetPos &mx, &my
Click cx, cy
Sleep 120

if (action = "look") {
    dx := 0, dy := 0
    if (arg2 = "right")
        dx := amount
    else if (arg2 = "left")
        dx := -amount
    else if (arg2 = "up")
        dy := -amount
    else if (arg2 = "down")
        dy := amount
    else {
        MsgBox "Unknown look dir: " arg2
        ExitApp 5
    }

    steps := 10
    stepX := dx / steps
    stepY := dy / steps
    Loop steps {
        MouseMove mx + Round(stepX*A_Index), my + Round(stepY*A_Index), 0
        Sleep 10
    }
    MouseMove mx, my, 0
    ExitApp 0
}

if (action = "cmd") {
    cmd := ""
    for i, a in A_Args {
        if (i <= 1)
            continue
        cmd .= (cmd = "" ? "" : " ") a
    }

    ; --- Chat zuverlässig öffnen ---
    ; Versuch 1
    Send chatKey
    Sleep 120
    ; Manche Setups brauchen ein zweites Drücken (RDP/Focus/Overlay)
    Send chatKey
    Sleep openChatWait  ; deine 900ms sind ok

    ; --- Eingabe säubern & Befehl schicken ---
    ; (Wenn Chat offen ist, wirkt ^a / backspace im Inputfeld)
    Send "^a"
    Sleep 30
    Send "{Backspace}"
    Sleep 30

    ; SendText ist oft stabiler als Clipboard
    SendText cmd
    Sleep 60
    Send "{Enter}"
    Sleep 120

    ; Chat schließen (optional). Wenn du willst:
    Send "{Esc}"

    ExitApp 0
}


ExitApp 9
