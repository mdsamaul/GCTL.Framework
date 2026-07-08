namespace GCTL.Core.Common;

/// <summary>
/// Common audit fields. Inherit this in your EF Power Tools scaffolded
/// entities if you want consistent CreatedBy/CreatedDate style tracking,
/// or ignore it entirely and let the scaffolder generate POCOs as-is.
/// </summary>
public abstract class BaseEntity
{
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Standard API/service response wrapper used across GCTL.Service methods.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ServiceResult<T> Fail(string message) =>
        new() { Success = false, Message = message, Data = default };
}

/// <summary>
/// Mirrors your existing AccessCode pattern (0001 Admin, 0002/0003 Team Leader, 0005 User).
/// Centralize the constants here instead of magic strings scattered across controllers.
/// </summary>
public static class AccessCodes
{
    public const string Admin = "0001";
    public const string TeamLeaderA = "0002";
    public const string TeamLeaderB = "0003";
    public const string User = "0005";
}
