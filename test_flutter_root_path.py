#!/usr/bin/env python3
"""
Test Flutter format with ROOT PATH (/) like Flutter does
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
print("FLUTTER ROOT PATH TEST - options.path = '/'")
print("=" * 80)

# Real endpoint (will be encrypted)
real_endpoint = "/api/auth/verify_number"
encrypted_endpoint = encrypt_data(real_endpoint)

print(f"\n[ENDPOINT INFO]")
print(f"  Real endpoint: {real_endpoint}")
print(f"  Encrypted endpoint: {encrypted_endpoint[:60]}...")
print(f"  Flutter sends to: / (root path)")
print(f"  Backend will decrypt and route to: {real_endpoint}")

# Body data
body_data = {"phone_number": "916714835"}
encrypted_body = encrypt_data(json.dumps(body_data))

# Headers data (Flutter format)
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

print("\n" + "=" * 80)
print("SENDING REQUEST TO ROOT PATH")
print("=" * 80)
print(f"  URL: {BASE_URL}/")
print(f"  encrypted-endpoint header contains: {real_endpoint}")

try:
    response = requests.post(
        f"{BASE_URL}/",  # Root path like Flutter
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
        print("  Backend successfully:")
        print("    1. Decrypted endpoint from encrypted-endpoint header")
        print("    2. Changed path from / to /api/auth/verify_number")
        print("    3. Routed to correct controller")
    elif response.status_code == 404:
        print("\n[FAILED] 404 Not Found")
        print("  This means endpoint decryption or routing failed")
        print("  Check backend logs for details")
    else:
        print(f"\n[INFO] Status: {response.status_code}")
        print(f"  Response: {response.text[:200]}")

except requests.exceptions.ConnectionError:
    print("\n[ERROR] Cannot connect to backend!")
    print("  Make sure server is running on port 5084")
except Exception as e:
    print(f"\n[ERROR] {e}")

print("\n" + "=" * 80)
print("FLUTTER CODE PATTERN")
print("=" * 80)
print("""
// Flutter tarafda:
String originalPath = "/api/auth/verify_number";
final encryptedEndpoint = encrypt(originalPath);

options.path = '/';  // Fake root path
options.headers['encrypted-endpoint'] = encryptedEndpoint;
options.headers['encrypted-headers'] = encryptedHeaders;

// Backend avtomatik:
// 1. Decrypt encrypted-endpoint → "/api/auth/verify_number"
// 2. Change path from "/" to "/api/auth/verify_number"
// 3. Route to AuthController.VerifyNumber
""")
