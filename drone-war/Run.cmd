@echo off
REM Spusteni DroneWar aplikace se scenarem
REM Pouziti: Run.cmd "Cislo_scenare"

REM Pokud neni zadany parametr, pouzij 0
if "%~1"=="" (
    set "SCENARIO=0"
) else (
    set "SCENARIO=%~1"
)

REM Vystupni slozka vytvorena Build.cmd
set "OUT_DIR=bin\Release\net8.0"
set "EXE_NAME=DroneWar.exe"
set "DLL_NAME=DroneWar.dll"

if not exist "%OUT_DIR%" (
    echo Spustitelny adresar nenalezen: %OUT_DIR%
    echo Zkuste nejprve spustit Build.cmd pro kompilaci aplikace.
    exit /b 1
)

REM Zkontrolovat, zda existuje datovy soubor scenare v distribuovanych datech
if not exist "%OUT_DIR%\data\%SCENARIO%.ter" (
    echo Varovani: Datovy soubor scenare '%OUT_DIR%\data\%SCENARIO%.ter' nenalezen!
    echo Aplikace se pokusi spustit, ale muze selhat.
)

echo Spoustim DroneWar se scenarem: %SCENARIO%
pushd "%OUT_DIR%"
REM Prioritne pouzij EXE, jinak pokud existuje DLL spust pouzitim dotnet
if exist "%EXE_NAME%" (
    "%CD%\%EXE_NAME%" %SCENARIO%
    set "EXITCODE=%ERRORLEVEL%"
    popd
    exit /b %EXITCODE%
) else if exist "%DLL_NAME%" (
    dotnet "%CD%\%DLL_NAME%" %SCENARIO%
    set "EXITCODE=%ERRORLEVEL%"
    popd
    exit /b %EXITCODE%
) else (
    echo Spustitelny soubor nebyl nalezen v: %OUT_DIR%\%EXE_NAME% ani %OUT_DIR%\%DLL_NAME%
    popd
    exit /b 1
)
