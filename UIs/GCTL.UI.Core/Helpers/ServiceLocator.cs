using System;

namespace GCTL.UI.Core.Helpers
{
    /// <summary>
    /// Minimal service locator used only to access IHttpContextAccessor from
    /// extension methods where DI is not available. Use sparingly.
    /// </summary>
    public static class ServiceLocator
    {
        public static IServiceProvider? ServiceProvider { get; set; }
    }
}
