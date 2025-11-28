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