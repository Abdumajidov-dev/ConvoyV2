using Microsoft.AspNetCore.Authorization;

namespace Convoy.Api.Authorization;

/// <summary>
/// Permission-based authorization requirement
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
