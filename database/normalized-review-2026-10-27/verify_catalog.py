"""Compare actual SQL Server catalog evidence with the candidate manifest, offline."""
from pathlib import Path
from collections import defaultdict
import json
import re
import argparse

parser=argparse.ArgumentParser()
parser.add_argument('--allow-data',action='store_true',help='Verify schema while preserving seeded/application rows.')
options=parser.parse_args()

root=Path(__file__).resolve().parent
evidence=root.parents[1]/'.artifacts'/'database-normalization-20261027'
manifest=json.loads((root/'schema-manifest.json').read_text(encoding='utf-8'))
catalog=json.loads((evidence/'catalog.json').read_text(encoding='utf-8-sig'))
sets=[s['rows'] for s in catalog]
column_rows=next(s for s in sets if s and 'sql_type' in s[0] and 'column_name' in s[0])
key_rows=next(s for s in sets if s and 'key_ordinal' in s[0])
fk_rows=next(s for s in sets if s and 'constraint_column_id' in s[0])
column_rows=[r for r in column_rows if r['name']!='sysdiagrams']
key_rows=[r for r in key_rows if r['table_name']!='sysdiagrams']
fk_rows=[r for r in fk_rows if r['table_name']!='sysdiagrams']
actual_columns={(r['name'],r['column_name']):r for r in column_rows}
expected_columns={}
for table in manifest['tables']:
    for column in table['columns']:
        key=(table['name'],column['name']); expected_columns[key]=column
        actual=actual_columns[key]
        expected_type=column['sql_type']; base=expected_type.split('(')[0]
        assert actual['sql_type']==base,(key,actual,expected_type)
        assert bool(actual['is_nullable'])==column['nullable'],key
        assert bool(actual['is_identity'])==column['identity'],key
        if base=='nvarchar':
            size=expected_type.split('(')[1].rstrip(')')
            assert actual['max_length']==(-1 if size=='max' else int(size)*2),key
        if base=='decimal':
            precision,scale=map(int,re.findall(r'\d+',expected_type))
            assert (actual['precision'],actual['scale'])==(precision,scale),key
        if base=='datetime2': assert actual['scale']==int(re.findall(r'\d+',expected_type)[-1]),key
assert set(actual_columns)==set(expected_columns)
actual_keys=defaultdict(list)
for row in sorted(key_rows,key=lambda r:(r['table_name'],r['constraint_name'],r['key_ordinal'])):
    actual_keys[(row['table_name'],row['constraint_name'],row['type'])].append(row['column_name'])
expected_keys={}
actual_fks=defaultdict(list); expected_fks={}
for row in sorted(fk_rows,key=lambda r:(r['table_name'],r['constraint_name'],r['constraint_column_id'])):
    actual_fks[(row['table_name'],row['constraint_name'],row['referenced_table'])].append([row['column_name'],row['referenced_column']])
for table in manifest['tables']:
    name=table['name']; expected_keys[(name,'PK_'+name,'PK')]=table['pk']
    for i,key in enumerate(table['unique'],1): expected_keys[(name,f'UQ_{name}_{i}','UQ')]=key
    for i,fk in enumerate(table['foreign'],1):
        expected_fks[(name,f'FK_{name}_{i}',fk['table'])]=[list(pair) for pair in zip(fk['columns'],fk['references'])]
assert dict(actual_keys)==expected_keys
assert dict(actual_fks)==expected_fks
rows=next(s for s in sets if s and 'row_count' in s[0])
ignored_tables=[r['name'] for r in rows if r['name']=='sysdiagrams']
rows=[r for r in rows if r['name']!='sysdiagrams']
business_rows=sum(r['row_count'] for r in rows)
assert options.allow_data or business_rows==0,'Data is present. Use --allow-data for schema-only verification after seeding.'
result=dict(status='PASS',tables=len(manifest['tables']),columns_compared=len(actual_columns),
            key_constraints_compared=len(actual_keys),foreign_keys_compared=len(actual_fks),business_rows=business_rows,
            ignored_helper_tables=ignored_tables,
            qualification='Actual SQL catalog versus candidate manifest; does not certify completeness of business FDs.')
(evidence/'catalog-comparison.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result))
