//#region  Example Section


//<select class="form-select choiceDD" asp-for="LicenceTypeID" asp-items="@ViewBag.LicenseTypeDD as SelectList">
//    <option value="">@Html.SmartLocalize("Select License Type")</option>
//</select>


//choiceManager.clearChoice('LicenceTypeID');
//choiceManager.clearChoice('LicenceTypeID', 'LicenceTypeID');
//choiceManager.setChoiceValue('LicenceTypeID', '5');
//choiceManager.setChoiceValue('LicenceTypeID', '');
//choiceManager.getChoiceValue('EmployeePersonalId')


//choiceManager.resetChoice('dropdown1', 'dropdown2'); // Method 1: Complete reset (destroys and recreates)



// Example: Populate multiple dropdowns with auto-detected keys
const branchDataDummy = [
    { organizationBranchID: 1, organizationBranchName: "Googleplex" },
    { organizationBranchID: 2, organizationBranchName: "New York, USA" },
    { organizationBranchID: 3, organizationBranchName: "London, UK" },
    { organizationBranchID: 4, organizationBranchName: "Singapore" }
];

// Placeholder will be taken from HTML
//choiceManager.populateDropdown(['table1BranchDropdown', 'table2BranchDropdown'], branchDataDummy);

// Example: Populate department dropdown with auto-detected keys
const departmentDataDummy = [
    { departmentID: 101, departmentName: "Engineering" },
    { departmentID: 102, departmentName: "Marketing" },
    { departmentID: 103, departmentName: "Sales" }
];

//// Placeholder will be taken from HTML
//choiceManager.populateDropdown('table3DepartmentDropdown', departmentDataDummy);

//// Example: Override placeholder if needed
//choiceManager.populateDropdown('table4BranchDropdown', branchDataDummy, {
//    placeholder: 'Custom Select a Branch'
//});

//#endregion


const deb = false


//#region Unversal Choice Version 3

class UniversalChoices {
    constructor(className = 'choiceDD') {
        this.className = className;
        this.instances = {};
        this.defaultConfig = {
            removeItemButton: true,
            shouldSort: false,
            placeholderValue: 'Select one', // Default placeholder if none in HTML
            allowHTML: true
        };
        this.populator = new ChoicePopulator(this);
    }

    initAll() {
        document.querySelectorAll(`select.${this.className}`).forEach(select => {
            const id = select.id;
            if (!id) {
                if (deb) console.warn('Choices dropdown must have an ID:', select);
                return;
            }

            const config = { ...this.defaultConfig };
            try {
                this.instances[id] = new Choices(select, config);
                if (deb) console.debug(`Choices initialized for ID: ${id}`);
            } catch (error) {
                console.error(`Failed to initialize Choices for ID: ${id}`, error);
            }
        });
    }


    //choiceManager.clearChoice('dropdown1');
    //choiceManager.clearChoice('dropdown1', 'dropdown2');

    clearChoice(...ids) {
        ids.forEach(id => {
            const instance = this.instances[id];
            if (instance) {
                instance.removeActiveItems();
                instance.setChoiceByValue('');
                $(`#${id}`).val('').trigger('change');
                if (deb) console.debug(`Cleared choice for ID: ${id}`);
            } else {
                $(`#${id}`).val('').trigger('change');
                if (deb) console.warn(`No Choices instance found for ID: ${id} during clearChoice`);
            }
        });
    }


    //choiceManager.setChoiceValue('dropdown1', '5');
    //choiceManager.setChoiceValue('dropdown1', '');


    setChoiceValue(id, values) {
        // Ensure values is an array, even for single value
        const valueArray = Array.isArray(values) ? values : [values];
        const instance = this.instances[id];

        if (instance) {
            // Clear existing selections
            instance.removeActiveItems();

            // Set multiple values for multi-select
            valueArray.forEach(value => {
                const strValue = String(value);
                instance.setChoiceByValue(strValue);
                if (deb) console.debug(`Set value ${strValue} for ID: ${id}`);
            });

            // Update the underlying <select> and trigger change
            $(`#${id}`).val(valueArray).trigger('change');
            if (deb) console.debug(`Set values ${valueArray.join(', ')} for ID: ${id}`);
        } else {
            // Fallback to jQuery if no Choices instance
            $(`#${id}`).val(valueArray).trigger('change');
            if (deb) console.warn(`No Choices instance found for ID: ${id} during setChoiceValue`);
        }
    }


    //#region Reset Choice


    //choiceManager.resetChoice('dropdown1');
    //choiceManager.resetChoice('dropdown1', 'dropdown2');

    resetChoice(...ids) {
        ids.forEach(id => {
            const selectElement = document.getElementById(id);
            if (!selectElement) {
                if (deb) console.warn(`No select element found for ID: ${id}`);
                return;
            }

            const instance = this.instances[id];
            if (instance) {
                // Destroy the current instance
                instance.destroy();
                if (deb) console.debug(`Destroyed Choices instance for ID: ${id}`);
            }

            // Reset the select element to its original state
            selectElement.selectedIndex = 0;
            $(selectElement).val('').trigger('change');

            // Recreate the Choices instance
            try {
                const config = { ...this.defaultConfig };
                this.instances[id] = new Choices(selectElement, config);
                if (deb) console.debug(`Recreated Choices instance for ID: ${id}`);
            } catch (error) {
                console.error(`Failed to recreate Choices for ID: ${id}`, error);
            }
        });
    }

    // choiceManager.resetAllChoices()

    resetAllChoices() {
        // Find all select elements with class 'choiceDD'
        const selects = document.querySelectorAll("select.choiceDD");

        selects.forEach(selectElement => {
            const id = selectElement.id || "(no id)";

            // If there is a Choices instance for this select, destroy it
            const instance = this.instances[selectElement.id];
            if (instance) {
                instance.destroy();
                if (deb) console.debug(`Destroyed Choices instance for ID: ${id}`);
            }

            // Reset the select element to default (first option or empty)
            selectElement.selectedIndex = 0;
            $(selectElement).val('').trigger('change');

            // Recreate the Choices instance
            try {
                const config = { ...this.defaultConfig };
                this.instances[selectElement.id] = new Choices(selectElement, config);
                if (deb) console.debug(`Recreated Choices instance for ID: ${id}`);
            } catch (error) {
                console.error(`Failed to recreate Choices for ID: ${id}`, error);
            }
        });
    }


    //#endregion


    //choiceManager.getChoiceValue('dropdown1')

    getChoiceValue(id) {
        const instance = this.instances[id];
        if (instance) {
            const selected = instance.getValue(true);
            if (deb) console.debug(`Got value ${selected} for ID: ${id}`);
            return selected;
        } else {
            const value = $(`#${id}`).val();
            if (deb) console.warn(`No Choices instance found for ID: ${id} during getChoiceValue, using jQuery value: ${value}`);
            return value;
        }
    }

    // Get placeholder from HTML select element
    getPlaceholderFromHtml(id) {
        const select = document.getElementById(id);
        if (!select) {
            if (deb) console.warn(`No select element found for ID: ${id}`);
            return null;
        }

        // Check for data-placeholder attribute
        const dataPlaceholder = select.getAttribute('data-placeholder');
        if (dataPlaceholder) {
            if (deb) console.debug(`Found data-placeholder for ID: ${id}: ${dataPlaceholder}`);
            return dataPlaceholder;
        }

        // Check for an option with empty value
        const placeholderOption = select.querySelector('option[value=""]');
        if (placeholderOption) {
            const text = placeholderOption.textContent.trim();
            if (deb) console.debug(`Found placeholder option for ID: ${id}: ${text}`);
            return text;
        }

        if (deb) console.debug(`No placeholder found in HTML for ID: ${id}`);
        return null;
    }





    //const departmentDataDummy = [
    //    { departmentID: 101, departmentName: "Engineering" },
    //    { departmentID: 102, departmentName: "Marketing" },
    //    { departmentID: 103, departmentName: "Sales" }
    //];


    //choiceManager.populateDropdown('dropdown1', departmentDataDummy);

    populateDropdown(dropdownIds, data = [], config = {}) {
        const defaultConfig = {
            placeholder: this.defaultConfig.placeholderValue, // Use class default
            labelKey: null,
            valueKey: null
        };
        const mergedConfig = { ...defaultConfig, ...config };

        // Auto-detect keys if not provided
        let { labelKey, valueKey } = mergedConfig;
        if (data.length > 0 && (!labelKey || !valueKey)) {
            const keys = Object.keys(data[0]);
            if (keys.length >= 2) {
                valueKey = valueKey || keys[0];
                labelKey = labelKey || keys[1];
                if (deb) console.debug(`Auto-detected keys for data: valueKey=${valueKey}, labelKey=${labelKey}`);
            } else if (keys.length === 1) {
                valueKey = valueKey || keys[0];
                labelKey = labelKey || keys[0];
                if (deb) console.debug(`Single key detected for data: valueKey=${valueKey}, labelKey=${labelKey}`);
            } else {
                if (deb) console.warn('No keys found in data for auto-detection');
                return;
            }
        }

        // Handle single ID or array of IDs
        const ids = Array.isArray(dropdownIds) ? dropdownIds : [dropdownIds];

        ids.forEach(id => {
            const instance = this.instances[id];
            if (!instance) {
                if (deb) console.warn(`No Choices instance found for ID: ${id} during populateDropdown`);
                return;
            }

            // Get placeholder from HTML or fall back to config
            const placeholder = this.getPlaceholderFromHtml(id) || mergedConfig.placeholder;
            if (deb) console.debug(`Using placeholder for ID: ${id}: ${placeholder}`);

            // Clear existing options and selections
            instance.clearStore();
            instance.removeActiveItems();
            if (deb) console.debug(`Cleared store and active items for ID: ${id}`);

            // Format data
            let formattedData;
            try {
                formattedData = data.map(item => ({
                    value: String(item[valueKey]),
                    label: String(item[labelKey]).trim(),
                    selected: item.selected || false,
                    disabled: item.disabled || false
                }));
                if (deb) console.debug(`Formatted data for ID: ${id}`, formattedData);
            } catch (error) {
                console.error(`Error formatting data for ID: ${id}`, error);
                return;
            }

            // Populate dropdown
            try {
                this.populator.populateStatic(id, formattedData, {
                    labelKey: 'label',
                    valueKey: 'value',
                    placeholder
                });
                if (deb) console.debug(`Populated dropdown for ID: ${id} with ${formattedData.length} options`);
            } catch (error) {
                console.error(`Error populating dropdown for ID: ${id}`, error);
            }
        });
    }

    disableChoice(id) {
        const instance = this.instances[id];
        if (instance) {
            instance.disable();
            // Manually add 'is-disabled' class if you need extra customization
            const container = document.querySelector(`#${id}`);
            if (container) {
                container.classList.add('is-disabled');
            }
            if (deb) console.debug(`Disabled choice for ID: ${id}`);
        } else {
            if (deb) console.warn(`No Choices instance found for ID: ${id} during disableChoice`);
        }
    }

    enableChoice(id) {
        const instance = this.instances[id];
        if (instance) {
            instance.enable();
            // Remove 'is-disabled' class if it was added
            const container = document.querySelector(`#${id}`);
            if (container) {
                container.classList.remove('is-disabled');
            }
            if (deb) console.debug(`Enabled choice for ID: ${id}`);
        } else {
            if (deb) console.warn(`No Choices instance found for ID: ${id} during enableChoice`);
        }
    }



}

class ChoicePopulator {
    constructor(choiceManager) {
        this.choiceManager = choiceManager;
        this.defaultFetchOptions = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        };
    }

    populateStatic(id, options, config = {}) {
        const {
            labelKey = 'label',
            valueKey = 'value',
            placeholder = 'Select one' // Fallback placeholder
        } = config;

        const instance = this.choiceManager.instances[id];
        if (!instance) {
            if (deb) console.warn(`No Choices instance found for ID: ${id} in populateStatic`);
            return;
        }

        const formattedOptions = options.map(option => ({
            value: String(option[valueKey]),
            label: option[labelKey],
            selected: option.selected || false,
            disabled: option.disabled || false
        }));

        try {
            // Clear store explicitly
            instance.clearStore();

            // Always add placeholder as first option
            instance.setChoices([{
                value: '',
                label: placeholder,
                placeholder: true,
                selected: true // Ensure placeholder is selected by default
            }], 'value', 'label', false);
            if (deb) console.debug(`Set placeholder for ID: ${id}: ${placeholder}`);

            // Add options
            instance.setChoices(formattedOptions, 'value', 'label', true);
            if (deb) console.debug(`Set ${formattedOptions.length} options for ID: ${id}`);
        } catch (error) {
            console.error(`Error setting choices for ID: ${id}`, error);
        }
    }

    async populateFromApi(id, url, config = {}) {
        const {
            labelKey = 'label',
            valueKey = 'value',
            placeholder = 'Select one',
            fetchOptions = {}
        } = config;

        const instance = this.choiceManager.instances[id];
        if (!instance) {
            if (deb) console.warn(`No Choices instance found for ID: ${id} in populateFromApi`);
            return;
        }

        try {
            const response = await fetch(url, {
                ...this.defaultFetchOptions,
                ...fetchOptions
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const data = await response.json();

            const formattedOptions = data.map(item => ({
                value: String(item[valueKey]),
                label: item[labelKey],
                selected: item.selected || false,
                disabled: item.disabled || false
            }));

            instance.clearStore();

            // Always add placeholder
            instance.setChoices([{
                value: '',
                label: placeholder,
                placeholder: true,
                selected: true
            }], 'value', 'label', false);

            instance.setChoices(formattedOptions, 'value', 'label', true);
            if (deb) console.debug(`Populated ${formattedOptions.length} options from API for ID: ${id}`);
            return true;
        } catch (error) {
            console.error(`Error populating choices for ID ${id} from ${url}:`, error);
            return false;
        }
    }

    clearOptions(id) {
        const instance = this.choiceManager.instances[id];
        if (instance) {
            instance.clearStore();
            if (deb) console.debug(`Cleared options for ID: ${id}`);
        } else {
            if (deb) console.warn(`No Choices instance found for ID: ${id} in clearOptions`);
        }
    }

    addOption(id, config = {}) {
        const instance = this.choiceManager.instances[id];
        if (!instance) {
            if (deb) console.warn(`No Choices instance found for ID: ${id} in addOption`);
            return;
        }

        const {
            value,
            label,
            selected = false,
            disabled = false
        } = config;

        try {
            instance.setChoices([{
                value: String(value),
                label,
                selected,
                disabled
            }], 'value', true);
            if (deb) console.debug(`Added option ${label} for ID: ${id}`);
        } catch (error) {
            console.error(`Error adding option for ID: ${id}`, error);
        }
    }
}

// Initialize on page load
const choiceManager = new UniversalChoices();
window.addEventListener('DOMContentLoaded', () => {
    choiceManager.initAll();


});

// Optional global access
window.choiceManager = choiceManager;

//#endregion


//#region Universal Choice XA Version

//#region ChoiceXA - jQuery Style Dropdown Manager

class ChoiceXA {
    constructor(className = 'choiceXA') {
        this.className = className;
        this.instances = {};
        this.defaultConfig = {
            placeholder: 'Select one',
            allowCustomOptions: false,
            searchEnabled: false
        };
    }

    initAll() {
        document.querySelectorAll(`select.${this.className}`).forEach(select => {
            const id = select.id;
            if (!id) {
                if (typeof deb !== 'undefined' && deb) console.warn('ChoiceXA dropdown must have an ID:', select);
                return;
            }

            try {
                this.instances[id] = {
                    element: select,
                    $element: $(select),
                    config: { ...this.defaultConfig }
                };

                // Set up initial placeholder if it doesn't exist
                this.setupPlaceholder(id);

                if (typeof deb !== 'undefined' && deb) console.debug(`ChoiceXA initialized for ID: ${id}`);
            } catch (error) {
                console.error(`Failed to initialize ChoiceXA for ID: ${id}`, error);
            }
        });
    }

    setupPlaceholder(id) {
        const instance = this.instances[id];
        if (!instance) return;

        const $select = instance.$element;
        const placeholder = this.getPlaceholderFromHtml(id) || instance.config.placeholder;

        // Add placeholder option if it doesn't exist
        if ($select.find('option[value=""]').length === 0) {
            $select.prepend(`<option value="">${placeholder}</option>`);
        }

        // Set placeholder as selected if no other option is selected
        if (!$select.val()) {
            $select.val('');
        }
    }

    // Get placeholder from HTML select element
    getPlaceholderFromHtml(id) {
        const select = document.getElementById(id);
        if (!select) return null;

        // Check for data-placeholder attribute
        const dataPlaceholder = select.getAttribute('data-placeholder');
        if (dataPlaceholder) return dataPlaceholder;

        // Check for an option with empty value
        const placeholderOption = select.querySelector('option[value=""]');
        if (placeholderOption) {
            return placeholderOption.textContent.trim();
        }

        return null;
    }

    // jQuery-style methods

    // Get value: var selectedValue = choiceXA.val('myDropdown') or $('#myDropdown').val()
    val(id, value = undefined) {
        const instance = this.instances[id];
        if (!instance) {
            if (typeof deb !== 'undefined' && deb) console.warn(`No ChoiceXA instance found for ID: ${id}`);
            return value === undefined ? $('#' + id).val() : $('#' + id).val(value);
        }

        if (value === undefined) {
            // Get value
            const currentValue = instance.$element.val();
            if (typeof deb !== 'undefined' && deb) console.debug(`Got value ${currentValue} for ID: ${id}`);
            return currentValue;
        } else {
            // Set value
            instance.$element.val(value).trigger('change');
            if (typeof deb !== 'undefined' && deb) console.debug(`Set value ${value} for ID: ${id}`);
            return this;
        }
    }

    // Clear dropdown: choiceXA.clear('myDropdown')
    clear(...ids) {
        ids.forEach(id => {
            const instance = this.instances[id];
            if (instance) {
                instance.$element.val('').trigger('change');
                if (typeof deb !== 'undefined' && deb) console.debug(`Cleared choice for ID: ${id}`);
            } else {
                $('#' + id).val('').trigger('change');
                if (typeof deb !== 'undefined' && deb) console.warn(`No ChoiceXA instance found for ID: ${id} during clear`);
            }
        });
    }

    // Empty and populate: choiceXA.populate('myDropdown', options)
    populate(id, options = [], config = {}) {
        const instance = this.instances[id];
        if (!instance) {
            if (typeof deb !== 'undefined' && deb) console.warn(`No ChoiceXA instance found for ID: ${id}`);
            return;
        }

        const defaultConfig = {
            valueKey: 'value',
            textKey: 'text',
            labelKey: null, // Alternative to textKey
            preservePlaceholder: true
        };
        const mergedConfig = { ...defaultConfig, ...config };

        // Auto-detect keys if not provided and options exist
        if (options.length > 0) {
            const keys = Object.keys(options[0]);
            if (!mergedConfig.textKey && !mergedConfig.labelKey && keys.length >= 2) {
                mergedConfig.valueKey = mergedConfig.valueKey || keys[0];
                mergedConfig.textKey = keys.find(key => key.toLowerCase().includes('name') || key.toLowerCase().includes('text') || key.toLowerCase().includes('label')) || keys[1];
                if (typeof deb !== 'undefined' && deb) console.debug(`Auto-detected keys: valueKey=${mergedConfig.valueKey}, textKey=${mergedConfig.textKey}`);
            }
        }

        const textKey = mergedConfig.labelKey || mergedConfig.textKey;
        const $dropdown = instance.$element;

        // Store current placeholder
        const placeholder = this.getPlaceholderFromHtml(id) || instance.config.placeholder;

        // Clear existing options
        $dropdown.empty();

        // Add placeholder if preserving
        if (mergedConfig.preservePlaceholder) {
            $dropdown.append($('<option>', {
                value: '',
                text: placeholder
            }));
        }

        // Add options
        $.each(options, function (index, item) {
            const optionValue = typeof item === 'object' ? item[mergedConfig.valueKey] : item;
            const optionText = typeof item === 'object' ? item[textKey] : item;

            $dropdown.append($('<option>', {
                value: optionValue,
                text: optionText
            }));
        });

        if (typeof deb !== 'undefined' && deb) console.debug(`Populated ${options.length} options for ID: ${id}`);
    }

    // Add single option: choiceXA.addOption('myDropdown', {value: 'val', text: 'text'})
    addOption(id, option) {
        const instance = this.instances[id];
        if (!instance) {
            if (typeof deb !== 'undefined' && deb) console.warn(`No ChoiceXA instance found for ID: ${id}`);
            return;
        }

        const $dropdown = instance.$element;
        const value = typeof option === 'object' ? option.value : option;
        const text = typeof option === 'object' ? (option.text || option.label) : option;

        $dropdown.append($('<option>', {
            value: value,
            text: text
        }));

        if (typeof deb !== 'undefined' && deb) console.debug(`Added option ${text} to ID: ${id}`);
    }

    // Remove option by value: choiceXA.removeOption('myDropdown', 'value')
    removeOption(id, value) {
        const instance = this.instances[id];
        if (!instance) {
            if (typeof deb !== 'undefined' && deb) console.warn(`No ChoiceXA instance found for ID: ${id}`);
            return;
        }

        instance.$element.find(`option[value="${value}"]`).remove();
        if (typeof deb !== 'undefined' && deb) console.debug(`Removed option with value ${value} from ID: ${id}`);
    }

    // Enable/Disable dropdown
    disable(id) {
        const instance = this.instances[id];
        if (instance) {
            instance.$element.prop('disabled', true);
            if (typeof deb !== 'undefined' && deb) console.debug(`Disabled dropdown for ID: ${id}`);
        }
    }

    enable(id) {
        const instance = this.instances[id];
        if (instance) {
            instance.$element.prop('disabled', false);
            if (typeof deb !== 'undefined' && deb) console.debug(`Enabled dropdown for ID: ${id}`);
        }
    }

    // Get all options: choiceXA.getOptions('myDropdown')
    getOptions(id) {
        const instance = this.instances[id];
        if (!instance) return [];

        const options = [];
        instance.$element.find('option').each(function () {
            options.push({
                value: $(this).val(),
                text: $(this).text()
            });
        });

        return options;
    }

    // Check if option exists: choiceXA.hasOption('myDropdown', 'value')
    hasOption(id, value) {
        const instance = this.instances[id];
        if (!instance) return false;

        return instance.$element.find(`option[value="${value}"]`).length > 0;
    }

    // Get selected text (not just value): choiceXA.getSelectedText('myDropdown')
    getSelectedText(id) {
        const instance = this.instances[id];
        if (!instance) return '';

        return instance.$element.find('option:selected').text();
    }

    // Select by text: choiceXA.selectByText('myDropdown', 'Display Text')
    selectByText(id, text) {
        const instance = this.instances[id];
        if (!instance) return;

        const $option = instance.$element.find('option').filter(function () {
            return $(this).text() === text;
        });

        if ($option.length > 0) {
            instance.$element.val($option.val()).trigger('change');
            if (typeof deb !== 'undefined' && deb) console.debug(`Selected option with text "${text}" for ID: ${id}`);
        }
    }

    // Reset to placeholder: choiceXA.reset('myDropdown')
    reset(...ids) {
        ids.forEach(id => {
            this.val(id, '');
            if (typeof deb !== 'undefined' && deb) console.debug(`Reset dropdown to placeholder for ID: ${id}`);
        });
    }
}

// Create global instance
const choiceXA = new ChoiceXA();

// Initialize on page load
window.addEventListener('DOMContentLoaded', () => {
    choiceXA.initAll();
});

// Optional global access
window.choiceXA = choiceXA;

//#endregion

/*
Usage Examples:

HTML:
<select id="myDropdown" class="choiceXA" data-placeholder="Choose an option">
    <option value="">Choose an option</option>
</select>

JavaScript:

// Get value (jQuery style still works)
var selectedValue = $('#myDropdown').val();
// OR using choiceXA
var selectedValue = choiceXA.val('myDropdown');

// Set value (jQuery style still works)
$('#myDropdown').val('desiredValue').trigger('change');
// OR using choiceXA
choiceXA.val('myDropdown', 'desiredValue');

// Populate dropdown (enhanced method)
const options = [
    {value: 'apple', text: 'Apple'},
    {value: 'banana', text: 'Banana'},
    {value: 'orange', text: 'Orange'}
];
choiceXA.populate('myDropdown', options);

// jQuery style populate (still works)
var $dropdown = $('#myDropdown');
$dropdown.empty();
$.each(options, function(index, item) {
    $dropdown.append($('<option>', {
        value: item.value,
        text: item.text
    }));
});
$dropdown.val('banana').trigger('change');

// Clear dropdown
choiceXA.clear('myDropdown');

// Add single option
choiceXA.addOption('myDropdown', {value: 'grape', text: 'Grape'});

// Get selected text
var selectedText = choiceXA.getSelectedText('myDropdown');

// Select by text
choiceXA.selectByText('myDropdown', 'Apple');

// Enable/Disable
choiceXA.disable('myDropdown');
choiceXA.enable('myDropdown');

// Check if option exists
var exists = choiceXA.hasOption('myDropdown', 'apple');

// Get all options
var allOptions = choiceXA.getOptions('myDropdown');

// Reset to placeholder
choiceXA.reset('myDropdown');

*/

//#endregion

//#region Unversal Choice Version 2

//class UniversalChoices {
//    constructor(className = 'choiceDD') {
//        this.className = className;
//        this.instances = {};
//        this.defaultConfig = {
//            removeItemButton: true,
//            shouldSort: false,
//            placeholderValue: 'Select an option',
//            allowHTML: true // ✅ Suppress deprecation warning
//        };
//    }

//    initAll() {
//        document.querySelectorAll(`select.${this.className}`).forEach(select => {
//            const id = select.id;
//            if (!id) {
//                if (deb) console.warn('Choices dropdown must have an ID:', select);
//                return;
//            }

//            const config = { ...this.defaultConfig };
//            this.instances[id] = new Choices(select, config); // ✅ Pass actual element
//        });
//    }

//    clearChoice(...ids) {
//        ids.forEach(id => {
//            const instance = this.instances[id];
//            if (instance) {
//                instance.removeActiveItems();
//                instance.setChoiceByValue('');
//                $(`#${id}`).val('').trigger('change');
//            } else {
//                $(`#${id}`).val('').trigger('change');
//            }
//        });
//    }

//    setChoiceValue(id, value) {
//        value = String(value); // Ensure value is a string
//        const instance = this.instances[id];

//        if (instance) {
//            instance.setChoiceByValue(value);
//            $(`#${id}`).val(value).trigger('change');
//        } else {
//            $(`#${id}`).val(value).trigger('change');
//        }
//    }

//    getChoiceValue(id) {
//        const instance = this.instances[id];
//        if (instance) {
//            const selected = instance.getValue(true); // `true` returns raw value(s)
//            return selected;
//        } else {

//            return $(`#${id}`).val(); // or document.getElementById(id).value
//        }
//    }



//}

//// ✅ Initialize on page load
//const choiceManager = new UniversalChoices();
//window.addEventListener('DOMContentLoaded', () => {
//    choiceManager.initAll();
//});

//// ✅ Optional global access
//window.choiceManager = choiceManager;

//#endregion

//#region Universal Choices Dropdown Manager

//class UniversalChoices {
//    constructor(className = 'choiceDD') {

//        this.className = className;
//        this.instances = {};
//        this.defaultConfig = {
//            removeItemButton: true,
//            shouldSort: false,
//            placeholderValue: 'Select an option'
//        };
//    }

//    initAll() {
//        document.querySelectorAll(`select.${this.className}`).forEach(select => {
//            const id = select.id;
//            if (!id) return if (deb) console.warn('Choices dropdown must have an ID:', select);

//            const config = { ...this.defaultConfig };
//            this.instances[id] = new Choices(`#${id}`, config);
//        });
//    }

//    //clearChoice(id) {
//    //    const instance = this.instances[id];
//    //    if (instance) {
//    //        instance.removeActiveItems();
//    //        instance.setChoiceByValue('');
//    //        $(`#${id}`).val('').trigger('change');
//    //    } else {
//    //        $(`#${id}`).val('').trigger('change');
//    //    }
//    //}

//    clearChoice(...ids) {
//        ids.forEach(id => {
//            const instance = this.instances[id];
//            if (instance) {
//                instance.removeActiveItems();
//                instance.setChoiceByValue('');
//                $(`#${id}`).val('').trigger('change');
//            } else {
//                $(`#${id}`).val('').trigger('change');
//            }
//        });
//    }





//    setChoiceValue(id, value) {
//        value = String(value); // Ensure value is converted to string
//        const instance = this.instances[id];

//        if (instance) {
//            instance.setChoiceByValue(value);
//            $(`#${id}`).val(value).trigger('change');
//        } else {
//            $(`#${id}`).val(value).trigger('change');
//        }
//    }


//}


//const choiceManager = new UniversalChoices();
//window.addEventListener('DOMContentLoaded', () => {
//    choiceManager.initAll();
//});


//window.choiceManager = choiceManager;

//#endregion

