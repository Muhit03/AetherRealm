@echo off
REM ============================================================
REM  AetherRealm - create the database on a local SQL Server
REM  (SQL Server Express / Developer - NO Docker)
REM ============================================================
REM
REM  If your instance is not the default one, change SERVER below,
REM  e.g.  set SERVER=localhost\SQLEXPRESS
REM
set SERVER=localhost

echo Creating AetherRealmDB on %SERVER% ...
sqlcmd -S %SERVER% -E -C -i "%~dp0AetherRealmDB_Schema.sql"

if errorlevel 1 (
    echo.
    echo Could not run sqlcmd. Make sure SQL Server is installed and running,
    echo and that "sqlcmd" is on your PATH ^(it ships with SQL Server / SSMS^).
    echo Try:  set SERVER=localhost\SQLEXPRESS   then run this file again.
) else (
    echo.
    echo Done. Now check Assets\StreamingAssets\db_config.txt points at %SERVER%.
)
pause
