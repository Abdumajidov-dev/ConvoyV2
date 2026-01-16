using Microsoft.AspNetCore.Authorization;

namespace Convoy.Api.Authorization;

/// <summary>
/// Permission-based authorization attribute
/// Controller yoki action'larda permission tekshirish uchun ishlatiladi
///
/// Usage:
/// [HasPermission(Permissions.Users.View)]
/// public async Task<IActionResult> GetUsers() { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(permission)
    {
    }
}
