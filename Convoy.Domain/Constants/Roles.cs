namespace Convoy.Domain.Constants;

/// <summary>
/// Sistema rollari ro'yxati (Best Practice: centralized role names)
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Driver = "Driver";
    public const string Viewer = "Viewer";

    /// <summary>
    /// Barcha rollar ro'yxatini olish (seeding uchun)
    /// </summary>
    public static List<(string Name, string DisplayName, string Description, List<string> Permissions)> GetAll()
    {
        return new List<(string, string, string, List<string>)>
        {
            (
                SuperAdmin,
                "Super Administrator",
                "Sistema super administratori - barcha ruxsatlarga ega",
                new List<string>
                {
                    // ALL PERMISSIONS
                    Permissions.Users.View,
                    Permissions.Users.Create,
                    Permissions.Users.Update,
                    Permissions.Users.Delete,
                    Permissions.Users.Manage,
                    Permissions.Locations.View,
                    Permissions.Locations.Create,
                    Permissions.Locations.Update,
                    Permissions.Locations.Delete,
                    Permissions.Locations.ViewAll,
                    Permissions.Locations.Export,
                    Permissions.Reports.View,
                    Permissions.Reports.Export,
                    Permissions.Reports.Create,
                    Permissions.Roles.View,
                    Permissions.Roles.Create,
                    Permissions.Roles.Update,
                    Permissions.Roles.Delete,
                    Permissions.Roles.AssignPermissions,
                    Permissions.PermissionsManagement.View,
                    Permissions.PermissionsManagement.Assign,
                    Permissions.Dashboard.ViewOwn,
                    Permissions.Dashboard.ViewAll,
                    Permissions.Dashboard.ViewStatistics,
                    Permissions.Settings.View,
                    Permissions.Settings.Update
                }
            ),
            (
                Admin,
                "Administrator",
                "Sistema administratori - ko'pgina ruxsatlarga ega",
                new List<string>
                {
                    Permissions.Users.View,
                    Permissions.Users.Create,
                    Permissions.Users.Update,
                    Permissions.Locations.View,
                    Permissions.Locations.ViewAll,
                    Permissions.Locations.Export,
                    Permissions.Reports.View,
                    Permissions.Reports.Export,
                    Permissions.Reports.Create,
                    Permissions.Dashboard.ViewAll,
                    Permissions.Dashboard.ViewStatistics,
                    Permissions.Settings.View
                }
            ),
            (
                Manager,
                "Manager",
                "Menejer - foydalanuvchilar va lokatsiyalarni boshqaradi",
                new List<string>
                {
                    Permissions.Users.View,
                    Permissions.Locations.View,
                    Permissions.Locations.ViewAll,
                    Permissions.Locations.Export,
                    Permissions.Reports.View,
                    Permissions.Reports.Export,
                    Permissions.Dashboard.ViewAll,
                    Permissions.Dashboard.ViewStatistics
                }
            ),
            (
                Driver,
                "Driver",
                "Haydovchi - faqat o'z ma'lumotlarini ko'radi va yaratadi",
                new List<string>
                {
                    Permissions.Locations.View,
                    Permissions.Locations.Create,
                    Permissions.Dashboard.ViewOwn
                }
            ),
            (
                Viewer,
                "Viewer",
                "Ko'ruvchi - faqat ma'lumotlarni ko'rish huquqi",
                new List<string>
                {
                    Permissions.Users.View,
                    Permissions.Locations.View,
                    Permissions.Reports.View,
                    Permissions.Dashboard.ViewOwn
                }
            )
        };
    }
}
