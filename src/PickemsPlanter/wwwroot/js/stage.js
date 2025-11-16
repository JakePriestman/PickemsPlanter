function resetDropzoneStyle(dropzone) {

    switch (dropzone.id) {
        case "pick0":
        case "pick1":
            dropzone.textContent = "3-0";
            break;
        case "pick2":
        case "pick3":
        case "pick4":
            dropzone.textContent = "3-1";
            break;
        case "pick5":
        case "pick6":
        case "pick7":
            dropzone.textContent = "3-2";
            break;
        case "pick8":
        case "pick9":
            dropzone.textContent = "0-3";
            break;
    }
}

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated')
        .forEach(dz => {
            dz.addEventListener('drop', (ev) => drop(ev, false));
            dz.addEventListener('dragover', allowDrop);
        })
})

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId, stage } = window.pageData;
    await LoadImagesAsync(eventId, steamId, stage, false);
});

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId, stage} = window.pageData;

    await LoadPicksAsync(eventId, steamId, stage, false);
});

document.getElementById("showResults").addEventListener('change', async function() {

    const { eventId, steamId, stage } = window.pageData;

    if (this.checked) {
        toggleSaveForm();
        await LoadResultsAsync(eventId, stage, false);
        
    }
    else {
        await LoadPicksAsync(eventId, steamId, stage, false);

        toggleSaveForm();
        await checkDropzonesFilledAsync();
    }
});
