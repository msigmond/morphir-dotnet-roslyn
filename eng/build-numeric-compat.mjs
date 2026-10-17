// Explicit production compatibility-library build. Never runs during validation.
import fs from 'node:fs';
import {emitCompatCSharp} from './emit-compat-csharp.mjs';
import path from 'node:path';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
process.chdir(root);
if(process.argv[2]!=='--write') throw new Error('Explicit --write required.');
const toolchain=JSON.parse(fs.readFileSync('eng/reference-toolchain.json'));
const source='eng/numeric-compat';
const output='src/Morphir.Frontend.CSharp/Compatibility/numeric.json';
fs.mkdirSync(path.dirname(output),{recursive:true});
execFileSync(process.execPath,['eng/node_modules/morphir-elm/cli/morphir-elm.js','make','-p',source,'-o',output],{stdio:'inherit'});
const generated='src/Morphir.Frontend.CSharp/Compatibility/NumericLibrary.Generated.cs';
emitCompatCSharp(JSON.parse(fs.readFileSync(output)),generated);
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const sources=[];
function inventory(dir){for(const name of fs.readdirSync(dir).sort()){if(['elm-stuff','node_modules'].includes(name))continue;const p=path.join(dir,name);if(fs.statSync(p).isDirectory())inventory(p);else if(/\.(json|elm)$/.test(name)&&name!=='morphir-hashes.json')sources.push({path:p,sha256:hash(p)});}}
inventory(source);
fs.writeFileSync('docs/references/numeric-compat-provenance.json',JSON.stringify({toolchain,kind:'production compatibility library compiled from original pure Elm sources',mode:'default',output,sha256:hash(output),generated:{path:generated,sha256:hash(generated)},sources},null,2)+'\n');
