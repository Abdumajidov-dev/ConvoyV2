#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.1"

using Npgsql;
using System;

// Connection string - update with your credentials
var connectionString = "Host=localhost;Port=5432;Database=convoy_db;Username=postgres;Password=YrSIsEidlvQRLXLjpMkdHmDnsWsiqHkH;Include Error Detail=true";

Console.WriteLine("🔧 Updating users table schema...\n");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    Console.WriteLine("✅ Connected to database: convoy_db\n");

    // Add branch_name column
    Console.WriteLine("➡️  Adding branch_name column...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_name VARCHAR(200)", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ branch_name column added\n");
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

    // Create index
    Console.WriteLine("➡️  Creating index on role column...");
    using (var cmd = new NpgsqlCommand("CREATE INDEX IF NOT EXISTS idx_users_role ON users(role) WHERE role IS NOT NULL", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ Index created\n");
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
