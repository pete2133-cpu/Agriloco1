// wwwroot/js/gridpainter-map.js

let agrilocoMap;
let agrilocoGeocoder;

window.AgrilocoInitMap = function () {
    agrilocoGeocoder = new google.maps.Geocoder();

    const mapDiv = document.getElementById("agrilocoMap");
    if (!mapDiv) return;

    // Default center (Canada-ish) until we geocode.
    agrilocoMap = new google.maps.Map(mapDiv, {
        center: { lat: 45.4215, lng: -75.6972 },
        zoom: 14,
        mapTypeId: "hybrid",
        disableDefaultUI: false
    });

    // Address comes from a data-* attribute on the map container.
    const address = mapDiv.getAttribute("data-farm-address");
    if (address && address.trim().length > 0) {
        agrilocoGeocoder.geocode({ address }, (results, status) => {
            if (status === "OK" && results && results.length > 0) {
                const loc = results[0].geometry.location;
                agrilocoMap.setCenter(loc);
                // Optional marker:
                new google.maps.Marker({ map: agrilocoMap, position: loc });
            } else {
                console.warn("Geocode failed:", status);
            }
        });
    }
};

// Helpers for future: resize/recenter on layout changes
window.AgrilocoMapResize = function () {
    if (!agrilocoMap) return;
    google.maps.event.trigger(agrilocoMap, "resize");
};