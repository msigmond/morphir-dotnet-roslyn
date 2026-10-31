#!/usr/bin/env python3
"""Validate fixed-scope ownership; --require-complete blocks premature final acceptance."""
import argparse
import json
from pathlib import Path
import re
import base64

parser = argparse.ArgumentParser()
parser.add_argument('--require-complete', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
parity = json.loads((root/'fixtures/elm-parity/cases.json').read_text())
parity_cases = {case['id']: case for case in parity['cases']}
if len(parity_cases) != len(parity['cases']):
    raise SystemExit('Duplicate exact-parity case ID')
for case in parity_cases.values():
    if not case['features'] or not case['sources'] or case['reference'] not in parity['references']:
        raise SystemExit(f'Incomplete parity case: {case["id"]}')
    for file in case['sources']:
        if not (root/file).is_file():
            raise SystemExit(f'Missing paired C# input: {file}')
for reference in parity['references'].values():
    if not (root/reference['project']/'morphir.json').is_file() or not (root/reference['expected']).is_file():
        raise SystemExit(f'Missing Elm input or make output: {reference}')
    if reference.get('dependencyPackages'):
        config = json.loads((root/reference['project']/'morphir.json').read_text())
        expected = ['data:application/json;base64,' + base64.b64encode((root/parity['references'][dependency]['expected']).read_bytes()).decode()
                    for dependency in reference['dependencyPackages']]
        if config.get('dependencies') != expected:
            raise SystemExit(f'Dependency fixture differs from independently compiled package: {reference["project"]}')
pending = []
count = 0
for file, collection, identity in [('forms.json','forms','form'),('operations.json','operations','operation'),('framework.json','types','type'),('numeric-members.json','members','member'),('decimal-members.json','members','member')]:
    rows = json.loads((root/'docs/catalogs'/file).read_text())[collection]
    keys = set()
    for row in rows:
        key = row[identity]
        if key in keys:
            raise SystemExit(f'Duplicate {file} entry: {key}')
        keys.add(key)
        if not re.fullmatch(r'D(?:[1-9]|1[0-7]|18\.[1-4])', row['owner']):
            raise SystemExit(f'Invalid owning delivery: {key}')
        if row['state'] not in ('supported','pending','required-rejection'):
            raise SystemExit(f'Invalid support state: {key}')
        if row['state'] == 'supported' and (not row.get('parityCases') or any(case not in parity_cases for case in row['parityCases'])):
            raise SystemExit(f'Supported feature lacks exact Elm parity cases: {file}: {key}')
        incomplete = row['state'] == 'pending' or row.get('memberState') == 'pending'
        if row['state'] == 'required-rejection' and not row.get('evidence'):
            incomplete = True
        if collection == 'types' and any(not row.get(kind) for kind in ('positiveFixtures','negativeFixtures','boundaryFixtures')):
            incomplete = True
        if incomplete:
            pending.append(f'{row["owner"]}: {key}')
        count += 1
if args.require_complete and pending:
    raise SystemExit(f'Final acceptance blocked: {len(pending)} catalog obligations remain.\n' + '\n'.join(pending))
print(f'Catalog ownership/states valid: {count} entries; {len(pending)} outstanding final-acceptance obligations retained.')
