# Source language coverage

Contract: [source v1](../source-structure.md). Current evidence: [migration](../deliveries/source-structure-state.md).
The cumulative sample contains all prior delivered feature sections plus SourceStructure.cs.
The approved [62-case matrix](../../tests/Morphir.Frontend.CSharp.Tests/AuditMatrixTests.cs) is checked against the audit IDs.

| Delivery | Delivered structure | Sample and checks |
| --- | --- | --- |
| D1 | Selection, owners, access, literals, simple operations, fields, record data | CurrentSupportedExample.cs; SourceStructureTests |
| D2 | Actual projects, references, configuration, generated partial source | ProjectMode.cs; StructureIntegrationTests |
| D3 | Blocks, declarations, assignments, returns, decisions, short circuit | Blocks.cs, SourceStructure.cs; SourceStructureTests |
| D4 | Explicit Maybe types, null/default, tests, nullable access and operators | Presence.cs, SourceStructure.cs; audit N1–N7 |
| D5 | Numeric types, source casts, constructors, literals, operators | Numeric.cs; audit A1–A10 |
| D6 | Decimal literals, direct products, constructors, casts, members | Decimal.cs, SourceStructure.cs; audit D1–D7 |
| D7 | Calls, named/optional/params syntax, method references, local functions | Calls.cs; audit C1–C10 |
| D8 | Enum initializers/operators, tuples, labels, equality, deconstruction | EnumsTuples.cs; audit T1–T6 |
| D9 | Data forms, explicit members, writes, new, with, identity, cycles | Data.cs; audit O1–O11 |

Declaration and token parity includes unused/private code and unreachable branches.
Compiler-generated project input is retained. CLR-synthesized backing/equality/state machinery is not emitted.
No runtime guard, proof, helper library, ownership condition, or termination rule controls these forms.

The [forms](forms.json), [Roslyn operation inventory](operations.json), and rewritten delivery plans retain later mapping gaps.
General source syntax capture does not authorize an unsupported form.
D10–D21 remain pending. Historical behavior/parity references are frozen context, not current frontend output.
