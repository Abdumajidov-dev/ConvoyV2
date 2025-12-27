import requests
import json

# Your real token
token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1Mjc3IiwidW5pcXVlX25hbWUiOiLQkNC90LLQsNGA0YXQvtC9INCc0YPRgNC-0YLRhdC-0L3QvtCyIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiI5MTY3MTQ4MzUiLCJ3b3JrZXJfZ3VpZCI6IjM3MWY3ZjAwLTRmMTAtMTFlZi1iYzQxLTAwMGMyOTQxNzkyYSIsImJyYW5jaF9ndWlkIjoiZTc0MGIxYmQtM2MxYS0xMWViLTk2NDItMTgzMWJmYjc4NTBjIiwiYnJhbmNoX25hbWUiOiLQpNCw0YDQs9C-0L3QsCIsInBvc2l0aW9uX2lkIjoiODYiLCJqdGkiOiJlOTZjMDc0NC0wODkxLTQwZWYtYWJkZS03YmIwOTNmMTlmYjciLCJuYmYiOjE3NjY3Mjk3NzcsImV4cCI6MTc2NjgxNjE3NywiaWF0IjoxNzY2NzI5Nzc3LCJpc3MiOiJDb252b3lBcGkiLCJhdWQiOiJDb252b3lDbGllbnRzIn0.ILxRTB2mF195g7c3lDxfKShLYryZA75Y-MoI67U1DCw"

# API endpoint
base_url = "http://localhost:5084"  # Change this if your API runs on different port
endpoint = f"{base_url}/api/auth/me"

print("=" * 70)
print("Testing /api/auth/me endpoint")
print("=" * 70)

# Test 1: Without Authorization header
print("\nTest 1: WITHOUT Authorization header:")
print("-" * 70)
response = requests.get(endpoint)
print(f"Status Code: {response.status_code}")
print(f"Response: {response.text}")

# Test 2: With Authorization header (Bearer token)
print("\nTest 2: WITH Authorization: Bearer {token}:")
print("-" * 70)
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}
response = requests.get(endpoint, headers=headers)
print(f"Status Code: {response.status_code}")
print(f"Response Headers:")
for key, value in response.headers.items():
    print(f"  {key}: {value}")
print(f"\nResponse Body:")
try:
    print(json.dumps(response.json(), indent=2, ensure_ascii=False))
except:
    print(response.text)

# Test 3: Decode JWT token to see claims
print("\nTest 3: JWT Token Claims (Base64 decoded payload):")
print("-" * 70)
import base64
parts = token.split('.')
if len(parts) == 3:
    # Decode payload (add padding if needed)
    payload = parts[1]
    padding = 4 - len(payload) % 4
    if padding != 4:
        payload += '=' * padding

    decoded = base64.b64decode(payload)
    claims = json.loads(decoded)
    print(json.dumps(claims, indent=2, ensure_ascii=False))

print("\n" + "=" * 70)
print("Test completed!")
print("=" * 70)
