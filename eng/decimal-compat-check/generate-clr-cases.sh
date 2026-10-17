#!/bin/sh
set -eu
cd "$(dirname "$0")/../.."
mkdir -p artifacts/decimal-clr-check
cat > artifacts/decimal-clr-check/ClrCases.csproj <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="../../eng/decimal-compat-check/ClrCases.cs" Link="Program.cs" /></ItemGroup></Project>
PROJECT
dotnet restore artifacts/decimal-clr-check/ClrCases.csproj --packages .packages --ignore-failed-sources
dotnet run --project artifacts/decimal-clr-check/ClrCases.csproj --no-restore
