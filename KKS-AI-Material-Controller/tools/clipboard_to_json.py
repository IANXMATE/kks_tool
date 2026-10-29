#!/usr/bin/env python3
"""Make a lightweight controller-compatible partial JSON from MEClipboard XML.

Only Float/Color/Vector records are transferred. Shader/mat names remain unchanged.
This is a parameter *patch*, not the source texture asset; XML embedded texture bytes
are deliberately excluded. The in-game controller validates all imported values.
"""
import argparse
import json
import math
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_clipboard(path: Path, entry_index: int = 0):
    root = ET.parse(path).getroot()
    if root.tag != 'MEClipboard' or root.findtext('Version') != '1':
        raise ValueError('Expected MEClipboard XML version 1')
    entries = root.findall('Entry')
    if not 0 <= entry_index < len(entries):
        raise ValueError(f'Entry index {entry_index} out of range (found {len(entries)})')
    entry = entries[entry_index]
    label = entry.findtext('Label')
    shader = entry.findtext('Shader/Name')
    if not label or not shader:
        raise ValueError('Clipboard entry must have a Label and Shader/Name')
    properties = []
    seen = set()
    for node in entry:
        tag = node.tag
        if tag not in ('Float', 'Color', 'Vector'):
            continue
        name = node.findtext('Name')
        if not name or name in seen:
            raise ValueError('Duplicate or empty property name')
        seen.add(name)
        p = {'name': name, 'type': tag}
        if tag == 'Float':
            p['number'] = float(node.findtext('Value'))
            values = [p['number']]
        else:
            values = [float(node.findtext(ch)) for ch in ('R','G','B','A')]
            p['vector'] = values
        if not all(math.isfinite(n) and abs(n) <= 1e6 for n in values):
            raise ValueError(f'Invalid numeric value: {name}')
        properties.append(p)
    return {
        'schemaVersion': 1,
        'source': 'MEClipboard-v1-converter (no embedded textures)',
        'materialName': label,
        'shaderName': shader,
        'properties': properties
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('clipboard_xml', type=Path)
    parser.add_argument('-o', '--output', default='inbox.json', type=Path)
    parser.add_argument('--entry', type=int, default=0)
    args = parser.parse_args()
    result = parse_clipboard(args.clipboard_xml, args.entry)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Wrote {args.output} ({len(result["properties"])} numeric/color properties, 0 textures)')


if __name__ == '__main__':
    main()
