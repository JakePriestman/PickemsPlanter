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

function enableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "true");
    item.setAttribute("ondragstart", "drag(event)");
}

//function disableDrag(itemId) {
//    const item = document.getElementById(itemId);
//    if (!item) return;
//    item.removeAttribute("draggable");
//    item.removeEventListener("ondragstart", drag);
//}

document.addEventListener("DOMContentLoaded", function () {
    const { eventId, steamId, stage } = window.pageData;
    fetch(`/PickEms/Playoffs?handler=Images&eventId=${eventId}&steamId=${steamId}`)
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
    // Possibly auto-fill from server
    checkDropzonesFilled(); // Run after populating with data
});

document.addEventListener("DOMContentLoaded", () => {
    const stages = document.querySelectorAll(".stage");

    stages.forEach(stage => {
        stage.addEventListener("click", () => {
            // remove active class from all
            stages.forEach(s => s.classList.remove("active"));
            // add active class to clicked
            stage.classList.add("active");
        });
    });
});