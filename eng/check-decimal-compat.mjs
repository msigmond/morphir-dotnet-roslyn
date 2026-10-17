// Read-only behavior validation. Expected cases are produced by pinned CLR decimal operations.
import {createRequire} from 'node:module';
import {execFileSync} from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));process.chdir(root);
execFileSync('sh',['eng/decimal-compat-check/generate-clr-cases.sh'],{stdio:'inherit'});
const data=JSON.parse(fs.readFileSync('artifacts/decimal-clr-cases.json'));
execFileSync(path.resolve('eng/node_modules/.bin/elm'),['make','Main.elm','--output=../../artifacts/decimal-check.js'],{cwd:'eng/decimal-compat-check',stdio:'inherit',env:{...process.env,ELM_HOME:path.resolve('artifacts/elm-home')}});
const {Elm}=createRequire(import.meta.url)('../artifacts/decimal-check.js');
function coefficient(v){let n=0n;for(let i=v.coefficient.digits.length-1;i>=0;i--)n=(n<<16n)+BigInt(v.coefficient.digits[i]);return n;}
function rationalText(text){const negative=text[0]==='-';if(negative)text=text.slice(1);const [whole,fraction='']=text.split('.');return {coefficient:BigInt(whole+fraction),scale:fraction.length,negative};}
const worker=Elm.Main.init({flags:data.cases});
await new Promise((resolve,reject)=>{worker.ports.failure.subscribe(reject);worker.ports.output.subscribe(results=>{
 try{if(results.length!==data.cases.length)throw new Error('Missing results');const failures=[];
 for(let i=0;i<results.length;i++){
  const a=results[i],e=data.expected[i];
  if(coefficient(a)!==coefficient(e)||a.scale!==e.scale||a.negative!==e.negative)failures.push({i,case:data.cases[i],expected:e,actual:a});
  const sdk=rationalText(a.value);const signed=x=>x.negative?-1n:1n;
  if(signed(sdk)*sdk.coefficient*10n**BigInt(a.scale)!==signed(a)*coefficient(a)*10n**BigInt(sdk.scale))throw new Error(`SDK value differs from representation at ${i}`);
 }
 if(failures.length){fs.writeFileSync('artifacts/decimal-failures.json',JSON.stringify(failures,null,2));throw new Error(`${failures.length} CLR Decimal mismatches; see artifacts/decimal-failures.json`);}
 fs.rmSync('artifacts/decimal-failures.json',{force:true});
 console.log(`Decimal compatibility passed: ${results.length} CLR coefficient/scale/sign and exact SDK value cases.`);resolve();}catch(e){reject(e);}
});});
