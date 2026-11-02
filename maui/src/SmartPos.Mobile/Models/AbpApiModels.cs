using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class AbpResponse<T>
{
    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public AbpErrorInfo? Error { get; set; }

    [JsonPropertyName("unAuthorizedRequest")]
    public bool UnAuthorizedRequest { get; set; }
}

public class AbpErrorInfo
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

public class AbpPagedResult<T>
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();
}

public class AbpListResult<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();
}

public class EntityDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class EntityDtoLong
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
