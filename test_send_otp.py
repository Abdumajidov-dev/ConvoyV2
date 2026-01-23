#!/usr/bin/env python3
"""
Test send_otp endpoint with plain JSON request
"""
import requests
import json

BASE_URL = "http://localhost:5084/api/auth"

print("=" * 70)
print("TEST: SEND OTP - PLAIN JSON REQUEST")
print("=" * 70)

# Plain JSON request
plain_data = {"phone_number": "916714835"}

try:
    response = requests.post(
        f"{BASE_URL}/send_otp",
        json=plain_data,
        headers={"Content-Type": "application/json"}
    )
    print(f"[STATUS] {response.status_code}")
    print(f"[RESPONSE] {response.text}")

    if response.status_code == 200:
        print("\n✅ OTP successfully sent!")
        response_json = response.json()
        if response_json.get("status"):
            print(f"Message: {response_json.get('message')}")
        else:
            print(f"Error: {response_json.get('message')}")
    else:
        print("\n❌ Failed to send OTP")

except Exception as e:
    print(f"[ERROR] {e}")

print("\n" + "=" * 70)
print("CURL COMMAND:")
print("=" * 70)
print(f'curl -X POST {BASE_URL}/send_otp \\')
print(f'  -H "Content-Type: application/json" \\')
print(f'  -d \'{json.dumps(plain_data)}\'')
