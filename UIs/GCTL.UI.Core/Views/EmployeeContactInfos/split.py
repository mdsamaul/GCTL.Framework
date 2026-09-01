import sys

with open('d:\\Project\\GCTL.Framework\\UIs\\GCTL.UI.Core\\Views\\EmployeeContactInfos\\Setup.cshtml', 'r', encoding='utf-8') as f:
    lines = f.readlines()

header = '@model GCTL.Core.ViewModels.EmployeeContactInfos.EmployeeContactInfosSetupViewModel\n@using GCTL.UI.Core.ViewModels\n\n'
setup_content = header + ''.join(lines[93:726])

with open('d:\\Project\\GCTL.Framework\\UIs\\GCTL.UI.Core\\Views\\EmployeeContactInfos\\_Setup.cshtml', 'w', encoding='utf-8') as f:
    f.write(setup_content)

new_setup_lines = lines[:93]
new_setup_lines.append('                            <div class="js-EmployeeContactInfos-form-container">\n')
new_setup_lines.append('                                @await Html.PartialAsync("_Setup", Model)\n')
new_setup_lines.append('                            </div>\n')
new_setup_lines.extend(lines[726:])

with open('d:\\Project\\GCTL.Framework\\UIs\\GCTL.UI.Core\\Views\\EmployeeContactInfos\\Setup.cshtml', 'w', encoding='utf-8') as f:
    f.write(''.join(new_setup_lines))
