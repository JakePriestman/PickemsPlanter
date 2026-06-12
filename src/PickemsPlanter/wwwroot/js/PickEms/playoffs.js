function drawBracketConnectors() {
    const bracket = document.querySelector('.bracket');
    const existing = bracket.querySelector('.bracket-connectors');
    if (existing) existing.remove();

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.classList.add('bracket-connectors');
    bracket.appendChild(svg);

    const br = bracket.getBoundingClientRect();

    function rel(el) {
        const r = el.getBoundingClientRect();
        return {
            left:  r.left  - br.left,
            right: r.right - br.left,
            cy:    (r.top + r.bottom) / 2 - br.top,
        };
    }

    const qf    = [...document.querySelectorAll('.quarter-finals .match-card')].map(rel);
    const sf    = [...document.querySelectorAll('.semi-finals .match-card')].map(rel);
    const gf    = [...document.querySelectorAll('.grand-final .match-card')].map(rel);
    const champ = rel(document.querySelector('.champion-card'));

    const connectorStroke = 'rgba(232, 234, 240, 0.15)';
    const separatorStroke = 'rgba(232, 234, 240, 0.12)';
    const connectorPaths  = [];
    const separatorGaps   = new Map(); // midX → [[y1, y2], ...]

    function addBracketArm(top, bottom, target) {
        const midX  = (top.right + target.left) / 2;
        const midcy = (top.cy + bottom.cy) / 2;
        const r     = Math.min(6, (midX - top.right) / 2, (bottom.cy - top.cy) / 4);

        if (!separatorGaps.has(midX)) separatorGaps.set(midX, []);
        separatorGaps.get(midX).push([top.cy, bottom.cy]);

        connectorPaths.push(
            `M ${top.right} ${top.cy} ` +
            `H ${midX - r} ` +
            `Q ${midX} ${top.cy} ${midX} ${top.cy + r} ` +
            `V ${bottom.cy - r} ` +
            `Q ${midX} ${bottom.cy} ${midX - r} ${bottom.cy} ` +
            `H ${bottom.right}`
        );
        connectorPaths.push(`M ${midX} ${midcy} H ${target.left}`);
    }

    addBracketArm(qf[0], qf[1], sf[0]);
    addBracketArm(qf[2], qf[3], sf[1]);
    addBracketArm(sf[0], sf[1], gf[0]);

    const midX_gf_ch = (gf[0].right + champ.left) / 2;
    separatorGaps.set(midX_gf_ch, [[gf[0].cy, gf[0].cy]]);
    connectorPaths.push(`M ${gf[0].right} ${gf[0].cy} H ${champ.left}`);

    // Separators: draw at each midX with gradient fade toward connectors
    const bracketH  = br.height;
    const gapMargin = 28;
    const defs      = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
    svg.appendChild(defs);
    let gradId = 0;

    function drawSeparatorSegment(x, y1, y2, fadeTop, fadeBottom) {
        const id   = `sg${gradId++}`;
        const col  = separatorStroke;
        const none = 'rgba(232, 234, 240, 0)';
        const grad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        grad.setAttribute('id', id);
        grad.setAttribute('x1', '0'); grad.setAttribute('y1', String(y1));
        grad.setAttribute('x2', '0'); grad.setAttribute('y2', String(y2));
        grad.setAttribute('gradientUnits', 'userSpaceOnUse');

        const stops = (fadeTop && fadeBottom)
            ? [[0, none], [50, col], [100, none]]
            : [[0, fadeTop ? none : col], [100, fadeBottom ? none : col]];

        for (const [pct, color] of stops) {
            const s = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
            s.setAttribute('offset', `${pct}%`);
            s.setAttribute('stop-color', color);
            grad.appendChild(s);
        }
        defs.appendChild(grad);

        const p = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        p.setAttribute('d', `M ${x} ${y1} V ${y2}`);
        p.setAttribute('stroke', `url(#${id})`);
        p.setAttribute('stroke-width', '1');
        p.setAttribute('fill', 'none');
        svg.appendChild(p);
    }

    for (const [x, gaps] of separatorGaps) {
        const sorted = gaps.slice().sort((a, b) => a[0] - b[0]);
        let y = 0;
        for (let i = 0; i < sorted.length; i++) {
            const [g1, g2] = sorted[i];
            const segEnd = g1 - gapMargin;
            if (y < segEnd) drawSeparatorSegment(x, y, segEnd, i > 0, true);
            y = g2 + gapMargin;
        }
        if (y < bracketH) drawSeparatorSegment(x, y, bracketH, sorted.length > 0, false);
    }

    // Connector paths drawn on top of separators
    for (const d of connectorPaths) {
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', d);
        path.setAttribute('stroke', connectorStroke);
        path.setAttribute('stroke-width', '1');
        path.setAttribute('fill', 'none');
        svg.appendChild(path);
    }
}

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
    if (!picksAllowed) {
        document.getElementById('picksLockedBadge')?.classList.add('show');
    }

    const toastContainer = document.getElementById('toast-container');
    const bracketPanel = document.querySelector('.playoff-bracket');
    if (toastContainer && bracketPanel) bracketPanel.appendChild(toastContainer);

    drawBracketConnectors();
    initMobileTapMode();
});

window.addEventListener('resize', drawBracketConnectors);

const showResultsCheckmark = document.getElementById("showResults");

showResultsCheckmark.addEventListener('change', async () => {
    await showResultsAsync(showResultsCheckmark);
});

document.getElementById("clearAllPicks").addEventListener('click', () => {
    clearAllDropzones();
    showToast('Picks cleared', 'info');
});

document.getElementById("randomPicks").addEventListener('click', () => {
    selectRandomPicks();
    showToast('Random picks applied', 'info');
});