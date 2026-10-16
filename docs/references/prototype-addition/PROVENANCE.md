# Addition regression — historical reference

- role: D1 migration input; no runtime dependency; no frontend implementation
- source: previous DotNetMorphir prototype
- upstream: `msigmond/morphir-jvm` commit `3086f7ad501d13239b474e23baa3d7fc88baf881`
- compiler: Morphir Elm `2.96.1`; fallback `make -f`; historical generation, not new-repository baseline
- admitted runtime evidence: small nonoverflowing int cases only
- current master reference: `2.100.0`; independent regeneration/review required in D1; archived golden unchanged
- layout: `Add.cs` source; `elm/src/Arithmetic/Add.elm` + `elm/morphir.json` archived source/config; `add.elm.morphir-ir.json` independently generated golden
- original scripts/package lock: previous prototype only; no runnable bootstrap commands claimed

## Upstream identities

- [Elm source](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/test-fixtures/elm/add/src/Arithmetic/Add.elm)
- [Configuration](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/test-fixtures/elm/add/morphir.json)
- [Compiler lock](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/package-lock.json)
- [Generation command](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/build.mill)
