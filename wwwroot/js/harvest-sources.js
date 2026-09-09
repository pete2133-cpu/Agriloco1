(() => {
    const data = document.getElementById("harvest-sources");
    if (!data) return;
    const sources = JSON.parse(data.textContent);
    const crop = document.getElementById("harvest-crop");
    const variety = document.getElementById("harvest-variety");
    const location = document.getElementById("harvest-location");
    const help = document.getElementById("harvest-source-help");
    function populate(select, choices, prompt, keep) {
        const previous = keep ? select.value : "";
        select.replaceChildren(new Option(prompt, ""));
        choices.forEach(choice => select.add(new Option(choice.name, String(choice.id))));
        select.value = choices.some(c => String(c.id) === previous) ? previous : "";
        select.required = choices.length > 0;
        select.disabled = choices.length === 0;
    }
    function updateLocations(keep) {
        const choices = sources.locations.filter(x => String(x.cropId) === crop.value && (x.varietyId == null ? "" : String(x.varietyId)) === variety.value);
        populate(location, choices, choices.length ? "Choose a saved row / location" : "No saved rows for this selection", keep);
        help.textContent = !crop.value ? "Choose a crop to see its saved varieties and rows." :
            variety.required && !variety.value ? "Choose a variety to see its saved rows." :
            choices.length ? "Choose the row harvested. Save a separate lot for each row to keep its quantity clear." :
            "No row is saved for this selection. The harvest will record the selected crop and variety.";
    }
    function updateVarieties(keep) {
        const choices = sources.varieties.filter(x => String(x.cropId) === crop.value);
        populate(variety, choices, choices.length ? "Choose a variety" : "No saved varieties for this crop", keep);
        updateLocations(keep);
    }
    crop.addEventListener("change", () => updateVarieties(false));
    variety.addEventListener("change", () => updateLocations(false));
    updateVarieties(true);
})();