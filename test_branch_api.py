#!/usr/bin/env python3
"""
Branch API test script
"""
import requests
import json

BASE_URL = "http://localhost:5084"

def test_branch_api():
    print("=" * 60)
    print("Testing Branch API")
    print("=" * 60)

    # Test 1: Empty body (barcha filiallar)
    print("\n1. Test - Empty body (bo'sh object):")
    response = requests.post(
        f"{BASE_URL}/api/branches/branch-list",
        headers={"Content-Type": "application/json"},
        json={}
    )
    print(f"Status Code: {response.status_code}")
    data = response.json()
    print(f"Status: {data['status']}")
    print(f"Message: {data['message']}")
    print(f"Branch Count: {len(data['data'])}")
    if data['data']:
        print(f"First Branch: {data['data'][0]['name']}")

    # Test 2: Empty string search (barcha filiallar)
    print("\n2. Test - Empty string search (''):")
    response = requests.post(
        f"{BASE_URL}/api/branches/branch-list",
        headers={"Content-Type": "application/json"},
        json={"search": ""}
    )
    print(f"Status Code: {response.status_code}")
    data = response.json()
    print(f"Status: {data['status']}")
    print(f"Message: {data['message']}")
    print(f"Branch Count: {len(data['data'])}")

    # Test 3: Null search (barcha filiallar)
    print("\n3. Test - Null search:")
    response = requests.post(
        f"{BASE_URL}/api/branches/branch-list",
        headers={"Content-Type": "application/json"},
        json={"search": None}
    )
    print(f"Status Code: {response.status_code}")
    data = response.json()
    print(f"Status: {data['status']}")
    print(f"Message: {data['message']}")
    print(f"Branch Count: {len(data['data'])}")

    # Test 4: Whitespace only (barcha filiallar)
    print("\n4. Test - Whitespace only ('   '):")
    response = requests.post(
        f"{BASE_URL}/api/branches/branch-list",
        headers={"Content-Type": "application/json"},
        json={"search": "   "}
    )
    print(f"Status Code: {response.status_code}")
    data = response.json()
    print(f"Status: {data['status']}")
    print(f"Message: {data['message']}")
    print(f"Branch Count: {len(data['data'])}")

    # Test 5: Actual search term
    print("\n5. Test - Search with term ('Наманган'):")
    response = requests.post(
        f"{BASE_URL}/api/branches/branch-list",
        headers={"Content-Type": "application/json"},
        json={"search": "Наманган"}
    )
    print(f"Status Code: {response.status_code}")
    data = response.json()
    print(f"Status: {data['status']}")
    print(f"Message: {data['message']}")
    print(f"Branch Count: {len(data['data'])}")
    if data['data']:
        print(f"First Branch: {data['data'][0]['name']}")
        print(f"State: {data['data'][0].get('state_name', 'N/A')}")
        print(f"Region: {data['data'][0].get('region_name', 'N/A')}")

    print("\n" + "=" * 60)
    print("All tests completed!")
    print("=" * 60)

if __name__ == "__main__":
    try:
        test_branch_api()
    except requests.exceptions.ConnectionError:
        print("❌ Error: Cannot connect to API. Make sure the server is running at http://localhost:5084")
    except Exception as e:
        print(f"❌ Error: {e}")
