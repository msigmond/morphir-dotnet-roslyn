using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool PrimaryDataConstructor(IMethodSymbol constructor) => !constructor.IsImplicitlyDeclared && constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ClassDeclarationSyntax { ParameterList: not null } or StructDeclarationSyntax { ParameterList: not null } or RecordDeclarationSyntax { ParameterList: not null });
        private static IFieldSymbol? PrimaryCapture(IParameterSymbol parameter) => parameter.ContainingSymbol is not IMethodSymbol constructor || !PrimaryDataConstructor(constructor) ? null : parameter.ContainingType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field => field.IsImplicitlyDeclared && field.AssociatedSymbol is null && field.Name == "<" + parameter.Name + ">P");
        private static bool PrimaryCaptureField(IFieldSymbol field) => field.IsImplicitlyDeclared && field.AssociatedSymbol is null && field.ContainingType.InstanceConstructors.Where(PrimaryDataConstructor).SelectMany(constructor => constructor.Parameters).Any(parameter => SymbolEqualityComparer.Default.Equals(PrimaryCapture(parameter), field));
        private TypedValue InitializePrimaryCaptures(IMethodSymbol constructor, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            if (PrimaryDataConstructor(constructor)) foreach (var parameter in constructor.Parameters)
                    if (PrimaryCapture(parameter) is { } field)
                    {
                        var location = Location(parameter)!; var previous = bindings; bindings = environment; TypedValue value;
                        try { value = ReadBinding(parameter, location); } finally { bindings = previous; }
                        WriteDataField(origin, field, Adapt(value, Type(field.Type, location)), environment, location);
                    }
            return InitializePositionalRecord(constructor, origin, environment, continuation);
        }
        private static IFieldSymbol? PrimaryCaptureTarget(IOperation operation) => operation is ISimpleAssignmentOperation { Target: IParameterReferenceOperation parameter } ? PrimaryCapture(parameter.Parameter) : null;
        private TypedValue? PrimaryCaptureExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (PrimaryCaptureTarget(operation) is not { } field) return null;
            var assignment = (ISimpleAssignmentOperation)operation; var location = operation.Syntax.GetLocation();
            if (currentDataConstructionOrigin is not { } origin) return Fail<TypedValue>("MCS3002", "Captured primary parameters are receiver state; input receiver mutation is excluded.", location);
            return Expression(assignment.Value, environment, (value, state) => { WriteDataField(origin, field, Adapt(value, Type(field.Type, location)), state, location); provedDataOperations.Add(operation); return continuation(value, state); });
        }
        private TypedValue? ReadPrimaryCapture(IOperation operation)
        {
            if (operation is not IParameterReferenceOperation reference || bindings.ContainsKey(reference.Parameter) && currentDataConstructionOrigin is null || PrimaryCapture(reference.Parameter) is not { } field || currentReceiver is null) return null;
            var location = operation.Syntax.GetLocation();
            return DataLocationMember(DemandPresent(DataReceiverValue(location), location), new(field, field.Type, operation), bindings, location);
        }
    }
}
