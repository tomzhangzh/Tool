import os, re, glob

base = r"E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\Areas\Component\Views"
count = 0
for root, dirs, files in os.walk(base):
    for f in files:
        if not f.endswith('.cshtml'): continue
        # skip layouts
        if f.startswith('_'): continue
        p = os.path.join(root, f)
        with open(p, 'r', encoding='utf-8') as fh:
            c = fh.read()
        # Match: @{...} <template> BODY </template> @section...
        # We want to remove the outer <template> ... </template> that wraps the body
        m = re.search(r'(<template>\s*)([\s\S]*?)(\s*</template>)', c)
        if not m: continue
        # Only strip if it's the outermost (after the @{ } block)
        prefix = c[:m.start()].rstrip()
        # The prefix should end with } (from @{ }) or be empty
        if not prefix.endswith('}'):
            continue
        new_c = c[:m.start()] + '\n    ' + m.group(2).strip() + '\n' + c[m.end():]
        with open(p, 'w', encoding='utf-8') as fh:
            fh.write(new_c)
        count += 1
        print(f"Fixed: {os.path.relpath(p, base)}")
print(f"\nTotal: {count}")
