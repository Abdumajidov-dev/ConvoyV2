using Convoy.Domain.Commons;
using System.Text.Json.Serialization;

namespace Convoy.Domain.Entities;

public class UserStatusReport:Auditable
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    [JsonPropertyName("status")]
    public bool Status { get; set; }
}
