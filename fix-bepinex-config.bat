@echo off
setlocal
echo ========================================================
echo House Access - Automatic BepInEx Configuration Fix
echo ========================================================
echo.

if not exist "BepInEx\config\BepInEx.cfg" (
    echo [!] BepInEx\config\BepInEx.cfg was not found in this folder.
    echo.
    echo Please make sure:
    echo 1. You have copied this script into your main House Party game folder
    echo    (right next to HouseParty.exe).
    echo 2. You have installed BepInEx 6 and launched House Party once so
    echo    BepInEx could generate its files.
    echo.
    pause
    exit /b 1
)

echo Found BepInEx.cfg! Updating UnityLogListening setting to false...
powershell -NoProfile -Command "(Get-Content 'BepInEx\config\BepInEx.cfg') -replace 'UnityLogListening\s*=\s*true', 'UnityLogListening = false' | Set-Content 'BepInEx\config\BepInEx.cfg'"

echo.
echo [SUCCESS] BepInEx configuration patched successfully!
echo The start-up crash is now prevented.
echo You can now launch House Party with your screen reader running.
echo.
pause
