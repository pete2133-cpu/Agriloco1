// Presentation only: no form submissions, request interception, or saved values.
document.querySelectorAll("nav a").forEach(link => {
    if (new URL(link.href).pathname.toLowerCase() === location.pathname.toLowerCase()) link.setAttribute("aria-current", "page");
});
document.querySelectorAll("main table").forEach(table => {
    if (table.closest(".table-scroll")) return;
    const wrap = document.createElement("div");
    wrap.className = "table-scroll";
    wrap.tabIndex = 0;
    wrap.setAttribute("role", "region");
    wrap.setAttribute("aria-label", "Table — scroll horizontally for more columns");
    table.before(wrap);
    wrap.append(table);
});
// Associate existing plain labels with their fields without changing names or values.
document.querySelectorAll("form label:not([for])").forEach(label => {
    const field = label.parentElement.querySelector("input:not([type=hidden]),select,textarea");
    if (field && field.id && !label.contains(field)) label.htmlFor = field.id;
});const compactNavigation = window.matchMedia("(max-width:600px)");
function sizeInventoryNavigation() {
    document.querySelectorAll(".inventory-menu").forEach(menu => { menu.open = !compactNavigation.matches; });
}
sizeInventoryNavigation();
compactNavigation.addEventListener("change", sizeInventoryNavigation);
