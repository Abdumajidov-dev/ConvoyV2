import psycopg2
from psycopg2 import sql

# Database connection parameters
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'convoy_db',
    'user': 'postgres',
    'password': '2001'
}

def add_image_column():
    """Add image column to users table if it doesn't exist."""
    try:
        # Connect to database
        print("Connecting to database...")
        conn = psycopg2.connect(**DB_CONFIG)
        cursor = conn.cursor()

        # Check if column exists
        cursor.execute("""
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = 'users'
            AND column_name = 'image'
        """)

        if cursor.fetchone():
            print("Column 'image' already exists in users table")
        else:
            # Add column
            print("Adding 'image' column to users table...")
            cursor.execute("ALTER TABLE users ADD COLUMN image TEXT;")
            conn.commit()
            print("Column 'image' added successfully!")

        # Verify the column structure
        print("\nCurrent users table structure:")
        cursor.execute("""
            SELECT column_name, data_type, is_nullable
            FROM information_schema.columns
            WHERE table_name = 'users'
            ORDER BY ordinal_position
        """)

        columns = cursor.fetchall()
        for col_name, col_type, is_nullable in columns:
            print(f"  - {col_name}: {col_type} (nullable: {is_nullable})")

        cursor.close()
        conn.close()
        print("\nDone!")

    except Exception as e:
        print(f"Error: {e}")
        if 'conn' in locals():
            conn.rollback()
            conn.close()

if __name__ == "__main__":
    add_image_column()
