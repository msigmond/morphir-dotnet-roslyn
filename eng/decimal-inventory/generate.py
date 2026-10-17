#!/usr/bin/env python3
"""Explicit pinned Decimal public-symbol inventory; never called during validation."""
import json,re,sys
from pathlib import Path
if sys.argv[1:]!=['--write']: raise SystemExit('Explicit inventory maintenance requires --write')
root=Path(__file__).resolve().parents[2]
rows=[]
for signature in (root/'artifacts/decimal-members.txt').read_text().splitlines():
    if not signature or ': error ' in signature: raise SystemExit('Invalid symbol output')
    state='pending'; owner='D18.4'; domain='Remaining pure Decimal API closure; no current support claim.'
    if 'Span<' in signature or re.search(r'\w\*',signature):
        state='required-rejection';domain='Ref-like/pointer representations excluded by the master contract.'
    elif '[]' in signature:
        owner='D11';domain='Array/owned-buffer safety prerequisite.'
    elif '<' in signature and 'operator' not in signature:
        owner='D10';domain='Resolved generic constraints/type arguments prerequisite.'
    elif '(object' in signature:
        owner='D17';domain='Object boxing/type-domain prerequisite.'
    elif 'System.MidpointRounding' in signature:
        owner='D8';domain='Enum representation and member binding prerequisite; default ToEven rounding is delivered.'
    elif 'operator ++' in signature or 'operator --' in signature:
        owner='D15';domain='Increment and owned rebinding prerequisite.'
    elif 'operator' in signature or re.fullmatch(r'decimal.Decimal\((?:int|uint|long|ulong|float|double)?\)',signature):
        state='supported';owner='D6';domain='Exact coefficient/scale/sign and D3 range/nonzero/conversion proofs; mappings/D6.md.'
        if re.search(r'operator decimal\((?:float|double|System.Half)\)',signature) or re.fullmatch(r'decimal.Decimal\((?:float|double)\)',signature):
            state='required-rejection';domain='Binary-floating Decimal construction excluded by exact-construction policy; MCS2704.'
    elif signature=='decimal.Decimal(int, int, int, bool, byte)':
        state='supported';owner='D6';domain='Exact 96-bit parts with scale proved 0..28.'
    elif re.fullmatch(r'decimal\.(?:Abs|Negate|Ceiling|Floor|Truncate|Sign|IsNegative|Round|Add|Subtract|Multiply|Divide|Remainder|Compare|Max|Min|CompareTo|Equals)\(decimal(?:, decimal|, int)?\)',signature):
        state='supported';owner='D6';domain='Exact resolved Decimal overload with D3 safety and member preconditions.'
    elif signature in ['decimal.Scale','decimal.Zero','decimal.One','decimal.MinusOne','decimal.MinValue','decimal.MaxValue']:
        state='supported';owner='D6';domain='Exact coefficient, scale and sign, including signed zero and full CLR range.'
    row={'member':signature,'state':state,'owner':owner,'domain':domain}
    if state=='required-rejection':row['evidence']='DecimalTests.UnprovedDecimalFailureOrInexactConstructionFailsWholeConversion' if owner=='D6' else 'FrontendTests.CheckedOperatorAndRefStructAreSyntaxRejections + ref-like/pointer type rejection'
    rows.append(row)
if len(rows)!=len({r['member'] for r in rows}):raise SystemExit('Duplicate Decimal member')
(root/'docs/catalogs/decimal-members.json').write_text(json.dumps({'referencePack':'Microsoft.NETCore.App.Ref/10.0.0','roslyn':'5.0.0','policy':'Exact public declarations including default construction; accessors represented by properties. Decimal language/representation support does not imply complete framework API closure. Later dependencies remain explicit.','members':rows},indent=2)+'\n')
print(f'Inventoried {len(rows)} Decimal public symbols; review classifications before validation.')
