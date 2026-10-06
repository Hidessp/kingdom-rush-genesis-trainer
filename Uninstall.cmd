@echo off
cd /d "%~dp0"
where py >nul 2>nul
if errorlevel 1 goto usepython
py -3 install.py --uninstall %*
goto finish
:usepython
python install.py --uninstall %*
:finish
if errorlevel 1 echo Operation failed. Close the game and check that Python 3 is installed.
pause
