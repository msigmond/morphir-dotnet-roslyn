using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using Definition = Morphir.IR.Classic.ValueDefinition<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private Library<Unit, IrType<Unit>> classicInput = null!;
    private Location? classicLocation;
    private HashSet<string> classicBindings = [];
    private V ClassicFailure(string message) => Fail<V>("MCS4001", message, classicLocation);
    private static string Property(CSharpOperation<Unit, IrType<Unit>> source, string name) => source.Properties.FirstOrDefault(p => p.Key == name)?.Value ?? "";
    private ISymbol? ClassicSymbol(FQName name) => identities.FirstOrDefault(p => p.Value.Module.Key == name.Module.Key && p.Value.Local.Key == name.Local.Key && declared[p.Key] is not INamedTypeSymbol).Key is { } key ? declared[key] : null;
    private Library<Unit, IrType<Unit>> ProjectClassic(Library<Unit, IrType<Unit>> input)
    {
        classicInput = input;
        foreach (var type in declared.Values.OfType<INamedTypeSymbol>())
        {
            classicLocation = Location(type);
            foreach (var member in type.GetMembers().Where(m => !m.IsStatic && !m.IsImplicitlyDeclared))
                if (member.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null } or PropertyDeclarationSyntax { Initializer: not null }))
                    ClassicFailure("Instance initializers need an explicit Classic constructor mapping.");
        }
        var aliases = input.Definition.Modules.SelectMany(m => m.Value.Value.Types.Where(t => t.Value.Value.Value is AliasDefinition<Unit>)
            .Select(t => (Key: new FQName(package,m.Key,t.Key), Alias: (AliasDefinition<Unit>)t.Value.Value.Value))).ToArray();
        bool Cycle(IrType<Unit> type, HashSet<string> path)
        {
            if (type is ReferenceType<Unit> reference)
            {
                var alias = aliases.FirstOrDefault(a => a.Key.Package.Key == reference.Name.Package.Key && a.Key.Module.Key == reference.Name.Module.Key && a.Key.Local.Key == reference.Name.Local.Key);
                if (alias.Alias is not null)
                {
                    var key = alias.Key.Module.Key + "/" + alias.Key.Local.Key;
                    if (!path.Add(key)) return true;
                    var found = Cycle(alias.Alias.Body,path);path.Remove(key);if(found)return true;
                }
                return reference.Arguments.Any(a => Cycle(a,path));
            }
            return type switch { RecordType<Unit> r => r.Fields.Any(f => Cycle(f.Value,path)), TupleType<Unit> t => t.Elements.Any(a => Cycle(a,path)), FunctionType<Unit> f => Cycle(f.Argument,path) || Cycle(f.Result,path), _ => false };
        }
        foreach (var alias in aliases)
        {
            classicLocation = declared.Values.OfType<INamedTypeSymbol>().FirstOrDefault(t => identities[Id(t)].Module.Key == alias.Key.Module.Key && identities[Id(t)].Local.Key == alias.Key.Local.Key) is { } type ? Location(type) : null;
            if (Cycle(alias.Alias.Body,[])) ClassicFailure("Recursive alias data needs a separate Classic custom-type mapping.");
        }
        var result = new List<Entry<IrPath, AccessControlled<ModuleDefinition<Unit, IrType<Unit>>>>>();
        foreach (var module in input.Definition.Modules)
        {
            var values = new List<Entry<Name, AccessControlled<Documented<Definition>>>>();
            foreach (var entry in module.Value.Value.Values)
            {
                var id = new FQName(input.Package, module.Key, entry.Key);
                var symbol = ClassicSymbol(id);
                classicLocation = symbol is null ? null : Location(symbol);
                var definition = entry.Value.Value.Value;
                if (definition.Body is CSharpOperation<Unit, IrType<Unit>> { Kind: "PrimaryConstructor" })
                {
                    if (symbol is IMethodSymbol { ContainingType.IsRecord: true }) continue;
                }
                // A written automatic accessor has no implementation body to export; its slot is in the record alias.
                if (definition.Body is CSharpOperation<Unit, IrType<Unit>> { Kind: "Declaration" } && symbol is IMethodSymbol { AssociatedSymbol: IPropertySymbol property } && StoredProperty(property)) continue;
                if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertySet or MethodKind.StaticConstructor }) ClassicFailure("Setters and static initialization have no approved pure-data mapping.");
                foreach (var argument in definition.Inputs) ClassicType(argument.Type);
                ClassicType(definition.Output);
                classicBindings = definition.Inputs.Select(i => i.Name.Key).ToHashSet(StringComparer.Ordinal);
                classicBindingTypes.Clear();foreach(var argument in definition.Inputs)classicBindingTypes[argument.Name.Key]=argument.Attributes;
                var body = symbol is IMethodSymbol {MethodKind:MethodKind.Constructor} constructor ? ClassicDataConstructor(constructor,definition) : definition.Body is CSharpOperation<Unit, IrType<Unit>> {Kind:"Block"} ? ClassicValue(definition.Body) : ClassicExpected(definition.Body,definition.Output);
                if (Shape(body.Attributes) != Shape(definition.Output)) ClassicFailure("The declared result needs an explicit Classic conversion mapping.");
                values.Add(new(entry.Key, new(entry.Value.Access, new(entry.Value.Value.Doc, definition with { Body = body, Inputs = definition.Inputs.Select(i=>i with{Attributes=classicBindingTypes.GetValueOrDefault(i.Name.Key,i.Attributes)}).ToArray() }))));
            }
            foreach (var type in module.Value.Value.Types)
            {
                classicLocation = declared.Values.OfType<INamedTypeSymbol>().FirstOrDefault(t => identities[Id(t)].Module.Key == module.Key.Key && identities[Id(t)].Local.Key == type.Key.Key) is { } owner ? Location(owner) : null;
                if (type.Value.Value.Value is AliasDefinition<Unit> alias) ClassicType(alias.Body);
            }
            if (module.Value.Value.Types.Count != 0 || values.Count != 0)
                result.Add(new(module.Key, new(module.Value.Access, module.Value.Value with { Values = values })));
        }
        return input with { Definition = new(result) };
    }
    private void ClassicType(IrType<Unit> type)
    {
        switch (type)
        {
            case ReferenceType<Unit> r:
                foreach (var argument in r.Arguments) ClassicType(argument);
                break;
            case RecordType<Unit> r: foreach (var field in r.Fields) ClassicType(field.Value); break;
            case TupleType<Unit> t: foreach (var element in t.Elements) ClassicType(element); break;
            case FunctionType<Unit> f: ClassicType(f.Argument); ClassicType(f.Result); break;
        }
    }
    private V ClassicValue(V value)
    {
        ClassicType(value.Attributes);
        return value switch
        {
            CSharpOperation<Unit, IrType<Unit>> s => ClassicSource(s),
            ApplyValue<Unit, IrType<Unit>> a => a with { Function = ClassicValue(a.Function), Argument = ClassicValue(a.Argument) },
            FieldValue<Unit, IrType<Unit>> f => ClassicField(f),
            IfValue<Unit, IrType<Unit>> i => ClassicConditional(i),
            ListValue<Unit, IrType<Unit>> l => l with { Elements = l.Elements.Select(ClassicValue).ToArray() },
            TupleValue<Unit, IrType<Unit>> t => t with { Elements = t.Elements.Select(ClassicValue).ToArray() },
            LiteralValue<Unit, IrType<Unit>> { Literal: UnmappedNumberLiteral } => ClassicFailure("This numeric literal exceeds the Classic literal domain."),
            LiteralValue<Unit, IrType<Unit>> { Literal: DecimalLiteral literal } => new ApplyValue<Unit, IrType<Unit>>(Types.Decimal,
                new ReferenceValue<Unit, IrType<Unit>>(Types.Function(Types.Float, Types.Decimal), new(Types.Sdk, IrPath.From("Decimal"), Name.From("fromFloat"))),
                new LiteralValue<Unit, IrType<Unit>>(Types.Float, new FloatLiteral(double.Parse(literal.Value, System.Globalization.CultureInfo.InvariantCulture)))),
            VariableValue<Unit, IrType<Unit>> variable => classicBindings.Contains(variable.Name.Key) ? variable with{Attributes=classicBindingTypes.GetValueOrDefault(variable.Name.Key,variable.Attributes)} : ClassicFailure("A source capture or local has no Classic binding: " + variable.Name.Key.Replace('\0', ' ') + "."),
            LiteralValue<Unit, IrType<Unit>> or ReferenceValue<Unit, IrType<Unit>> or ConstructorValue<Unit, IrType<Unit>> or UnitValue<Unit, IrType<Unit>> => value,
            _ => ClassicFailure("This value has no Classic projection: " + value.GetType().Name + ".")
        };
    }
    private V ClassicConditional(IfValue<Unit, IrType<Unit>> value)
    {
        var condition = ClassicValue(value.Condition); var then = ClassicValue(value.Then); var otherwise = ClassicValue(value.Else);
        if (Shape(condition.Attributes) != Shape(Types.Bool) || Shape(then.Attributes) != Shape(value.Attributes) || Shape(otherwise.Attributes) != Shape(value.Attributes))
            return ClassicFailure("Conditional branches need an explicit Classic conversion mapping.");
        return value with { Condition = condition, Then = then, Else = otherwise };
    }
    private V ClassicField(FieldValue<Unit, IrType<Unit>> field)
    {
        var subject = ClassicValue(field.Subject);
        var shape = subject.Attributes is ReferenceType<Unit> reference && reference.Name.Package.Key == package.Key
            ? classicInput.Definition.Modules.FirstOrDefault(m => m.Key.Key == reference.Name.Module.Key)?.Value.Value.Types.FirstOrDefault(t => t.Key.Key == reference.Name.Local.Key)?.Value.Value.Value as AliasDefinition<Unit> : null;
        var fields = subject.Attributes as RecordType<Unit> ?? shape?.Body as RecordType<Unit>;
        var slot = fields?.Fields.FirstOrDefault(f => f.Key.Key == field.Field.Key);
        if (slot is null || Shape(slot.Value) != Shape(field.Attributes)) return ClassicFailure("This member read has no direct Classic record field mapping.");
        return field with { Subject = subject };
    }
    private V ClassicSource(CSharpOperation<Unit, IrType<Unit>> source) => source.Kind switch
    {
        "Block" => ClassicBlock(source),
        "Return" when source.Children.Count == 0 && source.Attributes is UnitType<Unit> => new UnitValue<Unit, IrType<Unit>>(Types.Unit),
        "Return" when source.Children.Count == 1 => ClassicExpected(source.Children[0],source.Attributes),
        "Parenthesized" or "NullForgiving" or "checked" or "unchecked" when source.Children.Count == 1 => ClassicValue(source.Children[0]),
        "Null" => MaybeElement(source.Attributes) is not null ? Nothing(source.Attributes) : ClassicFailure("Null requires an explicit nullable context."),
        "Default" => ClassicDefault(source.Attributes),
        "MemberAccess" => ClassicMember(source),
        "MemberBinding" => ClassicConditionalMember(source),
        "ConditionalReceiver" => classicConditionalSubject ?? ClassicFailure("A conditional call has no receiver."),
        "IsPattern" => ClassicNullPattern(source),
        "ConditionalAccess" => ClassicConditionalAccess(source),
        "Invocation" => ClassicCall(source),
        "MethodReference" => ClassicMethodReference(source),
        "DelegateInvocation" => ClassicDelegateCall(source),
        "New" => ClassicConstruction(source),
        "Unary" => ClassicUnary(source),
        "Binary" => ClassicBinary(source),
        "Cast" => ClassicCast(source),
        "With" => ClassicUpdate(source),
        "TupleField" => ClassicTupleField(source),
        "ArrayCreation" when source.Children.SingleOrDefault() is CSharpOperation<Unit, IrType<Unit>> { Kind: "Initializer" } elements => new ListValue<Unit, IrType<Unit>>(source.Attributes, elements.Children.Select(e=>ClassicExpected(e,((ReferenceType<Unit>)source.Attributes).Arguments[0])).ToArray()),
        _ => ClassicFailure("Source operation '" + source.Kind + "' has no documented Classic mapping in this delivery.")
    };
    private V ClassicBlock(CSharpOperation<Unit, IrType<Unit>> source)
    {
        var statements = source.Attributes is UnitType<Unit> && !Terminates(source)
            ? source.Children.Concat([Source(Types.Unit, "Return", [])]).ToArray() : source.Children;
        if(statements.Any(s=>s is CSharpOperation<Unit,IrType<Unit>> {Kind:"LocalFunction"}))
        {
            var prefix=statements.TakeWhile(s=>s is CSharpOperation<Unit,IrType<Unit>> {Kind:"LocalFunction" or "LocalDeclaration" or "EmptyStatement"}).ToHashSet();
            var bindings=new List<CSharpOperation<Unit,IrType<Unit>>>();var rest=new List<V>();
            foreach(var statement in statements)
            {
                if(statement is CSharpOperation<Unit,IrType<Unit>> {Kind:"LocalFunction"} function)bindings.Add(function);
                else if(prefix.Contains(statement)&&statement is CSharpOperation<Unit,IrType<Unit>> {Kind:"LocalDeclaration",Children.Count:1} declaration && declaration.Children[0] is CSharpOperation<Unit,IrType<Unit>> {Kind:"Variables"} variables)
                    bindings.AddRange(variables.Children.Cast<CSharpOperation<Unit,IrType<Unit>>>());
                else rest.Add(statement);
            }
            return ClassicLocalFunctions(bindings,()=>ClassicStatements(rest,0));
        }
        return ClassicStatements(statements, 0);
    }
    private V ClassicStatements(IReadOnlyList<V> statements, int index)
    {
        if (index >= statements.Count) return ClassicFailure("A Classic function must end in a value return.");
        if (statements[index] is not CSharpOperation<Unit, IrType<Unit>> statement) return ClassicFailure("A block contains an unmapped statement.");
        if (statement.Kind == "Return")
        {
            if (index != statements.Count - 1) return ClassicFailure("Written statements after a return cannot be retained in Classic output.");
            return ClassicValue(statement);
        }
        if (statement.Kind == "EmptyStatement") return ClassicStatements(statements, index + 1);
        if (statement is { Kind: "ExpressionStatement", Children.Count: 1 } && statement.Children[0] is CSharpOperation<Unit, IrType<Unit>> { Kind: "Assignment", Children.Count: 2 } assignment && assignment.Children[0] is CSharpOperation<Unit, IrType<Unit>> { Kind: "DeclarationExpression" } declaration)
            return ClassicDeconstruction(declaration, ClassicValue(assignment.Children[1]), () => ClassicStatements(statements, index + 1));
        if (statement.Kind == "LocalFunction")
        {
            var locals = statements.Skip(index).TakeWhile(s => s is CSharpOperation<Unit, IrType<Unit>> { Kind: "LocalFunction" }).Cast<CSharpOperation<Unit, IrType<Unit>>>().ToArray();
            return ClassicLocalFunctions(locals, () => ClassicStatements(statements, index + locals.Length));
        }
        if (statement.Kind == "If")
        {
            var condition = ClassicValue(statement.Children[0]);
            var then = ClassicStatementValue(statement.Children[1]);
            V otherwise;
            if (statement.Children.Count == 3 && index == statements.Count - 1) otherwise = ClassicStatementValue(statement.Children[2]);
            else if (statement.Children.Count == 2 && index < statements.Count - 1) otherwise = ClassicStatements(statements, index + 1);
            else return ClassicFailure("This statement branch needs a value return on each path without duplicating following statements.");
            if (Shape(condition.Attributes) != Shape(Types.Bool) || Shape(then.Attributes) != Shape(otherwise.Attributes))
                return ClassicFailure("The statement branch results require another conversion mapping.");
            return new IfValue<Unit, IrType<Unit>>(then.Attributes, condition, then, otherwise);
        }
        if (statement.Kind is "checked" or "unchecked" && index == statements.Count - 1) return ClassicStatementValue(statement.Children.Single());
        if (statement.Kind == "LocalDeclaration" && statement.Children.SingleOrDefault() is CSharpOperation<Unit, IrType<Unit>> { Kind: "Variables" } variables)
        {
            V Bind(int variableIndex)
            {
                if (variableIndex == variables.Children.Count) return ClassicStatements(statements, index + 1);
                if (variables.Children[variableIndex] is not CSharpOperation<Unit, IrType<Unit>> { Kind: "VariableDeclaration", Children.Count: 1 } variable) return ClassicFailure("Classic local bindings require one explicit initializer.");
                var initializer = ClassicExpected(variable.Children[0],variable.Attributes);
                if (Shape(initializer.Attributes) != Shape(variable.Attributes)) return ClassicFailure("The local initializer needs an explicit Classic conversion mapping.");
                var name = Name.From(Property(variable, "name"));
                if (!classicBindings.Add(name.Key)) return ClassicFailure("Local names collide after Classic normalization: " + Property(variable, "name") + ".");
                try
                {
                    var rest = Bind(variableIndex + 1);
                    return new LetValue<Unit, IrType<Unit>>(rest.Attributes, name, new([], variable.Attributes, initializer), rest);
                }
                finally { classicBindings.Remove(name.Key); }
            }
            return Bind(0);
        }
        if (statement.Kind == "Block" && index == statements.Count - 1) return ClassicStatements(statement.Children, 0);
        return ClassicFailure("Statement '" + statement.Kind + "' has no Classic mapping in this delivery.");
    }
    private V ClassicMethodReference(CSharpOperation<Unit, IrType<Unit>> reference)
    {
        if (classicLocalFunctions.TryGetValue(Property(reference, "symbol"), out var local)) return new VariableValue<Unit, IrType<Unit>>(local.Type, local.Name);
        if (reference.Children.LastOrDefault() is ReferenceValue<Unit, IrType<Unit>> target)
        {
            var definition = Lookup.Value(classicInput, target.Name);
            IrType<Unit> type = definition?.Output ?? target.Attributes;
            if (definition is not null) foreach (var input in definition.Inputs.Reverse()) type = Types.Function(input.Type, type);
            V result = target with { Attributes = type };
            if (reference.Children.Count == 2)
            {
                var receiver = ClassicValue(reference.Children[0]);
                if (type is not FunctionType<Unit> function || Shape(receiver.Attributes) != Shape(function.Argument)) return ClassicFailure("The bound receiver does not match its source function.");
                result = new ApplyValue<Unit, IrType<Unit>>(function.Result, result, receiver);
            }
            return result;
        }
        return ClassicFailure("This method reference has no direct Classic target.");
    }
    private V ClassicCall(CSharpOperation<Unit, IrType<Unit>> invocation)
    {
        if (invocation.Children.FirstOrDefault() is not CSharpOperation<Unit, IrType<Unit>> { Kind: "MethodReference" } reference)
            return ClassicFailure("The call has no mapped Classic function reference.");
        var target = ClassicMethodReference(reference);
        var definition = target is ReferenceValue<Unit, IrType<Unit>> global ? Lookup.Value(classicInput, global.Name) : null;
        var arguments = new List<V>();
        foreach (var argument in invocation.Children.Skip(1))
        {
            if (argument is not CSharpOperation<Unit, IrType<Unit>> { Kind: "Argument", Children.Count: 1 } source || Property(source, "name") != "") return ClassicFailure("Named or non-positional call arguments require a separate Classic mapping.");
            arguments.Add(source.Children[0]);
        }
        IrType<Unit> signature = target.Attributes;
        if (definition is not null)
        {
            if (definition.Inputs.Count != arguments.Count) return ClassicFailure("Omitted or expanded call arguments require a separate Classic mapping.");
            signature = definition.Output;
            foreach (var input in definition.Inputs.Reverse()) signature = Types.Function(input.Type, signature);
        }
        V result = target;
        foreach (var sourceArgument in arguments)
        {
            if (signature is not FunctionType<Unit> function) return ClassicFailure("Call arity differs from its Classic function signature.");
            var argument = ClassicExpected(sourceArgument,function.Argument);
            if (Shape(function.Argument) != Shape(argument.Attributes)) return ClassicFailure("Call argument types require an explicit Classic conversion mapping.");
            signature = function.Result;
            result = new ApplyValue<Unit, IrType<Unit>>(signature, result, argument);
        }
        return Shape(signature) == Shape(invocation.Attributes) ? result : ClassicFailure("The call has omitted arguments or an unmapped result conversion.");
    }
    private V ClassicConstruction(CSharpOperation<Unit, IrType<Unit>> creation)
    {
        if (creation.Attributes is TupleType<Unit> tuple && creation.Children.Count == tuple.Elements.Count)
            return new TupleValue<Unit, IrType<Unit>>(tuple, creation.Children.Select((c,i) => ClassicExpected(((CSharpOperation<Unit, IrType<Unit>>)c).Children.Single(),tuple.Elements[i])).ToArray());
        if (Shape(creation.Attributes) == Shape(Types.Decimal) && creation.Children.Count == 1 && creation.Children[0] is CSharpOperation<Unit, IrType<Unit>> { Kind: "Argument", Children.Count: 1 } numericArgument)
        {
            var value = ClassicValue(numericArgument.Children[0]);
            if (Shape(value.Attributes) == Shape(Types.Int)) return Sdk("Decimal", "fromInt", Types.Decimal, value);
            if (Shape(value.Attributes) == Shape(Types.Float)) return Sdk("Decimal", "fromFloat", Types.Decimal, value);
        }
        if (creation.Children.Count == 1 && declared.GetValueOrDefault(Property(creation, "symbol")) is null && creation.Children[0] is CSharpOperation<Unit, IrType<Unit>> { Kind: "Argument", Children.Count: 1 } delegateArgument && delegateArgument.Children[0] is CSharpOperation<Unit, IrType<Unit>> { Kind: "MethodReference" })
        {
            var function = ClassicValue(delegateArgument.Children[0]);
            if (Shape(function.Attributes) == Shape(creation.Attributes)) return function;
        }
        if(declared.GetValueOrDefault(Property(creation,"symbol")) is IMethodSymbol {MethodKind:MethodKind.Constructor} explicitConstructor && !explicitConstructor.DeclaringSyntaxReferences.Any(r=>r.GetSyntax() is RecordDeclarationSyntax))
        {
            if(creation.Children.Any(c=>c is not CSharpOperation<Unit,IrType<Unit>> {Kind:"Argument",Children.Count:1}))return ClassicFailure("Object initialization needs a separate approved Elm mapping.");
            return ClassicFunction(identities[Id(explicitConstructor)],creation.Children.Select(c=>((CSharpOperation<Unit,IrType<Unit>>)c).Children.Single()).ToArray());
        }
        if (creation.Attributes is not ReferenceType<Unit> recordType || !declared.TryGetValue(Property(creation, "symbol"), out var symbol) || symbol is not IMethodSymbol { ContainingType.IsRecord: true } constructor
            || !constructor.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is RecordDeclarationSyntax)) return ClassicFailure("Only positional record construction has a direct Classic alias constructor in this delivery.");
        var record = classicInput.Definition.Modules.Single(m => m.Key.Key == recordType.Name.Module.Key).Value.Value.Types.Single(t => t.Key.Key == recordType.Name.Local.Key).Value.Value.Value as AliasDefinition<Unit>;
        if (record?.Body is not RecordType<Unit> fields || fields.Fields.Count != creation.Children.Count || constructor.Parameters.Length != creation.Children.Count) return ClassicFailure("Record construction does not match its Classic alias fields.");
        IrType<Unit> signature = creation.Attributes;
        foreach (var field in fields.Fields.Reverse()) signature = Types.Function(field.Value, signature);
        V result = new ConstructorValue<Unit, IrType<Unit>>(signature, recordType.Name);
        for (int i = 0; i < creation.Children.Count; i++)
        {
            if (creation.Children[i] is not CSharpOperation<Unit, IrType<Unit>> { Kind: "Argument", Children.Count: 1 } argument || Property(argument, "name") != "") return ClassicFailure("Record initializers and named arguments require a separate Classic mapping.");
            var function = (FunctionType<Unit>)signature;
            var value = ClassicExpected(argument.Children[0],function.Argument);
            if (fields.Fields[i].Key.Key != Name.From(constructor.Parameters[i].Name).Key || Shape(function.Argument) != Shape(value.Attributes)) return ClassicFailure("Record arguments differ from the source alias field order or types.");
            signature = function.Result;
            result = new ApplyValue<Unit, IrType<Unit>>(signature, result, value);
        }
        return result;
    }
    private V ClassicUnary(CSharpOperation<Unit, IrType<Unit>> unary)
    {
        if(declared.GetValueOrDefault(Property(unary,"symbol")) is IMethodSymbol && identities.TryGetValue(Property(unary,"symbol"),out var operatorName))return ClassicFunction(operatorName,unary.Children.ToArray());
        var operand = ClassicValue(unary.Children.Single());
        var op = Property(unary, "operator");
        if(MaybeElement(operand.Attributes) is {} element && Shape(unary.Attributes)==Shape(operand.Attributes))
        {
            var isDecimal=Shape(element)==Shape(Types.Decimal);
            var isNumber=isDecimal||Shape(element)==Shape(Types.Int)||Shape(element)==Shape(Types.Float);
            if(op=="+"&&isNumber)return operand;
            var lifted=op=="-"&&isNumber?"negate":op=="!"&&Shape(element)==Shape(Types.Bool)?"not":null;
            if(lifted is not null)return Sdk("Maybe","map",unary.Attributes,new ReferenceValue<Unit,IrType<Unit>>(Types.Function(element,element),new(Types.Sdk,IrPath.From(isDecimal?"Decimal":"Basics"),Name.From(lifted))),operand);
        }
        var numeric = Shape(operand.Attributes) == Shape(Types.Int) || Shape(operand.Attributes) == Shape(Types.Float);
        var decimalOperand = Shape(operand.Attributes) == Shape(Types.Decimal);
        if (Shape(operand.Attributes) != Shape(unary.Attributes)) return ClassicFailure("This unary operator needs an explicit Classic conversion mapping.");
        if (op == "+" && (numeric || decimalOperand)) return operand;
        var name = op switch { "-" when numeric || decimalOperand => "negate", "!" when Shape(operand.Attributes) == Shape(Types.Bool) => "not", _ => null };
        if (name is null || Property(unary, "symbol") != "" && declared.ContainsKey(Property(unary, "symbol"))) return ClassicFailure("This unary operator needs a separate Classic mapping.");
        return new ApplyValue<Unit, IrType<Unit>>(unary.Attributes, new ReferenceValue<Unit, IrType<Unit>>(Types.Function(operand.Attributes, unary.Attributes), new(Types.Sdk, IrPath.From(decimalOperand ? "Decimal" : "Basics"), Name.From(name))), operand);
    }
    private V ClassicBinary(CSharpOperation<Unit, IrType<Unit>> binary)
    {
        if(declared.GetValueOrDefault(Property(binary,"symbol")) is IMethodSymbol && identities.TryGetValue(Property(binary,"symbol"),out var operatorName))return ClassicFunction(operatorName,binary.Children.ToArray());
        if (Property(binary,"operator") == "??" || binary.Children.Any(c => MaybeElement(c.Attributes) is not null || c is CSharpOperation<Unit, IrType<Unit>> {Kind:"Null"})) return ClassicNullableBinary(binary);
        var left = ClassicValue(binary.Children[0]); var right = ClassicValue(binary.Children[1]);
        if (Property(binary, "operator") == "%" && Shape(left.Attributes) == Shape(Types.Int) && Shape(right.Attributes) == Shape(Types.Int))
            return Sdk("Basics", "remainderBy", Types.Int, right, left);
        if (Shape(left.Attributes) == Shape(Types.Decimal) && Shape(right.Attributes) == Shape(Types.Decimal))
        {
            var operation = Property(binary, "operator") switch { "==" => "eq", "!=" => "neq", "<" => "lt", "<=" => "lte", ">" => "gt", ">=" => "gte", _ => null };
            if (operation is not null) return Sdk("Decimal", operation, Types.Bool, left, right);
        }
        if (Property(binary, "operator") is "==" or "!=" && Shape(left.Attributes) == Shape(right.Attributes) && (RecordValueType(left.Attributes) || EnumValueType(left.Attributes) || left.Attributes is TupleType<Unit> || Shape(left.Attributes) == Shape(Types.String)))
            return Sdk("Basics", Property(binary, "operator") == "==" ? "equal" : "notEqual", Types.Bool, left, right);
        var name = Property(binary, "operator") switch { "&&" or "&" when Shape(left.Attributes) == Shape(Types.Bool) => "and", "||" or "|" when Shape(left.Attributes) == Shape(Types.Bool) => "or", "^" when Shape(left.Attributes) == Shape(Types.Bool) => "xor", _ => null };
        if (name is null || Property(binary, "symbol") != "" && declared.ContainsKey(Property(binary, "symbol"))) return ClassicFailure("This binary operator needs a separate Classic mapping.");
        var partial = Types.Function(right.Attributes, binary.Attributes);
        return new ApplyValue<Unit, IrType<Unit>>(binary.Attributes, new ApplyValue<Unit, IrType<Unit>>(partial, new ReferenceValue<Unit, IrType<Unit>>(Types.Function(left.Attributes, partial), new(Types.Sdk, IrPath.From("Basics"), Name.From(name))), left), right);
    }
}
