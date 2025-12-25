namespace Convoy.Domain.Constants;

/// <summary>
/// Sistema ruxsatlari ro'yxati (Best Practice: centralized permission names)
/// Naming convention: <Resource>.<Action>
/// </summary>
public static class Permissions
{
    // Users permissions
    public static class Users
    {
        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string Manage = "users.manage"; // Full user management
    }

    // Locations permissions
    public static class Locations
    {
        public const string View = "locations.view";
        public const string Create = "locations.create";
        public const string Update = "locations.update";
        public const string Delete = "locations.delete";
        public const string ViewAll = "locations.view_all"; // View all users' locations
        public const string Export = "locations.export";
    }

    // Reports permissions
    public static class Reports
    {
        public const string View = "reports.view";
        public const string Export = "reports.export";
        public const string Create = "reports.create";
    }

    // Roles permissions
    public static class Roles
    {
        public const string View = "roles.view";
        public const string Create = "roles.create";
        public const string Update = "roles.update";
        public const string Delete = "roles.delete";
        public const string AssignPermissions = "roles.assign_permissions";
    }

    // Permissions management
    public static class PermissionsManagement
    {
        public const string View = "permissions.view";
        public const string Assign = "permissions.assign"; // Assign permissions to roles
    }

    // Dashboard permissions
    public static class Dashboard
    {
        public const string ViewOwn = "dashboard.view_own"; // View only own data
        public const string ViewAll = "dashboard.view_all"; // View all users' data
        public const string ViewStatistics = "dashboard.view_statistics";
    }

    // Settings permissions
    public static class Settings
    {
        public const string View = "settings.view";
        public const string Update = "settings.update";
    }

    /// <summary>
    /// Barcha ruxsatlar ro'yxatini olish (seeding uchun)
    /// </summary>
    public static List<(string Name, string DisplayName, string Resource, string Action, string Description)> GetAll()
    {
        return new List<(string, string, string, string, string)>
        {
            // Users
            (Users.View, "View Users", "users", "view", "Foydalanuvchilar ro'yxatini ko'rish"),
            (Users.Create, "Create Users", "users", "create", "Yangi foydalanuvchi yaratish"),
            (Users.Update, "Update Users", "users", "update", "Foydalanuvchi ma'lumotlarini o'zgartirish"),
            (Users.Delete, "Delete Users", "users", "delete", "Foydalanuvchini o'chirish"),
            (Users.Manage, "Manage Users", "users", "manage", "Foydalanuvchilarni to'liq boshqarish"),

            // Locations
            (Locations.View, "View Locations", "locations", "view", "O'z lokatsiyalarini ko'rish"),
            (Locations.Create, "Create Locations", "locations", "create", "Yangi lokatsiya yaratish"),
            (Locations.Update, "Update Locations", "locations", "update", "Lokatsiya ma'lumotlarini o'zgartirish"),
            (Locations.Delete, "Delete Locations", "locations", "delete", "Lokatsiyani o'chirish"),
            (Locations.ViewAll, "View All Locations", "locations", "view_all", "Barcha foydalanuvchilarning lokatsiyalarini ko'rish"),
            (Locations.Export, "Export Locations", "locations", "export", "Lokatsiyalarni export qilish"),

            // Reports
            (Reports.View, "View Reports", "reports", "view", "Hisobotlarni ko'rish"),
            (Reports.Export, "Export Reports", "reports", "export", "Hisobotlarni export qilish"),
            (Reports.Create, "Create Reports", "reports", "create", "Yangi hisobot yaratish"),

            // Roles
            (Roles.View, "View Roles", "roles", "view", "Rollarni ko'rish"),
            (Roles.Create, "Create Roles", "roles", "create", "Yangi rol yaratish"),
            (Roles.Update, "Update Roles", "roles", "update", "Rol ma'lumotlarini o'zgartirish"),
            (Roles.Delete, "Delete Roles", "roles", "delete", "Rolni o'chirish"),
            (Roles.AssignPermissions, "Assign Permissions to Roles", "roles", "assign_permissions", "Rollarga ruxsat berish"),

            // Permissions
            (PermissionsManagement.View, "View Permissions", "permissions", "view", "Ruxsatlarni ko'rish"),
            (PermissionsManagement.Assign, "Assign Permissions", "permissions", "assign", "Ruxsatlarni tayinlash"),

            // Dashboard
            (Dashboard.ViewOwn, "View Own Dashboard", "dashboard", "view_own", "O'z dashboard'ini ko'rish"),
            (Dashboard.ViewAll, "View All Dashboard", "dashboard", "view_all", "Barcha dashboard'larni ko'rish"),
            (Dashboard.ViewStatistics, "View Statistics", "dashboard", "view_statistics", "Statistikalarni ko'rish"),

            // Settings
            (Settings.View, "View Settings", "settings", "view", "Sozlamalarni ko'rish"),
            (Settings.Update, "Update Settings", "settings", "update", "Sozlamalarni o'zgartirish"),
        };
    }
}
