using Newtonsoft.Json;

namespace Convoy.Service.DTOs;

public class DeviceInfoDto
{
    [JsonProperty("device_system")]
    public string DeviceSystem { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("device_id")]
    public string DeviceId { get; set; }
    [JsonProperty("device_token")]
    public string DeviceToken { get; set; }
    [JsonProperty("is_physical_device")]
    public bool IsPhysicalDevice { get; set; }
}