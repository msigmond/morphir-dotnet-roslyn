#!/usr/bin/env python3
"""Explicit numeric public-member inventory maintenance; never called by validation."""
import json
from pathlib import Path
import re
import sys
if sys.argv[1:] != ['--write']:
    raise SystemExit('Explicit inventory maintenance requires --write')
root=Path(__file__).resolve().parents[2]
rows=[]
for signature in (root/'artifacts/numeric-members.txt').read_text().splitlines():
    if not signature or ': error ' in signature:
        raise SystemExit('Invalid reference symbol output')
    state='pending'; owner='D18.4'; domain='Remaining pure numeric framework member closure; no current support claim.'
    if 'checked ' in signature:
        state='required-rejection';owner='D5';domain='Explicit/semantic checked numeric operations are excluded: NumericSafetyTests.'
    elif 'decimal' in signature:
        state='supported';owner='D6';domain='Exact Decimal coefficient/scale/sign conversion with D3 target-range proof; mappings/D6.md and DecimalTests.'
        if re.search(r'operator decimal\(System.Half\)',signature):
            state='required-rejection';domain='Binary-floating Decimal construction excluded by exact-construction policy.'
    elif 'Span<' in signature or re.search(r'\w\*',signature):
        state='required-rejection';owner='D18.4';domain='Ref-like/pointer data is excluded by the master contract.'
    elif '[]' in signature:
        owner='D11';domain='Array/owned-buffer representation and safety prerequisite.'
    elif '<' in signature and 'operator' not in signature:
        owner='D10';domain='Resolved generic numeric constraints/type arguments prerequisite.'
    elif 'operator ++' in signature or 'operator --' in signature:
        owner='D15';domain='Increment/rebinding/owned update operation prerequisite (operations.json).'
    elif 'operator' in signature:
        state='supported';owner='D5';domain='Exact bound numeric operator/conversion; source-domain and failure proofs in mappings/D5.md.'
    elif '(object' in signature:
        owner='D17';domain='Object boxing/type-domain prerequisite.'
    elif re.search(r'\.(?:Char|Byte|SByte|Int16|UInt16|Int32|UInt32|Int64|UInt64|Single|Double|Half|Int128|UInt128|IntPtr|UIntPtr|BigInteger|Complex)\(\)$',signature):
        state='supported';owner='D5';domain='Zero value construction in the numeric domain.'
    elif re.fullmatch(r'System\.(?:Int128|UInt128)\.(?:Int128|UInt128)\(ulong, ulong\)',signature):
        state='supported';owner='D5';domain='Upper/lower UInt64 constructor, exact limb representation.'
    elif re.fullmatch(r'System.Numerics.Complex.Complex\(double, double\)',signature):
        state='supported';owner='D5';domain='Ordered real/imaginary Double constructor.'
    elif re.fullmatch(r'System.Numerics.BigInteger.BigInteger\((?:int|uint|long|ulong|float|double)\)',signature):
        state='supported';owner='D5';domain='Integral or proved-finite floating construction.'
    elif re.fullmatch(r'(?:float|double|System.Half)\.Is(?:Finite|NaN|Infinity)\((?:float|double|System.Half)\)',signature):
        state='supported';owner='D5';domain='Exact IEEE classification predicates; IsFinite source dominance proof.'
    elif '(' not in signature:
        local=signature.rsplit('.',1)[-1]
        if signature.startswith('System.Numerics.Complex.'):
            supported=local in ['Real','Imaginary','Zero','One','ImaginaryOne','Infinity','NaN']
        elif signature.startswith('System.Numerics.BigInteger.'):
            supported=local in ['Zero','One','MinusOne','Sign','IsEven','IsOne','IsZero']
        else:
            supported=local in ['MinValue','MaxValue','Zero','One','NegativeOne','NegativeZero','Epsilon','NaN','PositiveInfinity','NegativeInfinity','E','Pi','Tau','MultiplicativeIdentity','Size']
        if supported:
            state='supported';owner='D5';domain='Exact resolved numeric constant/property; no name-only external dispatch.'
    row={'member':signature,'state':state,'owner':owner,'domain':domain}
    if state=='required-rejection': row['evidence']='NumericSafetyTests' if owner=='D5' else 'DecimalTests.UnprovedDecimalFailureOrInexactConstructionFailsWholeConversion' if owner=='D6' else 'FrontendTests.CheckedOperatorAndRefStructAreSyntaxRejections + fixed pointer/ref-like type validation'
    rows.append(row)
if len(rows)!=len({r['member'] for r in rows}):
    raise SystemExit('Duplicate numeric member')
output={'referencePack':'Microsoft.NETCore.App.Ref/10.0.0','roslyn':'5.0.0','policy':'Exact public declared symbol inventory including default value constructors; accessor methods represented by their property. Inherited Object and generic constraint/explicit-interface closure remain assigned by the master catalog. D5 delivers numeric representation/operators/conversions and exact listed members; unimplemented pure APIs remain pending, never implicitly supported.','members':rows}
(root/'docs/catalogs/numeric-members.json').write_text(json.dumps(output,indent=2)+'\n')
print(f'Inventoried {len(rows)} numeric public symbols; review classifications before validation.')
