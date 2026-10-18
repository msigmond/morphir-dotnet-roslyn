// Explicit generation only. Validation never imports this script or rewrites expected JSON.
import {execFileSync} from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const root = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
process.chdir(root);
if (process.argv[2] !== '--write') throw new Error('Explicit reference generation requires --write; review all changed fixtures afterward.');
const toolchain = JSON.parse(fs.readFileSync('eng/reference-toolchain.json'));
const run = (cmd,args,options={}) => execFileSync(cmd,args,{stdio:'inherit',...options});
const reference = 'artifacts/morphir-elm';
fs.mkdirSync('artifacts',{recursive:true});
if (!fs.existsSync(reference)) run('git',['clone',toolchain.sourceRepository,reference]);
run('git',['-C',reference,'checkout','--detach',toolchain.sourceCommit]);
if (execFileSync('git',['-C',reference,'rev-parse','HEAD'],{encoding:'utf8'}).trim() !== toolchain.sourceCommit) throw new Error('Wrong reference source revision');
if (execFileSync('git',['-C',reference,'status','--porcelain'],{encoding:'utf8'}).trim()) throw new Error('Reference source has local changes');
const require = createRequire(import.meta.url);
if (require('./node_modules/morphir-elm/package.json').version !== toolchain.morphirElmVersion) throw new Error('Wrong Morphir compiler');
const records = [];
const host='fixtures/D10/decimal-factory-codec-reference';
run(path.resolve('eng/node_modules/.bin/elm'),['make','Main.elm','--output=../../../artifacts/d10-decimal-factory-codec-host.js'],{cwd:host,env:{...process.env,ELM_HOME:path.resolve('artifacts/elm-home')}});
const {Elm}=require('../artifacts/d10-decimal-factory-codec-host.js');
await new Promise(resolve=>Elm.Main.init({flags:JSON.parse(fs.readFileSync('fixtures/D6/expected/helpers.json'))}).ports.output.subscribe(cases=>{
    if(cases.error) throw new Error(cases.error);
    for (const [name,data] of Object.entries(cases)) {
        const output=`fixtures/D10/expected/${name}.json`;
        const bytes=JSON.stringify(data);
        if(fs.existsSync(output) && fs.readFileSync(output,'utf8')!==bytes) throw new Error('Frozen reference differs: '+output);
        if(!fs.existsSync(output)) fs.writeFileSync(output,bytes);
        records.push({name,output,kind:'independent complete Elm codec distribution',mode:'Elm 0.19.1 host',source:host});
    }
    resolve();
}));
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const sources = [];
function inventory(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
        const p=path.join(dir,name);
        if (['elm-stuff','node_modules','.git'].includes(name)) continue;
        if (fs.statSync(p).isDirectory()) inventory(p);
        else if (/\.(elm|json|cs)$/.test(name) && name !== 'morphir-hashes.json') sources.push({path:p,sha256:hash(p)});
    }
}
inventory(host); sources.push({path:'fixtures/D6/expected/helpers.json',sha256:hash('fixtures/D6/expected/helpers.json')}); sources.push({path:'fixtures/D10/csharp/DecimalFactories.cs',sha256:hash('fixtures/D10/csharp/DecimalFactories.cs')});
const previous=fs.existsSync('fixtures/D10/expected/provenance.json')?JSON.parse(fs.readFileSync('fixtures/D10/expected/provenance.json')):{sources:[],fixtures:[]};
for(const source of previous.sources) if(hash(source.path)!==source.sha256) throw new Error('Frozen reference source changed: '+source.path);
records.unshift(...previous.fixtures.filter(fixture=>!records.some(record=>record.output===fixture.output)));
sources.unshift(...previous.sources.filter(source=>!sources.some(item=>item.path===source.path)));
fs.writeFileSync('fixtures/D10/expected/provenance.json',JSON.stringify({toolchain,fixtures:records.map(r=>({...r,sha256:hash(r.output)})),sources},null,2)+'\n');
console.log('Independent references generated. Review JSON and provenance; no C# output was used.');
