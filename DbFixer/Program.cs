using Npgsql;

var connectionString = "Host=10.21.61.51;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass;Include Error Detail=true";

Console.WriteLine("=== Fixing Foreign Key Constraint ===\n");

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Step 1: Drop existing FK constraint
    // Check if user_id has UNIQUE constraint
    Console.WriteLine("0. Checking users.user_id constraints...");
    await using (var checkCmd = new NpgsqlCommand(@"
        SELECT conname, contype
        FROM pg_constraint
        WHERE conrelid = 'users'::regclass
          AND conkey @> ARRAY[(SELECT attnum FROM pg_attribute WHERE attrelid = 'users'::regclass AND attname = 'user_id')]
        ORDER BY conname;", connection))
    {
        await using var reader = await checkCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"   Constraint: {reader["conname"]} (type: {reader["contype"]})");
        }
    }
    Console.WriteLine();

    Console.WriteLine("1. Dropping old FK constraint...");
    await using (var cmd = new NpgsqlCommand("ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("   ✅ Old FK constraint dropped\n");
    }

    // Step 2: Ensure user_id has UNIQUE constraint
    Console.WriteLine("2. Ensuring users.user_id has UNIQUE constraint...");
    await using (var uniqueCmd = new NpgsqlCommand(@"
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conrelid = 'users'::regclass
                  AND contype = 'u'
                  AND conkey @> ARRAY[(SELECT attnum FROM pg_attribute WHERE attrelid = 'users'::regclass AND attname = 'user_id')]
            ) THEN
                ALTER TABLE users ADD CONSTRAINT users_user_id_unique UNIQUE (user_id);
                RAISE NOTICE 'UNIQUE constraint added to users.user_id';
            ELSE
                RAISE NOTICE 'UNIQUE constraint already exists on users.user_id';
            END IF;
        END $$;", connection))
    {
        await uniqueCmd.ExecuteNonQueryAsync();
        Console.WriteLine("   ✅ UNIQUE constraint verified\n");
    }

    // Step 3: Create new FK constraint
    Console.WriteLine("3. Creating new FK constraint (referencing users.user_id)...");
    await using (var cmd = new NpgsqlCommand("ALTER TABLE locations ADD CONSTRAINT locations_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;", connection))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("   ✅ New FK constraint created\n");
    }

    Console.WriteLine("=== Creating Missing Tables ===\n");

    // Step 4: Run notification system SQL script
    var currentDir = Directory.GetCurrentDirectory();
    // If running from DbFixer/bin/Debug/netX.0, go up to solution root
    while (!File.Exists(Path.Combine(currentDir, "add-notification-system.sql")) && Directory.GetParent(currentDir) != null)
    {
        currentDir = Directory.GetParent(currentDir)!.FullName;
    }
    var sqlFile = Path.Combine(currentDir, "add-notification-system.sql");
    if (File.Exists(sqlFile))
    {
        Console.WriteLine($"4. Running {Path.GetFileName(sqlFile)}...");
        var sql = await File.ReadAllTextAsync(sqlFile);

        // Execute the entire SQL script as one transaction
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.CommandTimeout = 300; // 5 minutes timeout
        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine("   ✅ Tables created successfully\n");
    }
    else
    {
        Console.WriteLine($"   ⚠️ Warning: {sqlFile} not found\n");
    }

    Console.WriteLine("=== Verification ===\n");

    // Verify FK
    Console.WriteLine("5. Verifying FK constraint...");
    await using (var cmd = new NpgsqlCommand(@"
        SELECT conname, a.attname AS column_name, af.attname AS foreign_column
        FROM pg_constraint c
        JOIN pg_attribute a ON a.attnum = ANY(c.conkey) AND a.attrelid = c.conrelid
        JOIN pg_attribute af ON af.attnum = ANY(c.confkey) AND af.attrelid = c.confrelid
        WHERE c.conrelid = 'locations'::regclass AND c.contype = 'f';", connection))
    {
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"   FK: {reader["conname"]} ({reader["column_name"]}) -> users.{reader["foreign_column"]}");
        }
    }
    Console.WriteLine();

    // Verify tables
    Console.WriteLine("6. Verifying tables exist...");
    await using (var cmd = new NpgsqlCommand(@"
        SELECT tablename
        FROM pg_tables
        WHERE tablename IN ('user_status_reports', 'admin_notifications', 'device_tokens')
        ORDER BY tablename;", connection))
    {
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"   ✅ Table: {reader["tablename"]}");
        }
    }

    Console.WriteLine("\n=== DONE ===");
    Console.WriteLine("Database fixes applied successfully!");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}
