"""
Migration script to add branch_guid column to users table
"""
import psycopg2
from psycopg2 import sql
import sys

# Set UTF-8 encoding for Windows console
if sys.platform == 'win32':
    import os
    os.system('chcp 65001 > nul')

# Database connection settings
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'convoy_db',
    'user': 'postgres',
    'password': '2001'
}

def run_migration():
    """Add branch_guid column to users table"""
    print("=" * 80)
    print("Migration: Add branch_guid to users table")
    print("=" * 80)

    try:
        # Connect to database
        print("\n1. Connecting to database...")
        conn = psycopg2.connect(**DB_CONFIG)
        cursor = conn.cursor()
        print("   [OK] Connected successfully")

        # Check if column already exists
        print("\n2. Checking if branch_guid column exists...")
        cursor.execute("""
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = 'users'
              AND column_name = 'branch_guid'
        """)

        exists = cursor.fetchone()

        if exists:
            print("   ⚠️  Column 'branch_guid' already exists. Skipping...")
        else:
            # Add column
            print("\n3. Adding branch_guid column...")
            cursor.execute("""
                ALTER TABLE users
                ADD COLUMN branch_guid VARCHAR(255)
            """)
            print("   ✅ Column added successfully")

            # Add index
            print("\n4. Creating index on branch_guid...")
            cursor.execute("""
                CREATE INDEX IF NOT EXISTS idx_users_branch_guid
                ON users(branch_guid)
            """)
            print("   ✅ Index created successfully")

            # Add comment
            print("\n5. Adding column comment...")
            cursor.execute("""
                COMMENT ON COLUMN users.branch_guid
                IS 'PHP API dan keluvchi branch GUID (nullable)'
            """)
            print("   ✅ Comment added successfully")

            # Commit changes
            conn.commit()
            print("\n6. Committing changes...")
            print("   ✅ Changes committed successfully")

        # Verify column
        print("\n7. Verifying column structure...")
        cursor.execute("""
            SELECT column_name, data_type, is_nullable, character_maximum_length
            FROM information_schema.columns
            WHERE table_name = 'users'
              AND column_name = 'branch_guid'
        """)

        result = cursor.fetchone()
        if result:
            print(f"   Column Name: {result[0]}")
            print(f"   Data Type: {result[1]}")
            print(f"   Nullable: {result[2]}")
            print(f"   Max Length: {result[3]}")
            print("   ✅ Verification successful")
        else:
            print("   ❌ Column not found after migration")

        # Close connection
        cursor.close()
        conn.close()
        print("\n" + "=" * 80)
        print("Migration completed successfully!")
        print("=" * 80)

    except psycopg2.Error as e:
        print(f"\n❌ Database error: {e}")
        if 'conn' in locals():
            conn.rollback()
            conn.close()
    except Exception as e:
        print(f"\n❌ Error: {e}")


if __name__ == "__main__":
    run_migration()
