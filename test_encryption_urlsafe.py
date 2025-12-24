#!/usr/bin/env python3
"""
Test encryption with URL-safe Base64 encoding
"""
import base64
import json
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from Crypto.Random import get_random_bytes

# Same key as backend
KEY_BASE64 = "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
key = base64.b64decode(KEY_BASE64)

# Test data
test_data = {"phone_number": "916714835"}
plain_text = json.dumps(test_data)

print("=" * 60)
print("ENCRYPTION TEST (URL-SAFE BASE64)")
print("=" * 60)

# Generate random IV
iv = get_random_bytes(16)

# Encrypt
cipher = AES.new(key, AES.MODE_CBC, iv)
encrypted_bytes = cipher.encrypt(pad(plain_text.encode('utf-8'), AES.block_size))

# Combine IV + encrypted data
combined = iv + encrypted_bytes

# ✅ URL-SAFE Base64 encoding (+ becomes -, / becomes _)
encrypted_urlsafe = base64.urlsafe_b64encode(combined).decode()

# ❌ STANDARD Base64 (for comparison)
encrypted_standard = base64.b64encode(combined).decode()

print(f"[PLAIN] Plain text: {plain_text}")
print(f"\n[STANDARD] Standard Base64: {encrypted_standard}")
print(f"[URL-SAFE] URL-safe Base64: {encrypted_urlsafe}")
print(f"\n[DIFF] Has + or /? Standard: {('+' in encrypted_standard or '/' in encrypted_standard)}, URL-safe: {('+' in encrypted_urlsafe or '/' in encrypted_urlsafe)}")

print("\n" + "=" * 60)
print("CURL COMMAND (URL-SAFE)")
print("=" * 60)
print(f'curl -X POST http://localhost:5084/api/encryption-test/decrypt \\')
print(f'  -H "Content-Type: text/plain" \\')
print(f'  --data-raw "{encrypted_urlsafe}"')

print("\n" + "=" * 60)
print("DECRYPTION TEST")
print("=" * 60)

# Decrypt from URL-safe
combined_decoded = base64.urlsafe_b64decode(encrypted_urlsafe)
iv_extracted = combined_decoded[:16]
encrypted_extracted = combined_decoded[16:]

cipher2 = AES.new(key, AES.MODE_CBC, iv_extracted)
decrypted = cipher2.decrypt(encrypted_extracted)

from Crypto.Util.Padding import unpad
decrypted_text = unpad(decrypted, AES.block_size).decode('utf-8')

print(f"[DECRYPTED] Decrypted: {decrypted_text}")
print(f"[MATCH] Match: {decrypted_text == plain_text}")
