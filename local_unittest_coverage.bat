@echo off
@setlocal

set start=%time%

:: Runs your command
call :main

set end=%time%
set options="tokens=1-4 delims=:.,"
for /f %options% %%a in ("%start%") do set start_h=%%a&set /a start_m=100%%b %% 100&set /a start_s=100%%c %% 100&set /a start_ms=100%%d %% 100
for /f %options% %%a in ("%end%") do set end_h=%%a&set /a end_m=100%%b %% 100&set /a end_s=100%%c %% 100&set /a end_ms=100%%d %% 100

set /a hours=%end_h%-%start_h%
set /a mins=%end_m%-%start_m%
set /a secs=%end_s%-%start_s%
set /a ms=%end_ms%-%start_ms%
if %ms% lss 0 set /a secs = %secs% - 1 & set /a ms = 100%ms%
if %secs% lss 0 set /a mins = %mins% - 1 & set /a secs = 60%secs%
if %mins% lss 0 set /a hours = %hours% - 1 & set /a mins = 60%mins%
if %hours% lss 0 set /a hours = 24%hours%
if 1%ms% lss 100 set ms=0%ms%

:: Mission accomplished
set /a totalsecs = %hours%*3600 + %mins%*60 + %secs%
echo command took %hours%:%mins%:%secs%.%ms% (%totalsecs%.%ms%s total)
pause
goto :eof

:main
    setlocal EnableDelayedExpansion
	
	:: region init env
        for /F %%a in ('echo prompt $E ^| cmd') do (
        set "ESC=%%a"
        )
        set DEBUG=false
	:: endregion
	
	
	call :logvb "checking required package..."
	
    :: region get global tool list
        set count=0
        FOR /F "delims=" %%a IN ('dotnet tool list -g') do (
            SET iml[!count!]=%%a
            set /A count+=1
        )
        set /A count-=1
    :: endregion


    call :isInstalled dotnet-reportgenerator-globaltool
    if !isInstalledReturn!==1 (
        call :loginfo "    dotnet-reportgenerator-globaltool is installed"
    ) else (
        call :logerror "    dotnet-reportgenerator-globaltool is not installed"
        call :logvb "installing dotnet-reportgenerator-globaltool..."
        dotnet tool install -g dotnet-reportgenerator-globaltool -v n
    )

    call :isInstalled dotnet-coverage
    if !isInstalledReturn!==1 (
        call :loginfo "    dotnet-coverage is installed"
    ) else (
        call :logerror "    dotnet-coverage is not installed"
        call :logvb "installing dotnet-coverage..."
        dotnet tool install -g dotnet-coverage -v n
    )


    dotnet coverage collect dotnet test --output .\TestResult\CodeCoverage --output-format cobertura
    reportgenerator -reports:".\TestResult\CodeCoverage" -targetdir:".\TestResult\CoverageReport" -reporttypes:HtmlInline;Cobertura -assemblyfilters:+ArtWiz
    start "" ".\TestResult\CoverageReport\index.html"
goto :eof

:showToolList
    (for /L %%i in (0,1,!count!) do (
    echo !iml[%%i]!
    ))
goto :eof

:isInstalled
    for /L %%i in (0,1,!count!) do (
        call :isContained "!iml[%%i]!" %1
        if !isContainedReturn!==1 (
            call :logdebug "isInstalled_isContainedReturn=!isContainedReturn!"
            set isInstalledReturn=1
            goto :eof
        )
    )
goto :eof

:: Eg: call :isContained mxbd bd
:isContained
    set str1=%~1
    set pat=%~2
    call :logdebug "calling isContained_!str1!,!pat!_"
    if not "x!str1:%~2%=!"=="x!str1!" (
        call :logdebug "'!str1!' contains: '!pat!'"
        set isContainedReturn=1
        call :logdebug "isContained_isContainedReturn=!isContainedReturn!"
    )
goto :eof


:logvb
    echo !ESC![37m%~1!ESC![0m
goto :eof

:logdebug
    if "%DEBUG%"=="true" (
        echo !ESC![32m"%~1!ESC![0m
    )
goto :eof

:logerror
    echo !ESC![31m%~1!ESC![0m
goto :eof

:loginfo
    echo !ESC![36m%~1!ESC![0m
goto :eof
