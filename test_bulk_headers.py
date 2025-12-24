#!/usr/bin/env python3
"""
Test encrypted-headers format (bulk headers in one encrypted value)
"""
import requests
import json
import base64
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from Crypto.Random import get_random_bytes

BASE_URL = "http://localhost:5084/api/auth"
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
print("BULK HEADERS TEST (encrypted-headers format)")
print("=" * 80)

# Body data
body_data = {"phone_number": "916714835"}
encrypted_body = encrypt_data(json.dumps(body_data))

# Headers data (barcha header'lar bitta JSON'da)
headers_data = {
    "device-info": json.dumps({"model": "Python Test", "os": "Test OS", "version": "1.0"}),
    "Authorization": "Bearer test_jwt_token_12345",
    "device_type": "Python"
}

# Header'larni shifrlash
encrypted_headers = encrypt_data(json.dumps(headers_data))

print("\n[HEADERS JSON]")
print(json.dumps(headers_data, indent=2))

print("\n[ENCRYPTED]")
print(f"  encrypted-headers (first 80 chars): {encrypted_headers[:80]}...")
print(f"  Body (first 50 chars): {encrypted_body[:50]}...")

print("\n" + "=" * 80)
print("SENDING REQUEST")
print("=" * 80)

try:
    response = requests.post(
        f"{BASE_URL}/verify_number",
        data=encrypted_body,
        headers={
            "Content-Type": "text/plain",
            "encrypted-headers": encrypted_headers,  # Bitta shifrlangan header
        },
        timeout=10
    )

    print(f"\n[RESPONSE]")
    print(f"  Status Code: {response.status_code}")
    print(f"  Content-Type: {response.headers.get('content-type')}")

    if response.status_code == 200:
        print("\n[SUCCESS] Request processed successfully!")
    elif response.status_code == 415:
        print("\n[FAILED] 415 Unsupported Media Type")
    else:
        print(f"\n[INFO] Status: {response.status_code}")

except requests.exceptions.ConnectionError:
    print("\n[ERROR] Cannot connect to backend!")
    print("   Make sure server is running: dotnet run")
except Exception as e:
    print(f"\n[ERROR] {e}")

print("\n" + "=" * 80)
print("EXPECTED BACKEND LOGS")
print("=" * 80)
print("""
[SUCCESS] Kutilgan log'lar:
  🔐 Attempting to decrypt bulk headers. Length: XXX
  ✅ Bulk headers decrypted successfully
  📦 Found 3 headers in encrypted-headers
    ✅ Added header: device-info = {"model":"Python Test",...}
    ✅ Added header: Authorization = Bearer test_jwt_token_12345
    ✅ Added header: device_type = Python
  🔐 Attempting to decrypt request for /api/auth/verify_number
  ✅ Request decrypted successfully
  ✅ Content-Type changed to application/json

[FAILED] Agar xato bo'lsa:
  ❌ Failed to decrypt bulk headers
  (Bu degani format noto'g'ri yoki key mos kelmayapti)
""")
