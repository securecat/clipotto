@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo csc.exe not found. .NET Framework 4.x is required.
  pause
  exit /b 1
)
set ICONOPTS=
if exist Clipotto.ico (
  set ICONOPTS=/win32icon:Clipotto.ico /resource:Clipotto.ico,Clipotto.ico
  echo Using Clipotto.ico
) else (
  echo Clipotto.ico not found. Building with default icon.
)
"%CSC%" /nologo /codepage:65001 /target:winexe /out:Clipotto.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll %ICONOPTS% Clipotto.cs
if errorlevel 1 (
  echo Build failed.
) else (
  echo Build succeeded: Clipotto.exe
)
pause
