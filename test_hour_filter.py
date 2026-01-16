"""
Test script for hour filter feature
Tests the new start_hour and end_hour query parameters
"""
import requests
import json
from datetime import datetime, timedelta

# API configuration
BASE_URL = "http://localhost:5084"
API_URL = f"{BASE_URL}/api"

# Test user credentials
TEST_USER_ID = 1
TEST_PHONE = "+998901234567"

def get_auth_token():
    """Get JWT token for testing"""
    print("\n=== Getting Authentication Token ===")

    # Step 1: Verify phone
    verify_response = requests.post(
        f"{API_URL}/auth/verify_number",
        json={"phone_number": TEST_PHONE}
    )
    print(f"Verify phone: {verify_response.status_code}")

    # Step 2: Send OTP
    otp_response = requests.post(
        f"{API_URL}/auth/send_otp",
        json={"phone_number": TEST_PHONE}
    )
    print(f"Send OTP: {otp_response.status_code}")

    # Step 3: Get OTP from console (manually)
    otp_code = input("Enter OTP code from console: ")

    # Step 4: Verify OTP and get token
    token_response = requests.post(
        f"{API_URL}/auth/verify_otp",
        json={"phone_number": TEST_PHONE, "otp_code": otp_code}
    )

    if token_response.status_code == 200:
        data = token_response.json()
        if data.get("status") and data.get("data"):
            token = data["data"]["token"]
            print(f"✅ Token received: {token[:50]}...")
            return token

    print(f"❌ Failed to get token: {token_response.text}")
    return None


def test_hour_filter(token):
    """Test hour filter functionality"""
    print("\n=== Testing Hour Filter ===")

    headers = {"Authorization": f"Bearer {token}"}

    # Calculate date range (last 7 days)
    end_date = datetime.now()
    start_date = end_date - timedelta(days=7)

    # Test 1: Get all locations (no hour filter)
    print("\n--- Test 1: All locations (no hour filter) ---")
    response = requests.get(
        f"{API_URL}/locations/user/{TEST_USER_ID}",
        headers=headers,
        params={
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat()
        }
    )
    print(f"Status: {response.status_code}")
    if response.status_code == 200:
        data = response.json()
        count = len(data.get("data", [])) if data.get("data") else 0
        print(f"Total locations: {count}")

    # Test 2: Working hours (9:00 - 18:00)
    print("\n--- Test 2: Working hours (9:00 - 18:00) ---")
    response = requests.get(
        f"{API_URL}/locations/user/{TEST_USER_ID}",
        headers=headers,
        params={
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat(),
            "start_hour": 9,
            "end_hour": 18
        }
    )
    print(f"Status: {response.status_code}")
    if response.status_code == 200:
        data = response.json()
        locations = data.get("data", [])
        count = len(locations) if locations else 0
        print(f"Locations during working hours: {count}")

        # Show sample hours
        if locations:
            print("\nSample hours from results:")
            for loc in locations[:5]:
                recorded_at = datetime.fromisoformat(loc["recorded_at"].replace("Z", "+00:00"))
                print(f"  - {recorded_at.strftime('%Y-%m-%d %H:%M:%S')} (hour: {recorded_at.hour})")

    # Test 3: Night hours (22:00 - 6:00) - only start_hour
    print("\n--- Test 3: After 22:00 (start_hour only) ---")
    response = requests.get(
        f"{API_URL}/locations/user/{TEST_USER_ID}",
        headers=headers,
        params={
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat(),
            "start_hour": 22
        }
    )
    print(f"Status: {response.status_code}")
    if response.status_code == 200:
        data = response.json()
        count = len(data.get("data", [])) if data.get("data") else 0
        print(f"Locations after 22:00: {count}")

    # Test 4: Morning hours (before 6:00) - only end_hour
    print("\n--- Test 4: Before 6:00 (end_hour only) ---")
    response = requests.get(
        f"{API_URL}/locations/user/{TEST_USER_ID}",
        headers=headers,
        params={
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat(),
            "end_hour": 6
        }
    )
    print(f"Status: {response.status_code}")
    if response.status_code == 200:
        data = response.json()
        count = len(data.get("data", [])) if data.get("data") else 0
        print(f"Locations before 6:00: {count}")

    # Test 5: Invalid hour range (should return 400)
    print("\n--- Test 5: Invalid hour range (start_hour=25) ---")
    response = requests.get(
        f"{API_URL}/locations/user/{TEST_USER_ID}",
        headers=headers,
        params={
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat(),
            "start_hour": 25
        }
    )
    print(f"Status: {response.status_code}")
    if response.status_code == 400:
        data = response.json()
        print(f"✅ Validation works: {data.get('message')}")
    else:
        print(f"❌ Expected 400, got {response.status_code}")


if __name__ == "__main__":
    print("=" * 60)
    print("Hour Filter Test Script")
    print("=" * 60)

    # Get auth token
    token = get_auth_token()

    if token:
        # Run tests
        test_hour_filter(token)
    else:
        print("\n❌ Cannot proceed without authentication token")

    print("\n" + "=" * 60)
    print("Test completed")
    print("=" * 60)
