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
    dropzone.removeAttribute("style");

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

document.addEventListener("DOMContentLoaded", function (e) {
    const { picksAllowed } = window.pageData;
    const teams = document.querySelectorAll('.team');

    teams.forEach(team => {
        if (picksAllowed) {
            enableDrag(team.id)
            team.removeAttribute('disabled');
        }
        else {
            disableDrag(team.id);
            team.setAttribute('disabled', 'true');
        }
    });
});

document.addEventListener("DOMContentLoaded", function () {
    const { eventId, steamId, stage } = window.pageData;
    fetch(`/PickEms/Stage?handler=Images&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
        .then(response => response.json())
        .then(imageUrls => {
            imageUrls.forEach((url, index) => {
                const container = document.getElementById(`team${index}`);
                if (container) {
                    const img = document.createElement("img");
                    img.src = url;
                    img.className = "team-img";
                    container.appendChild(img);
                }
            });
        });
});

document.addEventListener("DOMContentLoaded", function () {
    const { eventId, steamId, stage } = window.pageData;
    fetch(`/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
        .then(response => response.json())
        .then(imageUrls => {
            console.log("Fetched image URLs:", imageUrls);

            imageUrls.forEach((url, index) => {
                const container = document.getElementById(`pick${index}`);

                if (container) {
                    placeImageInDropzone(url, container);
                } else {
                    console.warn(`Dropzone with id="pick${index}" not found`);
                }
            });
        });
});

document.addEventListener("DOMContentLoaded", () => {
    checkDropzonesFilled();
});

document.addEventListener("DOMContentLoaded", function (e) {
    const { eventId, stage } = window.pageData;
    fetch(`/PickEms/Playoffs?handler=PicksAllowed&eventId=${eventId}&stage=${stage}`)
        .then(response => response.json())
        .then(picksAreAllowed => {
            if (!picksAreAllowed) {
                confirm("Picks are not allowed on this stage as of now.");
            }
        });
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

document.getElementById("showResults").addEventListener('change', function (e) {

    const { eventId, steamId, stage } = window.pageData;

    if (this.checked) {
        fetch(`/PickEms/Stage?handler=Results&eventId=${eventId}&stage=${stage}`)
            .then(response => response.json())
            .then(imageUrls => {
                console.log("Fetched image URLs:", imageUrls);

                imageUrls.forEach((url, index) => {
                    const container = document.getElementById(`pick${index}`);

                    if (container) {
                        placeImageInDropzone(url, container);
                    } else {
                        console.warn(`Dropzone with id="pick${index}" not found`);
                    }
                });
            });
    }
    else {
        fetch(`/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
            .then(response => response.json())
            .then(imageUrls => {
                console.log("Fetched image URLs:", imageUrls);

                imageUrls.forEach((url, index) => {
                    const container = document.getElementById(`pick${index}`);

                    if (container) {
                        placeImageInDropzone(url, container);
                    } else {
                        console.warn(`Dropzone with id="pick${index}" not found`);
                    }
                });
            });
    }
});
