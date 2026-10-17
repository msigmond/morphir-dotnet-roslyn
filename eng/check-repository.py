#!/usr/bin/env python3
"""Read-only bootstrap contracts, source stability, and frozen reference hashes."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

root = Path(__file__).resolve().parent.parent
errors = []
def check(condition, message):
    if not condition:
        errors.append(message)

plans = root / 'Morphir_CSharp_Frontend_deliveries'
check(len(list(plans.glob('*.md'))) == 24, 'Expected all 24 child plans')
for file in [root/'Morphir_CSharp_Frontend_delivery_plan.md', *plans.glob('*.md')]:
    original = subprocess.check_output(['git', 'show', f'HEAD:{file.relative_to(root)}'], cwd=root).decode()
    check(re.findall(r'```(?:csharp|elm)\n(.*?)```', file.read_text(), re.S) == re.findall(r'```(?:csharp|elm)\n(.*?)```', original, re.S), f'Embedded paired code changed: {file}')

files = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard'], cwd=root).decode().splitlines()
for name in files:
    file = root/name
    if file.suffix != '.md':
        continue
    content = file.read_text()
    check(len(re.findall(r'^```', content, re.M)) % 2 == 0, f'Unbalanced Markdown fences: {name}')
    # Links inside inline/fenced code are examples, not repository link contracts.
    content = re.sub(r'```.*?```', '', content, flags=re.S)
    content = re.sub(r'`[^`]*`', '', content)
    for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)', content):
        if re.match(r'[a-z]+:', target) or target.startswith('#'):
            continue
        target = target.split('#')[0]
        check((file.parent/target).exists(), f'Broken Markdown link: {name}: {target}')

for manifest_path in sorted((root/'fixtures').glob('*/expected/provenance.json')):
    manifest = json.loads(manifest_path.read_text())
    for item in manifest['fixtures'] + manifest['sources']:
        name = item.get('output', item.get('path'))
        check(hashlib.sha256((root/name).read_bytes()).hexdigest() == item['sha256'], f'Frozen reference hash differs: {name}')
compat_manifest=root/'docs/references/numeric-compat-provenance.json'
if compat_manifest.exists():
    compat=json.loads(compat_manifest.read_text())
    items=[{'path':compat['output'],'sha256':compat['sha256']},compat['generated'],*compat['sources']]
    for item in items:
        check(hashlib.sha256((root/item['path']).read_bytes()).hexdigest()==item['sha256'],f"Compatibility source/output hash differs: {item['path']}")
for name, digest in [('LICENSE','b40930bbcf80744c86c46a12bc9da056641d722716c378f5659b9e555ef833e1'),('NOTICE','24564967220aaade6950d5442cdab8d48033abde6c7906285a03a8b98a45542a')]:
    check(hashlib.sha256((root/name).read_bytes()).hexdigest() == digest, f'Protected bytes changed: {name}')
check((root/'CLAUDE.md').read_bytes() == b'@AGENTS.md\n', 'CLAUDE.md must remain pointer-only')
result = subprocess.run(['git','diff','--cached','--check'], cwd=root)
check(result.returncode == 0, 'Staged whitespace check failed')
result = subprocess.run(['git','diff','--check'], cwd=root)
check(result.returncode == 0, 'Tracked whitespace check failed')
for name in files:
    if subprocess.run(['git','ls-files','--error-unmatch',name],cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
        result = subprocess.run(['git','diff','--no-index','--check','/dev/null',name],cwd=root,capture_output=True,text=True)
        check(result.returncode in (0, 1) and not result.stdout and not result.stderr, f'New-file whitespace check failed: {name}: {result.stdout}{result.stderr}')
if errors:
    raise SystemExit('\n'.join(errors))
print('Repository contracts passed: 24 plans, paired code, Markdown links/fences, frozen references, licenses, CLAUDE, tracked/new/staged whitespace.')
