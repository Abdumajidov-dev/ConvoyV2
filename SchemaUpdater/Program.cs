using Npgsql;

// Connection string - Docker production database
var connectionString = "Host=172.17.0.1;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass;Include Error Detail=true";

Console.WriteLine("🔧 Updating users table schema...\n");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    Console.WriteLine("✅ Connected to database: convoydb\n");

    // Add user_id column (PHP API worker_id)
    Console.WriteLine("➡️  Adding user_id column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS user_id INTEGER UNIQUE", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ user_id column added\n");
    }

    // Add branch_guid column
    Console.WriteLine("➡️  Adding branch_guid column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_guid VARCHAR(100)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ branch_guid column added\n");
    }

    // Add branch_name column
    Console.WriteLine("➡️  Adding branch_name column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_name VARCHAR(200)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ branch_name column added\n");
    }

    // Add worker_guid column
    Console.WriteLine("➡️  Adding worker_guid column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS worker_guid VARCHAR(100)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ worker_guid column added\n");
    }

    // Add position_id column
    Console.WriteLine("➡️  Adding position_id column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS position_id INTEGER", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ position_id column added\n");
    }

    // Add image column
    Console.WriteLine("➡️  Adding image column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS image VARCHAR(500)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ image column added\n");
    }

    // Add user_type column
    Console.WriteLine("➡️  Adding user_type column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS user_type VARCHAR(50)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ user_type column added\n");
    }

    // Add role column
    Console.WriteLine("➡️  Adding role column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS role VARCHAR(100)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ role column added\n");
    }

    // Create indexes
    Console.WriteLine("➡️  Creating indexes...");
    using (var cmd = new NpgsqlCommand(@"
        CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id) WHERE user_id IS NOT NULL;
        CREATE INDEX IF NOT EXISTS idx_users_role ON users(role) WHERE role IS NOT NULL;
        CREATE INDEX IF NOT EXISTS idx_users_phone ON users(phone) WHERE phone IS NOT NULL;
    ", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ Indexes created\n");
    }

    // Verify columns
    Console.WriteLine("➡️  Verifying users table structure...\n");
    using (var cmd = new NpgsqlCommand(@"
        SELECT column_name, data_type, character_maximum_length, is_nullable
        FROM information_schema.columns
        WHERE table_name = 'users'
        ORDER BY ordinal_position", connection))
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        Console.WriteLine("📋 Current users table structure:");
        Console.WriteLine("────────────────────────────────────────────────────────");
        Console.WriteLine($"{"Column Name",-25} {"Type",-20} {"Max Length",-12} {"Nullable",-10}");
        Console.WriteLine("────────────────────────────────────────────────────────");

        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var maxLength = reader.IsDBNull(2) ? "-" : reader.GetInt32(2).ToString();
            var isNullable = reader.GetString(3);

            Console.WriteLine($"{columnName,-25} {dataType,-20} {maxLength,-12} {isNullable,-10}");
        }
        Console.WriteLine("────────────────────────────────────────────────────────\n");
    }

    Console.WriteLine("✅ Schema update completed successfully!\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"   Stack: {ex.StackTrace}");
    Environment.Exit(1);
}
