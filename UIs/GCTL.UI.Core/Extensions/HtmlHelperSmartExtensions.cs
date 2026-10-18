using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Extensions
{
    public static class HtmlHelperSmartExtensions
    {
        public static IHtmlContent SmartLocalize(this IHtmlHelper htmlHelper, string defaultText)
        {
            if (string.IsNullOrEmpty(defaultText))
                return HtmlString.Empty;

            return new HtmlString(defaultText);
        }
    }
}
