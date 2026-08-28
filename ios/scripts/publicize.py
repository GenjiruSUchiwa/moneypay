#!/usr/bin/env python3
"""One-shot migration helper for the Milly-style package split.

Promotes package-internal declarations to `public` and synthesises the public
memberwise `init` that Swift only generates as internal. Mechanical only: it
never reorders or rewrites logic, and it is safe to re-run.

Usage: python3 scripts/publicize.py [file.swift ...]
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PKGS = os.path.join(ROOT, 'Packages')

ATTRS = r'(?:@\w+(?:\([^)]*\))?\s+)*'
MODS = ATTRS + r'(?:public\s+|private\s+|fileprivate\s+|internal\s+|open\s+|final\s+|indirect\s+|static\s+|class\s+(?=func|var|let)|nonisolated(?:\(unsafe\))?\s+|override\s+)*'
DECL = re.compile(r'^(?P<ind>[ ]*)(?P<attrs>' + ATTRS + r')(?P<mods>' + MODS[len(ATTRS):] + r')'
                  r'(?P<kw>struct|class|enum|actor|protocol|extension|func|var|let|init|subscript|typealias)\b')
ACCESS = re.compile(r'\b(public|private|fileprivate|internal|open)\b')
STORED = re.compile(r'^[ ]{4}(?P<wrap>(?:@\w+(?:\([^)]*\))?\s+)*)(?:public\s+)?'
                    r'(?P<kw>var|let)\s+(?P<name>\w+)\s*'
                    r'(?::\s*(?P<type>[^={]+?))?\s*(?:=\s*(?P<default>.+?))?\s*$')

def strip_code(line):
    line = re.sub(r'"(?:\\.|[^"\\])*"', '""', line)
    return re.sub(r'//.*$', '', line)

def ext_declares_conformance(head):
    return ':' in head.split(' where ')[0]

def publicize(text):
    out, depth, proto_depth, private_depth, ext_depth = [], 0, None, None, None
    for line in text.split('\n'):
        m = DECL.match(line)
        if m and proto_depth is not None:
            # 'public' is not allowed on protocol requirements
            line = re.sub(r'\bpublic ', '', line, count=1)
        if m and proto_depth is None:
            kw, ind = m.group('kw'), len(m.group('ind'))
            head = line.split('{')[0]
            skip_ext = kw == 'extension' and ext_declares_conformance(head)
            # Nested types stay internal: Swift only infers Sendable for
            # non-public types, and `public` would break that inference.
            nested_type = kw in ('struct', 'class', 'enum', 'actor', 'protocol') and ind > 0
            # inside a `public extension`, `public` on a member is redundant
            redundant = ext_depth is not None and depth == ext_depth + 1
            if (not redundant and not ACCESS.search(head) and private_depth is None
                    and depth <= 1 and ind <= 4 and not skip_ext and not nested_type):
                at = m.end('attrs')
                line = line[:at] + 'public ' + line[at:]
            if kw == 'extension' and line.startswith('public extension'):
                ext_depth = depth
            if kw in ('struct', 'class', 'enum', 'actor', 'protocol'):
                if kw == 'protocol':
                    proto_depth = depth
                elif re.search(r'\b(private|fileprivate)\b', head) and private_depth is None:
                    private_depth = depth
        code = strip_code(line)
        depth += code.count('{') - code.count('}')
        if proto_depth is not None and depth <= proto_depth:
            proto_depth = None
        if private_depth is not None and depth <= private_depth:
            private_depth = None
        if ext_depth is not None and depth <= ext_depth:
            ext_depth = None
        out.append(line)
    return '\n'.join(out)

LITERALS = [(re.compile(r'^(true|false)$'), 'Bool'),
            (re.compile(r'^"'), 'String'),
            (re.compile(r'^-?\d+$'), 'Int'),
            (re.compile(r'^-?\d+\.\d+$'), 'Double')]

def infer(default):
    for rx, t in LITERALS:
        if rx.match((default or '').strip()):
            return t
    return None

def add_inits(text):
    """Give every public struct an explicit public memberwise init."""
    lines = text.split('\n')
    out, i = [], 0
    while i < len(lines):
        line = lines[i]
        out.append(line)
        m = re.match(r'^public struct (\w+)', line)
        if not m or not line.rstrip().endswith('{'):
            i += 1
            continue
        depth, j, body = 1, i + 1, []
        while j < len(lines) and depth > 0:
            depth += strip_code(lines[j]).count('{') - strip_code(lines[j]).count('}')
            if depth > 0:
                body.append(lines[j])
            j += 1
        if any(re.match(r'^[ ]{4}(?:public |private |)init\b', b) for b in body):
            i += 1
            continue
        params, assigns, ok = [], [], True
        stored_any = False
        for b in body:
            if not b.strip() or b.strip().startswith('//') or b.strip().startswith('///'):
                continue
            sm = STORED.match(b)
            if not sm:
                continue
            eq, brace = b.find('='), b.find('{')
            if brace != -1 and (eq == -1 or brace < eq):
                continue                      # computed property, not stored
            if re.search(r'\bstatic\b', b.split('=')[0]):
                continue
            stored_any = True
            if re.search(r'\b(private|fileprivate)\b', b.split('=')[0]):
                continue
            name, typ, dflt = sm.group('name'), sm.group('type'), sm.group('default')
            wrap = sm.group('wrap').strip()
            if sm.group('kw') == 'let' and dflt:
                continue
            if typ is None:
                typ = infer(dflt)
                if typ is None:
                    ok = False
                    break
            typ = typ.strip()
            if wrap:
                w = re.match(r'@(\w+)', wrap).group(1)
                if w == 'Binding':
                    params.append(f'{name}: Binding<{typ}>')
                    assigns.append(f'self._{name} = {name}')
                    continue
                if w == 'ViewBuilder':
                    if '->' in typ:
                        params.append(f'@ViewBuilder {name}: @escaping {typ}')
                        assigns.append(f'self.{name} = {name}')
                    else:
                        params.append(f'@ViewBuilder {name}: () -> {typ}')
                        assigns.append(f'self.{name} = {name}()')
                    continue
                if w == 'State':
                    params.append(f'{name}: {typ}' + (f' = {dflt}' if dflt else ''))
                    assigns.append(f'self._{name} = State(initialValue: {name})')
                    continue
                ok = False
                break
            if '->' in typ and not typ.endswith('?'):
                typ = '@escaping ' + typ
            params.append(f'{name}: {typ}' + (f' = {dflt}' if dflt else ''))
            assigns.append(f'self.{name} = {name}')
        if ok and (params or not stored_any):
            out.append('    public init(' + ', '.join(params) + ') {')
            out += ['        ' + a for a in assigns]
            out.append('    }')
            out.append('')
        i += 1
    return '\n'.join(out)

def files():
    for dp, _, fns in os.walk(PKGS):
        if os.sep + 'Tests' in dp:
            continue
        for fn in sorted(fns):
            if fn.endswith('.swift'):
                yield os.path.join(dp, fn)

if __name__ == '__main__':
    targets = sys.argv[1:] or list(files())
    for t in targets:
        src = open(t).read()
        open(t, 'w').write(add_inits(publicize(src)))
    print(f'publicized {len(targets)} files')
