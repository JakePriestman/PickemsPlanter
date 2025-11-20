function allowDrop(ev) {
    ev.preventDefault();
}

function enableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "true");
    item.setAttribute("ondragstart", "drag(event)");
}

function disableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "false");
}

function drag(ev) {
    if (ev.target.src.includes("unknown"))
        return;
    ev.dataTransfer.setData("sourceId", ev.currentTarget.id);
}

function allowPlayoffDrop(sourceId, targetId) {
    if ((sourceId == "team0" || sourceId == "team1") && targetId == "pick0") return true;
    if ((sourceId == "team2" || sourceId == "team3") && targetId == "pick1") return true;
    if ((sourceId == "team4" || sourceId == "team5") && targetId == "pick2") return true;
    if ((sourceId == "team6" || sourceId == "team7") && targetId == "pick3") return true;

    if ((sourceId == "pick0" || sourceId == "pick1") && targetId == "pick4") return true;
    if ((sourceId == "pick2" || sourceId == "pick3") && targetId == "pick5") return true;

    if ((sourceId == "pick4" || sourceId == "pick5") && targetId == "pick6") return true;

    else return false;
}

function removeDuplicateImage(filename) {
    const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');
    allDropzones.forEach(zone => {
        const existingImg = zone.querySelector('img');
        if (existingImg) {
            const existingFilename = existingImg.src.split('/').pop();
            if (existingFilename === filename) {
                zone.innerHTML = '';
                resetDropzoneStyle(zone);
                disableDrag(zone);
            }
        }
    });
}

function swapImagesInDropzones(div, dropzone) {
    const innerHTML = div.innerHTML;

    div.innerHTML = dropzone.innerHTML;
    dropzone.innerHTML = innerHTML;
}

async function drop(ev, isPlayoffs) {
    ev.preventDefault();
    
    const sourceId = ev.dataTransfer.getData("sourceId");
    const div = document.getElementById(sourceId);
    const imageSrc = div.querySelector("img").src;

    let dropzone = ev.target;

    const { eventId, stage } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, stage, isPlayoffs);

    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    if (sourceId.includes('pick') && !isPlayoffs)
        swapImagesInDropzones(div, dropzone);
    else
        placeImageInDropzoneAsync(imageSrc, dropzone, isPlayoffs, picksAllowed);
}


async function checkDropzonesFilledAsync(picksAllowed) {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

    const allFilled = Array.from(dropzones).every(zone =>
        zone !== null && zone.querySelector('img.dropped-img') !== null
    );

    const saveButton = document.getElementById('saveButton');

    if (saveButton) {
        saveButton.disabled = !allFilled || !picksAllowed;
        saveButton.textContent = allFilled ? "Plant Picks" : "All picks need to be within the dropzones to plant your picks";
    }
}

function getDropzonesAfter(currentId, allDropzones) {
    const currentNumber = parseInt(currentId.replace('pick', ''), 10);

    return Array.from(allDropzones).filter(dz => {
        const dzNumber = parseInt(dz.id.replace('pick', ''), 10);
        return dzNumber > currentNumber;
    });
}

function removeSucceedingImages(imageInDropzone, dropzone) {
    const imageToRemoveName = imageInDropzone.src.split('/').pop()
    const allDropzones = document.querySelectorAll('.match-dropzone-advanced');
    const dropzonesToRemove = getDropzonesAfter(dropzone.id, allDropzones);

    dropzonesToRemove.forEach(zone => {
        const existingImg = zone.querySelector('img');
        if (existingImg) {
            const existingFilename = existingImg.src.split('/').pop();

            if (existingFilename === imageToRemoveName) {
                zone.innerHTML = '';
                resetDropzoneStyle(zone);
                disableDrag(zone);
            }
        }
    });
}

function addImageToDropzone(dropzone, imageSrc, imageInDropzone, picksAllowed) {

    if (picksAllowed) {
        enableDrag(dropzone.id);

        if (imageInDropzone) {
            const filename = imageInDropzone.src.split('/').pop();
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
        }

        const filename = imageSrc.split('/').pop();
        const teams = document.querySelectorAll('.team');
        teams.forEach(team => {
            const existingImg = team.querySelector('img');
            if (existingImg) {
                const existingFilename = existingImg.src.split('/').pop();
                if (existingFilename === filename) {
                    team.setAttribute('disabled', 'true');
                }
            }
        });
    }

    dropzone.innerHTML = '';

    const img = document.createElement("img");
    img.src = imageSrc;
    img.className = "dropped-img";

    dropzone.appendChild(img);
}


async function placeImageInDropzoneAsync(imageSrc, dropzone, isPlayoffs, picksAllowed) {
    if (!dropzone) return;

    if (!imageSrc || imageSrc.includes("unknown")) {
        resetDropzoneStyle(dropzone);
        return;
    }

    const filename = imageSrc.split('/').pop();
    const imageInDropzone = dropzone.querySelector('img');

    //Only remove the images after the current dropzone in playoffs mode
    if (isPlayoffs && imageInDropzone != null)
        removeSucceedingImages(imageInDropzone, dropzone);

    addImageToDropzone(dropzone, imageSrc, imageInDropzone, picksAllowed);

    await checkDropzonesFilledAsync(picksAllowed);
}


async function getPicksAllowedAsync(eventId, stage, isPlayoffs) {
    const url = isPlayoffs ? 
        `/PickEms/Playoffs?handler=PicksAllowed&eventId=${eventId}` :
        `/PickEms/Stage?handler=PicksAllowed&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url)
    return await response.json();
}

function toggleImageFunctionality(team, picksAllowed) {
    if (picksAllowed) {
        const image = team.querySelector('img');

        if (image) {
            if (Array.from(image.classList).includes('unknown')) {
                disableDrag(team.id);
                team.setAttribute('disabled', 'true');
            }
            else {
                enableDrag(team.id);
                team.removeAttribute('disabled');
            }
        }
        else {
            team.setAttribute('disabled', 'true');
        }
    }
    else {
        disableDrag(team.id);
        team.setAttribute('disabled', 'true');
    }
}


async function LoadImagesAsync(eventId, steamId, stage, isPlayoffs) {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Images&eventId=${eventId}&steamId=${steamId}` :
        `/PickEms/Stage?handler=Images&eventId=${eventId}&steamId=${steamId}&stage=${stage}`;

    const imagesResponse = await fetch(url);
    const imageUrls = await imagesResponse.json();

    const picksAllowed = await getPicksAllowedAsync(eventId, stage, isPlayoffs);

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

    const teams = document.querySelectorAll('.team');

    teams.forEach(team => {
        toggleImageFunctionality(team, picksAllowed);
    });

    if (isPlayoffs) {
        const dropzones = document.querySelectorAll('.match-dropzone-advanced');

        dropzones.forEach(dz => {
            toggleImageFunctionality(dz, picksAllowed);
        })
    }

    if (!picksAllowed)
        togglePicksNotAllowedConfirmation();

    await checkDropzonesFilledAsync(picksAllowed);
}


async function LoadPicksAsync(eventId, steamId, stage, isPlayoffs) {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Picks&eventId=${eventId}&steamId=${steamId}` :
        `/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`;

    const response = await fetch(url);
    const imageUrls = await response.json();
    const picksAllowed = await getPicksAllowedAsync(eventId, stage, isPlayoffs);

    for (let [index, url] of imageUrls.entries()) {
        const container = document.getElementById(`pick${index}`);

        if (container) {
            await placeImageInDropzoneAsync(url, container,  isPlayoffs, picksAllowed);
        } else {
            console.warn(`Dropzone with id="pick${index}" not found`);
        }
    };
}


async function LoadResultsAsync(eventId, stage, isPlayoffs) {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Results&eventId=${eventId}` :
        `/PickEms/Stage?handler=Results&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url);
    const imageUrls = await response.json();
    const picksAllowed = await getPicksAllowedAsync(eventId, isPlayoffs);

    for (let [index, url] of imageUrls.entries()) {
        const container = document.getElementById(`pick${index}`);

        if (container) {
            await placeImageInDropzoneAsync(url, container, isPlayoffs,  picksAllowed);
        } else {
            console.warn(`Dropzone with id="pick${index}" not found`);
        }
    };
}

function toggleSaveForm() {
    const saveButton = document.getElementById('saveForm');
    if (saveButton) {
        saveButton.style.visibility = saveButton.style.visibility === 'hidden' ? 'visible' : 'hidden';
    }
}

document.getElementById('saveForm').addEventListener('submit', function (e) {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');

    const imagesData = Array.from(dropzones).map(zone => {
        const img = zone.querySelector('img.dropped-img');
        if (!img) return "";

        const fullSrc = img.src;
        const fileName = fullSrc.substring(fullSrc.lastIndexOf('/') + 1);

        return fileName;
    });

    const jsonData = JSON.stringify(imagesData);
    const imageData = document.getElementById('DroppedImagesData');

    if (imageData == null) return "";

    imageData.value = jsonData
});

document.addEventListener("DOMContentLoaded", () => {
    const { eventId } = window.pageData;

    var link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = `/css/events/event-${eventId}.css`;
    document.head.appendChild(link);
});

document.getElementById("profileImage").addEventListener('click', function (e) {

    const navigation = document.getElementById("navigation");

    const dropdown = document.getElementById("dropDown");

    switch (dropdown.style.display) {
        case "flex":
            dropdown.style.display = "";
            navigation.style.borderRadius = "inherit";
            break;
        case "none":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 50px 0px 0px";
            break;
        case "":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 50px 0px 0px";
            break;
    }
});