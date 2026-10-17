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
for (const name of ['eligibility','prefix','addition']) {
    const output=`fixtures/D1/expected/${name}.json`;
    run(process.execPath,['eng/node_modules/morphir-elm/cli/morphir-elm.js','make','-p',`fixtures/D1/elm/${name}`,'-o',output]);
    records.push({name,output,kind:'ordinary Elm',mode:'default',source:`fixtures/D1/elm/${name}`});
}
const host='fixtures/D1/codec-reference';
run(path.resolve('eng/node_modules/.bin/elm'),['make','Main.elm','--output=../../../artifacts/codec-host.js'],{cwd:host,env:{...process.env,ELM_HOME:path.resolve('artifacts/elm-home')}});
const {Elm}=require('../artifacts/codec-host.js');
await new Promise(resolve=>Elm.Main.init({flags:null}).ports.output.subscribe(cases=>{
    for (const [name,data] of Object.entries(cases)) {
        const output=`fixtures/D1/expected/${name}.json`;
        fs.writeFileSync(output,JSON.stringify(data));
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
        else if (/\.(elm|json)$/.test(name) && name !== 'morphir-hashes.json') sources.push({path:p,sha256:hash(p)});
    }
}
inventory('fixtures/D1/elm'); inventory(host);
fs.writeFileSync('fixtures/D1/expected/provenance.json',JSON.stringify({toolchain,fixtures:records.map(r=>({...r,sha256:hash(r.output)})),sources},null,2)+'\n');
console.log('Independent references generated. Review JSON and provenance; no C# output was used.');
