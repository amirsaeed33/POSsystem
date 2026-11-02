using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class AuthenticateModel
{
    [JsonPropertyName("userNameOrEmailAddress")]
    public string UserNameOrEmailAddress { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("rememberClient")]
    public bool RememberClient { get; set; } = true;

    [JsonPropertyName("tenancyName")]
    public string? TenancyName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}

public class AuthenticateResultModel
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("encryptedAccessToken")]
    public string? EncryptedAccessToken { get; set; }

    [JsonPropertyName("expireInSeconds")]
    public int ExpireInSeconds { get; set; }

    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("tenantId")]
    public int? TenantId { get; set; }

    [JsonPropertyName("tenancyName")]
    public string? TenancyName { get; set; }

    [JsonPropertyName("attendanceAutoCheckedIn")]
    public bool AttendanceAutoCheckedIn { get; set; }
}

public class IsTenantAvailableInput
{
    [JsonPropertyName("tenancyName")]
    public string TenancyName { get; set; } = string.Empty;
}

public class IsTenantAvailableOutput
{
    [JsonPropertyName("state")]
    public int State { get; set; }

    [JsonPropertyName("tenantId")]
    public int? TenantId { get; set; }

    public bool IsAvailable => State == 1;
}

public class GetCurrentLoginInformationsOutput
{
    [JsonPropertyName("user")]
    public UserLoginInfoDto? User { get; set; }

    [JsonPropertyName("tenant")]
    public TenantLoginInfoDto? Tenant { get; set; }
}

public class UserLoginInfoDto : EntityDtoLong
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("userImageUrl")]
    public string? UserImageUrl { get; set; }

    [JsonPropertyName("branchId")]
    public int? BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("branchBusinessType")]
    public int? BranchBusinessType { get; set; }

    [JsonPropertyName("isDeliveryRider")]
    public bool IsDeliveryRider { get; set; }

    [JsonPropertyName("riderStaffId")]
    public int? RiderStaffId { get; set; }

    [JsonPropertyName("designation")]
    public string? Designation { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("isWaiter")]
    public bool IsWaiter { get; set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace($"{Name} {Surname}".Trim())
            ? (UserName ?? "User")
            : $"{Name} {Surname}".Trim();
}

public class TenantLoginInfoDto : EntityDto
{
    [JsonPropertyName("tenancyName")]
    public string? TenancyName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public enum AppRoleMode
{
    GuestCustomer,
    Customer,
    Waiter,
    Rider,
    Admin,
    UnsupportedStaff
}
