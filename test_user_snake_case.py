"""
Test script to verify snake_case JSON serialization for User endpoints
"""
import requests
import json

# API configuration
BASE_URL = "http://localhost:5084"
API_URL = f"{BASE_URL}/api"

def test_user_endpoints():
    """Test User endpoints for snake_case"""
    print("\n=== Testing User Endpoints Snake Case ===")

    # Test 1: Get all users (pagination)
    print("\n--- Test 1: GET /api/users (pagination response) ---")
    response = requests.get(f"{API_URL}/users", params={"page": 1, "page_size": 5})
    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        data = response.json()
        print("\nResponse keys (should be snake_case):")
        for key in data.keys():
            print(f"  - {key}")

        # Check if response uses snake_case
        expected_keys = ["status", "message", "data", "total_count", "page", "page_size", "total_pages", "has_next_page", "has_previous_page"]
        actual_keys = list(data.keys())

        print("\nExpected keys:", expected_keys)
        print("Actual keys:", actual_keys)

        if actual_keys == expected_keys:
            print("✅ Response keys are snake_case!")
        else:
            print("❌ Response keys mismatch!")
            print(f"Missing: {set(expected_keys) - set(actual_keys)}")
            print(f"Extra: {set(actual_keys) - set(expected_keys)}")

        # Check user data fields
        if data.get("data") and len(data["data"]) > 0:
            user = data["data"][0]
            print("\nFirst user keys (should be snake_case):")
            for key in user.keys():
                print(f"  - {key}")

            # Expected user keys
            expected_user_keys = ["id", "name", "phone", "is_active", "created_at", "updated_at"]
            actual_user_keys = list(user.keys())

            if all(key in actual_user_keys for key in expected_user_keys):
                print("✅ User data keys are snake_case!")
            else:
                print("❌ User data keys might not be snake_case")

    # Test 2: Get single user by ID
    print("\n--- Test 2: GET /api/users/1 ---")
    response = requests.get(f"{API_URL}/users/1")
    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        data = response.json()
        print("\nResponse keys:")
        for key in data.keys():
            print(f"  - {key}")

        if "data" in data and data["data"]:
            user = data["data"]
            print("\nUser keys:")
            for key in user.keys():
                print(f"  - {key}")

    # Test 3: Create user (test request and response)
    print("\n--- Test 3: POST /api/users (create user) ---")

    # Test with snake_case request
    print("\nAttempt 1: Request with snake_case:")
    new_user_snake = {
        "name": "Test User Snake",
        "username": "testsnake123",
        "phone": "+998901111111",
        "is_active": True
    }
    print(f"Request body: {json.dumps(new_user_snake, indent=2)}")

    response = requests.post(f"{API_URL}/users", json=new_user_snake)
    print(f"Status: {response.status_code}")

    if response.status_code in [200, 201]:
        data = response.json()
        print("✅ Request with snake_case ACCEPTED")
        print(f"Response keys: {list(data.keys())}")
        if "data" in data and data["data"]:
            print(f"Created user keys: {list(data['data'].keys())}")
    else:
        print(f"❌ Request failed: {response.text}")

    # Test with PascalCase request
    print("\n\nAttempt 2: Request with PascalCase:")
    new_user_pascal = {
        "Name": "Test User Pascal",
        "Username": "testpascal123",
        "Phone": "+998902222222",
        "IsActive": True
    }
    print(f"Request body: {json.dumps(new_user_pascal, indent=2)}")

    response = requests.post(f"{API_URL}/users", json=new_user_pascal)
    print(f"Status: {response.status_code}")

    if response.status_code in [200, 201]:
        data = response.json()
        print("✅ Request with PascalCase ACCEPTED")
        print(f"Response keys: {list(data.keys())}")
    else:
        print(f"❌ Request failed: {response.text}")

    # Test with camelCase request
    print("\n\nAttempt 3: Request with camelCase:")
    new_user_camel = {
        "name": "Test User Camel",
        "username": "testcamel123",
        "phone": "+998903333333",
        "isActive": True
    }
    print(f"Request body: {json.dumps(new_user_camel, indent=2)}")

    response = requests.post(f"{API_URL}/users", json=new_user_camel)
    print(f"Status: {response.status_code}")

    if response.status_code in [200, 201]:
        data = response.json()
        print("✅ Request with camelCase ACCEPTED")
        print(f"Response keys: {list(data.keys())}")
    else:
        print(f"❌ Request failed: {response.text}")


if __name__ == "__main__":
    print("=" * 80)
    print("User Endpoints Snake Case Test")
    print("=" * 80)

    test_user_endpoints()

    print("\n" + "=" * 80)
    print("Test completed")
    print("=" * 80)
