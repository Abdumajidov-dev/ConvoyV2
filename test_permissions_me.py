"""
Test script for /api/auth/me endpoint with permissions
Bu script authentication flow ni boshlaydi va permissions qaytarishni test qiladi
"""

import requests
import json

# API base URL (local development)
BASE_URL = "http://localhost:5084"

def test_auth_me_with_permissions():
    """
    Test /api/auth/me endpoint that should return user with permissions
    """
    print("=" * 60)
    print("Testing /api/auth/me with Permissions")
    print("=" * 60)

    # IMPORTANT: Bu yerda real JWT token kerak
    # Token olish uchun:
    # 1. POST /api/auth/verify_number
    # 2. POST /api/auth/send_otp
    # 3. POST /api/auth/verify_otp -> token qaytaradi

    # Bu yerga verify_otp dan kelgan tokenni qo'ying
    token = "YOUR_JWT_TOKEN_HERE"

    if token == "YOUR_JWT_TOKEN_HERE":
        print("\n❌ ERROR: Please update the script with a valid JWT token")
        print("\nTo get a token:")
        print("1. Run: POST http://localhost:5084/api/auth/verify_number")
        print('   Body: {"phone_number": "998901234567"}')
        print("\n2. Run: POST http://localhost:5084/api/auth/send_otp")
        print('   Body: {"phone_number": "998901234567"}')
        print("\n3. Run: POST http://localhost:5084/api/auth/verify_otp")
        print('   Body: {"phone_number": "998901234567", "otp_code": "1234"}')
        print("\n4. Copy the token from step 3 and paste it in this script")
        return

    # GET /api/auth/me with Authorization header
    url = f"{BASE_URL}/api/auth/me"
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }

    print(f"\n📤 REQUEST: GET {url}")
    print(f"Headers: Authorization: Bearer {token[:20]}...")

    try:
        response = requests.get(url, headers=headers)

        print(f"\n📥 RESPONSE: {response.status_code}")
        print(f"Response Headers: {dict(response.headers)}\n")

        if response.status_code == 200:
            data = response.json()
            print("✅ SUCCESS!")
            print(json.dumps(data, indent=2, ensure_ascii=False))

            # Validate response structure
            print("\n🔍 Validating response structure...")
            if "status" in data and data["status"]:
                result = data.get("data", {})

                # Check required fields
                required_fields = ["user_id", "name", "phone", "role", "role_id", "permissions"]
                missing_fields = [f for f in required_fields if f not in result]

                if missing_fields:
                    print(f"⚠️  Missing fields: {missing_fields}")
                else:
                    print("✅ All required fields present")

                    # Check permissions structure
                    permissions = result.get("permissions", [])
                    print(f"\n📋 Permissions count: {len(permissions)}")

                    if permissions:
                        print("Sample permission:")
                        print(json.dumps(permissions[0], indent=2, ensure_ascii=False))
                    else:
                        print("⚠️  No permissions found (user might not have permissions)")
            else:
                print("❌ Response status is false or missing")
        else:
            print(f"❌ ERROR: {response.status_code}")
            print(response.text)

    except Exception as e:
        print(f"❌ Exception: {e}")

if __name__ == "__main__":
    test_auth_me_with_permissions()
