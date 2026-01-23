#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re

# 1. Remove from Location.cs entity
print("Processing Location.cs...")
with open('Convoy.Domain/Entities/Location.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the entire property block
pattern = r'    /// <summary>\r?\n    /// Altitude above WGS84 reference ellipsoid \(meters\)\r?\n    /// </summary>\r?\n    public decimal\? EllipsoidalAltitude \{ get; set; \}\r?\n\r?\n'
content = re.sub(pattern, '', content)

with open('Convoy.Domain/Entities/Location.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] Location.cs")

# 2. Remove from LocationDtos.cs
print("Processing LocationDtos.cs...")
with open('Convoy.Service/DTOs/LocationDtos.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove property blocks (all 3 occurrences in DTOs)
pattern = r'    \[JsonPropertyName\("ellipsoidal_altitude"\)\]\r?\n    public decimal\? EllipsoidalAltitude \{ get; set; \}\r?\n\r?\n'
content = re.sub(pattern, '', content)

with open('Convoy.Service/DTOs/LocationDtos.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationDtos.cs")

# 3. Remove from LocationService.cs
print("Processing LocationService.cs...")
with open('Convoy.Service/Services/LocationService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the property assignment block
pattern = r'                // ellipsoidal_altitude: max 9999\.999999 \(DECIMAL\(10,6\)\)\r?\n                EllipsoidalAltitude = locationData\.EllipsoidalAltitude\.HasValue && locationData\.EllipsoidalAltitude\.Value > 9999\.999999m\r?\n                    \? 9999\.999999m\r?\n                    : locationData\.EllipsoidalAltitude,\r?\n\r?\n'
content = re.sub(pattern, '', content)

with open('Convoy.Service/Services/LocationService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationService.cs")

print("\n[SUCCESS] All files cleaned!")
