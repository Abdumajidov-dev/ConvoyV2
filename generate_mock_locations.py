import psycopg2
from datetime import datetime, timedelta
import random

# Database connection parameters
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'convoy_db',
    'user': 'postgres',
    'password': '2001'
}

# User IDs
USER_IDS = [5475, 5277, 1, 2, 3]

# Tashkent coordinates (approximate center)
TASHKENT_CENTER = {
    'lat': 41.2995,
    'lon': 69.2401
}

# Radius in degrees (approximately 10km)
RADIUS = 0.1

def generate_random_location(center_lat, center_lon, radius):
    """Generate random coordinates within radius of center point."""
    # Random angle and distance
    angle = random.uniform(0, 2 * 3.14159)
    distance = random.uniform(0, radius)

    # Calculate new coordinates
    lat = center_lat + (distance * random.choice([-1, 1]))
    lon = center_lon + (distance * random.choice([-1, 1]))

    return round(lat, 6), round(lon, 6)

def generate_mock_locations():
    """Generate mock locations for today for all users."""
    try:
        print("Connecting to database...")
        conn = psycopg2.connect(**DB_CONFIG)
        cursor = conn.cursor()

        # Get today's date
        today = datetime.now().date()
        print(f"Generating mock locations for date: {today}")

        # Check which partition exists for today
        month = today.month
        year = today.year
        partition_name = f"locations_{month:02d}_{year}"

        cursor.execute(f"""
            SELECT EXISTS (
                SELECT 1 FROM pg_tables
                WHERE tablename = '{partition_name}'
            );
        """)

        if not cursor.fetchone()[0]:
            print(f"ERROR: Partition {partition_name} does not exist!")
            print(f"Please create partition for {today.strftime('%B %Y')} first.")
            return

        print(f"Using partition: {partition_name}")

        total_inserted = 0

        for user_id in USER_IDS:
            print(f"\nGenerating locations for user_id={user_id}...")

            # Generate 20-50 random locations for each user throughout the day
            num_locations = random.randint(20, 50)

            # Start from 08:00 AM today
            start_time = datetime.combine(today, datetime.min.time()) + timedelta(hours=8)

            for i in range(num_locations):
                # Random time throughout the day (08:00 - 20:00)
                random_minutes = random.randint(0, 12 * 60)  # 12 hours in minutes
                recorded_at = start_time + timedelta(minutes=random_minutes)

                # Generate random location near Tashkent
                lat, lon = generate_random_location(
                    TASHKENT_CENTER['lat'],
                    TASHKENT_CENTER['lon'],
                    RADIUS
                )

                # Random accuracy (5-50 meters)
                accuracy = round(random.uniform(5.0, 50.0), 2)

                # Random speed (0-20 km/h converted to m/s)
                speed = round(random.uniform(0, 5.5), 2)

                # Random heading (0-360 degrees)
                heading = round(random.uniform(0, 360), 2)

                # Random altitude (200-500 meters)
                altitude = round(random.uniform(200, 500), 2)

                # Random activity
                activities = ['still', 'walking', 'running', 'in_vehicle']
                activity_type = random.choice(activities)
                activity_confidence = random.randint(60, 100)

                # Is moving (based on speed)
                is_moving = speed > 0.5

                # Random battery
                battery_level = random.randint(20, 100)
                is_charging = random.choice([True, False])

                # Insert location
                cursor.execute("""
                    INSERT INTO locations (
                        user_id, recorded_at, created_at,
                        latitude, longitude, accuracy, speed, heading, altitude,
                        activity_type, activity_confidence, is_moving,
                        battery_level, is_charging
                    ) VALUES (
                        %s, %s, %s,
                        %s, %s, %s, %s, %s, %s,
                        %s, %s, %s,
                        %s, %s
                    )
                """, (
                    user_id, recorded_at, datetime.now(),
                    lat, lon, accuracy, speed, heading, altitude,
                    activity_type, activity_confidence, is_moving,
                    battery_level, is_charging
                ))

                total_inserted += 1

            print(f"  -> Generated {num_locations} locations for user {user_id}")

        # Commit transaction
        conn.commit()
        print(f"\nSuccess! Total {total_inserted} mock locations inserted for {len(USER_IDS)} users.")

        # Show summary
        print("\nSummary:")
        cursor.execute("""
            SELECT user_id, COUNT(*) as count,
                   MIN(recorded_at) as first_location,
                   MAX(recorded_at) as last_location
            FROM locations
            WHERE DATE(recorded_at) = %s
            GROUP BY user_id
            ORDER BY user_id
        """, (today,))

        results = cursor.fetchall()
        for user_id, count, first_loc, last_loc in results:
            print(f"  User {user_id}: {count} locations from {first_loc.strftime('%H:%M')} to {last_loc.strftime('%H:%M')}")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error: {e}")
        if 'conn' in locals():
            conn.rollback()
            conn.close()

if __name__ == "__main__":
    generate_mock_locations()
