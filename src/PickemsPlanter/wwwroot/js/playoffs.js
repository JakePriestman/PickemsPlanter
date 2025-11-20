function resetDropzoneStyle(dropzone) {
    switch (dropzone.id) {
        case "pick6":
            dropzone.textContent = "Winner";
            break;
        case "pick5":
            dropzone.textContent = "S2";
            break;
        case "pick4":
            dropzone.textContent = "S1";
            break;
        case "pick3":
            dropzone.textContent = "Q4";
            break;
        case "pick2":
            dropzone.textContent = "Q3";
            break;
        case "pick1":
            dropzone.textContent = "Q2";
            break;
        case "pick0":
            dropzone.textContent = "Q1";
            break;
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId } = window.pageData;

    await LoadImagesAsync(eventId, steamId, null, true);
});

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId } = window.pageData;

    await LoadPicksAsync(eventId, steamId, null, true);
});

document.addEventListener("DOMContentLoaded", () => {
    const stages = document.querySelectorAll(".stage");

    stages.forEach(stage => {
        stage.addEventListener("click", () => {
            stages.forEach(s => s.classList.remove("active"));
            stage.classList.add("active");
        });
    });
});

document.getElementById("showResults").addEventListener('change', async function (e) {

    const { eventId, steamId } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, true);

    if (this.checked) {
        toggleSaveForm();
        await LoadResultsAsync(eventId, null, true);
    }

    else {
        await LoadPicksAsync(eventId, steamId, null, true);

        toggleSaveForm();
        await checkDropzonesFilledAsync(picksAllowed);
    }
});
