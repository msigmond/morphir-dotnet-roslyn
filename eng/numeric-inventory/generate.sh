#!/bin/sh
set -eu
[ "${1:-}" = '--write' ] || { echo 'Explicit inventory maintenance requires --write' >&2; exit 1; }
cd "$(dirname "$0")/../.."
mkdir -p artifacts/numeric-inventory
cat > artifacts/numeric-inventory/Inventory.csproj <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="../../eng/numeric-inventory/Program.cs" Link="Program.cs" /><ProjectReference Include="../../src/Morphir.Frontend.CSharp/Morphir.Frontend.CSharp.csproj" /></ItemGroup></Project>
PROJECT
# No local Program.cs: the maintained source is compiled explicitly above.
rm -f artifacts/numeric-inventory/Program.cs
dotnet restore artifacts/numeric-inventory/Inventory.csproj --packages .packages
dotnet run --project artifacts/numeric-inventory/Inventory.csproj --no-restore > artifacts/numeric-members.txt
python3 eng/numeric-inventory/generate.py --write
