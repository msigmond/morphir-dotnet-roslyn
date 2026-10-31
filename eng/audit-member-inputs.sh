#!/bin/sh
set -eu
[ "${1:-}" = '--write' ] || { echo 'Use --write for explicit member input maintenance.' >&2; exit 1; }
cd "$(dirname "$0")/.."
mkdir -p artifacts/d1d9-inventory
cat > artifacts/d1d9-inventory/Audit.csproj <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="../../eng/audit-member-inputs.cs" Link="Program.cs" /><ProjectReference Include="../../src/Morphir.Frontend.CSharp/Morphir.Frontend.CSharp.csproj" /></ItemGroup></Project>
PROJECT
dotnet restore artifacts/d1d9-inventory/Audit.csproj --packages .packages -m:1 -nr:false
dotnet run --project artifacts/d1d9-inventory/Audit.csproj --no-restore
