#!/usr/bin/env python3
"""
Test Flutter format with nested device_info object
"""
import requests
import json
import base64
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from Crypto.Random import get_random_bytes

BASE_URL = "http://localhost:5084"
KEY_BASE64 = "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
key = base64.b64decode(KEY_BASE64)

def encrypt_data(plain_text):
    """Encrypt data with random IV (URL-safe Base64)"""
    iv = get_random_bytes(16)
    cipher = AES.new(key, AES.MODE_CBC, iv)
    encrypted_bytes = cipher.encrypt(pad(plain_text.encode('utf-8'), AES.block_size))
    combined = iv + encrypted_bytes
    return base64.urlsafe_b64encode(combined).decode()

print("=" * 80)
print("FLUTTER FORMAT TEST - Nested device_info object")
print("=" * 80)

# Real endpoint (will be encrypted)
real_endpoint = "/api/auth/verify_number"
encrypted_endpoint = encrypt_data(real_endpoint)

# Body data
body_data = {"phone_number": "916714835"}
encrypted_body = encrypt_data(json.dumps(body_data))

# Headers data (Flutter format with nested device_info)
headers_data = {
    "device_type": "Android",
    "device_info": {
        "device_system": "android",
        "device_token": None,
        "model": "SM-S908N",
        "device_id": "PQ3B.190801.10101846",
        "is_physical_device": True
    },
    "token": ""
}

encrypted_headers = encrypt_data(json.dumps(headers_data))

print("\n[HEADERS DATA]")
print(json.dumps(headers_data, indent=2))

print("\n[ENCRYPTED VALUES]")
print(f"  encrypted-endpoint: {encrypted_endpoint[:60]}...")
print(f"  encrypted-headers: {encrypted_headers[:60]}...")
print(f"  encrypted body: {encrypted_body[:60]}...")

print("\n" + "=" * 80)
print("SENDING REQUEST")
print("=" * 80)

try:
    response = requests.post(
        f"{BASE_URL}/api/x",  # Fake endpoint
        data=encrypted_body,
        headers={
            "Content-Type": "text/plain",
            "encrypted-endpoint": encrypted_endpoint,
            "encrypted-headers": encrypted_headers,
        },
        timeout=10
    )

    print(f"\n[RESPONSE]")
    print(f"  Status Code: {response.status_code}")
    print(f"  Content-Type: {response.headers.get('content-type')}")

    if response.status_code == 200:
        print("\n[SUCCESS] Request processed!")
        print("  Backend successfully handled nested device_info object")
    elif response.status_code == 400:
        print("\n[FAILED] 400 Bad Request")
        print(f"  Response: {response.text[:200]}")
    elif response.status_code == 500:
        print("\n[FAILED] 500 Internal Server Error")
        print("  Check backend logs for JSON deserialization error")
    else:
        print(f"\n[INFO] Unexpected status: {response.status_code}")

except requests.exceptions.ConnectionError:
    print("\n[ERROR] Cannot connect to backend!")
except Exception as e:
    print(f"\n[ERROR] {e}")

print("\n" + "=" * 80)
print("EXPECTED BACKEND BEHAVIOR")
print("=" * 80)
print("""
[SUCCESS] Backend should:
  1. Decrypt endpoint from encrypted-endpoint header
  2. Change path from /api/x to /api/auth/verify_number
  3. Decrypt encrypted-headers
  4. Parse JSON with nested device_info object
  5. Add headers to request:
     - device_type: "Android" (string)
     - device_info: "{\"device_system\":\"android\",...}" (JSON string)
     - token: "" (empty string)
  6. Decrypt request body
  7. Route to AuthController.VerifyNumber

[WHAT WAS FIXED]
  Changed from: Dictionary<string, string>
  Changed to:   Dictionary<string, JsonElement>

  This allows nested objects in headers like device_info.
""")
