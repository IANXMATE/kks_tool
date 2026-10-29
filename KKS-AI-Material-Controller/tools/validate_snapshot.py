#!/usr/bin/env python3
"""Basic offline schema validator. Does NOT replace in-game shader property checks."""
import argparse
import json
import math
from pathlib import Path


def validate(obj):
    if not isinstance(obj, dict) or obj.get('schemaVersion') != 1:
        raise ValueError('schemaVersion must be 1')
    if not isinstance(obj.get('shaderName'), str) or not obj['shaderName']:
        raise ValueError('Missing shaderName')
    if not isinstance(obj.get('materialName'), str) or not obj['materialName']:
        raise ValueError('Missing materialName')
    props = obj.get('properties')
    if not isinstance(props, list):
        raise ValueError('properties must be a list')
    names = set()
    for p in props:
        if not isinstance(p, dict):
            raise ValueError('Each property must be an object')
        name, kind = p.get('name'), p.get('type')
        if not isinstance(name, str) or not name or name in names:
            raise ValueError(f'Invalid / duplicate property name: {name}')
        names.add(name)
        if kind in ('Float', 'Range'):
            vals = [p.get('number')]
        elif kind in ('Color', 'Vector'):
            vals = p.get('vector')
            if not isinstance(vals, list) or len(vals) != 4:
                raise ValueError(f'{name}: expected four-component vector')
        elif kind == 'Texture':
            continue
        else:
            raise ValueError(f'{name}: unsupported type {kind}')
        if not all(isinstance(v, (float, int)) and not isinstance(v, bool) and math.isfinite(v) and abs(v) <= 1e6 for v in vals):
            raise ValueError(f'{name}: bad numeric values')
    return len(props)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('json_file', type=Path)
    args = parser.parse_args()
    obj = json.loads(args.json_file.read_text(encoding='utf-8'))
    print(f'OK: {validate(obj)} properties; game-side compatibility not tested')


if __name__ == '__main__':
    main()
