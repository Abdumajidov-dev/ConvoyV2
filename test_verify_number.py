#!/usr/bin/env python3
"""
Test verify_number endpoint with both encrypted and plain requests
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

print("=" * 70)
print("TEST 1: PLAIN JSON REQUEST (Encryption disabled in Flutter)")
print("=" * 70)

# Plain JSON request
plain_data = {"phone_number": "916714835"}

try:
    response = requests.post(
        f"{BASE_URL}/verify_number",
        json=plain_data,
        headers={"Content-Type": "application/json"}
    )
    print(f"[STATUS] {response.status_code}")
    print(f"[RESPONSE] {response.text}")
except Exception as e:
    print(f"[ERROR] {e}")

print("\n" + "=" * 70)
print("TEST 2: ENCRYPTED REQUEST (Encryption enabled)")
print("=" * 70)

# Encrypt the data
plain_text = json.dumps(plain_data)
iv = get_random_bytes(16)
cipher = AES.new(key, AES.MODE_CBC, iv)
encrypted_bytes = cipher.encrypt(pad(plain_text.encode('utf-8'), AES.block_size))
combined = iv + encrypted_bytes
encrypted_urlsafe = base64.urlsafe_b64encode(combined).decode()

print(f"[PLAIN] {plain_text}")
print(f"[ENCRYPTED] {encrypted_urlsafe[:50]}...")

try:
    response = requests.post(
        f"{BASE_URL}/verify_number",
        data=encrypted_urlsafe,
        headers={"Content-Type": "text/plain"}
    )
    print(f"[STATUS] {response.status_code}")
    print(f"[RESPONSE] {response.text[:200]}...")

    # Decrypt response if encrypted
    if response.headers.get('content-type') == 'text/plain':
        print("\n[DECRYPTING RESPONSE]")
        encrypted_response = response.text.strip()
        combined_decoded = base64.urlsafe_b64decode(encrypted_response)
        iv_resp = combined_decoded[:16]
        encrypted_resp = combined_decoded[16:]

        cipher2 = AES.new(key, AES.MODE_CBC, iv_resp)
        from Crypto.Util.Padding import unpad
        decrypted = unpad(cipher2.decrypt(encrypted_resp), AES.block_size).decode('utf-8')
        print(f"[DECRYPTED] {decrypted}")

except Exception as e:
    print(f"[ERROR] {e}")

print("\n" + "=" * 70)
print("TEST 3: CURL COMMANDS")
print("=" * 70)
print("# Plain JSON:")
print(f'curl -X POST {BASE_URL}/verify_number \\')
print(f'  -H "Content-Type: application/json" \\')
print(f'  -d \'{json.dumps(plain_data)}\'')
print("\n# Encrypted:")
print(f'curl -X POST {BASE_URL}/verify_number \\')
print(f'  -H "Content-Type: text/plain" \\')
print(f'  --data-raw "{encrypted_urlsafe}"')
