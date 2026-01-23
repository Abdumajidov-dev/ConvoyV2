"""
Check users table structure in local database
"""
import psycopg2
import sys

if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

conn_string = "host=localhost port=5432 dbname=convoy_db user=postgres password=2001"

try:
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    print("=" * 70)
    print("USERS TABLE STRUCTURE")
    print("=" * 70)

    # Get table columns
    cur.execute("""
        SELECT column_name, data_type, is_nullable, column_default
        FROM information_schema.columns
        WHERE table_name = 'users'
        ORDER BY ordinal_position;
    """)

    columns = cur.fetchall()

    if columns:
        print("\nColumn'lar:")
        print("-" * 70)
        for col in columns:
            print(f"  {col[0]:30s} | {col[1]:15s} | Nullable: {col[2]:3s}")
        print("-" * 70)
    else:
        print("\n[ERROR] users jadvali topilmadi!")

    # Get sample data
    cur.execute("SELECT * FROM users LIMIT 5;")
    users = cur.fetchall()

    if users:
        print(f"\nSample data ({len(users)} ta user):")
        print("-" * 70)
        for user in users:
            print(f"  {user}")
    else:
        print("\n[INFO] users jadvalida ma'lumot yo'q")

    cur.close()
    conn.close()

except Exception as e:
    print(f"\n[ERROR] {e}")
    import traceback
    traceback.print_exc()
