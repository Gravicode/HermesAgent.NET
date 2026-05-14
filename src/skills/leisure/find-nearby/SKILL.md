# find-nearby

> Find nearby places (restaurants, cafes, bars, pharmacies, etc.) using OpenStreetMap. No API keys needed.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: leisure, maps, location, openstreetmap -->

## Overview

Uses the free Overpass API (OpenStreetMap) and Nominatim geocoder to find nearby places.
No API keys required.

## Geocode an Address

```bash
# Get coordinates from address
curl -s "https://nominatim.openstreetmap.org/search?q=Bogor,West+Java&format=json&limit=1" \
  -H "User-Agent: HermesAgent/1.0" | python3 -c "
import json, sys
r = json.load(sys.stdin)[0]
print(f'lat={r[\"lat\"]}, lon={r[\"lon\"]}')
"
```

## Find Nearby Places

```python
import requests

def find_nearby(lat, lon, amenity, radius_m=1000, limit=10):
    """
    amenity: restaurant, cafe, bar, pharmacy, hospital, school, 
             supermarket, atm, parking, fuel, library, cinema
    """
    query = f"""
    [out:json][timeout:25];
    node["amenity"="{amenity}"](around:{radius_m},{lat},{lon});
    out {limit};
    """
    resp = requests.post(
        "https://overpass-api.de/api/interpreter",
        data={"data": query},
        headers={"User-Agent": "HermesAgent/1.0"}
    )
    data = resp.json()
    
    results = []
    for el in data.get("elements", []):
        tags = el.get("tags", {})
        results.append({
            "name": tags.get("name", "Unnamed"),
            "type": tags.get("cuisine") or tags.get("amenity"),
            "lat": el["lat"], "lon": el["lon"],
            "opening_hours": tags.get("opening_hours", "unknown"),
            "phone": tags.get("phone", ""),
            "website": tags.get("website", "")
        })
    return results

# Example: restaurants near Bogor
places = find_nearby(-6.5971, 106.8060, "restaurant", radius_m=500)
for p in places:
    print(f"{p['name']} ({p['type']}) — {p['opening_hours']}")
```

## Get Directions (URL)

```python
def directions_url(from_lat, from_lon, to_lat, to_lon, mode="driving"):
    # Opens in browser or returns URL
    return f"https://www.openstreetmap.org/directions?engine=fossgis_osrm_{mode}&route={from_lat},{from_lon};{to_lat},{to_lon}"

print(directions_url(-6.5971, 106.8060, -6.6000, 106.8100))
```

## Tips

- Overpass API is free but rate-limited — cache results for repeated queries
- For real-time data (business hours accuracy), combine with Google Places API
- Use `tourism` instead of `amenity` for: hotel, museum, attraction, viewpoint
