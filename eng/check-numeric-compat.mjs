// Native BigInt is independent of the generated Morphir/Elm compatibility implementation.
import {createRequire} from 'node:module';
import {execFileSync} from 'node:child_process';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
process.chdir(root);
execFileSync(path.resolve('eng/node_modules/.bin/elm'),['make','Main.elm','--output=../../artifacts/numeric-check.js'],{cwd:'eng/numeric-compat-check',stdio:'inherit',env:{...process.env,ELM_HOME:path.resolve('artifacts/elm-home')}});
const {Elm}=createRequire(import.meta.url)('../artifacts/numeric-check.js');
function encode(n){const negative=n<0n;n=negative?-n:n;const digits=[];while(n){digits.push(Number(n&65535n));n>>=16n;}return {negative,digits};}
function decode(v){let n=0n;for(let i=v.digits.length-1;i>=0;i--)n=(n<<16n)+BigInt(v.digits[i]);return v.negative?-n:n;}
let seed=0x5d53n;
function next(){seed=(seed*6364136223846793005n+1442695040888963407n)&((1n<<128n)-1n);return seed;}
const values=[0n,1n,-1n,65535n,65536n,-65536n,2147483647n,-2147483648n,(1n<<64n)-1n,-(1n<<127n),(1n<<128n)-1n,(1n<<256n)+1n];
for(let i=0;i<48;i++)values.push(i%2?next():-next());
const cases=[],expected=[];
function add(operation,a,b,result,bits=32,signed=true,count=0){cases.push({operation,left:encode(a),right:encode(b),bits,signed,count});expected.push(result);}
for(let i=0;i<values.length;i++){
 const a=values[i],b=values[(i*7+3)%values.length];
 add('add',a,b,a+b);add('subtract',a,b,a-b);add('multiply',a,b,a*b);
 if(b){add('divide',a,b,a/b);add('remainder',a,b,a%b);}
 add('and',a,b,a&b);add('or',a,b,a|b);add('xor',a,b,a^b);add('complement',a,0n,~a);
 add('compare',a,b,a===b?0n:a<b?-1n:1n);
 for(const bits of [8,16,32,64,128])for(const signed of [false,true])add('wrap',a,0n,signed?BigInt.asIntN(bits,a):BigInt.asUintN(bits,a),bits,signed);
 for(const count of [-33,-1,0,1,16,33,129]){
  add('left',a,0n,count<0?a>>BigInt(-count):a<<BigInt(count),32,true,count);
  add('right',a,0n,count<0?a<<BigInt(-count):a>>BigInt(count),32,true,count);
  let logical;
  if(count<=0||a>=0n)logical=count<0?a<<BigInt(-count):a>>BigInt(count);
  else {const magnitude=-a;let bits=Math.max(32,Math.ceil(magnitude.toString(2).length/32)*32);if(magnitude>(1n<<BigInt(bits-1)))bits+=32;logical=count>=bits?-1n:BigInt.asIntN(bits-32*Math.floor(count/32),BigInt.asUintN(bits,a)>>BigInt(count));}
  add('logical',a,0n,logical,32,true,count);
 }
}
await new Promise((resolve,reject)=>Elm.Main.init({flags:cases}).ports.output.subscribe(results=>{
 try{if(results.length!==cases.length)throw new Error('Missing results');for(let i=0;i<results.length;i++)if(decode(results[i])!==expected[i])throw new Error(`Case ${i} ${cases[i].operation}: expected ${expected[i]}, actual ${decode(results[i])}`);console.log(`Exact integer behavior passed: ${results.length} boundary/random arithmetic, division, bitwise, shift and wrap cases.`);resolve();}catch(e){reject(e);}
}));
