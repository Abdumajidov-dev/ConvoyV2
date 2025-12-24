#!/usr/bin/env python3
"""
Test encryption with random IV (same as Flutter implementation)
"""
import base64
import json
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from Crypto.Random import get_random_bytes

# Same key as backend (from appsettings.json)
KEY_BASE64 = "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
key = base64.b64decode(KEY_BASE64)

# Test data (same as Flutter would send)
test_data = {"phone_number": "916714835"}
plain_text = json.dumps(test_data)

print("=" * 60)
print("ENCRYPTION TEST (Random IV like Flutter)")
print("=" * 60)

# Generate random IV (16 bytes)
iv = get_random_bytes(16)
print(f"[OK] Generated random IV: {base64.b64encode(iv).decode()}")

# Encrypt using AES-256-CBC with PKCS7 padding
cipher = AES.new(key, AES.MODE_CBC, iv)
encrypted_bytes = cipher.encrypt(pad(plain_text.encode('utf-8'), AES.block_size))

# Combine IV + encrypted data (same as Flutter)
combined = iv + encrypted_bytes

# Base64 encode
encrypted_base64 = base64.b64encode(combined).decode()

print(f"[PLAIN] Plain text: {plain_text}")
print(f"[ENCRYPTED] Encrypted (Base64): {encrypted_base64}")
print(f"[LENGTH] Length: {len(encrypted_base64)} chars")
print(f"[BYTES] Combined bytes: IV({len(iv)}) + Data({len(encrypted_bytes)}) = {len(combined)}")

print("\n" + "=" * 60)
print("DECRYPTION TEST (Backend should do this)")
print("=" * 60)

# Decrypt to verify
combined_decoded = base64.b64decode(encrypted_base64)
iv_extracted = combined_decoded[:16]
encrypted_extracted = combined_decoded[16:]

cipher2 = AES.new(key, AES.MODE_CBC, iv_extracted)
decrypted = cipher2.decrypt(encrypted_extracted)

# Remove padding
from Crypto.Util.Padding import unpad
decrypted_text = unpad(decrypted, AES.block_size).decode('utf-8')

print(f"[DECRYPTED] Decrypted: {decrypted_text}")
print(f"[MATCH] Match: {decrypted_text == plain_text}")

print("\n" + "=" * 60)
print("CURL COMMAND TO TEST BACKEND")
print("=" * 60)
print(f'curl -X POST http://localhost:5084/api/encryption-test/decrypt \\')
print(f'  -H "Content-Type: text/plain" \\')
print(f'  -d "{encrypted_base64}"')
