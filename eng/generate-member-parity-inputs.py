#!/usr/bin/env python3
"""Write paired source inputs from a reviewed member audit. Never read C# IR."""
import argparse
import json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--audit',required=True);p.add_argument('--write',action='store_true');args=p.parse_args()
if not args.write:p.error('Use --write for explicit input maintenance.')
root=Path(__file__).resolve().parent.parent
rows=json.loads((root/args.audit).read_text());catalog_path=root/'fixtures/elm-parity/cases.json';catalog=json.loads(catalog_path.read_text())
def carrier(t):
 t=t.removeprefix('global::System.')
 if t in ('float','double','Half'):return 'Float'
 if t=='decimal':return 'Decimal'
 if t=='bool':return 'Bool'
 if t=='char':return 'Char'
 if t=='string':return 'String'
 if t in ('byte','sbyte','short','ushort','int','uint','long','ulong','nint','nuint','Int128','UInt128','Numerics.BigInteger'):return 'Int'
 raise ValueError('No approved carrier: '+t)
def counterpart(row):
 expr=row['expr'];inputs=row['inputs'];out=carrier(row['output']);types=[carrier(i['type']) for i in inputs];names=[i['name'] for i in inputs]
 if expr.startswith('checked('):expr=expr[len('checked('):-1]
 if expr.startswith('new '):
  assert out=='Decimal' and len(types)==1
  return ('Decimal.fromInt ' if types[0]=='Int' else 'Decimal.fromFloat ')+names[0]
 if expr.startswith('('):
  src=types[0]
  if src==out:return names[0]
  functions={('Int','Float'):'toFloat',('Float','Int'):'truncate',('Int','Decimal'):'Decimal.fromInt',('Float','Decimal'):'Decimal.fromFloat',('Decimal','Float'):'Decimal.toFloat',('Int','Char'):'Char.fromCode',('Char','Int'):'Char.toCode'}
  return functions[src,out]+' '+names[0]
 if expr in ('+p0','-p0'):
  return 'p0' if expr[0]=='+' else ('Decimal.negate p0' if out=='Decimal' else '-p0')
 if expr.startswith('p0 '):
  op=expr.split()[1]
  if types[0]=='Decimal':return 'Decimal.'+{'+':'add','-':'sub','*':'mul','==':'eq','!=':'neq','<':'lt','<=':'lte','>':'gt','>=':'gte'}[op]+' p0 p1'
  if op=='%':return 'remainderBy p1 p0'
  if op=='/':op='//' if out=='Int' else '/'
  if op=='!=':op='/='
  assert op in ('+','-','*','/','//','==','/=','<','<=','>','>=')
  return 'p0 '+op+' p1'
 if expr.endswith(('.Zero','.One','.MinusOne')):
  assert out=='Decimal'
  return 'Decimal.'+{'Zero':'zero','One':'one','MinusOne':'minusOne'}[expr.rsplit('.',1)[1]]
 if '.IsNaN(' in expr:return 'isNaN p0'
 if '.IsInfinity(' in expr:return 'isInfinite p0'
 method=expr.split('.')[-1].split('(')[0]
 if types[0]=='Decimal':
  name={'Abs':'abs','Add':'add','Subtract':'sub','Multiply':'mul','Negate':'negate','Round':'round','Truncate':'truncate','Equals':'eq','ToString':'toString'}[method]
  return 'Decimal.'+name+' '+' '.join(names)
 raise ValueError('No independently defined Elm member counterpart: '+row['key'])
rejections=[]
for file in ('numeric-members','decimal-members'):
 positive=[r for r in rows if r['file']==file and r['success']]
 case='d5-member-inventory' if file=='numeric-members' else 'd6-member-inventory'
 base=Path('fixtures/elm-parity')/case
 cs=[];elm=['import Morphir.SDK.Decimal as Decimal exposing (Decimal)'];members={}
 for index,row in enumerate(positive):
  cs.append(row['member'].replace(' F(',f' M{index}('));name=f'm{index}';members[row['key']]=name
  elm += [name+' : '+' -> '.join([carrier(i['type']) for i in row['inputs']]+[carrier(row['output'])]),name+' '+' '.join(i['name'] for i in row['inputs'])+' = '+counterpart(row)]
 for path,text in [(base/'csharp/Input.cs','namespace Example.Completion;public static class Rules{\n'+'\n'.join(cs)+'\n}\n'),(base/'elm/src/Example/Completion/Rules.elm','module Example.Completion.Rules exposing (..)\n'+'\n'.join(elm)+'\n')]:
  target=root/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text)
 (root/base/'elm/morphir.json').write_text(json.dumps({'name':'Example','sourceDirectory':'src','exposedModules':['Completion.Rules']},indent=2)+'\n')
 catalog['references'][case]={'project':str(base/'elm'),'expected':str(base/'expected/elm.json')}
 catalog['cases']=[c for c in catalog['cases'] if c['id']!=case]
 catalog['cases'].append({'id':case,'reference':case,'package':'Example','namespaces':['Example.Completion'],'sources':[str(base/'csharp/Input.cs')],'features':['D5.member-inventory' if file=='numeric-members' else 'D6.member-inventory'],'members':members})
 for row in rows:
  if row['file']!=file or row['success']:continue
  code='namespace Example.Inventory;public static class Rules{'+row['member']+'}'
  rejections.append({'id':file+':'+row['key'],'member':row['key'],'source':code,'compilerBinding':any(d['Code']=='MCS0001' for d in row['diagnostics'])})
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n')
(root/'fixtures/elm-parity/member-rejections.json').write_text(json.dumps({'origin':'Pinned metadata declarations; source input only. No C# IR is used as an Elm reference.','cases':rejections},indent=2)+'\n')
print(f'Wrote {sum(r["success"] for r in rows)} paired member inputs and {len(rejections)} rejection inputs.')
