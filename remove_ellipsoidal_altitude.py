#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re
import sys
sys.stdout.reconfigure(encoding='utf-8')

# Remove from Location.cs entity
with open('Convoy.Domain/Entities/Location.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the property and its comment
pattern = r'    /// <summary>\s+/// Altitude above WGS84 reference ellipsoid \(meters\)\s+/// </summary>\s+public decimal\? EllipsoidalAltitude \{ get; set; \}\s+'
content = re.sub(pattern, '', content)

with open('Convoy.Domain/Entities/Location.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("[OK] Removed EllipsoidalAltitude from Location.cs")

# Remove from LocationDtos.cs
with open('Convoy.Service/DTOs/LocationDtos.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove all references
content = content.replace('ellipsoidal_altitude', '')
content = content.replace('EllipsoidalAltitude', '')

with open('Convoy.Service/DTOs/LocationDtos.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("[OK] Removed ellipsoidal_altitude from LocationDtos.cs")

# Remove from LocationService.cs
with open('Convoy.Service/Services/LocationService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the property assignment
pattern = r'                // ellipsoidal_altitude: max 9999\.999999 \(DECIMAL\(10,6\)\)\s+EllipsoidalAltitude = locationData\.EllipsoidalAltitude\.HasValue && locationData\.EllipsoidalAltitude\.Value > 9999\.999999m\s+\? 9999\.999999m\s+: locationData\.EllipsoidalAltitude,\s+'
content = re.sub(pattern, '', content)

with open('Convoy.Service/Services/LocationService.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("[OK] Removed EllipsoidalAltitude from LocationService.cs")
