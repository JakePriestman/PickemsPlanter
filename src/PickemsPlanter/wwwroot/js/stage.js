function drop(ev) {
    ev.preventDefault();
    const sourceId = ev.dataTransfer.getData("sourceId");
    const div = document.getElementById(sourceId);
    const imageSrc = div.querySelector("img").src;
    if (!imageSrc) return;

    if (imageSrc.includes("unknown")) {
        return;
    }

    let dropzone = ev.target;
    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    placeImageInDropzone(imageSrc, dropzone, false);
}

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

async function getPicksAllowed() {
    const { eventId } = window.pageData;
    const response = await fetch(`/PickEms/Stage?handler=PicksAllowed&eventId=${eventId}`)
    return await response.json();
}

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId, stage } = window.pageData;

    const imagesResponse = await fetch(`/PickEms/Stage?handler=Images&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
    const imageUrls = await imagesResponse.json();

    imageUrls.forEach((url, index) => {
        const container = document.getElementById(`team${index}`);
        if (container) {
            const img = document.createElement("img");
            img.src = url;
            img.className = "team-img";
            if (url.includes('unknown'))
                img.classList.add('unknown')
            container.appendChild(img);
        }
    });


    const picksAllowedResponse = await fetch(`/PickEms/Stage?handler=PicksAllowed&eventId=${eventId}&stage=${stage}`)
    const picksAllowed = await picksAllowedResponse.json();

    const teams = document.querySelectorAll('.team');

    teams.forEach(team => {
        if (picksAllowed) {
            const image = team.querySelector('img');

            if (Array.from(image.classList).includes('unknown')) {
                disableDrag(team.id);
                team.setAttribute('disabled', 'true');
            }
            else {
                enableDrag(team.id)
                team.removeAttribute('disabled');
            }
        }
        else {
            disableDrag(team.id);
            team.setAttribute('disabled', 'true');
        }
    });

    if (!picksAllowed)
        confirm("Picks are not allowed on this stage as of now.");

    await checkDropzonesFilled();
});

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId, stage } = window.pageData;

    const response = await fetch(`/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
    const imageUrls = await response.json();

    for (let [index, url] of imageUrls.entries()) {
        const container = document.getElementById(`pick${index}`);

        if (container) {
            placeImageInDropzone(url, container);
        } else {
            console.warn(`Dropzone with id="pick${index}" not found`);
        }
    };
});

function toggleHideSaveForm() {
    const saveButton = document.getElementById('saveForm');
    if (saveButton) {
        saveButton.style.visibility = saveButton.style.visibility === 'hidden' ? 'visible' : 'hidden';
    }
}

document.getElementById("showResults").addEventListener('change', async function (e) {

    const { eventId, steamId, stage } = window.pageData;

    if (this.checked) {
        const response = await fetch(`/PickEms/Stage?handler=Results&eventId=${eventId}&stage=${stage}`);
        const imageUrls = await response.json();

        for (let [index, url] of imageUrls.entries()) {
            const container = document.getElementById(`pick${index}`);

            if (container) {
                await placeImageInDropzone(url, container, false);
            } else {
                console.warn(`Dropzone with id="pick${index}" not found`);
            }
        };

        toggleHideSaveForm();
    }
    else {
        const response = await fetch(`/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
        const imageUrls = await response.json();

        for (let [index, url] of imageUrls.entries()) {
            const container = document.getElementById(`pick${index}`);

            if (container) {
                await placeImageInDropzone(url, container, false);
            } else {
                console.warn(`Dropzone with id="pick${index}" not found`);
            }
        }

        toggleHideSaveForm();
        await checkDropzonesFilled();
    }
});
