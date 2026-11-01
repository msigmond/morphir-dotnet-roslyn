using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private readonly Dictionary<string, (Name Name, IrType<Unit> Type)> classicLocalFunctions = [];
    private V ClassicLocalFunctions(IReadOnlyList<CSharpOperation<Unit, IrType<Unit>>> locals, Func<V> continuation)
    {
        var names = locals.Select(l => Name.From(Property(l, "name"))).ToArray();
        var symbols = locals.Select(l => Property(l, "symbol")).ToArray();
        var types = locals.Select(l=>l.Kind=="LocalFunction"?LocalFunctionType(l):l.Attributes).ToArray();
        if (names.Select(n => n.Key).Distinct().Count() != names.Length || names.Any(n => classicBindings.Contains(n.Key)))
            return ClassicFailure("Local function names collide after normalization.");
        foreach (var (local, index) in locals.Select((local, index) => (local, index)))
        {
            classicBindings.Add(names[index].Key);
            if(local.Kind=="LocalFunction")classicLocalFunctions.Add(symbols[index], (names[index], types[index]));
            else classicBindingTypes[names[index].Key]=types[index];
        }
        try
        {
            var definitions = locals.Select(l=>l.Kind=="LocalFunction"?LocalFunctionDefinition(l):LocalValueDefinition(l)).ToArray();
            var edges = locals.Select(l => symbols.Select((symbol, index) => (symbol, index))
                .Where(s => locals[s.index].Kind=="LocalFunction"?ReferencesLocal(l.Children[^1], s.symbol):ReferencesBinding(l.Children[^1],names[s.index].Key)).Select(s => s.index).ToHashSet()).ToArray();
            bool Reaches(int from, int to, HashSet<int> visited) => visited.Add(from) &&
                (from == to || edges[from].Any(next => Reaches(next, to, visited)));
            var components = new List<int[]>();
            var assigned = new HashSet<int>();
            for (var i = 0; i < locals.Count; i++)
            {
                if (assigned.Contains(i)) continue;
                var component = Enumerable.Range(0, locals.Count).Where(j => Reaches(i, j, []) && Reaches(j, i, [])).ToArray();
                components.Add(component); assigned.UnionWith(component);
            }
            var ordered = new List<int>(); var visited = new HashSet<int>();
            void Visit(int index)
            {
                if (!visited.Add(index)) return;
                foreach (var target in components[index].SelectMany(i => edges[i]).Distinct().Order())
                {
                    var dependency = components.FindIndex(c => c.Contains(target));
                    if (dependency != index) Visit(dependency);
                }
                ordered.Add(index);
            }
            for (var i = 0; i < components.Count; i++) Visit(i);
            var rest = continuation();
            foreach (var index in ordered.AsEnumerable().Reverse())
            {
                var component = components[index];
                rest = component.Length > 1 || edges[component[0]].Contains(component[0])
                    ? new LetRecursionValue<Unit, IrType<Unit>>(rest.Attributes, component.Select(i => new Entry<Name, ValueDefinition<Unit, IrType<Unit>>>(names[i], definitions[i])).OrderBy(e => e.Key.Key, StringComparer.Ordinal).ToArray(), rest)
                    : new LetValue<Unit, IrType<Unit>>(rest.Attributes, names[component[0]], definitions[component[0]], rest);
            }
            return rest;
        }
        finally
        {
            foreach (var name in names) classicBindings.Remove(name.Key);
            for(var i=0;i<locals.Count;i++)if(locals[i].Kind!="LocalFunction")classicBindingTypes.Remove(names[i].Key);
            foreach (var symbol in symbols) classicLocalFunctions.Remove(symbol);
        }
    }
    private ValueDefinition<Unit,IrType<Unit>> LocalValueDefinition(CSharpOperation<Unit,IrType<Unit>> variable)
    {
        if(variable.Children.Count!=1){ClassicFailure("A local binding requires an explicit initializer.");}
        return new([],variable.Attributes,ClassicExpected(variable.Children.Single(),variable.Attributes));
    }
    private static bool ReferencesBinding(V value, string name) => ContainsValue(value,
        node => node is VariableValue<Unit, IrType<Unit>> variable && variable.Name.Key == name);
    private static bool ReferencesLocal(V value, string symbol) => ContainsValue(value,
        node => node is CSharpOperation<Unit, IrType<Unit>> { Kind: "MethodReference" } source && Property(source, "symbol") == symbol);
    private static bool ContainsValue(V value, Func<V, bool> matches) => matches(value) || value switch
    {
        CSharpOperation<Unit, IrType<Unit>> source => source.Children.Any(child => ContainsValue(child, matches)),
        ApplyValue<Unit, IrType<Unit>> apply => ContainsValue(apply.Function, matches) || ContainsValue(apply.Argument, matches),
        IfValue<Unit, IrType<Unit>> branch => ContainsValue(branch.Condition, matches) || ContainsValue(branch.Then, matches) || ContainsValue(branch.Else, matches),
        FieldValue<Unit, IrType<Unit>> field => ContainsValue(field.Subject, matches),
        TupleValue<Unit, IrType<Unit>> tuple => tuple.Elements.Any(child => ContainsValue(child, matches)),
        ListValue<Unit, IrType<Unit>> list => list.Elements.Any(child => ContainsValue(child, matches)),
        _ => false
    };
    private static Input<Unit, IrType<Unit>>[] LocalFunctionInputs(CSharpOperation<Unit, IrType<Unit>> local) => local.Children.Take(local.Children.Count - 1)
        .Cast<CSharpOperation<Unit, IrType<Unit>>>().Select(p => new Input<Unit, IrType<Unit>>(Name.From(Property(p, "name")), p.Attributes, p.Attributes)).ToArray();
    private static IrType<Unit> LocalFunctionType(CSharpOperation<Unit, IrType<Unit>> local)
    {
        IrType<Unit> type = local.Attributes;
        foreach (var input in LocalFunctionInputs(local).Reverse()) type = Types.Function(input.Type, type);
        return type;
    }
    private ValueDefinition<Unit, IrType<Unit>> LocalFunctionDefinition(CSharpOperation<Unit, IrType<Unit>> local)
    {
        var inputs = LocalFunctionInputs(local);
        if (inputs.GroupBy(i => i.Name.Key).Any(g => g.Count() > 1) || inputs.Any(i => classicBindings.Contains(i.Name.Key)))
        { ClassicFailure("Elm does not permit this local parameter shadowing or name collision."); }
        foreach (var input in inputs) classicBindings.Add(input.Name.Key);
        try
        {
            var substitutions = LocalBodyTypeArguments(local);
            var implementation = SubstituteSourceTypes(local.Children[^1], substitutions);
            var body = ClassicStatementValue(implementation is CSharpOperation<Unit, IrType<Unit>> { Kind: "Block" } block ? block : Source(Substitute(local.Attributes, substitutions), "Return", [implementation]));
            foreach (var input in inputs.Reverse())
            {
                var type = Substitute(input.Type, substitutions);
                body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(type, body.Attributes), new AsPattern<IrType<Unit>>(type, new WildcardPattern<IrType<Unit>>(type), input.Name), body);
            }
            return new([], LocalFunctionType(local), body);
        }
        finally { foreach (var input in inputs) classicBindings.Remove(input.Name.Key); }
    }
    private V ClassicDelegateCall(CSharpOperation<Unit, IrType<Unit>> call)
    {
        V result = ClassicValue(call.Children[0]);
        foreach (var source in call.Children.Skip(1))
        {
            if (source is not CSharpOperation<Unit, IrType<Unit>> { Kind: "Argument", Children.Count: 1 } argument || Property(argument, "name") != "") return ClassicFailure("This delegate argument needs a positional mapping.");
            if (result.Attributes is not FunctionType<Unit> function) return ClassicFailure("Delegate argument count mismatch.");
            var value = ClassicExpected(argument.Children[0],function.Argument);
            if(Shape(function.Argument)!=Shape(value.Attributes))return ClassicFailure("Delegate argument type mismatch.");
            result = new ApplyValue<Unit, IrType<Unit>>(function.Result, result, value);
        }
        return Shape(result.Attributes) == Shape(call.Attributes) ? result : ClassicFailure("Delegate call result type mismatch.");
    }
}
