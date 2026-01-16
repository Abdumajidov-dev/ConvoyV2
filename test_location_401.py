"""
Test script to diagnose 401 error on location endpoints
"""
import requests
import json
from datetime import datetime, timedelta

# API configuration
BASE_URL = "http://localhost:5084"
API_URL = f"{BASE_URL}/api"

def test_without_token():
    """Test location endpoint without token (should get 401)"""
    print("\n" + "="*80)
    print("TEST 1: Location endpoint WITHOUT token")
    print("="*80)

    response = requests.get(f"{API_URL}/locations/user/1")
    print(f"Status Code: {response.status_code}")
    print(f"Response: {response.text[:200]}")

    if response.status_code == 401:
        print("✅ Expected: 401 Unauthorized (no token provided)")
    else:
        print(f"❌ Unexpected: Got {response.status_code} instead of 401")


def test_users_without_token():
    """Test users endpoint without token (should work)"""
    print("\n" + "="*80)
    print("TEST 2: Users endpoint WITHOUT token")
    print("="*80)

    response = requests.post(
        f"{API_URL}/users",
        json={"page": 1, "page_size": 5}
    )
    print(f"Status Code: {response.status_code}")

    if response.status_code == 200:
        data = response.json()
        print(f"✅ Success: Got {data.get('total_count', 0)} users")
        print(f"Response keys: {list(data.keys())}")
    else:
        print(f"❌ Failed: {response.text[:200]}")


def get_token():
    """Get authentication token"""
    print("\n" + "="*80)
    print("Getting Authentication Token")
    print("="*80)

    phone = input("Enter phone number (e.g., +998901234567): ")

    # Step 1: Verify phone
    print("\n1. Verifying phone number...")
    verify_response = requests.post(
        f"{API_URL}/auth/verify_number",
        json={"phone_number": phone}
    )
    print(f"   Status: {verify_response.status_code}")

    if verify_response.status_code != 200:
        print(f"   ❌ Failed: {verify_response.text}")
        return None

    # Step 2: Send OTP
    print("\n2. Sending OTP...")
    otp_response = requests.post(
        f"{API_URL}/auth/send_otp",
        json={"phone_number": phone}
    )
    print(f"   Status: {otp_response.status_code}")

    if otp_response.status_code != 200:
        print(f"   ❌ Failed: {otp_response.text}")
        return None

    # Step 3: Get OTP from user
    otp_code = input("\n3. Enter OTP code from console/SMS: ")

    # Step 4: Verify OTP
    print("\n4. Verifying OTP...")
    token_response = requests.post(
        f"{API_URL}/auth/verify_otp",
        json={"phone_number": phone, "otp_code": otp_code}
    )
    print(f"   Status: {token_response.status_code}")

    if token_response.status_code == 200:
        data = token_response.json()
        if data.get("status") and data.get("data"):
            token = data["data"]["token"]
            print(f"   ✅ Token received: {token[:50]}...")
            return token

    print(f"   ❌ Failed: {token_response.text}")
    return None


def test_with_token(token):
    """Test location endpoint with valid token"""
    print("\n" + "="*80)
    print("TEST 3: Location endpoint WITH token")
    print("="*80)

    headers = {"Authorization": f"Bearer {token}"}

    # Calculate date range
    end_date = datetime.now()
    start_date = end_date - timedelta(days=7)

    response = requests.get(
        f"{API_URL}/locations/user/1",
        headers=headers,
        params={
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat()
        }
    )

    print(f"Status Code: {response.status_code}")

    if response.status_code == 200:
        data = response.json()
        count = len(data.get("data", [])) if data.get("data") else 0
        print(f"✅ Success: Got {count} locations")
        print(f"Response keys: {list(data.keys())}")

        if count > 0:
            print("\nFirst location keys:")
            print(f"  {list(data['data'][0].keys())}")
    else:
        print(f"❌ Failed: {response.text[:200]}")


def test_with_invalid_token():
    """Test location endpoint with invalid token"""
    print("\n" + "="*80)
    print("TEST 4: Location endpoint with INVALID token")
    print("="*80)

    headers = {"Authorization": "Bearer invalid_token_12345"}

    response = requests.get(f"{API_URL}/locations/user/1", headers=headers)
    print(f"Status Code: {response.status_code}")
    print(f"Response: {response.text[:200]}")

    if response.status_code == 401:
        print("✅ Expected: 401 Unauthorized (invalid token)")
    else:
        print(f"❌ Unexpected: Got {response.status_code}")


def main():
    print("="*80)
    print("Location 401 Error Diagnostic Tool")
    print("="*80)

    # Test 1: No token (should fail)
    test_without_token()

    # Test 2: Users endpoint (should work without token)
    test_users_without_token()

    # Test 3: Invalid token (should fail)
    test_with_invalid_token()

    # Test 4: Get real token and test
    print("\n\nDo you want to test with a valid token? (y/n): ", end="")
    choice = input().lower()

    if choice == 'y':
        token = get_token()
        if token:
            test_with_token(token)
        else:
            print("\n❌ Could not get valid token")

    print("\n" + "="*80)
    print("Diagnostic Summary")
    print("="*80)
    print("""
Expected behavior:
1. ✅ GET /api/locations/user/1 WITHOUT token → 401 Unauthorized
2. ✅ POST /api/users (get all) WITHOUT token → 200 OK (no auth required)
3. ✅ GET /api/locations/user/1 WITH invalid token → 401 Unauthorized
4. ✅ GET /api/locations/user/1 WITH valid token → 200 OK

Common issues causing 401:
- Missing Authorization header
- Wrong token format (must be "Bearer <token>")
- Expired token
- Token blacklisted (logged out)
- Wrong secret key in appsettings.json
    """)


if __name__ == "__main__":
    main()
