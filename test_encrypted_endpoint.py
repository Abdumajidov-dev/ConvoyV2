#!/usr/bin/env python3
"""
Test encrypted-endpoint (endpoint URL ham shifrlangan)
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
print("ENCRYPTED ENDPOINT TEST")
print("=" * 80)

# Haqiqiy endpoint (shifrlangan bo'ladi)
real_endpoint = "/api/auth/verify_number"
encrypted_endpoint = encrypt_data(real_endpoint)

print(f"\n[ENDPOINT]")
print(f"  Real endpoint: {real_endpoint}")
print(f"  Encrypted (first 60 chars): {encrypted_endpoint[:60]}...")

# Body data
body_data = {"phone_number": "916714835"}
encrypted_body = encrypt_data(json.dumps(body_data))

# Headers data
headers_data = {
    "device-info": json.dumps({"model": "Python Test", "os": "Test OS"}),
    "Authorization": "Bearer test_token_123",
    "device_type": "Python"
}
encrypted_headers = encrypt_data(json.dumps(headers_data))

print(f"\n[REQUEST INFO]")
print(f"  Actual URL: {BASE_URL}/api/x  (fake endpoint)")
print(f"  encrypted-endpoint header: {encrypted_endpoint[:40]}...")
print(f"  Real endpoint will be: {real_endpoint}")

print("\n" + "=" * 80)
print("SENDING REQUEST TO FAKE ENDPOINT")
print("=" * 80)

try:
    # Request fake endpoint ga yuboriladi, lekin backend real endpoint'ga yo'naltiradi
    response = requests.post(
        f"{BASE_URL}/api/x",  # Fake endpoint (bu yerga hech qachon kelmaydi)
        data=encrypted_body,
        headers={
            "Content-Type": "text/plain",
            "encrypted-endpoint": encrypted_endpoint,  # Haqiqiy endpoint shifrlangan
            "encrypted-headers": encrypted_headers,
        },
        timeout=10
    )

    print(f"\n[RESPONSE]")
    print(f"  Status Code: {response.status_code}")
    print(f"  Content-Type: {response.headers.get('content-type')}")

    if response.status_code == 200:
        print("\n[SUCCESS] Endpoint decryption and routing worked!")
        print("  Backend successfully:")
        print("    1. Decrypted the endpoint from encrypted-endpoint header")
        print("    2. Changed path from /api/x to /api/auth/verify_number")
        print("    3. Routed to correct controller action")
    elif response.status_code == 404:
        print("\n[FAILED] 404 Not Found")
        print("  This means endpoint decryption failed or path change didn't work")
    elif response.status_code == 415:
        print("\n[FAILED] 415 Unsupported Media Type")
        print("  Endpoint worked but content-type issue")
    else:
        print(f"\n[INFO] Status: {response.status_code}")

except requests.exceptions.ConnectionError:
    print("\n[ERROR] Cannot connect to backend!")
except Exception as e:
    print(f"\n[ERROR] {e}")

print("\n" + "=" * 80)
print("EXPECTED BACKEND LOGS")
print("=" * 80)
print("""
[SUCCESS] Kutilgan log'lar:
  [Encryption] Attempting to decrypt endpoint. Length: XXX
  [Encryption] Endpoint decrypted: /api/auth/verify_number
  [Encryption] Changing path from /api/x to /api/auth/verify_number
  [Encryption] Path changed successfully to: /api/auth/verify_number
  [Encryption] Attempting to decrypt bulk headers...
  [Encryption] Bulk headers decrypted successfully
  [Encryption] Attempting to decrypt request...
  [Encryption] Request decrypted successfully
  [Encryption] Content-Type changed to application/json
  [AuthController] Successfully verified phone...

[FAILED] Agar xato bo'lsa:
  [ERROR] Failed to decrypt endpoint
  OR
  [WARN] Could not get IHttpRequestFeature to change path
  (Bu degani path o'zgartirib bo'lmadi)
""")

print("\n" + "=" * 80)
print("SECURITY BENEFITS")
print("=" * 80)
print("""
[XAVFSIZLIK]
1. Endpoint URL ham shifrlangan - hacker qaysi endpoint'ga request
   yuborilayotganini bilmaydi
2. Header'lar shifrlangan - qanday ma'lumot yuborilayotgani yashirin
3. Body shifrlangan - request data himoyalangan
4. Har request uchun random IV - bir xil data har safar har xil ko'rinadi

[FLUTTER UCHUN]
Flutter har doim /api/x ga request yuboradi, lekin:
- encrypted-endpoint: shifrlangan haqiqiy endpoint
- encrypted-headers: shifrlangan barcha header'lar
- body: shifrlangan request data

Backend avtomatik:
1. Endpoint'ni decrypt qiladi
2. Path'ni o'zgartiradi
3. Header'larni decrypt qiladi
4. Body'ni decrypt qiladi
5. To'g'ri controller'ga yo'naltiradi
""")
