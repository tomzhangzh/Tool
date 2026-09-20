import os, re

base = r"E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\Areas\Component\Views"
count = 0
for root, dirs, files in os.walk(base):
    for f in files:
        if not f.endswith('.cshtml'): continue
        if f.startswith('_'): continue
        p = os.path.join(root, f)
        with open(p, 'r', encoding='utf-8') as fh:
            lines = fh.readlines()
        # Find the @{ } block (first lines)
        # Then find @section setupScripts
        # The body is between them
        at_end = -1
        section_start = -1
        in_at = False
        brace_depth = 0
        for i, line in enumerate(lines):
            if at_end < 0:
                if line.strip().startswith('@{'):
                    in_at = True
                    brace_depth += line.count('{') - line.count('}')
                    if brace_depth <= 0:
                        at_end = i
                        in_at = False
                    continue
                if in_at:
                    brace_depth += line.count('{') - line.count('}')
                    if brace_depth <= 0:
                        at_end = i
                    continue
            if section_start < 0 and '@section' in line:
                section_start = i
                break
        if at_end < 0 or section_start < 0:
            continue
        # Body is lines[at_end+1 : section_start]
        body_lines = lines[at_end+1:section_start]
        body_text = ''.join(body_lines).strip()
        if not body_text:
            continue
        # Skip if already wrapped in <template>
        if re.match(r'^\s*<template[\s>]', body_text):
            continue
        # Reconstruct: at block + <template> + body + </template> + section
        new_lines = lines[:at_end+1]
        new_lines.append('\n<template>\n')
        new_lines.append(body_text + '\n')
        new_lines.append('</template>\n\n')
        new_lines.extend(lines[section_start:])
        with open(p, 'w', encoding='utf-8') as fh:
            fh.writelines(new_lines)
        count += 1
        print(f"Fixed: {os.path.relpath(p, base)}")
print(f"\nTotal: {count}")
