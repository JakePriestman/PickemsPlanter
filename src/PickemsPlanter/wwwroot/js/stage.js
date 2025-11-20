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
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, stage } = window.pageData;
    let picksAllowed = await getPicksAllowedAsync(eventId, stage, false);

    if (picksAllowed) {
        const teamSection = document.getElementById('teamSection');
        teamSection.addEventListener('dragover', (ev) => ev.preventDefault());
        teamSection.addEventListener('drop', (ev) => dropBackInTeamSection(ev, picksAllowed));
    }
    document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated')
        .forEach(dz => {
            if (picksAllowed) {
                dz.addEventListener('drop', (ev) => {
                    dz.classList.remove('drag-hover');
                    drop(ev, false);
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
});

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
