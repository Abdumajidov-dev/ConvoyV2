namespace Convoy.Service.DTOs;
public class DeviceTokenResultDto
{
    public long Id { get; set; }
    public long SupportId { get; set; }
    public string Token { get; set; }
    public string DeviceSystem { get; set; }
    public string Model { get; set; }
    public string DeviceId { get; set; }
    public bool IsPhysicalDevice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
