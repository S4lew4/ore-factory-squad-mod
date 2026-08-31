@echo off
chcp 65001 >nul
title Ore Factory Squad - Mod Installer
echo.
echo   Ore Factory Squad - Mod wird installiert...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
echo.
echo   Druecke eine Taste zum Schliessen.
pause >nul
