using Npgsql;

public class ApplyMigration
{
    public static async Task Run()
    {
        var connString = "Host=10.21.61.51;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass";

        try
        {
            using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            Console.WriteLine("✓ Connected to global database\n");

            // Read SQL script
            var sqlScript = await File.ReadAllTextAsync("../add-user-columns-to-global.sql");

            // Execute migration
            using var cmd = new NpgsqlCommand(sqlScript, conn);
            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine("\n✓ Migration executed successfully\n");

            // Verify new structure
            var verifySql = @"
                SELECT
                    column_name,
                    data_type,
                    character_maximum_length,
                    is_nullable
                FROM information_schema.columns
                WHERE table_name = 'users'
                ORDER BY ordinal_position;
            ";

            using var verifyCmd = new NpgsqlCommand(verifySql, conn);
            using var reader = await verifyCmd.ExecuteReaderAsync();

            Console.WriteLine("UPDATED USERS TABLE STRUCTURE:");
            Console.WriteLine("==============================");
            Console.WriteLine("{0,-30} {1,-20} {2,-10} {3}", "Column Name", "Data Type", "Length", "Nullable");
            Console.WriteLine(new string('-', 80));

            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLength = reader.IsDBNull(2) ? "" : reader.GetInt32(2).ToString();
                var nullable = reader.GetString(3);

                Console.WriteLine("{0,-30} {1,-20} {2,-10} {3}",
                    columnName, dataType, maxLength, nullable);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Migration failed: {ex.Message}");
            Console.WriteLine($"Details: {ex}");
        }
    }
}
