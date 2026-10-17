namespace Morphir.IR.Classic;

public static class Specifications
{
    /// <summary>The public package surface excludes private modules, types and values.</summary>
    public static PackageSpecification<TA> Public<TA, VA>(PackageDefinition<TA, VA> package) => new(
        package.Modules.Where(m => m.Value.Access == Access.Public).Select(m =>
            new Entry<Path, ModuleSpecification<TA>>(m.Key, new(
                m.Value.Value.Types.Where(t => t.Value.Access == Access.Public)
                    .Select(t => new Entry<Name, Documented<AliasDefinition<TA>>>(t.Key, t.Value.Value)).ToArray(),
                m.Value.Value.Values.Where(v => v.Value.Access == Access.Public)
                    .Select(v => new Entry<Name, Documented<ValueSpecification<TA>>>(v.Key,
                        new(v.Value.Value.Doc, new(v.Value.Value.Value.Inputs.Select(i => new Entry<Name, IrType<TA>>(i.Name, i.Type)).ToArray(), v.Value.Value.Value.Output)))).ToArray(),
                m.Value.Value.Doc))).ToArray());
}
