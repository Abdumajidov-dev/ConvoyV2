#!/usr/bin/env python3
"""
Check foreign key constraint on locations table
"""
import json
import psycopg2

# Read connection string
with open('Convoy.Api/appsettings.json', 'r') as f:
    config = json.load(f)

conn_string = config['ConnectionStrings']['DefaultConnection']
parts = dict(item.split('=', 1) for item in conn_string.split(';') if '=' in item)

conn = psycopg2.connect(
    host=parts['Host'],
    port=parts['Port'],
    database=parts['Database'],
    user=parts['Username'],
    password=parts['Password']
)

cursor = conn.cursor()

# Check foreign key constraint on locations table
print("=" * 70)
print("FOREIGN KEY CONSTRAINTS ON locations:")
print("=" * 70)

cursor.execute("""
    SELECT
        tc.constraint_name,
        tc.table_name,
        kcu.column_name,
        ccu.table_name AS foreign_table_name,
        ccu.column_name AS foreign_column_name
    FROM information_schema.table_constraints AS tc
    JOIN information_schema.key_column_usage AS kcu
        ON tc.constraint_name = kcu.constraint_name
        AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
        ON ccu.constraint_name = tc.constraint_name
        AND ccu.table_schema = tc.table_schema
    WHERE tc.constraint_type = 'FOREIGN KEY'
        AND tc.table_name LIKE 'locations%'
    ORDER BY tc.table_name
""")

fks = cursor.fetchall()
for fk in fks:
    print(f"  Table: {fk[1]}")
    print(f"  Constraint: {fk[0]}")
    print(f"  Column: {fk[2]} -> {fk[3]}.{fk[4]}")
    print()

# Check if partition tables have the FK
print("=" * 70)
print("CHECKING PARTITION TABLES:")
print("=" * 70)

cursor.execute("""
    SELECT tablename
    FROM pg_tables
    WHERE tablename LIKE 'locations_%'
    ORDER BY tablename
""")

partitions = cursor.fetchall()
for partition in partitions:
    print(f"  Partition: {partition[0]}")

# Test insert with existing user
print("\n" + "=" * 70)
print("TEST INSERT with user_id=1:")
print("=" * 70)

try:
    cursor.execute("""
        INSERT INTO locations (
            user_id, recorded_at, latitude, longitude,
            accuracy, speed, heading, altitude,
            activity_type, activity_confidence, is_moving,
            battery_level, is_charging,
            distance_from_previous, created_at
        ) VALUES (
            1, NOW(), 41.2995, 69.2401,
            10.5, 5.0, 90.0, 450.0,
            'walking', 95, true,
            80, false,
            0, NOW()
        )
        RETURNING id
    """)

    location_id = cursor.fetchone()[0]
    conn.commit()
    print(f"  SUCCESS! Location created with ID: {location_id}")

except Exception as e:
    conn.rollback()
    print(f"  FAILED: {e}")

cursor.close()
conn.close()
