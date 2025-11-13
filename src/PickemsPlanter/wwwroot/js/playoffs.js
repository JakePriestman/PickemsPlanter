function dropPlayoffs(ev) {
    ev.preventDefault();
    const sourceId = ev.dataTransfer.getData("sourceId");
    const div = document.getElementById(sourceId);
    const imageSrc = div.querySelector("img").src;

    if (!imageSrc) return;

    if (imageSrc.includes("unknown")) {
        return;
    }

    let dropzone = ev.target;

    const targetId = ev.currentTarget.id; 

    if (!allowPlayoffDrop(sourceId, targetId)) {
        return;
    }

    enableDrag(targetId);

    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    placeImageInDropzone(imageSrc, dropzone, true);
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

document.addEventListener("DOMContentLoaded", function (e) {
    const { picksAllowed } = window.pageData;


    const picks = document.querySelectorAll('.match-dropzone-advanced');

    picks.forEach(team => {
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

document.addEventListener("DOMContentLoaded", async function () {
    const { eventId, steamId } = window.pageData;

    const imagesResponse = await fetch(`/PickEms/Playoffs?handler=Images&eventId=${eventId}&steamId=${steamId}`)
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

    const picksAllowedResponse = await fetch(`/PickEms/Playoffs?handler=PicksAllowed&eventId=${eventId}`)
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
});

document.addEventListener("DOMContentLoaded", function () {
    const { eventId, steamId } = window.pageData;
    fetch(`/PickEms/Playoffs?handler=Picks&eventId=${eventId}&steamId=${steamId}`)
        .then(response => response.json())
        .then(imageUrls => {
            console.log("Fetched image URLs:", imageUrls);

            imageUrls.forEach((url, index) => {
                const container = document.getElementById(`pick${index}`);
                enableDrag(`pick${index}`)
                if (container) {
                    placeImageInDropzone(url, container, true);
                } else {
                    console.warn(`Dropzone with id="pick${index}" not found`);
                }
            });
        });
});

document.addEventListener("DOMContentLoaded", () => {
    checkDropzonesFilled();
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

    const { eventId, steamId } = window.pageData;

    if (this.checked) {
        fetch(`/PickEms/Playoffs?handler=Results&eventId=${eventId}`)
            .then(response => response.json())
            .then(imageUrls => {
                console.log("Fetched image URLs:", imageUrls);

                imageUrls.forEach((url, index) => {
                    const container = document.getElementById(`pick${index}`);

                    if (container) {
                        placeImageInDropzone(url, container, true);
                    } else {
                        console.warn(`Dropzone with id="pick${index}" not found`);
                    }
                });
            });
    }
    else {
        fetch(`/PickEms/Playoffs?handler=Picks&eventId=${eventId}&steamId=${steamId}`)
            .then(response => response.json())
            .then(imageUrls => {
                console.log("Fetched image URLs:", imageUrls);

                imageUrls.forEach((url, index) => {
                    const container = document.getElementById(`pick${index}`);

                    if (container) {
                        placeImageInDropzone(url, container, true);
                    } else {
                        console.warn(`Dropzone with id="pick${index}" not found`);
                    }
                });
            });
    }
});