using Newtonsoft.Json;

namespace Convoy.Service.DTOs;

public class NotifationCreateDto
{
    [JsonProperty("user_id")]
    public int UserId { get; set; }
    [JsonProperty("title")]
    public string Title { get; set; }
    [JsonProperty("body")]
    public string Body { get; set; }
}
