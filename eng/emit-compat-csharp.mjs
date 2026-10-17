// Typed source generation from the independently compiled compatibility IR.
// Fails on an unknown node; no JSON/foreign runtime is embedded in business values.
import fs from 'node:fs';
export function emitCompatCSharp(library, output, options={}){
 const className=options.className??'NumericLibrary';
 const generator=options.generator??'eng/build-numeric-compat.mjs';
 const packageJson=JSON.stringify(library.distribution[1]);
 const q=JSON.stringify;
 const name=n=>`new Name([${n.map(q).join(',')}])`;
 const path=p=>`new IrPath([${p.map(name).join(',')}])`;
 const fq=n=>`new FQName(${JSON.stringify(n[0])===packageJson?'package':path(n[0])},${path(n[1])},${name(n[2])})`;
 const methods=[];
 const array=items=>`[${items.join(',')}]`;
 for(let moduleIndex=0;moduleIndex<library.distribution[3].modules.length;moduleIndex++){
  const [modulePath,controlled]=library.distribution[3].modules[moduleIndex];
  const module=controlled.value,types=[],cache=new Map();
  function type(t){
   const key=JSON.stringify(t);if(cache.has(key))return `t${cache.get(key)}`;
   let code;
   switch(t[0]){
    case 'Reference':code=`new ReferenceType<Unit>(default,${fq(t[2])},${array(t[3].map(type))})`;break;
    case 'Function':code=`new FunctionType<Unit>(default,${type(t[2])},${type(t[3])})`;break;
    case 'Variable':code=`new VariableType<Unit>(default,${name(t[2])})`;break;
    case 'Record':code=`new RecordType<Unit>(default,${array(t[2].map(f=>`new Entry<Name,IrType<Unit>>(${name(f.name)},${type(f.tpe)})`))})`;break;
    default:throw new Error(`Unimplemented type ${t[0]}`);
   }
   const index=types.length;types.push(`IrType<Unit> t${index} = ${code};`);cache.set(key,index);return `t${index}`;
  }
  function literal(l){switch(l[0]){
   case 'WholeNumberLiteral':if(!Number.isSafeInteger(l[1]))throw new Error('Unsafe whole literal');return `new IntLiteral(${l[1]}L)`;
   case 'FloatLiteral':if(!Number.isFinite(l[1]))throw new Error('Non-finite JSON');return `new FloatLiteral(${Object.is(l[1],-0)?'-0.0':l[1]}D)`;
   case 'BoolLiteral':return `new BoolLiteral(${l[1]})`;
   case 'StringLiteral':return `new StringLiteral(${q(l[1])})`;
   default:throw new Error(`Unimplemented literal ${l[0]}`);
  }}
  function pattern(p){const a=type(p[1]);switch(p[0]){
   case 'WildcardPattern':return `new WildcardPattern<IrType<Unit>>(${a})`;
   case 'AsPattern':return `new AsPattern<IrType<Unit>>(${a},${pattern(p[2])},${name(p[3])})`;
   case 'EmptyListPattern':return `new EmptyListPattern<IrType<Unit>>(${a})`;
   case 'HeadTailPattern':return `new HeadTailPattern<IrType<Unit>>(${a},${pattern(p[2])},${pattern(p[3])})`;
   case 'ConstructorPattern':return `new ConstructorPattern<IrType<Unit>>(${a},${fq(p[2])},${array(p[3].map(pattern))})`;
   default:throw new Error(`Unimplemented pattern ${p[0]}`);
  }}
  function definition(d){return `new ValueDefinition<Unit,IrType<Unit>>(${array(d.inputTypes.map(i=>`new Input<Unit,IrType<Unit>>(${name(i[0])},${type(i[1])},${type(i[2])})`))},${type(d.outputType)},${value(d.body)})`;}
  function value(v){const a=type(v[1]),g='<Unit,IrType<Unit>>';switch(v[0]){
   case 'Literal':return `new LiteralValue${g}(${a},${literal(v[2])})`;
   case 'Variable':return `new VariableValue${g}(${a},${name(v[2])})`;
   case 'Reference':return `new ReferenceValue${g}(${a},${fq(v[2])})`;
   case 'Constructor':return `new ConstructorValue${g}(${a},${fq(v[2])})`;
   case 'Apply':return `new ApplyValue${g}(${a},${value(v[2])},${value(v[3])})`;
   case 'Field':return `new FieldValue${g}(${a},${value(v[2])},${name(v[3])})`;
   case 'IfThenElse':return `new IfValue${g}(${a},${value(v[2])},${value(v[3])},${value(v[4])})`;
   case 'LetDefinition':return `new LetValue${g}(${a},${name(v[2])},${definition(v[3])},${value(v[4])})`;
   case 'Record':return `new RecordValue${g}(${a},${array(v[2].map(([n,b])=>`new Entry<Name,Value<Unit,IrType<Unit>>>(${name(n)},${value(b)})`))})`;
   case 'List':return `new ListValue${g}(${a},${array(v[2].map(value))})`;
   case 'PatternMatch':return `new MatchValue${g}(${a},${value(v[2])},${array(v[3].map(([p,b])=>`new MatchCase${g}(${pattern(p)},${value(b)})`))})`;
   default:throw new Error(`Unimplemented value ${v[0]}`);
  }}
  const aliases=module.types.map(([n,c])=>{
   const d=c.value.value;if(d[0]!=='TypeAliasDefinition')throw new Error('Unknown definition');
   return `new Entry<Name,AccessControlled<Documented<AliasDefinition<Unit>>>>(${name(n)},new(Access.${c.access},new(${q(c.value.doc)},new(${array(d[1].map(name))},${type(d[2])}))))`;
  });
  const definitions=module.values.map(([n,c])=>`new Entry<Name,AccessControlled<Documented<ValueDefinition<Unit,IrType<Unit>>>>>(${name(n)},new(Access.${c.access},new(${q(c.value.doc)},${definition(c.value.value)})))`);
  methods.push(`internal static Entry<IrPath,AccessControlled<ModuleDefinition<Unit,IrType<Unit>>>> Module${moduleIndex}(IrPath package)\n{\n${types.join('\n')}\nreturn new(${path(modulePath)},new(Access.${controlled.access},new(${array(aliases)},${array(definitions)},${q(module.doc)})));\n}`);
 }
 fs.writeFileSync(output,`// <auto-generated />\n// Explicitly generated by ${generator} from original Elm compatibility sources.\nusing Morphir.IR.Classic;\nusing IrPath = Morphir.IR.Classic.Path;\nnamespace Morphir.Frontend.CSharp;\ninternal static class ${className}\n{\n${methods.join('\n')}\ninternal static Entry<IrPath,AccessControlled<ModuleDefinition<Unit,IrType<Unit>>>>[] Modules(IrPath package) => [${methods.map((_,i)=>`Module${i}(package)`).join(',')}];\n}\n`);
}
