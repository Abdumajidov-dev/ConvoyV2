#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.1"

using Npgsql;
using System;
using System.IO;

var connectionString = "Host=10.21.61.51;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass;Include Error Detail=true";

Console.WriteLine("=== Fixing Foreign Key Constraint ===");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    connection.Open();

    // Step 1: Drop existing FK constraint
    Console.WriteLine("Dropping old FK constraint...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;", connection))
    {
        cmd.ExecuteNonQuery();
        Console.WriteLine("✅ Old FK constraint dropped");
    }

    // Step 2: Create new FK constraint
    Console.WriteLine("Creating new FK constraint (referencing users.user_id)...");
    using (var cmd = new NpgsqlCommand("ALTER TABLE locations ADD CONSTRAINT locations_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;", connection))
    {
        cmd.ExecuteNonQuery();
        Console.WriteLine("✅ New FK constraint created");
    }

    Console.WriteLine("\n=== Creating Missing Tables ===");

    // Step 3: Run notification system SQL script
    var sqlFile = Path.Combine(Directory.GetCurrentDirectory(), "add-notification-system.sql");
    if (File.Exists(sqlFile))
    {
        Console.WriteLine($"Running {sqlFile}...");
        var sql = File.ReadAllText(sqlFile);
        using (var cmd = new NpgsqlCommand(sql, connection))
        {
            cmd.ExecuteNonQuery();
            Console.WriteLine("✅ Tables created successfully");
        }
    }
    else
    {
        Console.WriteLine($"⚠️ Warning: {sqlFile} not found");
    }

    Console.WriteLine("\n=== Verification ===");

    // Verify FK
    Console.WriteLine("Verifying FK constraint...");
    using (var cmd = new NpgsqlCommand(@"
        SELECT conname, confrelid::regclass AS referenced_table, a.attname AS column_name, af.attname AS foreign_column
        FROM pg_constraint c
        JOIN pg_attribute a ON a.attnum = ANY(c.conkey) AND a.attrelid = c.conrelid
        JOIN pg_attribute af ON af.attnum = ANY(c.confkey) AND af.attrelid = c.confrelid
        WHERE c.conrelid = 'locations'::regclass AND c.contype = 'f';", connection))
    {
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"  FK: {reader["conname"]} -> {reader["referenced_table"]}.{reader["foreign_column"]}");
        }
    }

    // Verify tables
    Console.WriteLine("\nVerifying tables exist...");
    using (var cmd = new NpgsqlCommand(@"
        SELECT tablename
        FROM pg_tables
        WHERE tablename IN ('user_status_reports', 'admin_notifications', 'device_tokens')
        ORDER BY tablename;", connection))
    {
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"  ✅ Table: {reader["tablename"]}");
        }
    }

    Console.WriteLine("\n=== DONE ===");
    Console.WriteLine("Database fixes applied successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}

return 0;
