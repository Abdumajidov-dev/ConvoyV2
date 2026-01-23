#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Check existing users and create missing ones
"""
import json
import psycopg2

# Read connection string from appsettings
with open('Convoy.Api/appsettings.json', 'r', encoding='utf-8') as f:
    config = json.load(f)

conn_string = config['ConnectionStrings']['DefaultConnection']

# Parse connection string
parts = dict(item.split('=', 1) for item in conn_string.split(';') if '=' in item)
host = parts.get('Host', 'localhost')
port = parts.get('Port', '5432')
database = parts.get('Database', 'convoy_db')
username = parts.get('Username', 'postgres')
password = parts.get('Password', '')

print(f"Connecting to database: {database}")
print(f"Host: {host}:{port}\n")

try:
    conn = psycopg2.connect(
        host=host,
        port=port,
        database=database,
        user=username,
        password=password
    )

    cursor = conn.cursor()

    # Check existing users
    print("=" * 70)
    print("EXISTING USERS:")
    print("=" * 70)
    cursor.execute("SELECT id, name, phone, is_active FROM users ORDER BY id LIMIT 10")
    users = cursor.fetchall()

    if users:
        for user in users:
            print(f"  ID: {user[0]:<5} Name: {user[1]:<20} Phone: {user[2]:<15} Active: {user[3]}")
    else:
        print("  No users found in database!")

    # Ask which user IDs to create
    print("\n" + "=" * 70)
    print("CREATE USERS:")
    print("=" * 70)

    user_ids_to_create = input("Enter user IDs to create (comma-separated, e.g., 1,2,3) or press Enter to skip: ").strip()

    if user_ids_to_create:
        user_ids = [int(uid.strip()) for uid in user_ids_to_create.split(',')]

        for user_id in user_ids:
            try:
                cursor.execute("""
                    INSERT INTO users (id, name, phone, is_active, created_at, updated_at)
                    VALUES (%s, %s, %s, true, NOW(), NOW())
                    ON CONFLICT (id) DO UPDATE
                    SET name = EXCLUDED.name,
                        phone = EXCLUDED.phone,
                        updated_at = NOW()
                    RETURNING id, name, phone
                """, (user_id, f'User {user_id}', f'+99890{user_id:07d}'))

                result = cursor.fetchone()
                conn.commit()
                print(f"  ✓ User created/updated: ID={result[0]}, Name={result[1]}, Phone={result[2]}")

            except Exception as e:
                print(f"  ✗ Failed to create user {user_id}: {e}")
                conn.rollback()

    # Show final user list
    print("\n" + "=" * 70)
    print("FINAL USER LIST:")
    print("=" * 70)
    cursor.execute("SELECT id, name, phone, is_active FROM users ORDER BY id LIMIT 20")
    users = cursor.fetchall()

    for user in users:
        print(f"  ID: {user[0]:<5} Name: {user[1]:<20} Phone: {user[2]:<15} Active: {user[3]}")

    cursor.close()
    conn.close()

    print("\n[SUCCESS] User check/creation completed!")

except Exception as e:
    print(f"\n[ERROR] {e}")
