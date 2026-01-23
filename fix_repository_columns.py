#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Fix LocationRepository to only use columns that exist in database
"""

# Read the file
with open('Convoy.Data/Repositories/LocationRepository.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Fix InsertAsync - line 29-46
old_insert = """            INSERT INTO locations (
                user_id, recorded_at, latitude, longitude,
                accuracy, speed, heading, altitude,
                ellipsoidal_altitude, heading_accuracy, speed_accuracy, altitude_accuracy, floor,
                activity_type, activity_confidence, is_moving,
                battery_level, is_charging,
                timestamp, age, event, mock, sample, odometer, uuid, extras,
                distance_from_previous, created_at
            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @EllipsoidalAltitude, @HeadingAccuracy, @SpeedAccuracy, @AltitudeAccuracy, @Floor,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging,
                @Timestamp, @Age, @Event, @Mock, @Sample, @Odometer, @Uuid,
                CAST(@Extras AS JSONB),
                @DistanceFromPrevious, @CreatedAt
            ) RETURNING id"""

new_insert = """            INSERT INTO locations (
                user_id, recorded_at, latitude, longitude,
                accuracy, speed, heading, altitude,
                activity_type, activity_confidence, is_moving,
                battery_level, is_charging,
                distance_from_previous, created_at
            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging,
                @DistanceFromPrevious, @CreatedAt
            ) RETURNING id"""

content = content.replace(old_insert, new_insert)

# Fix InsertBatchAsync - similar pattern
old_batch_insert_values = """            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @EllipsoidalAltitude, @HeadingAccuracy, @SpeedAccuracy, @AltitudeAccuracy, @Floor,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging,
                @Timestamp, @Age, @Event, @Mock, @Sample, @Odometer, @Uuid,
                CAST(@Extras AS JSONB),
                @DistanceFromPrevious, @CreatedAt
            )
            RETURNING id, user_id as UserId, recorded_at as RecordedAt,
                      latitude, longitude, accuracy, speed, heading, altitude,
                      ellipsoidal_altitude as EllipsoidalAltitude,
                      heading_accuracy as HeadingAccuracy,
                      speed_accuracy as SpeedAccuracy,
                      altitude_accuracy as AltitudeAccuracy,
                      floor,
                      activity_type as ActivityType, activity_confidence as ActivityConfidence,
                      is_moving as IsMoving, battery_level as BatteryLevel,
                      is_charging as IsCharging,
                      timestamp, age, event, mock, sample, odometer, uuid, extras,
                      distance_from_previous as DistanceFromPrevious,
                      created_at as CreatedAt"""

new_batch_insert_values = """            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging,
                @DistanceFromPrevious, @CreatedAt
            )
            RETURNING id, user_id as UserId, recorded_at as RecordedAt,
                      latitude, longitude, accuracy, speed, heading, altitude,
                      activity_type as ActivityType, activity_confidence as ActivityConfidence,
                      is_moving as IsMoving, battery_level as BatteryLevel,
                      is_charging as IsCharging,
                      distance_from_previous as DistanceFromPrevious,
                      created_at as CreatedAt"""

content = content.replace(old_batch_insert_values, new_batch_insert_values)

# Fix all SELECT statements - replace the long column list with shorter one
old_select = """                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                ellipsoidal_altitude as EllipsoidalAltitude,
                heading_accuracy as HeadingAccuracy,
                speed_accuracy as SpeedAccuracy,
                altitude_accuracy as AltitudeAccuracy,
                floor,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging,
                timestamp, age, event, mock, sample, odometer, uuid, extras,
                distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt"""

new_select = """                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging,
                distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt"""

content = content.replace(old_select, new_select)

# Also fix the version without leading spaces (in subqueries)
old_select_subquery = """                    id, user_id as UserId, recorded_at as RecordedAt,
                    latitude, longitude, accuracy, speed, heading, altitude,
                    ellipsoidal_altitude as EllipsoidalAltitude,
                    heading_accuracy as HeadingAccuracy,
                    speed_accuracy as SpeedAccuracy,
                    altitude_accuracy as AltitudeAccuracy,
                    floor,
                    activity_type as ActivityType, activity_confidence as ActivityConfidence,
                    is_moving as IsMoving, battery_level as BatteryLevel,
                    is_charging as IsCharging,
                    timestamp, age, event, mock, sample, odometer, uuid, extras,
                    distance_from_previous as DistanceFromPrevious,
                    created_at as CreatedAt,"""

new_select_subquery = """                    id, user_id as UserId, recorded_at as RecordedAt,
                    latitude, longitude, accuracy, speed, heading, altitude,
                    activity_type as ActivityType, activity_confidence as ActivityConfidence,
                    is_moving as IsMoving, battery_level as BatteryLevel,
                    is_charging as IsCharging,
                    distance_from_previous as DistanceFromPrevious,
                    created_at as CreatedAt,"""

content = content.replace(old_select_subquery, new_select_subquery)

# Write back
with open('Convoy.Data/Repositories/LocationRepository.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("[OK] LocationRepository.cs - fixed to use only existing database columns")
