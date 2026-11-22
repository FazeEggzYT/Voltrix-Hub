@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find the Windows C# compiler.
  exit /b 1
)
"%CSC%" /nologo /target:winexe /optimize+ /out:VOLTRIX-Hub.exe /win32icon:voltrix.ico /resource:voltrix.ico,AppIcon.ico /resource:voltrix-logo.png,Logo.png /resource:CHANGELOG.md,Changelog.md /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll *.cs
if errorlevel 1 (
  echo Build failed.
  if "%CI%"=="" pause
  exit /b 1
)
echo Built VOLTRIX-Hub.exe
if "%CI%"=="" pause
exit /b 0
