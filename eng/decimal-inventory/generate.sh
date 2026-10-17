#!/bin/sh
set -eu
[ "${1:-}" = '--write' ] || { echo 'Explicit inventory maintenance requires --write' >&2; exit 1; }
cd "$(dirname "$0")/../.."
mkdir -p artifacts/decimal-inventory
cat > artifacts/decimal-inventory/Inventory.csproj <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="../../eng/decimal-inventory/Program.cs" Link="Program.cs" /><ProjectReference Include="../../src/Morphir.Frontend.CSharp/Morphir.Frontend.CSharp.csproj" /></ItemGroup></Project>
PROJECT
# No local Program.cs: the maintained source is compiled explicitly above.
rm -f artifacts/decimal-inventory/Program.cs
dotnet restore artifacts/decimal-inventory/Inventory.csproj --packages .packages
dotnet run --project artifacts/decimal-inventory/Inventory.csproj --no-restore > artifacts/decimal-members.txt
python3 eng/decimal-inventory/generate.py --write
