"""
Test script for /api/auth/me endpoint with NEW grouped permissions format
Bu script yangi format'ni test qiladi: status, message, data
"""

import requests
import json

# API base URL (local development)
BASE_URL = "http://localhost:5084"

def test_auth_me_new_format():
    """
    Test /api/auth/me endpoint with new format
    Expected response format:
    {
        "status": true,
        "message": "...",
        "data": {
            "user_id": 1,
            "name": "...",
            "phone": "...",
            "image": null,
            "role": ["Admin"],
            "role_id": [1],
            "permissions": [
                {"users": ["view", "create", "update"]},
                {"locations": ["view", "create"]}
            ]
        }
    }
    """
    print("=" * 80)
    print("Testing /api/auth/me with NEW Grouped Permissions Format")
    print("=" * 80)

    # IMPORTANT: Bu yerda real JWT token kerak
    # Tokenni olish:
    # 1. POST /api/auth/verify_number
    # 2. POST /api/auth/send_otp
    # 3. POST /api/auth/verify_otp -> token qaytaradi

    # Bu yerga real token qo'ying (test_permissions_me.py dan yoki verify_otp dan)
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
        print("\n4. Copy the token from step 3 and paste it here")
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

        print(f"\n📥 RESPONSE STATUS: {response.status_code}")

        if response.status_code == 200:
            data = response.json()
            print("\n✅ SUCCESS!")
            print("\n📄 Full Response:")
            print(json.dumps(data, indent=2, ensure_ascii=False))

            # Validate response structure
            print("\n" + "=" * 80)
            print("🔍 Validating NEW Response Format...")
            print("=" * 80)

            # Check top-level fields
            if "status" not in data:
                print("❌ Missing 'status' field")
                return
            if "message" not in data:
                print("❌ Missing 'message' field")
                return
            if "data" not in data:
                print("❌ Missing 'data' field")
                return

            print("✅ Top-level fields OK (status, message, data)")

            if not data["status"]:
                print(f"❌ Response status is false: {data.get('message')}")
                return

            result = data.get("data", {})

            # Check required fields in data
            required_fields = ["user_id", "name", "phone", "role", "role_id", "permissions"]
            missing_fields = [f for f in required_fields if f not in result]

            if missing_fields:
                print(f"❌ Missing data fields: {missing_fields}")
                return

            print("✅ All required data fields present")

            # Check permissions structure (grouped format)
            permissions = result.get("permissions", [])
            print(f"\n📋 Permissions Groups: {len(permissions)}")

            if not isinstance(permissions, list):
                print("❌ Permissions should be a list")
                return

            if permissions:
                print("\n✅ Permissions Format (Flutter-friendly grouped):")
                for i, perm_group in enumerate(permissions[:3], 1):  # Show first 3
                    print(f"   {i}. {json.dumps(perm_group, ensure_ascii=False)}")

                if len(permissions) > 3:
                    print(f"   ... and {len(permissions) - 3} more groups")

                # Validate permission group structure
                for perm_group in permissions:
                    if not isinstance(perm_group, dict):
                        print("❌ Each permission group should be a dictionary")
                        return

                    for resource, actions in perm_group.items():
                        if not isinstance(actions, list):
                            print(f"❌ Actions for '{resource}' should be a list")
                            return

                print("\n✅ Permission groups structure is valid")
            else:
                print("⚠️  No permissions found (user might not have any roles assigned)")

            # Print summary
            print("\n" + "=" * 80)
            print("📊 Summary")
            print("=" * 80)
            print(f"User ID: {result.get('user_id')}")
            print(f"Name: {result.get('name')}")
            print(f"Phone: {result.get('phone')}")
            print(f"Roles: {result.get('role')}")
            print(f"Role IDs: {result.get('role_id')}")
            print(f"Permission Groups: {len(permissions)}")

            # Calculate total permissions
            total_perms = sum(len(list(pg.values())[0]) for pg in permissions if pg)
            print(f"Total Permissions: {total_perms}")

            print("\n✅ ALL CHECKS PASSED - NEW FORMAT IS WORKING!")

        elif response.status_code == 401:
            print(f"❌ UNAUTHORIZED: {response.status_code}")
            print("Token is invalid or expired. Please get a new token.")
            print(response.text)
        else:
            print(f"❌ ERROR: {response.status_code}")
            print(response.text)

    except Exception as e:
        print(f"❌ Exception: {e}")

if __name__ == "__main__":
    test_auth_me_new_format()
