using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace GCTL.UI.Core.ViewModels
{

    public enum FieldType
    {
        Text,
        Number,
        Date,
        Textarea,
        Select,
        MultiSelect,
        Checkbox,
        Radio,
        DualInput,      
        QuickAddSelect,
        Blank,
        ReadonlyDisplay
    }

    public class DynamicFieldColumnModel
    {
        public bool UseGridLayout { get; set; } = false;
        // ================= Layout =================
        public string ColumnClass { get; set; } = "col-lg-4 col-md-6";
        public int? Order { get; set; }                     // display order (mobile-first reordering)
        public string ContainerId { get; set; }              // wrapper div id, for JS targeting
        public string CustomCssClass { get; set; }           // extra class on wrapper

        // ================= Core Field Info =================
        public FieldType FieldType { get; set; } = FieldType.Text;
        public string InputId { get; set; }                  // REQUIRED - unique input id
        public string InputName { get; set; }                // name attribute (defaults to InputId if null)
        public string LabelText { get; set; } = "";
        public string Placeholder { get; set; } = "";
        public string DefaultValue { get; set; }
        public string HelpText { get; set; }                 // small text under label
        public string Tooltip { get; set; }                  // title attribute / hover text
        public string IconClass { get; set; }                 // e.g. "fas fa-calendar"

        // ================= State Flags =================
        public bool IsVisible { get; set; } = true;
        public bool ShowOnInit { get; set; } = true;
        public bool IsReadonly { get; set; } = false;
        public bool IsDisabled { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public bool AutoFocus { get; set; } = false;
        public string VisibilityGroup { get; set; }           // e.g. "seaFieldsGroup", "airFieldsGroup"

        // ================= Validation =================
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? Step { get; set; }
        public string ValidationPattern { get; set; }         // regex pattern
        public string ErrorMessage { get; set; }
        public string ValidationMessageId { get; set; }

        // ================= Textarea =================
        public int Rows { get; set; } = 1;

        // ================= Select / MultiSelect =================
        public SelectList SelectItems { get; set; }
        public int? MultiSelectMaxItems { get; set; }
        public string AjaxSourceUrl { get; set; }              // dynamic option loading
        public string DependsOnFieldId { get; set; }           // cascading parent field id

        // ================= Checkbox / Radio =================
        public bool CheckedState { get; set; } = false;
        public string GroupName { get; set; }                  // radio group "name" attribute
        public List<SelectListItem> RadioOptions { get; set; }
        public List<SelectListItem> CheckboxOptions { get; set; }

        // ================= Quick Add (Plus Button) =================
        public bool ShowQuickAdd { get; set; } = false;
        public string QuickAddUrl { get; set; }
        public string QuickAddTarget { get; set; }
        public string QuickAddReloadUrl { get; set; }
        public string QuickAddTitle { get; set; }
        public bool ShowCopyIcon { get; set; } = false;         // copy-to-clipboard icon (Select2 style)

        // ================= Prepend / Append =================
        public string PrependText { get; set; }                // input-group-text before input ($ , % )
        public string AppendText { get; set; }

        // ================= Dual Input (e.g. Quantity+Unit, Value+Currency) =================
        public DynamicFieldColumnModel SecondField { get; set; }

        // ================= Events / Binding =================
        public string OnChangeHandler { get; set; }             // JS function name to bind
        public string JsEventClass { get; set; }                // marker class for JS delegation
        public string DataBindField { get; set; }               // maps to server-side property name

        // ================= Extensibility =================
        public Dictionary<string, string> CustomAttributes { get; set; } = new Dictionary<string, string>();

        // ================= Computed Helpers =================
        public string ResolvedInputName => string.IsNullOrEmpty(InputName) ? InputId : InputName;
        public bool ReserveActionSlot { get; set; } = true; // default true = current behavior unchanged
        public bool LabelInline { get; set; } = false;
        public int LabelColWidth { get; set; } = 4;
        public List<SelectListItem> OptionItems { get; set; }
        public bool IsInline { get; set; } = true;
        public string InputType { get; set; }

        public bool IsMultiple { get; set; } = false;
        public string LabelColumnClass { get; set; } = "";
        public string LabelCssClass { get; set; } = "";    
        // DynamicFieldColumnModel.cs
        public bool IsActionDisabled { get; set; } = false;
        public string ValueCssClass { get; set; } = "readonly-value";

        public string CustomStyle { get; set; } = string.Empty; 
        public string LabelStyle { get; set; } = string.Empty;  
        public string InputStyle { get; set; } = string.Empty; 
        public string ResolvedInputCss
        {
            get
            {
                if (CustomAttributes != null && CustomAttributes.TryGetValue("table-view", out var tv) && tv == "true")
                {
                    return "form-select form-select-sm custom-table-dropdown";
                }

                if (FieldType == FieldType.MultiSelect || IsMultiple)
                    return "form-select form-select-sm custom-select-multi";

                if (FieldType == FieldType.Select || FieldType == FieldType.QuickAddSelect)
                    return "form-select form-select-sm custom-select-single";

                return "form-control form-control-sm";
            }
        }
        
        public string ResolvedDatepickerClass => FieldType == FieldType.Date ? "custom-datepicker caiCon" : "";
    }
}