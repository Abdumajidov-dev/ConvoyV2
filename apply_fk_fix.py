"""
Apply Foreign Key Fix to Convoy Database

Bu script quyidagi o'zgarishlarni amalga oshiradi:
1. locations.user_id FK'ni users.id dan users.user_id ga o'zgartiradi
2. users.user_id ga UNIQUE constraint qo'shadi
"""
import psycopg2
import sys

# Set encoding for Windows console
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

# Database connection - Railway
conn_string = "host=crossover.proxy.rlwy.net port=31579 dbname=railway user=postgres password=YrSIsEidlvQRLXLjpMkdHmDnsWsiqHkH"

print("=" * 70)
print("CONVOY DATABASE - FOREIGN KEY FIX")
print("=" * 70)
print()
print("Bu script quyidagi o'zgarishlarni amalga oshiradi:")
print("  [+] locations.user_id FK: users.id -> users.user_id")
print("  [+] users.user_id ga UNIQUE constraint qo'shadi")
print()
print("MUHIM: Bu o'zgartirish JWT token'dagi UserId (PHP worker_id) bilan")
print("       location create qilishni imkon beradi.")
print()
print("=" * 70)

try:
    # Connect to database
    print("\n[1/4] Databasega ulanmoqda...")
    conn = psycopg2.connect(conn_string)
    conn.autocommit = False
    cur = conn.cursor()
    print("      [OK] Ulanish muvaffaqiyatli")

    # Step 1: Drop existing foreign key
    print("\n[2/4] Eski foreign key constraint'ni o'chirmoqda...")
    cur.execute("ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;")
    print("      [OK] Eski constraint o'chirildi")

    # Step 2: Add UNIQUE constraint to users.user_id
    print("\n[3/4] users.user_id ga UNIQUE constraint qo'shmoqda...")
    cur.execute("""
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'users_user_id_unique'
            ) THEN
                ALTER TABLE users ADD CONSTRAINT users_user_id_unique UNIQUE (user_id);
                RAISE NOTICE 'UNIQUE constraint qo''shildi';
            ELSE
                RAISE NOTICE 'UNIQUE constraint allaqachon mavjud';
            END IF;
        END $$;
    """)
    print("      [OK] UNIQUE constraint qo'shildi")

    # Step 3: Create new foreign key to users.user_id
    print("\n[4/4] Yangi foreign key yaratmoqda (locations.user_id -> users.user_id)...")
    cur.execute("""
        ALTER TABLE locations
            ADD CONSTRAINT locations_user_id_fkey
            FOREIGN KEY (user_id)
            REFERENCES users(user_id)
            ON DELETE CASCADE;
    """)
    print("      [OK] Yangi foreign key yaratildi")

    # Commit changes
    conn.commit()

    # Verify changes
    print("\n" + "=" * 70)
    print("O'ZGARISHLARNI TEKSHIRISH")
    print("=" * 70)

    cur.execute("""
        SELECT
            tc.constraint_name,
            tc.table_name,
            kcu.column_name,
            ccu.table_name AS foreign_table_name,
            ccu.column_name AS foreign_column_name
        FROM
            information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
              ON tc.constraint_name = kcu.constraint_name
              AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
              ON ccu.constraint_name = tc.constraint_name
              AND ccu.table_schema = tc.table_schema
        WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_name = 'locations'
          AND kcu.column_name = 'user_id';
    """)

    result = cur.fetchone()
    if result:
        print(f"\n[SUCCESS] Foreign Key:")
        print(f"  Table: {result[1]}.{result[2]}")
        print(f"  References: {result[3]}.{result[4]}")
        print()
        print("[SUCCESS] O'ZGARISHLAR MUVAFFAQIYATLI QO'LLANDI!")
    else:
        print("\n[ERROR] Foreign key topilmadi!")

    print()
    print("=" * 70)
    print("XULOSA")
    print("=" * 70)
    print("Endi siz JWT tokendan kelgan UserId (PHP worker_id) bilan")
    print("to'g'ridan-to'g'ri location create qilishingiz mumkin!")
    print()
    print("Misol:")
    print("  POST /api/locations")
    print("  { \"user_id\": 123, \"latitude\": 41.0, \"longitude\": 69.0, ... }")
    print()
    print("Bu yerda user_id = JWT tokendan kelgan UserId (PHP worker_id)")
    print("=" * 70)

    cur.close()
    conn.close()

except psycopg2.Error as e:
    print(f"\n[ERROR] Database xatolik: {e}")
    try:
        if 'conn' in locals() and conn:
            conn.rollback()
            conn.close()
    except:
        pass
    print("\nO'zgarishlar bekor qilindi (rollback).")
    print("\nTekshiring:")
    print("  1. PostgreSQL ishlamoqda?")
    print("  2. Database 'railway' mavjud?")
    print("  3. Connection string to'g'ri?")
    exit(1)

except Exception as e:
    print(f"\n[ERROR] Xatolik: {e}")
    try:
        if 'conn' in locals() and conn:
            conn.rollback()
            conn.close()
    except:
        pass
    exit(1)
