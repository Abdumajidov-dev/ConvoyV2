import csv

# Check specific users: 3561 and 9541
problem_users = [3561, 9541]

print("=" * 80)
print("CHECKING PROBLEM USERS (distance = 0 despite having many locations)")
print("=" * 80)

for target_user in problem_users:
    print(f"\n\nUser ID: {target_user}")
    print("-" * 80)

    user_locations = []

    with open(r'C:\Users\abdum\source\repos\ConvoyV2\Convoy.Data\data-1772865763862.csv', 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row in reader:
            user_id = int(row['user_id'])
            if user_id == target_user:
                user_locations.append({
                    'id': row['id'],
                    'recorded_at': row['recorded_at'],
                    'distance': row['distance_from_previous']
                })

    if not user_locations:
        print(f"  [X] NO LOCATIONS FOUND IN CSV for user {target_user}")
        print(f"  -> This means CSV only contains 2026-03-06 data")
        print(f"  -> But the report shows user {target_user} had locations on 2026-03-05")
        continue

    print(f"  [OK] Found {len(user_locations)} locations in CSV")

    # Analyze distances
    distances = [float(loc['distance']) if loc['distance'] and loc['distance'] != 'NULL' else 0.0
                 for loc in user_locations]

    total_distance = sum(distances)
    null_count = sum(1 for loc in user_locations if loc['distance'] == 'NULL' or loc['distance'] == '')
    zero_count = sum(1 for d in distances if d == 0.0)
    non_zero_count = sum(1 for d in distances if d > 0.0)

    print(f"\n  Distance Analysis:")
    print(f"    Total distance: {total_distance:.2f} meters = {total_distance/1000:.2f} km")
    print(f"    NULL distances: {null_count}")
    print(f"    Zero distances: {zero_count}")
    print(f"    Non-zero distances: {non_zero_count}")

    print(f"\n  First 10 locations:")
    for i, loc in enumerate(user_locations[:10], 1):
        dist_val = loc['distance'] if loc['distance'] else 'NULL'
        print(f"    {i}. ID={loc['id']}, Time={loc['recorded_at']}, Distance={dist_val}")

    print(f"\n  Last 10 locations:")
    for i, loc in enumerate(user_locations[-10:], len(user_locations)-9):
        dist_val = loc['distance'] if loc['distance'] else 'NULL'
        print(f"    {i}. ID={loc['id']}, Time={loc['recorded_at']}, Distance={dist_val}")

print("\n" + "=" * 80)
print("CONCLUSION:")
print("=" * 80)
print("If no locations found: CSV contains different date data than the report")
print("If all distances are NULL/0: distance calculation failed during insert")
