using Npgsql;

var connString = "Host=10.21.61.51;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass";

try
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    Console.WriteLine("✓ Connected to global database\n");
    Console.WriteLine("Applying migration...\n");

    // SQL migration script
    var sqlScript = @"
-- Add user_id column (PHP API worker_id)
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='user_id') THEN
        ALTER TABLE users ADD COLUMN user_id INTEGER;
    END IF;
END $$;

-- Add branch_guid column
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='branch_guid') THEN
        ALTER TABLE users ADD COLUMN branch_guid VARCHAR(100);
    END IF;
END $$;

-- Add branch_name column
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='branch_name') THEN
        ALTER TABLE users ADD COLUMN branch_name VARCHAR(200);
    END IF;
END $$;

-- Add image column (user avatar URL)
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='image') THEN
        ALTER TABLE users ADD COLUMN image TEXT;
    END IF;
END $$;

-- Add user_type column
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='user_type') THEN
        ALTER TABLE users ADD COLUMN user_type VARCHAR(50);
    END IF;
END $$;

-- Add role column
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='role') THEN
        ALTER TABLE users ADD COLUMN role VARCHAR(100);
    END IF;
END $$;

-- Add worker_guid column
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='worker_guid') THEN
        ALTER TABLE users ADD COLUMN worker_guid VARCHAR(100);
    END IF;
END $$;

-- Add position_id column
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='position_id') THEN
        ALTER TABLE users ADD COLUMN position_id INTEGER;
    END IF;
END $$;

-- Create indexes for frequently queried columns
CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id);
CREATE INDEX IF NOT EXISTS idx_users_worker_guid ON users(worker_guid);
CREATE INDEX IF NOT EXISTS idx_users_branch_guid ON users(branch_guid);
CREATE INDEX IF NOT EXISTS idx_users_position_id ON users(position_id);
";

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
