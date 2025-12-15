function resetDropzoneStyle(dropzone) {
    switch (dropzone.id) {
        case "pick6":
            dropzone.textContent = "Winner";
            dropzone.title = "Winner";
            break;
        case "pick5":
        case "pick4":
            dropzone.textContent = "GF";
            dropzone.title = "Grand Final";
            break;
        case "pick3":
        case "pick2":
            dropzone.textContent = "S2";
            dropzone.title = "Semi Final 2";
            break;
        case "pick1":
        case "pick0":
            dropzone.textContent = "S1";
            dropzone.title = "Semi Final 1";
            break;
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    isPlayoffs = true;
    await LoadTeamsAndPicksAsync();
});

const showResultsCheckmark = document.getElementById("showResults");

showResultsCheckmark.addEventListener('change', async () => {
    await showResultsAsync(showResultsCheckmark);
});

document.getElementById("clearAllPicks").addEventListener('click', () => {
    clearAllDropzones()
});

document.getElementById("randomPicks").addEventListener('click', () => {
    selectRandomPicks()
});