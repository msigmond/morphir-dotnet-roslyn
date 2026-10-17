using System.Security.Cryptography;
using System.Text.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D1")]
public class ProvenanceTests
{
    [Fact]
    public void ReferenceHashesAreFrozenAndToolchainIsExact()
    {
        using var manifest = JsonDocument.Parse(Support.Read("fixtures/D1/expected/provenance.json"));
        var root = manifest.RootElement;
        Assert.Equal("2.100.0", root.GetProperty("toolchain").GetProperty("morphirElmVersion").GetString());
        Assert.Equal("71ca7ee385273b0af6f8db66582c26e9b410cbf2", root.GetProperty("toolchain").GetProperty("sourceCommit").GetString());
        foreach (var item in root.GetProperty("fixtures").EnumerateArray().Concat(root.GetProperty("sources").EnumerateArray()))
        {
            var path = item.TryGetProperty("output", out var output) ? output.GetString()! : item.GetProperty("path").GetString()!;
            var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(System.IO.Path.Combine(Support.Root, path))));
            Assert.Equal(item.GetProperty("sha256").GetString(), hash);
        }
    }
    [Fact]
    public void ProtectedLicenseBytesAndCanonicalAgentPointerArePreserved()
    {
        string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(System.IO.Path.Combine(Support.Root, p))));
        Assert.Equal("b40930bbcf80744c86c46a12bc9da056641d722716c378f5659b9e555ef833e1", Hash("LICENSE"));
        Assert.Equal("24564967220aaade6950d5442cdab8d48033abde6c7906285a03a8b98a45542a", Hash("NOTICE"));
        Assert.Equal("5021f8b9a8acf4e3040b2320199ead46981f9719b5a99d7947f5e37b8fb530f9", Hash("docs/references/upstream/morphir-dotnet.NOTICE"));
        Assert.Equal("@AGENTS.md\n", Support.Read("CLAUDE.md"));
    }
}
