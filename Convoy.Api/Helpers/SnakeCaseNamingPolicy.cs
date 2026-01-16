using System.Text;
using System.Text.Json;

namespace Convoy.Api.Helpers;

/// <summary>
/// Snake case naming policy for JSON serialization
/// Converts PascalCase to snake_case (e.g., UserId -> user_id)
/// </summary>
public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder();
        var previousUpper = false;

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                // Add underscore before uppercase letter (except first character)
                if (i > 0 && !previousUpper)
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLower(c));
                previousUpper = true;
            }
            else
            {
                builder.Append(c);
                previousUpper = false;
            }
        }

        return builder.ToString();
    }
}
