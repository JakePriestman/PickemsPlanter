function resetDropzoneStyle(dropzone) {

    switch (dropzone.id) {
        case "pick0":
        case "pick1":
            dropzone.textContent = "3-0";
            dropzone.title = "3-0";
            break;
        case "pick2":
        case "pick3":
        case "pick4":
            dropzone.textContent = "3-1";
            dropzone.title = "3-1";
            break;
        case "pick5":
        case "pick6":
        case "pick7":
            dropzone.textContent = "3-2";
            dropzone.title = "3-2";
            break;
        case "pick8":
        case "pick9":
            dropzone.textContent = "0-3";
            dropzone.title = "0-3";
            break;
    }
}

async function dropBackInTeamSection(ev, picksAllowed) {
    ev.preventDefault();

    const sourceId = ev.dataTransfer.getData("sourceId");
    const div = document.getElementById(sourceId);
    const imageSrc = div.querySelector("img").src;

    if (sourceId.includes('pick')) {
        div.innerHTML = '';
        resetDropzoneStyle(div);
        disableDrag(div);

        const filename = imageSrc.split('/').pop();
        const teams = document.querySelectorAll('.team[disabled]');
        teams.forEach(team => {
            const existingImg = team.querySelector('img');
            if (existingImg) {
                const existingFilename = existingImg.src.split('/').pop();
                if (existingFilename === filename) {
                    team.removeAttribute('disabled');
                }
            }
        });

        await checkDropzonesFilledAsync(picksAllowed);
        toggleClearAllDropzonesButton(picksAllowed);
        toggleRandomPicksButton(picksAllowed);
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    isPlayoffs = false;
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