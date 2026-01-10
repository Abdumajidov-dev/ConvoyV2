import psycopg2
from datetime import datetime

# Database connection parameters
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'convoy_db',
    'user': 'postgres',
    'password': '2001'
}

def verify_locations():
    """Verify mock locations were created for today."""
    try:
        print("Connecting to database...")
        conn = psycopg2.connect(**DB_CONFIG)
        cursor = conn.cursor()

        today = datetime.now().date()
        print(f"Checking locations for date: {today}\n")

        # Get sample locations for each user
        cursor.execute("""
            SELECT user_id, id, recorded_at, latitude, longitude,
                   activity_type, is_moving, battery_level
            FROM locations
            WHERE DATE(recorded_at) = %s
            ORDER BY user_id, recorded_at
            LIMIT 10
        """, (today,))

        print("Sample locations (first 10):")
        print("-" * 100)
        results = cursor.fetchall()
        for row in results:
            user_id, id, recorded_at, lat, lon, activity, is_moving, battery = row
            print(f"ID: {id} | User: {user_id} | Time: {recorded_at.strftime('%H:%M:%S')} | "
                  f"Coords: ({lat:.4f}, {lon:.4f}) | Activity: {activity} | "
                  f"Moving: {is_moving} | Battery: {battery}%")

        print("\n" + "=" * 100)
        print("All users' locations for today:")
        print("=" * 100)

        cursor.execute("""
            SELECT user_id,
                   COUNT(*) as total_locations,
                   MIN(recorded_at) as first_time,
                   MAX(recorded_at) as last_time,
                   ROUND(AVG(CAST(is_moving AS INT)) * 100, 0) as moving_percent
            FROM locations
            WHERE DATE(recorded_at) = %s
            GROUP BY user_id
            ORDER BY user_id
        """, (today,))

        results = cursor.fetchall()
        for user_id, total, first_time, last_time, moving_pct in results:
            duration = last_time - first_time
            hours = duration.seconds // 3600
            minutes = (duration.seconds % 3600) // 60
            print(f"\nUser {user_id}:")
            print(f"  Total locations: {total}")
            print(f"  Time range: {first_time.strftime('%H:%M')} - {last_time.strftime('%H:%M')} ({hours}h {minutes}m)")
            print(f"  Moving: {moving_pct:.0f}%")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error: {e}")
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    verify_locations()
