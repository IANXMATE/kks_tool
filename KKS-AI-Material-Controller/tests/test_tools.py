import json
import tempfile
import unittest
from pathlib import Path
from tools.clipboard_to_json import parse_clipboard
from tools.validate_snapshot import validate


class TestTools(unittest.TestCase):
    def test_convert(self):
        xml = '''<?xml version="1.0"?><MEClipboard><Version>1</Version><Entry>
          <Label>cf_m_panst_02</Label><Shader><Name>KKUTSalpha</Name></Shader>
          <Float><Name>MatCap</Name><Value>0.7</Value></Float>
          <Color><Name>BaseColor</Name><R>0.8</R><G>0.9</G><B>1</B><A>1</A></Color>
          <Texture><Name>MainTex</Name><Data>iVBORw0KGgo=</Data></Texture>
          </Entry></MEClipboard>'''
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'input.xml'
            path.write_text(xml, encoding='utf-8')
            data = parse_clipboard(path)
            self.assertEqual(validate(data), 2)
            self.assertEqual(data['shaderName'], 'KKUTSalpha')
            self.assertNotIn('iVBOR', json.dumps(data))

    def test_reject_duplicate(self):
        obj = {'schemaVersion': 1, 'materialName': 'm', 'shaderName': 's',
               'properties': [{'name': 'x', 'type': 'Float', 'number': 1},
                              {'name': 'x', 'type': 'Float', 'number': 2}]}
        with self.assertRaises(ValueError): validate(obj)

    def test_reject_nan(self):
        obj = {'schemaVersion': 1, 'materialName': 'm', 'shaderName': 's',
               'properties': [{'name': 'x', 'type': 'Float', 'number': float('nan')}]}
        with self.assertRaises(ValueError): validate(obj)


if __name__ == '__main__': unittest.main()
