#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re

print("Removing extended columns: heading_accuracy, speed_accuracy, altitude_accuracy, floor")

# Columns to remove
columns_to_remove = [
    'heading_accuracy',
    'speed_accuracy',
    'altitude_accuracy',
    'floor'
]

# 1. Remove from LocationRepository.cs
print("\n1. Processing LocationRepository.cs...")
with open('Convoy.Data/Repositories/LocationRepository.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove from column lists in SQL (with comma before)
for col in columns_to_remove:
    content = re.sub(rf'\s+{col},', '', content)
    content = re.sub(rf',\s+{col}', '', content)

# Remove from aliases (e.g., "heading_accuracy as HeadingAccuracy,")
for col in columns_to_remove:
    pascal_case = ''.join(word.capitalize() for word in col.split('_'))
    content = re.sub(rf'\s+{col} as {pascal_case},', '', content)
    content = re.sub(rf',\s+{col} as {pascal_case}', '', content)

# Remove parameter references (e.g., "@HeadingAccuracy, ")
for col in columns_to_remove:
    pascal_case = ''.join(word.capitalize() for word in col.split('_'))
    content = re.sub(rf'\s+@{pascal_case},', '', content)
    content = re.sub(rf',\s+@{pascal_case}', '', content)

with open('Convoy.Data/Repositories/LocationRepository.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationRepository.cs")

# 2. Remove from Location.cs entity
print("\n2. Processing Location.cs...")
with open('Convoy.Domain/Entities/Location.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove properties with their comments
property_patterns = [
    r'    /// <summary>\r?\n    /// Heading accuracy in degrees\r?\n    /// </summary>\r?\n    public decimal\? HeadingAccuracy \{ get; set; \}\r?\n\r?\n',
    r'    /// <summary>\r?\n    /// Speed accuracy in meters/second\r?\n    /// </summary>\r?\n    public decimal\? SpeedAccuracy \{ get; set; \}\r?\n\r?\n',
    r'    /// <summary>\r?\n    /// Altitude accuracy in meters\r?\n    /// </summary>\r?\n    public decimal\? AltitudeAccuracy \{ get; set; \}\r?\n\r?\n',
    r'    /// <summary>\r?\n    /// Floor within a building \(iOS only\)\r?\n    /// </summary>\r?\n    public int\? Floor \{ get; set; \}\r?\n\r?\n'
]

for pattern in property_patterns:
    content = re.sub(pattern, '', content)

with open('Convoy.Domain/Entities/Location.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] Location.cs")

# 3. Remove from LocationDtos.cs
print("\n3. Processing LocationDtos.cs...")
with open('Convoy.Service/DTOs/LocationDtos.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove JSON properties
json_patterns = [
    r'    \[JsonPropertyName\("heading_accuracy"\)\]\r?\n    public decimal\? HeadingAccuracy \{ get; set; \}\r?\n\r?\n',
    r'    \[JsonPropertyName\("speed_accuracy"\)\]\r?\n    public decimal\? SpeedAccuracy \{ get; set; \}\r?\n\r?\n',
    r'    \[JsonPropertyName\("altitude_accuracy"\)\]\r?\n    public decimal\? AltitudeAccuracy \{ get; set; \}\r?\n\r?\n',
    r'    \[JsonPropertyName\("floor"\)\]\r?\n    public int\? Floor \{ get; set; \}\r?\n\r?\n'
]

for pattern in json_patterns:
    content = re.sub(pattern, '', content)

with open('Convoy.Service/DTOs/LocationDtos.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationDtos.cs")

# 4. Remove from LocationService.cs
print("\n4. Processing LocationService.cs...")
with open('Convoy.Service/Services/LocationService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove property assignments with comments
service_patterns = [
    r'                // heading_accuracy: max 9999\.999999 \(DECIMAL\(10,6\)\)\r?\n                HeadingAccuracy = locationData\.HeadingAccuracy\.HasValue && locationData\.HeadingAccuracy\.Value > 9999\.999999m\r?\n                    \? 9999\.999999m\r?\n                    : locationData\.HeadingAccuracy,\r?\n\r?\n',
    r'                // speed_accuracy: max 9999\.999999 \(DECIMAL\(10,6\)\)\r?\n                SpeedAccuracy = locationData\.SpeedAccuracy\.HasValue && locationData\.SpeedAccuracy\.Value > 9999\.999999m\r?\n                    \? 9999\.999999m\r?\n                    : locationData\.SpeedAccuracy,\r?\n\r?\n',
    r'                // altitude_accuracy: max 9999\.999999 \(DECIMAL\(10,6\)\)\r?\n                AltitudeAccuracy = locationData\.AltitudeAccuracy\.HasValue && locationData\.AltitudeAccuracy\.Value > 9999\.999999m\r?\n                    \? 9999\.999999m\r?\n                    : locationData\.AltitudeAccuracy,\r?\n\r?\n',
    r'                // floor: building floor \(iOS only\)\r?\n                Floor = locationData\.Floor,\r?\n\r?\n'
]

for pattern in service_patterns:
    content = re.sub(pattern, '', content)

with open('Convoy.Service/Services/LocationService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("[OK] LocationService.cs")

print("\n[SUCCESS] All extended columns removed!")
print("Removed: heading_accuracy, speed_accuracy, altitude_accuracy, floor")
