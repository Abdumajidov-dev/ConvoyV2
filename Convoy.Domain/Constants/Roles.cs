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
    /// IMPORTANT: Permission assignments REMOVED - admin panel orqali boshqariladi
    /// </summary>
    public static List<(string Name, string DisplayName, string Description)> GetAll()
    {
        return new List<(string, string, string)>
        {
            (
                SuperAdmin,
                "Super Administrator",
                "Sistema super administratori - role va permission'larni boshqaradi"
            ),
            (
                Admin,
                "Administrator",
                "Sistema administratori - asosiy boshqaruv funksiyalari"
            ),
            (
                Manager,
                "Manager",
                "Menejer - foydalanuvchilar va hisobotlarni boshqaradi"
            ),
            (
                Driver,
                "Driver",
                "Haydovchi - GPS tracking va o'z ma'lumotlarini yuboradi"
            ),
            (
                Viewer,
                "Viewer",
                "Ko'ruvchi - faqat ma'lumotlarni ko'rish huquqi"
            )
        };
    }
}
