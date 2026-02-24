const sixMatchupPriorities = [
  [ [1,6], [2,5], [3,4] ],
  [ [1,6], [2,4], [3,5] ],
  [ [1,5], [2,6], [3,4] ],
  [ [1,5], [2,4], [3,6] ],
  [ [1,4], [2,6], [3,5] ],
  [ [1,4], [2,5], [3,6] ],
  [ [1,6], [2,3], [4,5] ],
  [ [1,5], [2,3], [4,6] ],
  [ [1,3], [2,6], [4,5] ],
  [ [1,3], [2,5], [4,6] ],
  [ [1,4], [2,3], [5,6] ], 
  [ [1,3], [2,4], [5,6] ],
  [ [1,2], [3,6], [4,5] ],
  [ [1,2], [3,5], [4,6] ],
  [ [1,2], [3,4], [5,6] ]  
];

let { eventId, stage } = window.pageData;

document.addEventListener("DOMContentLoaded", getEventSpecificStylesheet);

document.addEventListener("DOMContentLoaded", LoadAsync);

const resetButtons = document.querySelectorAll('.reset-button');

async function LoadAsync() {

    await createInitialMatchupsAsync();

    fillInEmptyMatchupsWithUnknown();
}

resetButtons.forEach(rb => {
    rb.addEventListener('click', () => {
        const swissRound = rb.closest('.swiss-round');

        resetStyles(swissRound);
    })
});

function selectTeam(mt) {
     changeStyles(mt);

    const allFilledInBracket = checkAllSelectedInBracket(mt);

    if (allFilledInBracket) {
        createNextMatchups(mt);
    }

    const allFilledInRound = checkAllFilledInRound(mt);

    if (allFilledInRound) {
        const currentRound = mt.closest('.swiss-round');

        const number = getRoundNumber(currentRound);

        if (number < 4) {
            const nextRound = document.getElementById(`round${number+1}`);

            const teamsInRound = nextRound.querySelectorAll('.team-img')
            finalise(teamsInRound, nextRound);
        }
    }
}

function finalise(teamsInRound, round) {
    const teams = [...teamsInRound].filter(t => !t.classList.contains('unknown'));

    calculateNewBuchholzScores(teams);

    const brackets = round.querySelectorAll('.matchups-container')

    for (const bracket of brackets) {

        let teamsInBracket = teams.filter(team => {
            if (team.parentElement == null) return false;
            const id = getBracketId(team);
            return id === bracket.id
        });

        const clonesInBracket = teamsInBracket.map(t => t.cloneNode(true));

        const matchupDivs = bracket.querySelectorAll('.matchup');

        const orderedTeams = orderSwissGroup(clonesInBracket);

        const matchups = createFinalisedMatchups(orderedTeams);

        addMatchupsToBracket(matchups, matchupDivs);

        const matchupTeams = bracket.querySelectorAll('.matchup-team');

        matchupTeams.forEach(mt => {
            addClickEvent(mt);
        });
    }
}


function addClickEvent(team) {
    if (team.className == 'team') 
        return team;

    const handler = () => selectTeam(team);
    team._selectHandler = handler;
    team.addEventListener('click', handler);

    return team;
}


function resetStyles(swissRound) {
    //This can most likely be written way better.
    const matchupTeams = swissRound.querySelectorAll('.matchup-team');

    matchupTeams.forEach(mt => {
        mt.className = "matchup-team";
    });

    const number = getRoundNumber(swissRound);

    if (number === 4) {
        const teamContainers = swissRound.querySelectorAll('.teams-container');

        const teamsToReset = [...teamContainers[1].querySelectorAll('.team'),
                                ...teamContainers[2].querySelectorAll('.team')];
        teamsToReset.forEach(t => {
            t.innerHTML = "";
            const image = createTeamImage('unknown.png', 0, 0);

            t.appendChild(image);
        });

        return;
    }

    for (var i = 4; i > number; i--) {
        const round = document.getElementById(`round${i}`);

        const matchupTeams = round.querySelectorAll('.matchup-team');

        matchupTeams.forEach(mt => {
            mt.innerHTML = "";
            mt.className = "matchup-team";
            const image = createTeamImage('unknown.png', 0, 0);

            if (mt._selectHandler) {
                mt.removeEventListener('click', mt._selectHandler);
                delete mt._selectHandler;
            }

            mt.appendChild(image);           
        });

        const teams = round.querySelectorAll('.team');
        teams.forEach(t => {
            t.innerHTML = "";
            const image = createTeamImage('unknown.png', 0, 0);
            t.appendChild(image);
        });
    }
}

function changeStyles(matchupTeam) {
    const matchup = matchupTeam.parentElement;

    const teams = matchup.querySelectorAll('.matchup-team');

    const loser = [...teams].find(team => team !== matchupTeam);

    switchStyle(matchupTeam, loser);
}

function switchStyle(winner, loser) {

    const classList = winner.classList;

    switch (true) {
        case classList.contains('eliminated'):
            winner.classList.remove('eliminated');
            winner.classList.add('advanced');
            loser.classList.remove('advanced');
            loser.classList.add('eliminated');
            break;

        case !classList.contains('advanced'):
            winner.classList.add('advanced');
            loser.classList.add('eliminated');
            break;
    }
}

function fillInEmptyMatchupsWithUnknown() {
    const emptyTeams = Array.from(document.querySelectorAll('.matchup-team, .team')).filter(x => x.innerHTML == "");

    for (const team of emptyTeams) {
        const image = createTeamImage('unknown.png', 0, 0);
        team.appendChild(image);
    }
}

function createTeamImage(imageSource, seed, buchholz) {
    const image = document.createElement("img");
    image.src = `https://sapickemsplanter.blob.core.windows.net/teamimages/${imageSource}`;
    image.className = "team-img";
    image.setAttribute('seed', seed);
    image.setAttribute('buchholz', buchholz);

    if (imageSource.includes('unknown')) {
        image.classList.add('unknown');
    }

    image.addEventListener('dragstart', ev => ev.preventDefault());

    return image;
}

function checkAllSelectedInBracket(mt) {
    const matchup = mt.parentElement;

    const contianer = matchup.closest('.matchups-container');

    const allTeams = contianer.querySelectorAll('.matchup-team');

    const allSelected = Array.from(allTeams).every(x => x.classList.contains('advanced') || x.classList.contains('eliminated'));

    return allSelected;
}

function checkAllFilledInRound(mt) {
    const round = mt.closest('.swiss-round');

    const allTeams = round.querySelectorAll('.matchup-team');

    return Array.from(allTeams).every(x => x.classList.contains('advanced') || x.classList.contains('eliminated'));
}



async function createInitialMatchupsAsync() {
    const url = `/Simulator?handler=Seeds&eventId=${eventId}&stage=${stage}`;

    const imagesResponse = await fetch(url);
    const seeds = await imagesResponse.json();

    const swissRound = document.getElementById('round0');

    const matchups = swissRound.querySelectorAll('.matchup');

    for (const [index, matchup] of matchups.entries()) {
        const j = index + 8;

        const teams = matchup.querySelectorAll('.matchup-team');

        const higherSeed = seeds[index];
        const lowerSeed = seeds[j];

        const higherSeedImg = createTeamImage(higherSeed.team, higherSeed.seed, 0);
        const lowerSeedImg = createTeamImage(lowerSeed.team, lowerSeed.seed, 0);

        teams[0].appendChild(higherSeedImg);
        teams[1].appendChild(lowerSeedImg);
    }

    const matchupsTeams = swissRound.querySelectorAll('.matchup-team');

    const teamImages = document.querySelectorAll('.team-img');

    const arrayFromTeams = Array.from(teamImages);

    if (arrayFromTeams.every(x => !x.src.includes("unknown"))) {

        matchupsTeams.forEach(mt => {
            mt.addEventListener('click', () => {
                selectTeam(mt);
            });
        });
    }
}

function createNextMatchups(mt) {
    const matchup = mt.parentElement;
    const swissRound = matchup.closest('.swiss-round');

    const number = getRoundNumber(swissRound);

    switch (number) {
        case 0:
            createRoundOneMatchups(swissRound);
            break;
        case 1:
            createRoundTwoMatchups(swissRound);
            break;
        case 2:
            createRoundThreeMatchups(swissRound);
            break;
        case 3:
            createRoundFourMatchups(swissRound);
            break;
        case 4:
            secureLastTeams(swissRound);
            break;
    }
}

function createRoundOneMatchups(swissRound) {
    const nextRound = document.getElementById('round1');

    const containers = nextRound.querySelectorAll('.swiss-pool-container');

    const advancedTeams = swissRound.querySelectorAll('.matchup-team.advanced');

    const losingTeams = swissRound.querySelectorAll('.matchup-team.eliminated');

    const oneZero = containers[0].querySelectorAll('.matchup');

    const zeroOne = containers[1].querySelectorAll('.matchup');

    if (oneZero.length > 0 && zeroOne.length > 0) {
        calculateMatchups(advancedTeams, oneZero);

        calculateMatchups(losingTeams, zeroOne);
    }

    resetStyles(nextRound);
}

function createRoundTwoMatchups(swissRound) {
    const nextRound = document.getElementById('round2');

    const containers = nextRound.querySelectorAll('.swiss-pool-container');

    const previousContainers = swissRound.querySelectorAll('.swiss-pool-container');

    const twoZeroTeams = previousContainers[0].querySelectorAll('.matchup-team.advanced');
    const twoZero = containers[0].querySelectorAll('.matchup');
    if (twoZeroTeams.length > 0)
        calculateMatchups(twoZeroTeams, twoZero);

    const oneOneTeams = [...previousContainers[0].querySelectorAll('.matchup-team.eliminated'), 
                        ...previousContainers[1].querySelectorAll('.matchup-team.advanced')];
    const oneOne = containers[1].querySelectorAll('.matchup');
    if (oneOneTeams.length === 8)
        calculateMatchups(oneOneTeams, oneOne);

    const zeroTwoTeams = previousContainers[1].querySelectorAll('.matchup-team.eliminated');

    const zeroTwo = containers[2].querySelectorAll('.matchup');

    if (zeroTwoTeams.length > 0) {
        calculateMatchups(zeroTwoTeams, zeroTwo);
    }

    resetStyles(nextRound);
}

function createRoundThreeMatchups(swissRound) {
    const nextRound = document.getElementById('round3');

    const containers = nextRound.querySelectorAll('.swiss-pool-container');

    const previousContainers = swissRound.querySelectorAll('.swiss-pool-container');

    const threeZeroTeams = previousContainers[0].querySelectorAll('.matchup-team.advanced');
    const threeZero = containers[0].querySelectorAll('.team');
    if (threeZeroTeams.length > 0)
        fillAdvancedOrEliminatedTeams(threeZeroTeams, threeZero);

    const twoOneTeams = [...previousContainers[0].querySelectorAll('.matchup-team.eliminated'), 
                            ...previousContainers[1].querySelectorAll('.matchup-team.advanced')];
    const twoOne = containers[1].querySelectorAll('.matchup');
    if (twoOneTeams.length > 0)
        calculateMatchups(twoOneTeams, twoOne);

    const oneTwoTeams = [...previousContainers[1].querySelectorAll('.matchup-team.eliminated'), 
                            ...previousContainers[2].querySelectorAll('.matchup-team.advanced')];
    const oneTwo = containers[2].querySelectorAll('.matchup');
    if (oneTwoTeams.length > 0)
        calculateMatchups(oneTwoTeams, oneTwo);

    const zeroThreeTeams = previousContainers[2].querySelectorAll('.matchup-team.eliminated');
    const zeroThree = containers[3].querySelectorAll('.team');
    if (zeroThreeTeams.length > 0)
        fillAdvancedOrEliminatedTeams(zeroThreeTeams, zeroThree);

    resetStyles(nextRound);
}

function createRoundFourMatchups(swissRound) {
    const nextRound = document.getElementById('round4');

    const containers = nextRound.querySelectorAll('.swiss-pool-container');

    const previousContainers = swissRound.querySelectorAll('.swiss-pool-container');

    const threeOneTeams = previousContainers[1].querySelectorAll('.matchup-team.advanced');
    const threeOne = containers[0].querySelectorAll('.team');
    if (threeOneTeams.length > 0)
        fillAdvancedOrEliminatedTeams(threeOneTeams, threeOne);

    const twoTwoTeams = [...previousContainers[1].querySelectorAll('.matchup-team.eliminated'),
                            ...previousContainers[2].querySelectorAll('.matchup-team.advanced')]
    const twoTwo = containers[2].querySelectorAll('.matchup');      
    
    if(twoTwoTeams.length > 0)
        calculateMatchups(twoTwoTeams, twoTwo);

    const oneThreeTeams = previousContainers[2].querySelectorAll('.matchup-team.eliminated');
    const oneThree = containers[4].querySelectorAll('.team');
    if (oneThreeTeams.length > 0)
        fillAdvancedOrEliminatedTeams(oneThreeTeams, oneThree);

    resetStyles(nextRound);
}

function secureLastTeams(swissRound) {
    const containers = swissRound.querySelectorAll('.swiss-pool-container');

    const threeTwoTeams = containers[2].querySelectorAll('.matchup-team.advanced');
    const threeTwo = containers[1].querySelectorAll('.team');
    if (threeTwoTeams.length > 0)
        fillAdvancedOrEliminatedTeams(threeTwoTeams, threeTwo);

    const twoThreeTeams = containers[2].querySelectorAll('.matchup-team.eliminated');
    const twoThree = containers[3].querySelectorAll('.team');
    if (twoThreeTeams.length > 0)
        fillAdvancedOrEliminatedTeams(twoThreeTeams, twoThree);
}

function calculateMatchups(incomingTeams, bracket) {
    let teams = Array.from(incomingTeams).map(at => at.querySelector('img').cloneNode(true));

    const teamsOrderedBySeed = orderBySeed(teams);

    const matchups = buildSwissMatchups(teamsOrderedBySeed);

    addMatchupsToBracket(matchups, bracket);
}

function createFinalisedMatchups(orderedTeamsInBracket) {

    let matchups = buildSwissMatchups(orderedTeamsInBracket);

    if (matchups.length >= 3)
        if (hasConflict(matchups))
            return matchups = resolveRepeatingMatchups(orderedTeamsInBracket, matchups);

    return matchups;
}

function addMatchupsToBracket(matchups, bracket) {

    for (const [index, matchup] of bracket.entries()) {
        let teams = matchups[index];

        if (!teams || teams.length === 0) continue;

        const matchupTeams = matchup.querySelectorAll('.matchup-team');

        const firstTeamImg = createTeamImage(teams[0].src.split('/').pop(),
            teams[0].getAttribute('seed'),
            teams[0].getAttribute('buchholz'));
        const secondTeamImg = createTeamImage(teams[1].src.split('/').pop(),
            teams[1].getAttribute('seed'),
            teams[1].getAttribute('buchholz'));

        addTeams(matchupTeams, firstTeamImg, secondTeamImg);
    }
}

function fillAdvancedOrEliminatedTeams(incomingTeams, bracket) {

    let teams = Array.from(incomingTeams).map(at => at.querySelector('img'));

    const firstTeam = createTeamImage(teams[0].src.split('/').pop(), teams[0].getAttribute('seed'));
    const secondTeam = createTeamImage(teams[1].src.split('/').pop(), teams[1].getAttribute('seed'));

    bracket[0].innerHTML = "";
    bracket[0].appendChild(firstTeam);

    bracket[1].innerHTML = "";
    bracket[1].appendChild(secondTeam);

    if (incomingTeams.length === 3) {
        const thirdTeam = createTeamImage(teams[2].src.split('/').pop(), teams[2].getAttribute('seed'));
        bracket[2].innerHTML = "";
        bracket[2].appendChild(thirdTeam);
    }
}

function resolveRepeatingMatchups(orderedTeamsInBracket, matchups) {

     if (matchups.length === 4) {
         matchups = swapConflict(matchups);

         if (!hasConflict(matchups)) {
             return matchups;
         }
     }

    if (matchups.length === 3) {

        for (let i = 0; i < sixMatchupPriorities.length; i++) {
            const candidate = shuffleMatchups(sixMatchupPriorities[i], orderedTeamsInBracket);

            if (!hasConflict(candidate)) {
                return candidate;
            }
        }
    }

    return matchups;
}

function shuffleMatchups(arrangementToTry, orderedTeamsInBracket) {

    const array =  [
        [orderedTeamsInBracket[(arrangementToTry[0][0]-1)], orderedTeamsInBracket[(arrangementToTry[0][1]-1)]],
        [orderedTeamsInBracket[(arrangementToTry[1][0]-1)], orderedTeamsInBracket[(arrangementToTry[1][1]-1)]],
        [orderedTeamsInBracket[(arrangementToTry[2][0]-1)], orderedTeamsInBracket[(arrangementToTry[2][1]-1)]]
    ];

    return array;
}

function buildSwissMatchups(orderedTeams) {
    const n = orderedTeams.length;

    if (n === 4) {
        return [
            [orderedTeams[0], orderedTeams[3]],
            [orderedTeams[1], orderedTeams[2]]
        ];
    }

    if (n === 6) {
        return [
            [orderedTeams[0], orderedTeams[5]],
            [orderedTeams[1], orderedTeams[4]],
            [orderedTeams[2], orderedTeams[3]]
        ];
    }

    if (n === 8) {
        return [
            [orderedTeams[0], orderedTeams[7]],
            [orderedTeams[1], orderedTeams[6]],
            [orderedTeams[2], orderedTeams[5]],
            [orderedTeams[3], orderedTeams[4]]
        ];
    }

    throw new Error("Swiss groups must contain 6 or 8 teams.");
}



function orderBySeed(teams) {
  return [...teams].sort((a, b) => {
    const seedA = Number(a.getAttribute("seed"));
    const seedB = Number(b.getAttribute("seed"));
    return seedA - seedB;
  });
} 

function orderSwissGroup(teams) {
  return [...teams].sort((a, b) => {
    const buchA = Number(a.getAttribute("buchholz"));
    const buchB = Number(b.getAttribute("buchholz"));
    if (buchA !== buchB) return buchB - buchA;

    const seedA = Number(a.getAttribute("seed"));
    const seedB = Number(b.getAttribute("seed"));
    return seedA - seedB;
  });
}

function hasConflict(matchups) {
    return matchups.some(([teamA, teamB]) => {
        const previousOpponents = getTeamsPreviousOpponents(teamA);
        const teamBSource = teamB.src.split("/").pop();
        return previousOpponents.includes(teamBSource);
    });
}

function swapConflict(matchups) {
    for (const [index, matchup] of matchups.entries()) {
        const previousOpponents = getTeamsPreviousOpponents(matchup[0]);
        const teamBSource = matchup[1].src.split("/").pop();
        if (previousOpponents.includes(teamBSource)) {

            if (index != 3) {
                const teamToSwap = matchups[index+1][1];
                matchups[index+1][1] = matchup[1];
                matchup[1] = teamToSwap;
            }
            else {
                const teamToSwap = matchups[index-1][1];
                matchups[index-1][1] = matchup[1];
                matchup[1] = teamToSwap;
            }

            return matchups;
        }
    }
    return matchups;
}


function addTeams(matchupTeams, higherTeam, lowerTeam) {
    matchupTeams[0].innerHTML = "";
    matchupTeams[0].className = "matchup-team";
    matchupTeams[0].appendChild(higherTeam);

    matchupTeams[1].innerHTML = "";
    matchupTeams[1].className = "matchup-team";
    matchupTeams[1].appendChild(lowerTeam);
}

function calculateNewBuchholzScores(teams) {

    if (teams.length < 16) {
        const threeZero = document.getElementById('3-0').querySelectorAll('.team-img');
        const zeroThree = document.getElementById('0-3').querySelectorAll('.team-img');

        teams = [...teams, ...threeZero, ...zeroThree];
    }

    for (const team of teams) {

        if (team.parentElement.className === "team") continue;

        const previousOpponents = getTeamsPreviousOpponents(team);

        let score = 0;

        for (const po of previousOpponents) { 
            const previousOpponent = Array.from(teams).find(t => t.src.split('/').pop() === po);

            const bracketId = getBracketId(previousOpponent);

            score += getScoreFromBracket(bracketId);
        }

        team.setAttribute('buchholz', score);
    }
}

function getBracketId(team) {
    const bracket = team.closest('.teams-container,.matchups-container');
    return bracket.id;
}

function getTeamsPreviousOpponents(team) {
    const imageSource = team.src.split('/').pop();

    const matchups = Array.from(document.querySelectorAll('.matchup')).filter(m => {
        const imgs = m.querySelectorAll('.team-img');

        return [...imgs].some(img => img.src.split('/').pop() === imageSource);
    });

    const opponents = matchups.map(matchup => {
        const imgs = [...matchup.querySelectorAll(".team-img")];
        return imgs.find(img => img.src.split('/').pop() !== imageSource);
    }).map(op => op.src.split('/').pop());

    opponents.pop();

    return opponents;
}

function getRoundNumber(swissRound) {
    const id = swissRound.id;

    const number = parseInt(id.match(/\d+/)[0], 10);

    return number;
}

function getScoreFromBracket(bracketId) {
    switch (bracketId) {
        case "0-0":
            return 0-0;    
        case "1-0":
            return 1-0;
        case "0-1":
            return 0-1
        case "1-1":
            return 1-1;
        case "2-1":
            return 2-1
        case "1-2":
            return 1-2;
        case "2-2":
            return 2-2
        case "2-0":
            return 2-0;
        case "0-2":
            return 0-2;
        case "3-0":
            return 3-0;
        case "0-3":
            return 0-3;
        case "3-1":
            return 3-1;
        case "1-3":
            return 1-3;
        case "0-3":
            return 0-3;
        case "3-2":
            return 3-2;
        case "2-3":
            return 2-3;
    } 
}