using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Common
{
    public class TabItemModel
    {
        public string TabId { get; set; }          // unique id, e.g. "vendorInfo"
        public string TabTitle { get; set; }        // Display name, e.g. "Vendor Info"
        public string ControllerName { get; set; }   // e.g. "VendorInfo"
        public string ActionName { get; set; } = "Index"; // default Index
        public string IconClass { get; set; }         // optional, e.g. "fa fa-truck"
        public bool IsActive { get; set; } = false;
        public object RouteValues { get; set; }        // optional extra params
        public string BtnClass { get; set; }           // optional extra class(es) for the button
        public string BtnId { get; set; }              // optional custom id for the button
    }

    public class TabGroupModel
    {
        public string GroupId { get; set; } = "globalTabGroup"; // container id
        public List<TabItemModel> Tabs { get; set; } = new List<TabItemModel>();
    }
}