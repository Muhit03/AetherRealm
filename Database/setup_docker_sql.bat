@echo off
REM AetherRealm SQL Server Setup Script for Windows (Native ARM64 & x64)
echo Setting up Docker SQL Server for AetherRealm on Windows ARM64 / x64...

REM Try Azure SQL Edge first (Native ARM64 & x64 architecture support)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=AetherRealm@2024" -p 1433:1433 --name aether-sqlserver -d mcr.microsoft.com/azure-sql-edge:latest 2>NUL

if errorlevel 1 (
    echo Trying SQL Server 2022 container with platform emulation...
    docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=AetherRealm@2024" -p 1433:1433 --name aether-sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
)

echo Waiting for SQL Server container to initialize...
timeout /t 10 /nobreak > NUL

docker cp Database/AetherRealmDB_Schema.sql aether-sqlserver:/tmp/AetherRealmDB_Schema.sql
docker exec -i aether-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "AetherRealm@2024" -i /tmp/AetherRealmDB_Schema.sql 2>NUL
if errorlevel 1 (
    docker exec -i aether-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "AetherRealm@2024" -C -i /tmp/AetherRealmDB_Schema.sql
)

echo AetherRealm SQL Server setup complete on Windows ARM64 / x64!
pause
