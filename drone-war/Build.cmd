@echo off
REM ============================================================
REM Build DroneWar
REM - Kompiluje projekt do root\bin\Release\net8.0
REM - Intermediate files (obj) presmeruje mimo repo do TEMP
REM Pouziti: Build.cmd
REM ============================================================

REM Prepnuti do adresare skriptu
pushd "%~dp0" || (
    echo [ERROR] Nelze se presunout do adresare skriptu: %~dp0
    exit /b 1
)

setlocal

set "CONFIG=Release"
set "TFM=net8.0"

REM ---------------------------------------------
REM Najdi csproj v rootu nebo ve slozce src
REM ---------------------------------------------
set "PROJECT_PATH="
set "PROJECT_DIR="

if exist "%~dp0DroneWar.csproj" (
    set "PROJECT_PATH=%~dp0DroneWar.csproj"
    set "PROJECT_DIR=%~dp0"
) else if exist "%~dp0src\DroneWar.csproj" (
    set "PROJECT_PATH=%~dp0src\DroneWar.csproj"
    set "PROJECT_DIR=%~dp0src\"
)

if "%PROJECT_PATH%"=="" (
    echo [ERROR] Soubor DroneWar.csproj nebyl nalezen.
    echo Ocekavano umisteni: %~dp0DroneWar.csproj nebo %~dp0src\DroneWar.csproj
    popd
    exit /b 1
)

REM ---------------------------------------------
REM Nastaveni vystupnich cest
REM ---------------------------------------------
set "OUT_DIR=%~dp0bin\%CONFIG%\%TFM%"
set "OBJ_DIR=%PROJECT_DIR%obj\%CONFIG%\%TFM%"

REM ---------------------------------------------
REM Vytvor adresare, pokud neexistuji
REM ---------------------------------------------
if not exist "%OUT_DIR%" mkdir "%OUT_DIR%" >nul
if not exist "%OBJ_DIR%" mkdir "%OBJ_DIR%" >nul

echo ============================================================
echo [BUILD] Projekt:  %PROJECT_PATH%
echo [BUILD] Cil:      %OUT_DIR%
echo [BUILD] Obj:      %OBJ_DIR%
echo ============================================================

REM ---------------------------------------------
REM Spusteni buildu
REM ---------------------------------------------
dotnet build "%PROJECT_PATH%" ^
    -c %CONFIG% ^
    -o "%OUT_DIR%" ^
    /p:BaseIntermediateOutputPath="%OBJ_DIR%/"

if errorlevel 1 (
    echo [ERROR] Build selhal.
    popd
    exit /b 1
)

echo [OK] Build probehl uspesne.

REM ---------------------------------------------
REM Kopirovani slozky data (pokud existuje)
REM ---------------------------------------------
if exist "%PROJECT_DIR%data" (
    echo [COPY] Kopiruji slozku data do "%OUT_DIR%\data" ...
    if not exist "%OUT_DIR%\data" mkdir "%OUT_DIR%\data" 2>nul
    xcopy "%PROJECT_DIR%data\*" "%OUT_DIR%\data\" /E /I /Y >nul
    echo [OK] Data zkopirovana.
) else (
    echo [WARN] Slozka "data" nebyla nalezena v "%PROJECT_DIR%data".
)

echo [INFO] Intermediate files (obj) jsou v: %OBJ_DIR%

endlocal
popd
exit /b 0
