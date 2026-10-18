@echo off
setlocal enabledelayedexpansion

rem ---------------------------------------------------------------
rem  Adofai Loading Screen - build script
rem  Usage:
rem     set ADOFAI_DIR=D:\Steam\steamapps\common\A Dance of Fire and Ice
rem     build.bat
rem ---------------------------------------------------------------

set ROOT=%~dp0
if "%ADOFAI_DIR%"=="" set ADOFAI_DIR=D:\Steam\steamapps\common\A Dance of Fire and Ice

set MANAGED=%ADOFAI_DIR%\A Dance of Fire and Ice_Data\Managed
set UMM=%MANAGED%\UnityModManager
set OUT=%ROOT%build\PhigrosLoading.dll

if not exist "%MANAGED%\Assembly-CSharp.dll" (
    echo [error] game assemblies not found under "%MANAGED%"
    echo         set ADOFAI_DIR to your game folder and try again.
    exit /b 1
)

rem ---- locate a Roslyn csc.exe -----------------------------------
set CSC=
if not "%ROSC%"=="" set CSC=%ROSC%

if "%CSC%"=="" if exist "%ROOT%tools\roslyn\tasks\net472\csc.exe" set CSC=%ROOT%tools\roslyn\tasks\net472\csc.exe

if "%CSC%"=="" (
    for /f "usebackq tokens=*" %%i in (`where.exe /R "%ProgramFiles(x86)%\Microsoft Visual Studio" csc.exe 2^>nul`) do (
        if "%CSC%"=="" set CSC=%%i
    )
)

if "%CSC%"=="" (
    for /f "usebackq tokens=*" %%i in (`where.exe /R "%ProgramFiles%\Microsoft Visual Studio" csc.exe 2^>nul`) do (
        if "%CSC%"=="" set CSC=%%i
    )
)

if "%CSC%"=="" (
    echo [error] csc.exe not found.
    echo         Set ROSC to a Roslyn csc.exe, or put one at tools\roslyn\tasks\net472\csc.exe.
    exit /b 1
)

echo using compiler: %CSC%
if not exist "%ROOT%build" mkdir "%ROOT%build"

"%CSC%" /nologo /target:library /nostdlib+ /noconfig /langversion:latest /codepage:65001 /optimize+ /debug- ^
  /out:"%OUT%" ^
  /r:"%MANAGED%\mscorlib.dll" ^
  /r:"%MANAGED%\System.dll" ^
  /r:"%MANAGED%\System.Core.dll" ^
  /r:"%MANAGED%\System.Xml.dll" ^
  /r:"%MANAGED%\netstandard.dll" ^
  /r:"%MANAGED%\Newtonsoft.Json.dll" ^
  /r:"%MANAGED%\UnityEngine.CoreModule.dll" ^
  /r:"%MANAGED%\UnityEngine.UIModule.dll" ^
  /r:"%MANAGED%\UnityEngine.UI.dll" ^
  /r:"%MANAGED%\UnityEngine.TextRenderingModule.dll" ^
  /r:"%MANAGED%\UnityEngine.ImageConversionModule.dll" ^
  /r:"%MANAGED%\UnityEngine.IMGUIModule.dll" ^
  /r:"%MANAGED%\UnityEngine.InputLegacyModule.dll" ^
  /r:"%MANAGED%\Assembly-CSharp.dll" ^
  /r:"%MANAGED%\Assembly-CSharp-firstpass.dll" ^
  /r:"%MANAGED%\RDTools.dll" ^
  /r:"%UMM%\UnityModManager.dll" ^
  /r:"%UMM%\0Harmony.dll" ^
  "%ROOT%src\*.cs"

echo exit=%errorlevel%
endlocal
