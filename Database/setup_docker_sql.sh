#!/bin/bash
# AetherRealm SQL Server Docker Setup Script (macOS / Linux ARM64 & x64)

echo "Setting up Docker SQL Server for AetherRealm..."

# Run Azure SQL Edge container (Native ARM64 and x86_64 support)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=AetherRealm@2024" -p 1433:1433 --name aether-sqlserver -d mcr.microsoft.com/azure-sql-edge:latest || \
docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=AetherRealm@2024" -p 1433:1433 --name aether-sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

echo "Waiting for container to start..."
sleep 10

echo "Executing Database Schema and Stored Procedures..."
docker cp Database/AetherRealmDB_Schema.sql aether-sqlserver:/tmp/AetherRealmDB_Schema.sql
docker exec -i aether-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "AetherRealm@2024" -i /tmp/AetherRealmDB_Schema.sql 2>/dev/null || \
docker exec -i aether-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "AetherRealm@2024" -C -i /tmp/AetherRealmDB_Schema.sql

echo "Database setup complete!"
