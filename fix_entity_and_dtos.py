#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Fix Location entity and DTOs to match database schema
Keep only: id, user_id, recorded_at, latitude, longitude, accuracy, speed, heading, altitude,
           activity_type, activity_confidence, is_moving, battery_level, is_charging,
           distance_from_previous, created_at
"""
import re

print("Fixing Location entity and DTOs to match database...")

# 1. Fix Location.cs entity
print("\n1. Processing Location.cs...")
with open('Convoy.Domain/Entities/Location.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove all extended properties (from line 59 to line 163)
# Find the end of Altitude property and the start of Activity section
pattern = r'(    public decimal\? Altitude \{ get; set; \})\s+// ============================\s+// Flutter Background Geolocation - Extended Coords\s+// ============================.*?// ============================\s+// Activity\s+// ============================'
replacement = r'\1\n\n    // ============================\n    // Activity\n    // ============================'

content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Remove Location Metadata section
pattern = r'    // ============================\s+// Flutter Background Geolocation - Location Metadata\s+// ============================.*?// ============================\s+// Calculated Fields\s+// ============================'
replacement = r'    // ============================\n    // Calculated Fields\n    // ============================'

content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open('Convoy.Domain/Entities/Location.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] Location.cs")

# 2. Fix LocationDtos.cs
print("\n2. Processing LocationDtos.cs...")
with open('Convoy.Service/DTOs/LocationDtos.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove extended properties from CreateLocationRequestDto
pattern = r'    // Flutter Background Geolocation - Extended Coords \(OPTIONAL\).*?    \[JsonPropertyName\("altitude"\)\]\s+public decimal\? Altitude \{ get; set; \}'
replacement = r'    [JsonPropertyName("altitude")]\n    public decimal? Altitude { get; set; }'
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Remove Location Metadata section from CreateLocationRequestDto
pattern = r'    // Flutter Background Geolocation - Location Metadata \(OPTIONAL\).*?    \[JsonPropertyName\("is_charging"\)\]\s+public bool\? IsCharging \{ get; set; \}'
replacement = r'    [JsonPropertyName("is_charging")]\n    public bool? IsCharging { get; set; }'
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open('Convoy.Service/DTOs/LocationDtos.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationDtos.cs")

# 3. Fix LocationService.cs
print("\n3. Processing LocationService.cs...")
with open('Convoy.Service/Services/LocationService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove all extended property assignments
pattern = r'                // Flutter Background Geolocation - Extended Coords \(OPTIONAL\) - with validation.*?                Altitude = locationData\.Altitude\.HasValue && locationData\.Altitude\.Value > 999999\.99m\s+\? 999999\.99m\s+: locationData\.Altitude,'
replacement = r'                Altitude = locationData.Altitude.HasValue && locationData.Altitude.Value > 999999.99m\n                    ? 999999.99m\n                    : locationData.Altitude,'
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Remove Location Metadata assignments
pattern = r'                // Flutter Background Geolocation - Location Metadata \(OPTIONAL\).*?                IsCharging = locationData\.IsCharging,'
replacement = r'                IsCharging = locationData.IsCharging,'
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open('Convoy.Service/Services/LocationService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationService.cs")

print("\n[SUCCESS] All files fixed to match database schema!")
print("Kept only: Core fields + Activity + Battery + Calculated fields")
