"""
Add user_id column to users table and fix foreign key

Bu script:
1. users jadvaliga user_id column qo'shadi (agar yo'q bo'lsa)
2. Hozirgi id qiymatlarini user_id ga copy qiladi
3. Foreign key constraint'ni to'g'rilaydi (locations.user_id -> users.user_id)
"""
import psycopg2
import sys

if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

conn_string = "host=localhost port=5432 dbname=convoy_db user=postgres password=2001"

print("=" * 70)
print("ADD user_id COLUMN AND FIX FOREIGN KEY")
print("=" * 70)
print()
print("Bu script quyidagilarni bajaradi:")
print("  1. users jadvaliga user_id column qo'shadi")
print("  2. Hozirgi id'larni user_id ga copy qiladi")
print("  3. Foreign key'ni to'g'rilaydi (locations.user_id -> users.user_id)")
print()
print("=" * 70)
print()

try:
    conn = psycopg2.connect(conn_string)
    conn.autocommit = False
    cur = conn.cursor()

    print("[1/5] Database'ga ulanish...")
    print("      [OK] Ulanish muvaffaqiyatli!\n")

    # Step 1: Check if user_id column exists
    print("[2/5] user_id column'ni tekshirish...")
    cur.execute("""
        SELECT column_name
        FROM information_schema.columns
        WHERE table_name = 'users' AND column_name = 'user_id';
    """)

    column_exists = cur.fetchone() is not None

    if column_exists:
        print("      [INFO] user_id column allaqachon mavjud")
    else:
        print("      [INFO] user_id column yo'q, qo'shilmoqda...")

        # Add user_id column
        cur.execute("""
            ALTER TABLE users
            ADD COLUMN IF NOT EXISTS user_id INTEGER;
        """)

        # Copy id values to user_id
        cur.execute("""
            UPDATE users
            SET user_id = id
            WHERE user_id IS NULL;
        """)

        # Make user_id UNIQUE
        cur.execute("""
            ALTER TABLE users
            ADD CONSTRAINT users_user_id_unique UNIQUE (user_id);
        """)

        print("      [OK] user_id column qo'shildi va id'lar copy qilindi")
        print("      [OK] UNIQUE constraint qo'shildi")

    conn.commit()

    # Step 2: Show users with both id and user_id
    print("\n[3/5] Hozirgi users'larni ko'rsatish...")
    cur.execute("SELECT id, user_id, name, phone FROM users ORDER BY id LIMIT 10;")
    users = cur.fetchall()

    if users:
        print(f"\n      Jami: {len(users)} ta user")
        print("      " + "-" * 66)
        print("      DB_ID | USER_ID | Name                 | Phone")
        print("      " + "-" * 66)
        for user in users:
            db_id, user_id, name, phone = user
            user_id_str = str(user_id) if user_id else "NULL"
            print(f"      {db_id:5d} | {user_id_str:7s} | {name[:20]:20s} | {phone or 'N/A':15s}")
        print("      " + "-" * 66)

    # Step 3: Check current FK
    print("\n[4/5] Foreign key constraint'ni tekshirish...")
    cur.execute("""
        SELECT
            tc.constraint_name,
            kcu.column_name,
            ccu.table_name AS foreign_table_name,
            ccu.column_name AS foreign_column_name
        FROM
            information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
              ON tc.constraint_name = kcu.constraint_name
            JOIN information_schema.constraint_column_usage AS ccu
              ON ccu.constraint_name = tc.constraint_name
        WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_name = 'locations'
          AND kcu.column_name = 'user_id';
    """)

    fk = cur.fetchone()
    if fk:
        print(f"      Hozirgi FK: locations.{fk[1]} -> {fk[2]}.{fk[3]}")

        if fk[3] == 'id':
            print("      [ACTION] FK'ni users.user_id ga o'zgartirish kerak\n")

            # Drop old FK
            print("      [4.1] Eski FK'ni o'chirish...")
            cur.execute("ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;")
            print("            [OK] O'chirildi")

            # Create new FK
            print("      [4.2] Yangi FK yaratish (-> users.user_id)...")
            cur.execute("""
                ALTER TABLE locations
                    ADD CONSTRAINT locations_user_id_fkey
                    FOREIGN KEY (user_id)
                    REFERENCES users(user_id)
                    ON DELETE CASCADE;
            """)
            print("            [OK] Yaratildi")

        elif fk[3] == 'user_id':
            print("      [OK] FK allaqachon to'g'ri!")
    else:
        print("      [INFO] FK topilmadi, yangi FK yaratilmoqda...")
        cur.execute("""
            ALTER TABLE locations
                ADD CONSTRAINT locations_user_id_fkey
                FOREIGN KEY (user_id)
                REFERENCES users(user_id)
                ON DELETE CASCADE;
        """)
        print("      [OK] FK yaratildi")

    conn.commit()

    # Verify
    print("\n[5/5] O'zgarishlarni tekshirish...")
    cur.execute("""
        SELECT
            tc.constraint_name,
            kcu.column_name,
            ccu.table_name AS foreign_table_name,
            ccu.column_name AS foreign_column_name
        FROM
            information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
              ON tc.constraint_name = kcu.constraint_name
            JOIN information_schema.constraint_column_usage AS ccu
              ON ccu.constraint_name = tc.constraint_name
        WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_name = 'locations'
          AND kcu.column_name = 'user_id';
    """)

    fk = cur.fetchone()
    if fk:
        print(f"\n      Yangi FK: locations.{fk[1]} -> {fk[2]}.{fk[3]}")

        if fk[3] == 'user_id':
            print("      [SUCCESS] Foreign key to'g'ri!\n")
        else:
            print("      [WARNING] Foreign key hali ham noto'g'ri!\n")

    print("=" * 70)
    print("SUCCESS!")
    print("=" * 70)
    print()
    print("Barcha o'zgarishlar muvaffaqiyatli amalga oshirildi!")
    print()
    print("Endi location create qilishda user_id sifatida:")
    print("  - JWT tokendan kelgan UserId ishlatishingiz mumkin")
    print("  - Yoki users.id (database primary key) ishlatishingiz mumkin")
    print("  - Ikkalasi ham bir xil qiymatga ega (id = user_id)")
    print()
    print("Test qilish:")
    print("  POST /api/locations")
    print("  {")
    print("    \"user_id\": 2,  // users jadvalidagi id yoki user_id")
    print("    \"latitude\": 41.0,")
    print("    \"longitude\": 69.0,")
    print("    ...")
    print("  }")
    print()
    print("=" * 70)

    cur.close()
    conn.close()

except psycopg2.Error as e:
    print(f"\n[ERROR] Database xatolik: {e}")
    if 'conn' in locals() and conn:
        conn.rollback()
        conn.close()
    print("\n[INFO] O'zgarishlar bekor qilindi")
    exit(1)

except Exception as e:
    print(f"\n[ERROR] {e}")
    import traceback
    traceback.print_exc()
    if 'conn' in locals() and conn:
        conn.rollback()
        conn.close()
    exit(1)
