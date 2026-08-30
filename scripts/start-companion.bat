@echo off
rem 启动 Cab Radio Companion（托盘程序，无窗口；管理请点系统托盘图标）
rem Launch the Cab Radio Companion (tray app; manage via the system tray icon)
start "" "%~dp0..\src\QqMusicRadio.Companion\bin\Release\net8.0-windows\win-x64\publish\CabRadio.exe"
