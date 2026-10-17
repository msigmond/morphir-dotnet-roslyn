#!/usr/bin/env python3
"""Validate fixed-scope ownership; --require-complete blocks premature final acceptance."""
import argparse
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('--require-complete', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
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
