#!/usr/bin/env python3
"""
FULL ENCRYPTION TEST
Tests both header and body encryption with the backend
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

def decrypt_data(encrypted_urlsafe):
    """Decrypt URL-safe Base64 encoded data"""
    combined = base64.urlsafe_b64decode(encrypted_urlsafe)
    iv = combined[:16]
    encrypted_bytes = combined[16:]

    cipher = AES.new(key, AES.MODE_CBC, iv)
    from Crypto.Util.Padding import unpad
    decrypted = unpad(cipher.decrypt(encrypted_bytes), AES.block_size)
    return decrypted.decode('utf-8')

print("=" * 80)
print("FULL ENCRYPTION TEST - HEADERS + BODY")
print("=" * 80)

# Test data
phone_number = "916714835"
device_info = {"model": "Python Test", "os": "Test OS", "version": "1.0"}

# Encrypt body
body_data = {"phone_number": phone_number}
encrypted_body = encrypt_data(json.dumps(body_data))

# Encrypt headers
encrypted_device_info = encrypt_data(json.dumps(device_info))
fake_token = "test_jwt_token_12345"
encrypted_token = encrypt_data(fake_token)

print("\n[ORIGINAL DATA]")
print(f"  Body: {body_data}")
print(f"  Device Info: {device_info}")
print(f"  Token: {fake_token}")

print("\n[ENCRYPTED DATA]")
print(f"  Body (first 50 chars): {encrypted_body[:50]}...")
print(f"  Device Info (first 50 chars): {encrypted_device_info[:50]}...")
print(f"  Token (first 50 chars): {encrypted_token[:50]}...")

print("\n" + "=" * 80)
print("SENDING REQUEST TO BACKEND")
print("=" * 80)

headers = {
    "Content-Type": "text/plain",
    "device-info": encrypted_device_info,
    "device_type": "Python",  # NOT encrypted
    "Authorization": f"Bearer {encrypted_token}",  # Encrypted token with Bearer prefix
}

try:
    response = requests.post(
        f"{BASE_URL}/verify_number",
        data=encrypted_body,
        headers=headers,
        timeout=10
    )

    print(f"\n[RESPONSE]")
    print(f"  Status Code: {response.status_code}")
    print(f"  Content-Type: {response.headers.get('content-type')}")
    print(f"  Response Length: {len(response.text)} bytes")

    if response.status_code == 200:
        print("\n✅ SUCCESS! Request was processed")
    elif response.status_code == 415:
        print("\n❌ FAILED! 415 Unsupported Media Type")
        print("   This means Content-Type issue still exists")
    else:
        print(f"\n⚠️  Unexpected status code: {response.status_code}")

    # Try to decrypt response if it's encrypted
    if response.headers.get('content-type') == 'text/plain':
        print("\n[DECRYPTING RESPONSE]")
        try:
            decrypted_response = decrypt_data(response.text.strip())
            print(f"  Decrypted Response: {decrypted_response}")
        except Exception as e:
            print(f"  ❌ Failed to decrypt response: {e}")
    else:
        print(f"\n[PLAIN RESPONSE]")
        print(f"  {response.text[:200]}...")

except requests.exceptions.ConnectionError:
    print("\n❌ ERROR: Cannot connect to backend!")
    print("   Make sure the server is running: dotnet run")
except Exception as e:
    print(f"\n❌ ERROR: {e}")

print("\n" + "=" * 80)
print("WHAT TO LOOK FOR IN BACKEND LOGS")
print("=" * 80)
print("""
✅ HEADER DECRYPTION (should see these):
   🔐 Attempting to decrypt header: device-info
   ✅ Header decrypted: device-info = {"model":"Python Test",...}

   🔐 Attempting to decrypt header: Authorization
   ✅ Header decrypted: Authorization = test_jwt_token_12345

✅ BODY DECRYPTION (should see these):
   🔐 Attempting to decrypt request for /api/auth/verify_number
   ✅ Request decrypted successfully for /api/auth/verify_number
   ✅ Content-Type changed to application/json

❌ IF YOU SEE THIS:
   ⚠️ Failed to decrypt header: device-info
   (This means header encryption format is wrong)

   Failed to decrypt request (This means body encryption format is wrong)

   415 Unsupported Media Type (Content-Type not changed to JSON)
""")

print("\n" + "=" * 80)
print("CURL COMMAND (for manual testing)")
print("=" * 80)
print(f'curl -X POST {BASE_URL}/verify_number \\')
print(f'  -H "Content-Type: text/plain" \\')
print(f'  -H "device-info: {encrypted_device_info[:50]}..." \\')
print(f'  -H "device_type: Python" \\')
print(f'  -H "Authorization: Bearer {encrypted_token[:50]}..." \\')
print(f'  --data-raw "{encrypted_body[:50]}..." \\')
print(f'  -v')
