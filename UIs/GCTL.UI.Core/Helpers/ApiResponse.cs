namespace GCTL.UI.Core.Helpers
{
    public static class ApiResponse
    {
        public static object Success(string message = "Operation completed successfully.", object? data = null)
            => new { success = true, message, data };

        public static object Error(string message = "Something went wrong.")
            => new { success = false, message };

        public static object PermissionDenied(string message = "You do not have permission to perform this action.")
            => new { success = false, message, isPermissionError = true };
    }
}