"""
Test script for branch_guid filter on users endpoint
"""
import requests
import json

# API configuration
BASE_URL = "http://localhost:5084"
API_URL = f"{BASE_URL}/api"

def test_users_filter():
    """Test user filtering by branch_guid"""
    print("=" * 80)
    print("Testing User Branch GUID Filter")
    print("=" * 80)

    # Test 1: Get all users (no filter)
    print("\n--- Test 1: Get all users (no filter) ---")
    response = requests.post(
        f"{API_URL}/users",
        json={
            "page": 1,
            "page_size": 10
        }
    )
    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        data = response.json()
        total = data.get("total_count", 0)
        users = data.get("data", [])
        print(f"Total users: {total}")
        print(f"Users in page: {len(users)}")

        # Show branch_guid examples
        if users:
            print("\nSample branch_guid values:")
            for user in users[:5]:
                branch_guid = user.get("branch_guid")
                branch_name = user["branch"]["name"] if user.get("branch") else None
                print(f"  - User ID {user['id']}: branch_guid={branch_guid}, branch_name={branch_name}")
    else:
        print(f"Error: {response.text}")

    # Test 2: Filter by specific branch_guid
    print("\n--- Test 2: Filter by specific branch_guid ---")

    # Get a branch_guid from first test
    if response.status_code == 200:
        data = response.json()
        users = data.get("data", [])

        # Find first user with branch_guid
        test_branch_guid = None
        for user in users:
            if user.get("branch_guid"):
                test_branch_guid = user["branch_guid"]
                break

        if test_branch_guid:
            print(f"Testing filter with branch_guid: {test_branch_guid}")

            response = requests.post(
                f"{API_URL}/users",
                json={
                    "branch_guid": test_branch_guid,
                    "page": 1,
                    "page_size": 10
                }
            )
            print(f"Status: {response.status_code}")

            if response.status_code == 200:
                data = response.json()
                total = data.get("total_count", 0)
                users = data.get("data", [])
                print(f"Users with branch_guid '{test_branch_guid}': {total}")

                # Verify all users have same branch_guid
                all_match = all(u.get("branch_guid") == test_branch_guid for u in users)
                if all_match:
                    print("[OK] All users have matching branch_guid")
                else:
                    print("[ERROR] Some users have different branch_guid!")

                # Show results
                if users:
                    print("\nFiltered users:")
                    for user in users:
                        branch_name = user["branch"]["name"] if user.get("branch") else "N/A"
                        print(f"  - ID {user['id']}: {user['name']} (Branch: {branch_name})")
            else:
                print(f"Error: {response.text}")
        else:
            print("No users with branch_guid found for testing")

    # Test 3: Filter with non-existent branch_guid
    print("\n--- Test 3: Filter with non-existent branch_guid ---")
    response = requests.post(
        f"{API_URL}/users",
        json={
            "branch_guid": "non-existent-guid-12345",
            "page": 1,
            "page_size": 10
        }
    )
    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        data = response.json()
        total = data.get("total_count", 0)
        print(f"Users found: {total}")
        if total == 0:
            print("[OK] No users found (expected)")
        else:
            print("[ERROR] Found users with non-existent branch_guid!")

    # Test 4: Combined filters (branch_guid + is_active)
    print("\n--- Test 4: Combined filters (branch_guid + is_active) ---")

    if test_branch_guid:
        response = requests.post(
            f"{API_URL}/users",
            json={
                "branch_guid": test_branch_guid,
                "is_active": True,
                "page": 1,
                "page_size": 10
            }
        )
        print(f"Status: {response.status_code}")

        if response.status_code == 200:
            data = response.json()
            total = data.get("total_count", 0)
            users = data.get("data", [])
            print(f"Active users with branch_guid '{test_branch_guid}': {total}")

            # Verify all are active
            all_active = all(u.get("is_active") for u in users)
            if all_active:
                print("[OK] All users are active")
            else:
                print("[ERROR] Some users are not active!")

    print("\n" + "=" * 80)
    print("Test completed")
    print("=" * 80)


if __name__ == "__main__":
    test_users_filter()
