"""
Fix Local Database Foreign Key Constraint

Bu script local PostgreSQL database'da quyidagi o'zgarishlarni amalga oshiradi:
1. locations.user_id FK constraint'ni users.user_id ga o'zgartiradi
2. users.user_id ga UNIQUE constraint qo'shadi
3. Test user yaratadi (agar kerak bo'lsa)
"""
import psycopg2
import sys

# Set encoding for Windows console
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

print("=" * 70)
print("CONVOY LOCAL DATABASE - FOREIGN KEY FIX")
print("=" * 70)
print()
print("Ushbu script quyidagilarni bajaradi:")
print("  1. Mavjud users'larni ko'rsatadi")
print("  2. Foreign key constraint'ni to'g'rilaydi")
print("  3. Test user yaratadi (agar kerak bo'lsa)")
print()
print("=" * 70)
print()

# Database connection - LOCAL
# MUHIM: Iltimos, quyidagi ma'lumotlarni o'zingizning local PostgreSQL connection ma'lumotlari bilan almashtiring!
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'dbname': 'convoy_db',
    'user': 'postgres',
    'password': '2001'
}

print(f"[INFO] Database: {DB_CONFIG['dbname']} @ {DB_CONFIG['host']}:{DB_CONFIG['port']}")
print(f"[INFO] User: {DB_CONFIG['user']}")
print()

# Connection string yaratish
conn_string = f"host={DB_CONFIG['host']} port={DB_CONFIG['port']} dbname={DB_CONFIG['dbname']} user={DB_CONFIG['user']}"
if DB_CONFIG['password']:
    conn_string += f" password={DB_CONFIG['password']}"

try:
    # Connect to database
    print("[1/5] Local database'ga ulanmoqda...")
    conn = psycopg2.connect(conn_string)
    conn.autocommit = False
    cur = conn.cursor()
    print("      [OK] Ulanish muvaffaqiyatli!")

    # Step 1: Check existing users
    print("\n[2/5] Mavjud users'larni tekshiryapman...")
    cur.execute("SELECT id, user_id, name, phone, is_active FROM users ORDER BY id;")
    users = cur.fetchall()

    if users:
        print(f"\n      Jami {len(users)} ta user topildi:")
        print("      " + "-" * 66)
        print("      DB_ID | USER_ID | Name                 | Phone           | Active")
        print("      " + "-" * 66)
        for user in users:
            db_id, user_id, name, phone, is_active = user
            user_id_str = str(user_id) if user_id else "NULL"
            active_str = "YES" if is_active else "NO"
            print(f"      {db_id:5d} | {user_id_str:7s} | {name[:20]:20s} | {phone or 'N/A':15s} | {active_str}")
        print("      " + "-" * 66)
    else:
        print("\n      [WARNING] Database'da user yo'q!")
        print("      [INFO] Keyinchalik test user yaratishingiz mumkin")

    # Step 2: Check current FK constraint
    print("\n[3/5] Hozirgi foreign key constraint'ni tekshiryapman...")
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

    fk_result = cur.fetchone()
    if fk_result:
        print(f"\n      Hozirgi FK: locations.{fk_result[1]} -> {fk_result[2]}.{fk_result[3]}")

        if fk_result[3] == 'id':
            print("      [PROBLEM] FK users.id ga ishora qilyapti (noto'g'ri)")
            print("      [ACTION] FK'ni users.user_id ga o'zgartirish kerak")
        else:
            print("      [OK] FK allaqachon users.user_id ga ishora qilyapti")
            print("\n[INFO] Foreign key allaqachon to'g'ri! O'zgartirish kerak emas.")

            # Faqat test user yaratish
            print("\n[5/5] Test user yaratish (optional)...")
            create_user = input("\nTest user yaratishni xohlaysizmi? (y/n): ").strip().lower()

            if create_user == 'y':
                user_id = input("User ID (PHP worker_id) kiriting: ").strip()
                name = input("Ism kiriting: ").strip()
                phone = input("Telefon raqam kiriting: ").strip()

                if user_id and name and phone:
                    cur.execute("""
                        INSERT INTO users (user_id, name, phone, is_active, created_at, updated_at)
                        VALUES (%s, %s, %s, true, NOW(), NOW())
                        ON CONFLICT (user_id) DO NOTHING
                        RETURNING id;
                    """, (int(user_id), name, phone))

                    result = cur.fetchone()
                    if result:
                        conn.commit()
                        print(f"\n      [OK] User yaratildi! DB ID: {result[0]}, User ID: {user_id}")
                    else:
                        print(f"\n      [INFO] User allaqachon mavjud (user_id={user_id})")
                        conn.commit()

            cur.close()
            conn.close()
            print("\n[SUCCESS] Operatsiya tugadi!")
            exit(0)
    else:
        print("      [INFO] Foreign key constraint topilmadi")

    # Step 3: Drop existing foreign key
    print("\n[4/5] Foreign key constraint'ni o'zgartirmoqda...")
    print("      [4.1] Eski constraint'ni o'chiryapman...")
    cur.execute("ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;")
    print("            [OK] Eski constraint o'chirildi")

    # Step 4: Add UNIQUE constraint to users.user_id
    print("      [4.2] users.user_id ga UNIQUE constraint qo'shyapman...")
    cur.execute("""
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint WHERE conname = 'users_user_id_unique'
            ) THEN
                ALTER TABLE users ADD CONSTRAINT users_user_id_unique UNIQUE (user_id);
                RAISE NOTICE 'UNIQUE constraint qo''shildi';
            ELSE
                RAISE NOTICE 'UNIQUE constraint allaqachon mavjud';
            END IF;
        END $$;
    """)
    print("            [OK] UNIQUE constraint qo'shildi")

    # Step 5: Create new foreign key to users.user_id
    print("      [4.3] Yangi FK yaratyapman (locations.user_id -> users.user_id)...")
    cur.execute("""
        ALTER TABLE locations
            ADD CONSTRAINT locations_user_id_fkey
            FOREIGN KEY (user_id)
            REFERENCES users(user_id)
            ON DELETE CASCADE;
    """)
    print("            [OK] Yangi FK yaratildi")

    # Commit all changes
    conn.commit()
    print("\n      [OK] Barcha o'zgarishlar saqlandi!")

    # Step 6: Verify changes
    print("\n" + "=" * 70)
    print("O'ZGARISHLARNI TEKSHIRISH")
    print("=" * 70)

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

    result = cur.fetchone()
    if result:
        print(f"\n[SUCCESS] Yangi Foreign Key:")
        print(f"  locations.{result[1]} -> {result[2]}.{result[3]}")
        print()

        if result[3] == 'user_id':
            print("[SUCCESS] Foreign key to'g'ri o'rnatildi!")
        else:
            print("[WARNING] Foreign key hali ham noto'g'ri!")

    # Optional: Create test user
    print("\n[5/5] Test user yaratish (optional)...")
    create_user = input("\nTest user yaratishni xohlaysizmi? (y/n): ").strip().lower()

    if create_user == 'y':
        user_id = input("User ID (PHP worker_id) kiriting (masalan: 123): ").strip()
        name = input("Ism kiriting (masalan: Test Driver): ").strip()
        phone = input("Telefon raqam kiriting (masalan: +998901234567): ").strip()

        if user_id and name and phone:
            try:
                cur.execute("""
                    INSERT INTO users (user_id, name, phone, is_active, created_at, updated_at)
                    VALUES (%s, %s, %s, true, NOW(), NOW())
                    ON CONFLICT (user_id) DO NOTHING
                    RETURNING id;
                """, (int(user_id), name, phone))

                result = cur.fetchone()
                conn.commit()

                if result:
                    print(f"\n[OK] Test user yaratildi!")
                    print(f"  DB ID: {result[0]}")
                    print(f"  User ID: {user_id}")
                    print(f"  Name: {name}")
                    print(f"  Phone: {phone}")
                else:
                    print(f"\n[INFO] User allaqachon mavjud (user_id={user_id})")

            except Exception as e:
                print(f"\n[ERROR] User yaratishda xatolik: {e}")
                conn.rollback()
        else:
            print("\n[INFO] Ma'lumotlar to'liq kiritilmadi, user yaratilmadi")

    print()
    print("=" * 70)
    print("XULOSA")
    print("=" * 70)
    print("[SUCCESS] Barcha o'zgarishlar muvaffaqiyatli amalga oshirildi!")
    print()
    print("Endi siz JWT tokendan kelgan UserId (PHP worker_id) bilan")
    print("to'g'ridan-to'g'ri location create qilishingiz mumkin!")
    print()
    print("API so'rov misoli:")
    print("  POST /api/locations")
    print("  {")
    print("    \"user_id\": 123,  // JWT tokendan kelgan UserId")
    print("    \"latitude\": 41.0,")
    print("    \"longitude\": 69.0,")
    print("    ...")
    print("  }")
    print()
    print("=" * 70)

    cur.close()
    conn.close()

except psycopg2.OperationalError as e:
    print(f"\n[ERROR] Database'ga ulanib bo'lmadi: {e}")
    print("\nTekshiring:")
    print("  1. PostgreSQL service ishlamoqda?")
    print("     Windows: services.msc -> 'postgresql-x64-XX' service")
    print("  2. Database nomi to'g'ri? (convoy_db)")
    print("  3. Port to'g'ri? (5432)")
    print("  4. Username/password to'g'ri?")
    print()
    print("PostgreSQL'ni tekshirish:")
    print("  pg_isready -h localhost -p 5432")
    exit(1)

except psycopg2.Error as e:
    print(f"\n[ERROR] Database xatolik: {e}")
    try:
        if 'conn' in locals() and conn:
            conn.rollback()
            conn.close()
    except:
        pass
    print("\n[INFO] O'zgarishlar bekor qilindi (rollback)")
    exit(1)

except Exception as e:
    print(f"\n[ERROR] Xatolik: {e}")
    import traceback
    traceback.print_exc()
    try:
        if 'conn' in locals() and conn:
            conn.rollback()
            conn.close()
    except:
        pass
    exit(1)
