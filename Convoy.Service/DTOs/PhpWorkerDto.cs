namespace Convoy.Service.DTOs;

public class PhpWorkerDto
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string WorkerGuid { get; set; } = string.Empty;
    public string BranchGuid { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int PositionId { get; set; }
}
