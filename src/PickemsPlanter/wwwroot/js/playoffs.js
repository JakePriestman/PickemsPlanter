function resetDropzoneStyle(dropzone) {
    switch (dropzone.id) {
        case "pick6":
            dropzone.textContent = "Winner";
            dropzone.title = "Winner";
            break;
        case "pick5":
            dropzone.textContent = "S2";
            dropzone.title = "Semi final 2";
            break;
        case "pick4":
            dropzone.textContent = "S1";
            dropzone.title = "Semi final 1";
            break;
        case "pick3":
            dropzone.textContent = "Q4";
            dropzone.title = "Quart final 4";
            break;
        case "pick2":
            dropzone.textContent = "Q3";
            dropzone.title = "Quart final 3";
            break;
        case "pick1":
            dropzone.textContent = "Q2";
            dropzone.title = "Quart final 2";
            break;
        case "pick0":
            dropzone.textContent = "Q1";
            dropzone.title = "Quart final 1";
            break;
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, null, true);

    document.querySelectorAll('.match-dropzone-advanced')
        .forEach(dz => {
            if (picksAllowed) {
                dz.addEventListener('drop', (ev) => {
                    dz.classList.remove('drag-hover');
                    drop(ev, true);
                });
                dz.addEventListener('dragover', (ev) => ev.preventDefault());
                dz.addEventListener('dragenter', () => dz.classList.add('drag-hover'));
                dz.addEventListener('dragleave', (e) => {
                    if (!dz.contains(e.relatedTarget))
                        dz.classList.remove('drag-hover');
                });
            }
            else {
                switch (dz.className) {
                    case "match-dropzone-advanced":
                        dz.className = "match-dropzone-advanced-not-allowed";
                        break;
                    case "match-dropzone-eliminated":
                        dz.className = "match-dropzone-eliminated-not-allowed";
                        break;
                }
            }
        });

    await LoadImagesAsync(eventId, steamId, null, true, picksAllowed);

    await LoadPicksAsync(eventId, steamId, null, true, picksAllowed);

    toggleClearAllDropzonesButton(picksAllowed);
    toggleRandomPicksButton(picksAllowed);
});

document.getElementById("showResults").addEventListener('change', async function (e) {

    const { eventId, steamId } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, null, false);

    if (this.checked) {
        toggleSaveForm();
        await LoadResultsAsync(eventId, null, true, picksAllowed);

        toggleClearAllDropzonesButton(picksAllowed);
        toggleRandomPicksButton(picksAllowed);
    }

    else {
        toggleSaveForm();
        await LoadPicksAsync(eventId, steamId, null, true, picksAllowed);

        const teams = document.querySelectorAll('.team');

        teams.forEach(team => {
            toggleImageFunctionality(team, picksAllowed);
        });   

        toggleClearAllDropzonesButton(picksAllowed);
        toggleRandomPicksButton(picksAllowed);
    }
});

document.getElementById("clearAllPicks").addEventListener('click', () => {
    clearAllDropzonesAsync(true);
});

document.getElementById("randomPicks").addEventListener('click', async () => {
    await selectRandomPicksAsync(true);
});