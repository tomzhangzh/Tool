import os, re

base = r"E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\Areas\Component\Views"
formitem_files = """Checkbox.cshtml CodeMirror.cshtml ColorPicker.cshtml ComCalc.cshtml ComCheckbox.cshtml ComCheckboxGroup.cshtml ComDate.cshtml ComDbSelect.cshtml ComHidden.cshtml ComInput.cshtml ComLabel.cshtml ComLookup.cshtml ComMarkdown.cshtml ComPassword.cshtml ComPrompt.cshtml ComRadio.cshtml ComRichText.cshtml ComSelect.cshtml ComSwitch.cshtml ComTextArea.cshtml ComUploadImage.cshtml DatePicker.cshtml DynCheckboxGroup.cshtml DynCodeMirror.cshtml DynRadioGroup.cshtml DynSelect.cshtml Input.cshtml InputNumber.cshtml Radio.cshtml Select.cshtml Slider.cshtml Switch.cshtml TimePicker.cshtml""".split()

count = 0
for root, dirs, files in os.walk(base):
    for f in files:
        if f not in formitem_files: continue
        p = os.path.join(root, f)
        with open(p, 'r', encoding='utf-8') as fh:
            c = fh.read()
        # Remove outer <template> ... </template> that wraps the body
        # Pattern: after @{...}, there's <template> BODY </template> then @section
        m = re.search(r'(<template>\s*)([\s\S]*?)(\s*</template>)', c)
        if not m: continue
        # Only strip if this is the outer wrapper (the body content)
        # Replace <template>BODY</template> with BODY
        new_c = c[:m.start()] + m.group(2).strip() + '\n\n' + c[m.end():]
        with open(p, 'w', encoding='utf-8') as fh:
            fh.write(new_c)
        count += 1
        print(f"Fixed: {f}")
print(f"\nTotal: {count}")
