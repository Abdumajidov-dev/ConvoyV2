#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Create test users in database
"""
import json

# Read connection string from appsettings
with open('Convoy.Api/appsettings.json', 'r', encoding='utf-8') as f:
    config = json.load(f)

conn_string = config['ConnectionStrings']['DefaultConnection']
print(f"Connection string: {conn_string[:50]}...")

# Parse connection string
parts = dict(item.split('=', 1) for item in conn_string.split(';') if '=' in item)
host = parts.get('Host', 'localhost')
port = parts.get('Port', '5432')
database = parts.get('Database', 'convoy_db')
username = parts.get('Username', 'postgres')
password = parts.get('Password', '')

print(f"\nDatabase: {database}")
print(f"Host: {host}:{port}")
print(f"Username: {username}")

# Try to connect with psycopg2
try:
    import psycopg2

    conn = psycopg2.connect(
        host=host,
        port=port,
        database=database,
        user=username,
        password=password
    )

    cursor = conn.cursor()

    # Create test users
    users_to_create = [
        (1, 'Test User 1', '+998901234567'),
        (123, 'Test User 123', '+998909876543'),
        (456, 'Test User 456', '+998901112233')
    ]

    for user_id, name, phone in users_to_create:
        cursor.execute("""
            INSERT INTO users (id, name, phone, is_active, created_at, updated_at)
            VALUES (%s, %s, %s, true, NOW(), NOW())
            ON CONFLICT (id) DO NOTHING
        """, (user_id, name, phone))

    conn.commit()

    # Check created users
    cursor.execute("SELECT id, name, phone, is_active FROM users WHERE id IN (1, 123, 456)")
    users = cursor.fetchall()

    print("\nTest users in database:")
    for user in users:
        print(f"  ID: {user[0]}, Name: {user[1]}, Phone: {user[2]}, Active: {user[3]}")

    cursor.close()
    conn.close()

    print("\n[SUCCESS] Test users created/verified!")

except ImportError:
    print("\n[ERROR] psycopg2 not installed. Install with: pip install psycopg2-binary")
    print("\nAlternative: Use this SQL manually:")
    print("""
INSERT INTO users (id, name, phone, is_active, created_at, updated_at)
VALUES
    (1, 'Test User 1', '+998901234567', true, NOW(), NOW()),
    (123, 'Test User 123', '+998909876543', true, NOW(), NOW()),
    (456, 'Test User 456', '+998901112233', true, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;
""")

except Exception as e:
    print(f"\n[ERROR] {e}")
    print("\nMake sure database is accessible and credentials are correct")
