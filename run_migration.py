#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Migration script to add username column to users table
"""
import psycopg2
import sys
import io

# Fix Windows console encoding
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Connection parameters from appsettings.json
conn_params = {
    'host': 'localhost',
    'port': 5432,
    'database': 'convoy_db',
    'user': 'postgres',
    'password': '2001'
}

def run_migration():
    """Execute migration SQL"""
    print("Connecting to database...")

    try:
        # Connect to database
        conn = psycopg2.connect(**conn_params)
        cursor = conn.cursor()

        print("Connected successfully!")
        print("\n--- Running Migration: Add username column ---\n")

        # Step 1: Add column
        print("Step 1: Adding username column...")
        cursor.execute("""
            ALTER TABLE users ADD COLUMN IF NOT EXISTS username VARCHAR(100);
        """)
        print("✓ Username column added")

        # Step 2: Create unique index
        print("\nStep 2: Creating unique index...")
        cursor.execute("""
            CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username
            ON users(username) WHERE username IS NOT NULL;
        """)
        print("✓ Unique index created")

        # Step 3: Verify
        print("\nStep 3: Verifying column exists...")
        cursor.execute("""
            SELECT column_name, data_type, character_maximum_length, is_nullable
            FROM information_schema.columns
            WHERE table_name = 'users' AND column_name = 'username';
        """)
        result = cursor.fetchone()

        if result:
            print(f"✓ Column verified: {result[0]} ({result[1]}({result[2]}), nullable={result[3]})")
        else:
            print("✗ Column not found!")
            return False

        # Commit changes
        conn.commit()
        print("\n✓ Migration completed successfully!")

        # Close connection
        cursor.close()
        conn.close()

        return True

    except psycopg2.Error as e:
        print(f"\n✗ Database error: {e}")
        return False
    except Exception as e:
        print(f"\n✗ Unexpected error: {e}")
        return False

if __name__ == "__main__":
    success = run_migration()
    sys.exit(0 if success else 1)
