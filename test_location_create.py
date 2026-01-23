#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test location creation with valid user_id
"""
import requests
import json
from datetime import datetime

BASE_URL = "http://localhost:5084/api/locations"

# Test location data - minimal required fields only
location_data = {
    "user_id": 1,  # User exists in database
    "recorded_at": datetime.utcnow().isoformat() + "Z",
    "latitude": 41.2995,
    "longitude": 69.2401,
    "accuracy": 10.5,
    "speed": 5.0,
    "heading": 90.0,
    "altitude": 450.0,
    "activity_type": "walking",
    "activity_confidence": 95,
    "is_moving": True,
    "battery_level": 80,
    "is_charging": False
}

print("=" * 70)
print("TEST: Create Location")
print("=" * 70)
print(f"URL: {BASE_URL}")
print(f"User ID: {location_data['user_id']}")
print(f"Latitude: {location_data['latitude']}, Longitude: {location_data['longitude']}")

try:
    response = requests.post(
        BASE_URL,
        json=location_data,
        headers={"Content-Type": "application/json"}
    )

    print(f"\n[STATUS] {response.status_code}")
    print(f"[RESPONSE] {response.text}")

    if response.status_code == 200:
        print("\n✓ Location created successfully!")
        response_json = response.json()
        if response_json.get("status"):
            location = response_json.get("data", {})
            print(f"  Location ID: {location.get('id')}")
            print(f"  Distance from previous: {location.get('distance_from_previous', 0)} meters")
    else:
        print("\n✗ Failed to create location")

except Exception as e:
    print(f"\n[ERROR] {e}")

print("\n" + "=" * 70)
print("CURL COMMAND:")
print("=" * 70)
print(f'curl -X POST {BASE_URL} \\')
print(f'  -H "Content-Type: application/json" \\')
print(f'  -d \'{json.dumps(location_data, indent=2)}\'')
