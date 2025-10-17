@echo off
REM === CONFIGURATION ===
set "SOURCE=resources"
set "DEST=DualWield.oiv"
set "SEVENZIP=7z"

REM === CLEANUP OLD FILE ===
if exist "%DEST%" del "%DEST%"

REM === COMPRESS SOURCE TO OIV (ZIP FORMAT) ===
"%SEVENZIP%" a -tzip "%DEST%" "%SOURCE%\*" -mx9 -y

echo.
echo Done! Created OIV "%DEST%"
echo REMEMBER THAT ONLY RELEASE BUILDS ARE COPIED!
pause
