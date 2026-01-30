#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.0"

using Npgsql;
using System;

var connString = "Host=10.21.61.51;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass";

try
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    Console.WriteLine("✓ Successfully connected to global database\n");

    // Get users table structure
    var sql = @"
        SELECT
            column_name,
            data_type,
            character_maximum_length,
            is_nullable,
            column_default
        FROM information_schema.columns
        WHERE table_name = 'users'
        ORDER BY ordinal_position;
    ";

    using var cmd = new NpgsqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    Console.WriteLine("USERS TABLE STRUCTURE:");
    Console.WriteLine("====================");
    Console.WriteLine("{0,-30} {1,-20} {2,-10} {3,-10} {4}", "Column Name", "Data Type", "Length", "Nullable", "Default");
    Console.WriteLine(new string('-', 100));

    while (await reader.ReadAsync())
    {
        var columnName = reader.GetString(0);
        var dataType = reader.GetString(1);
        var maxLength = reader.IsDBNull(2) ? "" : reader.GetInt32(2).ToString();
        var nullable = reader.GetString(3);
        var defaultValue = reader.IsDBNull(4) ? "" : reader.GetString(4);

        Console.WriteLine("{0,-30} {1,-20} {2,-10} {3,-10} {4}",
            columnName, dataType, maxLength, nullable, defaultValue);
    }

    Console.WriteLine("\n✓ Inspection complete");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.WriteLine($"Details: {ex}");
}
