"""Validate note YAML only; never modify notes. Usage: python AI/check-frontmatter.py FILE ..."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path.home() / '.local/share/colonysim/python'))
import yaml

class UniqueLoader(yaml.SafeLoader):
    pass

def mapping(loader, node, deep=False):
    result = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in result:
            raise ValueError(f'duplicate YAML key: {key}')
        result[key] = loader.construct_object(value_node, deep=deep)
    return result

UniqueLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, mapping)

def validate(text):
    lines = text.splitlines()
    if not lines or lines[0] != '---':
        raise ValueError('frontmatter must begin on first line')
    end = next((i for i in range(1, len(lines)) if lines[i] == '---'), None)
    if end is None:
        raise ValueError('missing closing frontmatter delimiter')
    data = yaml.load('\n'.join(lines[1:end]), Loader=UniqueLoader)
    if not isinstance(data, dict):
        raise ValueError('frontmatter must be a mapping')
    return data

if __name__ == '__main__':
    if len(sys.argv) < 2:
        raise SystemExit('Pass one or more Markdown paths')
    failures = 0
    for name in sys.argv[1:]:
        try:
            validate(Path(name).read_text(encoding='utf-8'))
            print(f'OK: {name}')
        except Exception as exc:
            failures += 1
            print(f'ERROR: {name}: {exc}', file=sys.stderr)
    raise SystemExit(bool(failures))
