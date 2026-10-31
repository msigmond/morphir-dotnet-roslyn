// Explicit reference generation. Tests never update expected files.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const root = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
process.chdir(root);
if (process.argv[2] !== '--write') throw new Error('Use --write for explicit, reviewed reference generation.');
const toolchain = JSON.parse(fs.readFileSync('eng/reference-toolchain.json'));
const compilerPackage = JSON.parse(fs.readFileSync('eng/node_modules/morphir-elm/package.json'));
if (compilerPackage.version !== toolchain.morphirElmVersion) throw new Error('Wrong Morphir Elm compiler version.');
const catalog = JSON.parse(fs.readFileSync('fixtures/elm-parity/cases.json'));
const commands = [];
for (const [id, reference] of Object.entries(catalog.references)) {
    if (reference.dependencyPackages) {
        const configPath = path.join(reference.project, 'morphir.json');
        const config = JSON.parse(fs.readFileSync(configPath));
        config.dependencies = reference.dependencyPackages.map(dependency =>
            'data:application/json;base64,' + fs.readFileSync(catalog.references[dependency].expected).toString('base64'));
        fs.writeFileSync(configPath, JSON.stringify(config, null, 2) + '\n');
    }
    const scratch = path.join('artifacts', 'exact-reference-generation', id);
    fs.rmSync(scratch, {recursive: true, force: true});
    fs.mkdirSync(scratch, {recursive: true});
    fs.cpSync(reference.project, scratch, {recursive: true});
    fs.mkdirSync(path.dirname(reference.expected), {recursive: true});
    const args = [path.relative(scratch, 'eng/node_modules/morphir-elm/cli/morphir-elm.js'), 'make', '-p', '.', '-o', path.relative(scratch, reference.expected)];
    execFileSync(process.execPath, args, {cwd: scratch, stdio: 'pipe'});
    commands.push({id, workingDirectory: scratch, command: ['node', ...args]});
    console.log(`Generated ${id} with morphir-elm make`);
}
const files = [];
function inventory(dir) {
    for (const entry of fs.readdirSync(dir).sort()) {
        const name = path.join(dir, entry);
        if (entry === 'provenance.json' || entry === 'elm-stuff' || entry === 'morphir-hashes.json' || entry === 'obj' || entry === 'bin') continue;
        if (fs.statSync(name).isDirectory()) inventory(name);
        else files.push({path: name, sha256: crypto.createHash('sha256').update(fs.readFileSync(name)).digest('hex')});
    }
}
inventory('fixtures/elm-parity');
for (const name of catalog.provenanceInputs ?? [])
    files.push({path: name, sha256: crypto.createHash('sha256').update(fs.readFileSync(name)).digest('hex')});
fs.writeFileSync('fixtures/elm-parity/provenance.json', JSON.stringify({
    origin: 'Independent paired inputs; expected distributions produced only by the pinned morphir-elm make command.',
    comparison: 'Complete JSON equality. Only whitespace and object-key order are immaterial.',
    toolchain, commands, files
}, null, 2) + '\n');
