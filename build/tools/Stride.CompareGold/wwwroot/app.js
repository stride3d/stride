// === State ===
let allSuites = [];
let currentPlatform = '';
let sources = [];      // [{id, type, label}]
let sourceDefs = [];   // tracks how sources were added for persistence
let suiteData = {};    // {suite: {gold: [{name}], sourceImages: {srcId: [{name}]}}}
let focusedKey = null;        // "suite:name" — currently selected row shown in bottom detail pane
let loading = new Set();      // "suite:name" — loading in progress
let selected = new Set();     // "suite:name"
let collapsedSuites = new Set();
let compareLeft = {};         // {"suite:name": "gold:<platform>" or "src:<sourceId>"}
let compareRight = {};        // {"suite:name": "gold:<platform>" or "src:<sourceId>"}
let cellStats = {};           // {`${sourceId}:${suite}:${name}`: stats}

// === Init ===
async function init() {
  // Show Stride root path
  try {
    const infoRes = await fetch('/api/info');
    const info = await infoRes.json();
    document.getElementById('strideRoot').textContent = info.strideRoot;
  } catch {}

  const res = await fetch('/api/suites');
  allSuites = await res.json();

  // Collect all platforms across all suites
  const allPlatforms = new Set();
  for (const suite of allSuites) {
    const pRes = await fetch(`/api/platforms?suite=${enc(suite)}`);
    (await pRes.json()).forEach(p => allPlatforms.add(p));
  }
  const platforms = [...allPlatforms].sort();
  const sel = document.getElementById('platformSelect');
  sel.innerHTML = platforms.map(p => `<option value="${p}">${p}</option>`).join('');
  // Use platform from restoreState() if valid, otherwise default to first
  if (!currentPlatform || !platforms.includes(currentPlatform))
    currentPlatform = platforms[0] || '';
  sel.value = currentPlatform;
  sel.onchange = onPlatformChange;

  document.getElementById('statusFilter').onchange = () => render();
  await reload();
}

async function onPlatformChange() {
  currentPlatform = document.getElementById('platformSelect').value;
  await reload();
}

async function reload() {
  if (!currentPlatform) return;
  suiteData = {};
  for (const suite of allSuites) {
    const gRes = await fetch(`/api/gold/images?suite=${enc(suite)}&platform=${enc(currentPlatform)}`);
    const gData = await gRes.json();
    // Merge primary + fallback gold, tagging fallbacks
    const gold = [
      ...(gData.images || []).map(g => ({ name: g.name, fallback: null })),
      ...(gData.fallbacks || []).map(g => ({ name: g.name, fallback: g.fallbackPlatform }))
    ];
    const srcImgs = {};
    for (const src of sources) {
      const sRes = await fetch(`/api/source/${src.id}/images?suite=${enc(suite)}&platform=${enc(currentPlatform)}`);
      srcImgs[src.id] = await sRes.json();
    }
    // Load thresholds for this suite
    const tRes = await fetch(`/api/thresholds?suite=${enc(suite)}`);
    const thresholdRules = tRes.ok ? await tRes.json() : [];
    // Only include suite if it has any images
    if (gold.length > 0 || Object.values(srcImgs).some(imgs => imgs.length > 0))
      suiteData[suite] = { gold, sourceImages: srcImgs, thresholdRules };
  }
  cellStats = {};
  resetAltGoldState();
  render();
}

// === Sources ===
async function addLocalSource() {
  const res = await fetch('/api/sources/add-local', { method: 'POST' });
  if (!res.ok) { alert(await res.text()); return; }
  const src = await res.json();
  sources.push(src);
  sourceDefs.push({ type: 'local' });
  await reload();
}

function showCiModal() {
  document.getElementById('ciModal').style.display = 'flex';
  selectedCiRun = null;
  document.getElementById('ciRunId').value = '';
  document.getElementById('ciDownloadBtn').disabled = true;
  loadCiRuns();
}
function closeCiModal() { document.getElementById('ciModal').style.display = 'none'; }

async function downloadCiRun() {
  const runId = String(document.getElementById('ciRunId').value).trim();
  if (!runId) { alert('Enter or select a run ID'); return; }

  const artifacts = selectedCiArtifacts.size > 0 ? [...selectedCiArtifacts] : ['test-artifacts-linux-vulkan'];

  const btn = document.getElementById('ciDownloadBtn');
  btn.disabled = true;

  try {
    for (let i = 0; i < artifacts.length; i++) {
      btn.textContent = `Downloading ${i + 1}/${artifacts.length}...`;
      const res = await fetch('/api/sources/add-ci', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ runId: String(runId), artifactName: artifacts[i], label: `CI #${String(runId).substring(0, 5)}` })
      });
      if (!res.ok) { alert(`Failed to download ${artifacts[i]}: ${await res.text()}`); continue; }
      const src = await res.json();
      // Only add if not already in sources list
      if (!sources.find(s => s.id === src.id)) {
        sources.push(src);
        sourceDefs.push({ type: 'ci', runId: String(runId), artifactName: artifacts[i], label: src.label });
      }
    }
    closeCiModal();
    await reload();
  } finally {
    btn.textContent = 'Download & Add';
    btn.disabled = false;
  }
}

async function removeSource(id) {
  const idx = sources.findIndex(s => s.id === id);
  await fetch(`/api/sources/${id}`, { method: 'DELETE' });
  sources = sources.filter(s => s.id !== id);
  if (idx >= 0) sourceDefs.splice(idx, 1);
  cellStats = {};
  resetAltGoldState();
  focusedKey = null;
  selected.clear();
  compareLeft = {};
  compareRight = {};
  if (sources.length === 0) {
    suiteData = {};
    render();
  } else {
    await reload();
  }
}

// === Render ===
function render() {
  renderSourceTags();
  renderPromoteSourceSelect();
  renderTable();
  updateActionCounts();
  // If the focused row is no longer visible (platform switch, filter, search, etc.),
  // clear the detail pane so it doesn't keep showing a stale selection.
  if (focusedKey && !document.querySelector(`tr.row[data-kb-key="${CSS.escape(focusedKey)}"]`)) {
    focusedKey = null;
    kbFocusKey = null;
    renderDetailPane(null);
  }
}

function renderSourceTags() {
  const el = document.getElementById('sourceTags');
  el.innerHTML = sources.map(s =>
    `<div class="source-tag ${s.type}">
      ${esc(s.label)}
      <span class="remove" onclick="removeSource('${s.id}')">&times;</span>
    </div>`
  ).join('');
}

function renderPromoteSourceSelect() {
  const sel = document.getElementById('promoteSource');
  sel.innerHTML = `<option value="__active__">Active source</option>` +
    sources.map(s => `<option value="${s.id}">${esc(s.label)}</option>`).join('');
}

function buildSuiteImages(suite) {
  const data = suiteData[suite];
  if (!data) return [];
  const allNames = new Set();
  data.gold.forEach(g => allNames.add(g.name));
  for (const srcId in data.sourceImages)
    data.sourceImages[srcId].forEach(s => allNames.add(s.name));

  return [...allNames].sort(naturalSort).map(name => {
    const goldEntry = data.gold.find(g => g.name === name);
    const hasGold = !!goldEntry;
    const goldFallback = goldEntry?.fallback || null;
    const sourcesWithImage = {};
    for (const src of sources)
      sourcesWithImage[src.id] = (data.sourceImages[src.id] || []).some(s => s.name === name);
    let status = 'pass';
    if (!hasGold && Object.values(sourcesWithImage).some(v => v)) status = 'new';
    else if (hasGold && Object.values(sourcesWithImage).some(v => v)) {
      // Per source: fail only when every gold in the suite fails — mirrors
      // Graphics.Regression's any-match semantics. The alternate-gold check
      // is lazy (kicked off when the preferred fails), so a cell whose
      // preferred gold failed stays "pending" until that completes.
      let anyFail = false;
      let anyPending = false;
      for (const src of sources) {
        if (!sourcesWithImage[src.id]) continue;
        const stats = cellStats[`${src.id}:${suite}:${name}`];
        if (!stats) { anyPending = true; computeCellStats(src.id, suite, name); continue; }
        const r = isCellPassing(src.id, suite, name, stats);
        if (r === null) anyPending = true;
        else if (!r) anyFail = true;
      }
      status = anyFail ? 'fail' : anyPending ? 'pending' : 'pass';
    }
    return { suite, name, hasGold, goldFallback, sourcesWithImage, status };
  });
}

function renderTable() {
  const filter = document.getElementById('statusFilter').value;

  // Render header
  const thead = document.getElementById('thead');
  thead.innerHTML = `<tr>
    <th class="cb"><input type="checkbox" id="selectAll" onchange="toggleSelectAll()"></th>
    <th>Test</th>
    <th>Gold</th>
    ${sources.map(s => `<th>${esc(s.label)}</th>`).join('')}
  </tr>`;

  const tbody = document.getElementById('tbody');
  tbody.innerHTML = '';
  let totalVisible = 0;

  const search = (document.getElementById('searchFilter')?.value || '').toLowerCase();

  for (const suite of Object.keys(suiteData).sort()) {
    let images = buildSuiteImages(suite);
    if (filter) images = images.filter(i => i.status === filter || (filter === 'fail' && i.status === 'pending'));
    if (search) images = images.filter(i => i.name.toLowerCase().includes(search));
    if (images.length === 0) continue;

    const sortMode = document.getElementById('sortSelect')?.value || 'name';
    if (sortMode === 'diff') {
      images.sort((a, b) => {
        const aMax = getMaxDiffForImage(a) ?? -1;
        const bMax = getMaxDiffForImage(b) ?? -1;
        return bMax - aMax; // worst first
      });
    }

    const failCount = images.filter(i => i.status === 'fail' || i.status === 'new').length;
    const pendingCount = images.filter(i => i.status === 'pending').length;
    const isCollapsed = collapsedSuites.has(suite);
    const shortSuite = suite.replace('Stride.', '').replace('.Tests', '').replace('.Regression', '');

    // Suite header row
    const suiteKeys = images.map(i => `${i.suite}:${i.name}`);
    const allSelected = suiteKeys.every(k => selected.has(k));
    const someSelected = !allSelected && suiteKeys.some(k => selected.has(k));
    const suiteTr = document.createElement('tr');
    suiteTr.className = 'suite-row';
    suiteTr.dataset.kbKey = suite;
    suiteTr.innerHTML = `
      <td class="cb"><input type="checkbox" ${allSelected ? 'checked' : ''} ${someSelected ? 'indeterminate' : ''}
        onclick="event.stopPropagation(); toggleSelectSuite('${esc(suite)}', this.checked)"></td>
      <td colspan="${2 + sources.length}">
        <span class="suite-toggle">${isCollapsed ? '▶' : '▼'}</span>
        <strong>${esc(shortSuite)}</strong>
        <span class="suite-badge">${images.length} tests</span>
        <span class="suite-badge fail" data-suite-fail="${esc(suite)}" ${failCount > 0 ? '' : 'style="display:none"'}>${failCount} failing</span><span class="suite-badge pending" data-suite-pending="${esc(suite)}" ${pendingCount > 0 ? '' : 'style="display:none"'}>${pendingCount} pending</span>
      </td>`;
    suiteTr.onclick = () => { toggleSuite(suite); };
    tbody.appendChild(suiteTr);
    // Set indeterminate state (can't do via HTML attribute)
    if (someSelected) suiteTr.querySelector('input[type=checkbox]').indeterminate = true;

    if (isCollapsed) continue;

    for (const img of images) {
      totalVisible++;
      const key = `${img.suite}:${img.name}`;

      const tr = document.createElement('tr');
      tr.className = `row${focusedKey === key ? ' kb-focus' : ''}`;
      tr.dataset.kbKey = key;
      tr.innerHTML = buildRowCells(img, key);
      tr.onmousedown = (e) => { tr._clickX = e.clientX; tr._clickY = e.clientY; };
      tr.onclick = (e) => {
        if (Math.abs(e.clientX - tr._clickX) > 3 || Math.abs(e.clientY - tr._clickY) > 3) return;
        focusRow(key);
      };
      tbody.appendChild(tr);
    }
  }
  document.getElementById('emptyMsg').style.display = totalVisible === 0 ? 'block' : 'none';
  updateSelectedCount();
}

function buildRowCells(img, key) {
  const isLoading = loading.has(key);
  const isSel = selected.has(key);

  let goldThumb = '';
  if (img.hasGold && sources.length > 0) {
    const thumbUrl = `/api/gold/image?suite=${enc(img.suite)}&platform=${enc(currentPlatform)}&name=${enc(img.name)}`;
    goldThumb = `<div class="thumb-row"><img class="thumb" src="${thumbUrl}"></div>`;
  }

  let cells = `
    <td class="cb"><input type="checkbox" ${isSel ? 'checked' : ''} onclick="event.stopPropagation(); toggleSelect('${esc(key)}')"></td>
    <td style="padding-left:24px">${esc(img.name)}${isLoading ? ' <span class="spinner"></span>' : ''}<span data-row-tag="${esc(key)}">${img.status === 'fail' ? '<span class="tag-fail">failing</span>' : img.status === 'new' ? '<span class="tag-new">new</span>' : img.status === 'pending' ? '<span class="tag-pending">...</span>' : ''}</span></td>
    <td><span class="cell ${img.goldFallback ? 'miss' : 'ref'}">${img.hasGold ? (img.goldFallback ? 'fb' : 'ref') : '—'}</span>${img.hasGold ? ` <span style="font-size:10px;color:#666">${esc(img.goldFallback || currentPlatform)}</span>` : ''}${goldThumb}</td>`;

  const activeRef = compareRight[key] || `src:${getSourceForKey(key)}`;
  for (const src of sources) {
    const has = img.sourcesWithImage[src.id];
    const isActive = activeRef === `src:${src.id}`;
    const statsKey = `${src.id}:${img.suite}:${img.name}`;
    const stats = cellStats[statsKey];
    let cellHtml;
    if (!has) {
      cellHtml = '<span class="cell miss">—</span>';
    } else if (!img.hasGold) {
      cellHtml = '<span class="cell new">○ new</span>';
    } else if (stats) {
      const result = checkCellThreshold(img.suite, img.name, stats);
      const cls = result.passed ? 'pass' : 'fail';
      const brief = formatThresholdBrief(result);
      cellHtml = `<span class="cell ${cls}">${cls === 'pass' ? '✓' : '✗'} ${brief}</span>`;
    } else {
      cellHtml = `<span class="cell" data-stats-key="${esc(statsKey)}" style="color:#666">...</span>`;
      computeCellStats(src.id, img.suite, img.name);
    }
    if (has) {
      const thumbSrc = `/api/source/${src.id}/image?suite=${enc(img.suite)}&platform=${enc(currentPlatform)}&name=${enc(img.name)}`;
      if (img.hasGold) {
        const thumbId = `thumb-${css(src.id)}-${css(key)}`;
        cellHtml += `<div class="thumb-row"><img class="thumb" src="${thumbSrc}"><canvas class="thumb" id="${thumbId}"></canvas></div>`;
        requestAnimationFrame(() => computeThumbDiff(img.suite, img.name, src.id, thumbId));
      } else {
        cellHtml += `<div class="thumb-row"><img class="thumb" src="${thumbSrc}"></div>`;
      }
    }
    cells += `<td onclick="event.stopPropagation(); setActiveSource('${esc(key)}','${src.id}')"${isActive ? ' class="active-source"' : ''}>${cellHtml}</td>`;
  }
  return cells;
}

function renderRow(key) {
  const suite = key.substring(0, key.indexOf(':'));
  const data = suiteData[suite];
  if (!data) return;
  const images = buildSuiteImages(suite);
  const img = images.find(i => `${i.suite}:${i.name}` === key);
  if (!img) return;

  const existingTr = document.querySelector(`tr.row[data-kb-key="${CSS.escape(key)}"]`);
  if (!existingTr) return;

  existingTr.className = `row${focusedKey === key ? ' kb-focus' : ''}`;
  existingTr.innerHTML = buildRowCells(img, key);
  existingTr.onmousedown = (e) => { existingTr._clickX = e.clientX; existingTr._clickY = e.clientY; };
  existingTr.onclick = (e) => {
    if (Math.abs(e.clientX - existingTr._clickX) > 3 || Math.abs(e.clientY - existingTr._clickY) > 3) return;
    focusRow(key);
  };
  updateSelectedCount();
  saveState();
}

function toggleSuite(suite) {
  if (collapsedSuites.has(suite)) collapsedSuites.delete(suite);
  else collapsedSuites.add(suite);
  render();
}

// === Detail ===
const detailVersion = {}; // track version to discard stale loads

function resolveImageRef(ref, suite, name) {
  if (!ref) return null;

  if (ref.startsWith('gold:')) {
    const plat = ref.slice(5);
    return `/api/gold/image?suite=${enc(suite)}&platform=${enc(plat)}&name=${enc(name)}`;
  }
  if (ref.startsWith('src:')) {
    const srcId = ref.slice(4);
    return `/api/source/${srcId}/image?suite=${enc(suite)}&platform=${enc(currentPlatform)}&name=${enc(name)}`;
  }
  return null;
}

function buildRefOptions(goldPlatforms, selectedRef) {
  let html = '<optgroup label="Gold">';
  if (goldPlatforms.length === 0) html += '<option value="" disabled>No gold</option>';
  for (const p of goldPlatforms)
    html += `<option value="gold:${esc(p.platform)}" ${selectedRef === 'gold:' + p.platform ? 'selected' : ''}>${esc(p.platform)}</option>`;
  html += '</optgroup>';
  if (sources.length > 0) {
    html += '<optgroup label="Sources">';
    for (const s of sources)
      html += `<option value="src:${esc(s.id)}" ${selectedRef === 'src:' + s.id ? 'selected' : ''}>${esc(s.label)}</option>`;
    html += '</optgroup>';
  }
  return html;
}

function pickDefaultLeft(goldPlatforms, img) {
  // Match the gold the table row shows: primary if it exists for currentPlatform,
  // otherwise the fallback the backend resolved. Only fall back to the scoring
  // heuristic when the row has no gold at all.
  if (img?.hasGold) {
    const plat = img.goldFallback || currentPlatform;
    if (goldPlatforms.some(p => p.platform === plat)) return `gold:${plat}`;
  }
  const best = pickBestGoldPlatform(goldPlatforms, currentPlatform);
  return best ? `gold:${best}` : (sources[0] ? `src:${sources[0].id}` : '');
}

function pickDefaultRight(img) {
  // Prefer first source that has this image
  const src = sources.find(s => img ? img.sourcesWithImage?.[s.id] : false);
  if (src) return `src:${src.id}`;
  return sources[0] ? `src:${sources[0].id}` : '';
}

async function loadDetail(suite, name) {
  const key = `${suite}:${name}`;
  const id = css(key);

  const ver = (detailVersion[key] || 0) + 1;
  detailVersion[key] = ver;

  const data = suiteData[suite];
  const img = data ? buildSuiteImages(suite).find(i => i.name === name) : null;

  // Fetch gold platforms
  const goldPlatforms = await fetch(`/api/gold/all?suite=${enc(suite)}&name=${enc(name)}`).then(r => r.json()).catch(() => []);
  if (detailVersion[key] !== ver) return;

  // Resolve left and right refs
  const leftHadUserChoice = compareLeft[key] != null;
  const rightHadUserChoice = compareRight[key] != null;
  const leftRef = compareLeft[key] || pickDefaultLeft(goldPlatforms, img);
  const rightRef = compareRight[key] || pickDefaultRight(img);
  const leftUrl = resolveImageRef(leftRef, suite, name);
  const rightUrl = resolveImageRef(rightRef, suite, name);

  // Load images in parallel
  const [leftImg, rightImg] = await Promise.all([
    leftUrl ? loadImg(leftUrl).catch(() => null) : null,
    rightUrl ? loadImg(rightUrl).catch(() => null) : null,
  ]);
  if (detailVersion[key] !== ver) return;

  fillDetail(id, key, suite, name, { ver, leftImg, rightImg, goldPlatforms, leftRef, rightRef, img, leftHadUserChoice, rightHadUserChoice });
}

async function fillDetail(id, key, suite, name, { ver, leftImg, rightImg, goldPlatforms, leftRef, rightRef, img, leftHadUserChoice, rightHadUserChoice }) {
  detailVersion[key] = ver;
  const container = document.getElementById(`images-${id}`);
  if (!container) return;

  // Build dropdowns
  const leftOpts = buildRefOptions(goldPlatforms, leftRef);
  const rightOpts = buildRefOptions(goldPlatforms, rightRef);
  const leftSelHtml = `<select onchange="switchDetailSide('${esc(key)}','left',this.value)">${leftOpts}</select>`;
  const rightSelHtml = `<select onchange="switchDetailSide('${esc(key)}','right',this.value)">${rightOpts}</select>`;

  // Build DOM
  let html = `<div class="images-row" id="zoomgroup-${id}">`;
  html += `<div class="image-box"><div class="lbl">${leftSelHtml}</div>`;
  if (leftImg) html += `<div class="zoom-container"><div class="zoom-inner"><canvas id="left-${id}"></canvas></div></div>`;
  else html += `<div class="empty-msg" style="padding:20px">No image</div>`;
  html += `</div>`;
  html += `<div class="image-box"><div class="lbl">${rightSelHtml}</div>`;
  if (rightImg) html += `<div class="zoom-container"><div class="zoom-inner"><canvas id="right-${id}"></canvas></div></div>`;
  else html += `<div class="empty-msg" style="padding:20px">No image</div>`;
  html += `</div>`;
  html += `<div class="image-box"><div class="lbl">Diff</div>`;
  if (leftImg && rightImg) html += `<div class="zoom-container"><div class="zoom-inner"><canvas id="diff-${id}"></canvas></div></div>`;
  else html += `<div class="empty-msg" style="padding:20px">—</div>`;
  html += `</div>`;
  html += '</div>';
  html += `<div class="detail-footer"><div class="zoom-controls">Scroll to zoom · Drag to pan · <button class="secondary" onclick="resetZoom('${id}')">Reset</button></div><div class="stats" id="stats-${id}"></div></div>`;
  container.innerHTML = html;

  if (leftImg) drawToCanvas(document.getElementById(`left-${id}`), leftImg);
  if (rightImg) drawToCanvas(document.getElementById(`right-${id}`), rightImg);
  if (leftImg && rightImg) {
    const canvas = document.getElementById(`diff-${id}`);
    const stats = computeImageDiff(leftImg, rightImg, canvas);
    // Resolve threshold for this image
    const data = suiteData[suite];
    const rules = data?.thresholdRules || [];
    const [platApi, device] = currentPlatform.split('/');
    const dotIdx = platApi?.indexOf('.') ?? -1;
    const plat = dotIdx >= 0 ? platApi.substring(0, dotIdx) : platApi;
    const api = dotIdx >= 0 ? platApi.substring(dotIdx + 1) : null;
    const allow = resolveThreshold(rules, name, plat, api, device);
    const thresholdResult = stats.pixelDiffs ? checkThreshold(stats.pixelDiffs, allow) : null;
    const statsEl = document.getElementById(`stats-${id}`);
    if (statsEl) statsEl.innerHTML = formatStats(stats, thresholdResult);
  }
  initZoomGroup(id);
  updatePaneBounds(leftImg || rightImg);

  // Compute compact stats for gold options vs the other side
  const otherImg = rightImg || leftImg;
  if (otherImg && goldPlatforms.length > 0) {
    const leftSel = container.querySelector(`select`);
    const rightSel = container.querySelectorAll(`select`)[1];
    for (const sel of [leftSel, rightSel]) {
      if (!sel) continue;
      const isLeft = sel === leftSel;
      const hadUserChoice = isLeft ? leftHadUserChoice : rightHadUserChoice;
      const otherRef = isLeft ? rightRef : leftRef;
      const otherImgForStats = isLeft ? rightImg : leftImg;
      if (!otherImgForStats) continue;
      const goldOpts = [...sel.options].filter(o => o.value.startsWith('gold:'));
      // Compute diffs for all gold options, then auto-select best passing one
      const optResults = [];
      await Promise.all(goldOpts.map(async (opt) => {
        const plat = opt.value.slice(5);
        try {
          const gImg = await loadImg(`/api/gold/image?suite=${enc(suite)}&platform=${enc(plat)}&name=${enc(name)}`);
          if (detailVersion[key] !== ver) return;
          const tmpCanvas = new OffscreenCanvas(gImg.width, gImg.height);
          const s = computeImageDiff(gImg, otherImgForStats, tmpCanvas);
          const result = checkCellThreshold(suite, name, s);
          const icon = result.passed ? '\u2713' : '\u2717';
          opt.textContent = `${icon} ${plat} (d=${s.maxDiff} px=${s.diffPixels})`;
          opt.dataset.passed = result.passed ? '1' : '0';
          opt.style.color = result.passed ? '#4caf50' : '#f44336';
          optResults.push({ opt, result, diffPixels: s.diffPixels });
        } catch {}
      }));
      // If currently selected gold fails, auto-switch to best passing one —
      // but only when the user hasn't explicitly picked this side, otherwise
      // we'd override their choice every time loadDetail re-renders.
      if (!hadUserChoice) {
        const selOpt = sel.options[sel.selectedIndex];
        if (selOpt?.dataset.passed === '0') {
          const passing = optResults.filter(r => r.result.passed).sort((a, b) => a.diffPixels - b.diffPixels);
          if (passing.length > 0) {
            passing[0].opt.selected = true;
            sel.dispatchEvent(new Event('change'));
          }
        }
      }
      const updateSelColor = () => {
        const so = sel.options[sel.selectedIndex];
        sel.style.color = so?.style.color || '';
      };
      updateSelColor();
      sel.addEventListener('change', updateSelColor);
    }
  }
}

// === Background cell stats ===
const statsQueue = new Set();
let statsRunning = false;
// "srcId:suite:name" → { checked: bool, passingPlatform: string|null }.
// Populated once the preferred gold has been checked against alternates.
// Drives the framework-style "any gold passes → cell passes" verdict.
const altGoldStatus = {};
// "suite:name" → { platform } when the row has a primary gold at currentPlatform
// that fails while some alternate passes. Used by Delete Gold to clean up the
// now-unneeded primary.
const fixableVia = {};

function resetAltGoldState() {
  Object.keys(altGoldStatus).forEach(k => delete altGoldStatus[k]);
  Object.keys(fixableVia).forEach(k => delete fixableVia[k]);
}

function computeCellStats(srcId, suite, name) {
  const key = `${srcId}:${suite}:${name}`;
  if (cellStats[key] || statsQueue.has(key)) return;
  statsQueue.add(key);
  if (!statsRunning) runStatsQueue();
}

async function runStatsQueue() {
  statsRunning = true;
  const BATCH = 8;
  while (statsQueue.size > 0) {
    const batch = [];
    for (const key of statsQueue) {
      batch.push(key);
      if (batch.length >= BATCH) break;
    }
    batch.forEach(k => statsQueue.delete(k));

    await Promise.all(batch.map(async (key) => {
      const parts = key.split(':');
      const srcId = parts[0];
      const suite = parts[1];
      const name = parts.slice(2).join(':');
      try {
        const goldUrl = `/api/gold/image?suite=${enc(suite)}&platform=${enc(currentPlatform)}&name=${enc(name)}`;
        const srcUrl = `/api/source/${srcId}/image?suite=${enc(suite)}&platform=${enc(currentPlatform)}&name=${enc(name)}`;
        const [goldImg, srcImg] = await Promise.all([loadImg(goldUrl), loadImg(srcUrl)]);
        const canvas = new OffscreenCanvas(goldImg.width, goldImg.height);
        const stats = computeImageDiff(goldImg, srcImg, canvas);
        cellStats[key] = stats;
        updateCellInline(key, stats);
        // If the preferred gold fails, scan the rest of the suite — the
        // framework passes the test if any gold matches, so we do too.
        const result = checkCellThreshold(suite, name, stats);
        if (!result.passed) {
          await checkAlternateGold(srcId, suite, name, srcImg);
          // Re-render cell and row tag with the final verdict (may flip to pass).
          updateCellInline(key, stats);
          scheduleCountUpdate();
        }
      } catch (e) {
        // Skip failed comparisons
      }
    }));
  }
  statsRunning = false;
  // Final count/badge update (no full render — everything was updated inline)
  updateActionCounts();
  updateSuiteBadges();
}

function checkCellThreshold(suite, name, stats) {
  const data = suiteData[suite];
  const rules = data?.thresholdRules || [];
  const [platApi, device] = currentPlatform.split('/');
  const dotIdx = platApi?.indexOf('.') ?? -1;
  const plat = dotIdx >= 0 ? platApi.substring(0, dotIdx) : platApi;
  const api = dotIdx >= 0 ? platApi.substring(dotIdx + 1) : null;
  const allow = resolveThreshold(rules, name, plat, api, device);
  if (stats.pixelDiffs) return checkThreshold(stats.pixelDiffs, allow);
  // Fallback for stats without pixelDiffs
  return { passed: stats.diffPixels === 0, details: [] };
}

// Returns true (pass), false (fail), or null (pending) — pending means the
// preferred gold failed but the alternate-gold scan hasn't finished yet.
function isCellPassing(srcId, suite, name, stats) {
  if (checkCellThreshold(suite, name, stats).passed) return true;
  // Graphics.Regression only enumerates fallbacks when the primary gold for
  // the current platform doesn't exist. If it does exist, that single file is
  // the sole judge — a passing alternate doesn't rescue a failing primary.
  const goldEntry = suiteData[suite]?.gold.find(g => g.name === name);
  if (goldEntry && goldEntry.fallback == null) return false;
  const alt = altGoldStatus[`${srcId}:${suite}:${name}`];
  if (!alt || !alt.checked) return null;
  return alt.passingPlatform != null;
}

function updateCellInline(key, stats) {
  const el = document.querySelector(`[data-stats-key="${CSS.escape(key)}"]`);
  const parts = key.split(':');
  const srcId = parts[0];
  const suite = parts[1];
  const name = parts.slice(2).join(':');
  const result = checkCellThreshold(suite, name, stats);
  const passing = isCellPassing(srcId, suite, name, stats);
  const cls = passing === true ? 'pass' : passing === false ? 'fail' : 'pending';
  if (el) {
    el.className = `cell ${cls}`;
    el.removeAttribute('style');
    el.removeAttribute('data-stats-key');
    const icon = passing === true ? '✓' : passing === false ? '✗' : '…';
    const brief = formatThresholdBrief(result);
    const viaAlt = passing === true && !result.passed ? ' (via alt)' : '';
    el.innerHTML = `${icon} ${brief}${viaAlt}`;
  }
  // Update the row tag once all sources for this image are resolved
  const rowKey = `${suite}:${name}`;
  const data = suiteData[suite];
  if (!data) return;
  let anyFail = false, anyPending = false;
  for (const src of sources) {
    if (!(data.sourceImages[src.id] || []).some(s => s.name === name)) continue;
    const s = cellStats[`${src.id}:${suite}:${name}`];
    if (!s) { anyPending = true; continue; }
    const r = isCellPassing(src.id, suite, name, s);
    if (r === null) anyPending = true;
    else if (!r) anyFail = true;
  }
  const tagEl = document.querySelector(`[data-row-tag="${CSS.escape(rowKey)}"]`);
  if (tagEl) {
    if (anyFail) tagEl.innerHTML = '<span class="tag-fail">failing</span>';
    else if (anyPending) tagEl.innerHTML = '<span class="tag-pending">...</span>';
    else tagEl.innerHTML = '';
  }
  if (!anyFail && !anyPending) {
    // Hide row if it no longer matches the active filter
    const filter = document.getElementById('statusFilter').value;
    if (filter && filter !== 'pass') {
      const tr = document.querySelector(`tr.row[data-kb-key="${CSS.escape(rowKey)}"]`);
      if (tr) {
        tr.style.display = 'none';
        const next = tr.nextElementSibling;
        if (next && !next.classList.contains('row') && !next.classList.contains('suite-row'))
          next.style.display = 'none';
      }
    }
    scheduleCountUpdate();
  }
}

async function checkAlternateGold(srcId, suite, name, srcImg) {
  const key = `${srcId}:${suite}:${name}`;
  if (altGoldStatus[key]?.checked) return;
  let passingPlatform = null;
  try {
    const platforms = await fetch(`/api/gold/all?suite=${enc(suite)}&name=${enc(name)}`).then(r => r.json());
    for (const p of platforms) {
      if (p.platform === currentPlatform) continue;
      try {
        const gImg = await loadImg(`/api/gold/image?suite=${enc(suite)}&platform=${enc(p.platform)}&name=${enc(name)}`);
        const canvas = new OffscreenCanvas(gImg.width, gImg.height);
        const s = computeImageDiff(gImg, srcImg, canvas);
        if (checkCellThreshold(suite, name, s).passed) {
          passingPlatform = p.platform;
          break;
        }
      } catch {}
    }
  } catch {}
  altGoldStatus[key] = { checked: true, passingPlatform };
  // Record a "fixable" hit only when the primary gold lives at currentPlatform
  // — deleting a fallback path would either be a no-op or hurt other platforms.
  const goldEntry = suiteData[suite]?.gold.find(g => g.name === name);
  if (passingPlatform && goldEntry && goldEntry.fallback == null) {
    const fixKey = `${suite}:${name}`;
    fixableVia[fixKey] = { platform: passingPlatform, goldFallback: currentPlatform };
  }
}

async function computeThumbDiff(suite, name, srcId, canvasId) {
  try {
  
    const goldUrl = `/api/gold/image?suite=${enc(suite)}&platform=${enc(currentPlatform)}&name=${enc(name)}`;
    const srcUrl = `/api/source/${srcId}/image?suite=${enc(suite)}&platform=${enc(currentPlatform)}&name=${enc(name)}`;
    const [goldImg, srcImg] = await Promise.all([loadImg(goldUrl), loadImg(srcUrl)]);
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const stats = computeImageDiff(goldImg, srcImg, canvas);
    // Also cache stats
    cellStats[`${srcId}:${suite}:${name}`] = stats;
  } catch { }
}


// === Actions ===
function focusRow(key) {
  if (!key) {
    focusedKey = null;
    kbFocusKey = null;
    document.querySelectorAll('tr.kb-focus').forEach(r => r.classList.remove('kb-focus'));
    renderDetailPane(null);
    saveState();
    return;
  }
  focusedKey = key;
  kbFocusKey = key;
  // Update border on rows without full re-render
  document.querySelectorAll('tr.kb-focus').forEach(r => r.classList.remove('kb-focus'));
  const row = document.querySelector(`tr[data-kb-key="${CSS.escape(key)}"]`);
  if (row) {
    row.classList.add('kb-focus');
    row.scrollIntoView({ block: 'nearest' });
  }
  renderDetailPane(key);
  saveState();
}

function renderDetailPane(key) {
  const pane = document.getElementById('detailPaneContent');
  if (!pane) return;
  if (!key) {
    pane.innerHTML = `<div class="detail-pane-empty">No selection</div>`;
    return;
  }
  const colon = key.indexOf(':');
  const suite = key.substring(0, colon);
  const name = key.substring(colon + 1);
  const id = css(key);
  pane.innerHTML = `<div class="detail" id="detail-${id}"><div id="images-${id}">Loading...</div></div>`;
  loadDetail(suite, name);
}

function setActiveSource(key, srcId) {
  compareRight[key] = `src:${srcId}`;
  // Update active-source highlight inline
  const row = document.querySelector(`tr.row[data-kb-key="${CSS.escape(key)}"]`);
  if (row) {
    row.querySelectorAll('td[onclick]').forEach(td => {
      const match = td.getAttribute('onclick')?.match(/setActiveSource\('[^']*','([^']*)'\)/);
      td.classList.toggle('active-source', match && match[1] === srcId);
    });
  }
  // If this is the focused row, refresh the detail pane
  if (focusedKey === key) {
    const [suite, name] = [key.substring(0, key.indexOf(':')), key.substring(key.indexOf(':') + 1)];
    loadDetail(suite, name);
  }
}

function switchDetailSide(key, side, value) {
  if (side === 'left') compareLeft[key] = value;
  else compareRight[key] = value;
  const [suite, name] = [key.substring(0, key.indexOf(':')), key.substring(key.indexOf(':') + 1)];
  loadDetail(suite, name);
  // Update active-source underline on the table row
  if (side === 'right') {
    const row = document.querySelector(`tr.row[data-kb-key="${CSS.escape(key)}"]`);
    if (row) {
      const activeSrcId = value.startsWith('src:') ? value.slice(4) : null;
      const srcCells = row.querySelectorAll('td[onclick]');
      srcCells.forEach(td => {
        const match = td.getAttribute('onclick')?.match(/setActiveSource\('[^']*','([^']*)'\)/);
        td.classList.toggle('active-source', match && match[1] === activeSrcId);
      });
    }
  }
}


function toggleSelect(name) {
  if (selected.has(name)) selected.delete(name);
  else selected.add(name);
  updateSelectedCount();
  saveState();
}

function toggleSelectSuite(suite, checked) {
  const filter = document.getElementById('statusFilter').value;
  let images = buildSuiteImages(suite);
  if (filter) images = images.filter(i => i.status === filter || (filter === 'fail' && i.status === 'pending'));
  images.forEach(i => {
    const key = `${i.suite}:${i.name}`;
    if (checked) selected.add(key); else selected.delete(key);
  });
  syncCheckboxes();
  updateSelectedCount();
  saveState();
}

function toggleSelectAll() {
  const cb = document.getElementById('selectAll');
  const checked = cb.checked;
  const filter = document.getElementById('statusFilter').value;
  for (const suite of Object.keys(suiteData)) {
    let images = buildSuiteImages(suite);
    if (filter) images = images.filter(i => i.status === filter || (filter === 'fail' && i.status === 'pending'));
    images.forEach(i => {
      const key = `${i.suite}:${i.name}`;
      if (checked) selected.add(key); else selected.delete(key);
    });
  }
  syncCheckboxes();
  updateSelectedCount();
  saveState();
}

function getMaxDiffForImage(img) {
  for (const src of sources) {
    const stats = cellStats[`${src.id}:${img.suite}:${img.name}`];
    if (stats) return stats.maxDiff;
  }
  return null;
}

function selectAllFailing() {
  selected.clear();
  for (const suite of Object.keys(suiteData)) {
    const images = buildSuiteImages(suite);
    images.filter(i => i.status === 'fail' || i.status === 'new').forEach(i => selected.add(`${i.suite}:${i.name}`));
  }
  syncCheckboxes();
  updateSelectedCount();
  saveState();
}

function selectFixable() {
  // With the framework's any-match semantics, "fixable" rows now pass (via an
  // alternate gold) while still carrying a stale primary gold at the current
  // platform. Selecting them lets the user clean up those redundant primaries.
  selected.clear();
  for (const suite of Object.keys(suiteData)) {
    for (const img of buildSuiteImages(suite)) {
      const fixKey = `${img.suite}:${img.name}`;
      if (fixableVia[fixKey]) selected.add(fixKey);
    }
  }
  syncCheckboxes();
  updateSelectedCount();
  saveState();
}

async function deleteSelectedGold() {
  if (selected.size === 0) return alert('No images selected.');
  // Group fixable images by the platform whose gold should be deleted
  const toDelete = {};
  for (const key of selected) {
    const fix = fixableVia[key];
    if (!fix) continue;
    const [suite, ...nameParts] = key.split(':');
    const name = nameParts.join(':');
    // Delete the gold for the current platform (the failing one)
    const k = `${suite}|${fix.goldFallback}`;
    if (!toDelete[k]) toDelete[k] = { suite, platform: fix.goldFallback, names: [] };
    toDelete[k].names.push(name);
  }
  const entries = Object.values(toDelete);
  if (entries.length === 0) return alert('No fixable images selected. Select images that show a green checkmark hint.');
  const total = entries.reduce((s, e) => s + e.names.length, 0);
  if (!confirm(`Delete ${total} device-specific gold image(s)? They will fall back to a passing alternate.`)) return;
  let totalDeleted = 0;
  for (const entry of entries) {
    const res = await fetch('/api/gold/delete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ suite: entry.suite, platform: entry.platform, names: entry.names })
    });
    const result = await res.json();
    totalDeleted += result.deleted;
  }
  alert(`Deleted ${totalDeleted} gold image(s).`);
  selected.clear();
  cellStats = {};
  resetAltGoldState();
  await reload();
}

function getSourceForKey(key) {
  // Use the right-side source from the detail view, or fall back to first source with the image
  const ref = compareRight[key];
  if (ref && ref.startsWith('src:')) return ref.slice(4);
  const [suite, name] = [key.substring(0, key.indexOf(':')), key.substring(key.indexOf(':') + 1)];
  const data = suiteData[suite];
  if (data) {
    for (const s of sources)
      if ((data.sourceImages[s.id] || []).some(i => i.name === name)) return s.id;
  }
  return sources[0]?.id || null;
}

async function promoteSelected() {
  if (selected.size === 0) return;
  const mode = document.getElementById('promoteSource').value;

  // Group by suite+source
  const groups = {}; // {`${srcId}:${suite}`: {srcId, suite, names[]}}
  for (const key of selected) {
    const [suite, name] = [key.substring(0, key.indexOf(':')), key.substring(key.indexOf(':') + 1)];
    const srcId = mode === '__active__' ? getSourceForKey(key) : mode;
    if (!srcId) continue;
    const gkey = `${srcId}:${suite}`;
    if (!groups[gkey]) groups[gkey] = { srcId, suite, names: [] };
    groups[gkey].names.push(name);
  }

  const srcLabels = [...new Set(Object.values(groups).map(g => sources.find(s => s.id === g.srcId)?.label || g.srcId))];
  if (!confirm(`Promote ${selected.size} image(s) from ${srcLabels.join(', ')} to gold?`)) return;

  let totalPromoted = 0;
  for (const { srcId, suite, names } of Object.values(groups)) {
    const res = await fetch('/api/promote', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sourceId: srcId, suite, platform: currentPlatform, names })
    });
    const result = await res.json();
    console.log('Promote result:', result);
    totalPromoted += result.promoted;
  }
  alert(`Promoted ${totalPromoted} image(s).`);
  selected.clear();
  cellStats = {};
  resetAltGoldState();
  compareLeft = {};
  compareRight = {};
  await reload();
}

function syncCheckboxes() {
  // Sync individual row checkboxes
  document.querySelectorAll('tr.row[data-kb-key]').forEach(tr => {
    const key = tr.dataset.kbKey;
    const cb = tr.querySelector('input[type=checkbox]');
    if (cb) cb.checked = selected.has(key);
  });
  // Sync suite-level checkboxes
  document.querySelectorAll('tr.suite-row[data-kb-key]').forEach(tr => {
    const suite = tr.dataset.kbKey;
    const cb = tr.querySelector('input[type=checkbox]');
    if (!cb) return;
    const images = buildSuiteImages(suite);
    const filter = document.getElementById('statusFilter').value;
    const filtered = filter ? images.filter(i => i.status === filter || (filter === 'fail' && i.status === 'pending')) : images;
    const keys = filtered.map(i => `${i.suite}:${i.name}`);
    const allSel = keys.length > 0 && keys.every(k => selected.has(k));
    const someSel = !allSel && keys.some(k => selected.has(k));
    cb.checked = allSel;
    cb.indeterminate = someSel;
  });
}

function updateSelectedCount() {
  document.getElementById('selectedCount').textContent = selected.size;
  document.getElementById('selectedCount2').textContent = selected.size;
}

let _countUpdateTimer = null;
function scheduleCountUpdate() {
  if (_countUpdateTimer) return;
  _countUpdateTimer = setTimeout(() => {
    _countUpdateTimer = null;
    updateActionCounts();
    updateSuiteBadges();
  }, 300);
}

function updateSuiteBadges() {
  for (const suite of Object.keys(suiteData)) {
    const images = buildSuiteImages(suite);
    const failCount = images.filter(i => i.status === 'fail' || i.status === 'new').length;
    const pendingCount = images.filter(i => i.status === 'pending').length;
    const failEl = document.querySelector(`[data-suite-fail="${CSS.escape(suite)}"]`);
    const pendEl = document.querySelector(`[data-suite-pending="${CSS.escape(suite)}"]`);
    if (failEl) { failEl.textContent = `${failCount} failing`; failEl.style.display = failCount > 0 ? '' : 'none'; }
    if (pendEl) { pendEl.textContent = `${pendingCount} pending`; pendEl.style.display = pendingCount > 0 ? '' : 'none'; }
  }
}

function updateActionCounts() {
  let failCount = 0, fixableCount = 0;
  for (const suite of Object.keys(suiteData)) {
    const images = buildSuiteImages(suite);
    for (const i of images) {
      if (i.status === 'fail' || i.status === 'new') failCount++;
      if (fixableVia[`${i.suite}:${i.name}`]) fixableCount++;
    }
  }
  document.getElementById('failingCount').textContent = failCount;
  document.getElementById('fixableCount').textContent = fixableCount;
}

// === Utils ===
function isSoftwareRenderer(platform) {
  const p = platform.toLowerCase();
  return p.includes('swiftshader') || p.includes('warp');
}

function getGfxApi(platform) {
  // "Windows.Direct3D11/WARP" → "Direct3D11", "Linux.Vulkan/SwiftShader" → "Vulkan"
  const platApi = platform.split('/')[0];
  const dot = platApi.indexOf('.');
  return dot >= 0 ? platApi.substring(dot + 1) : platApi;
}

function getOS(platform) {
  const platApi = platform.split('/')[0];
  const dot = platApi.indexOf('.');
  return dot >= 0 ? platApi.substring(0, dot) : platApi;
}

function scoreFallback(candidate, requested) {
  // Mirrors Program.cs ScoreFallback: exact > same OS > same gfx API >
  // same device (WARP↔WARP, Lavapipe↔Lavapipe) > same renderer class.
  const cPlatApi = candidate.split('/')[0];
  const rPlatApi = requested.split('/')[0];
  const cDevice = candidate.split('/')[1] || '';
  const rDevice = requested.split('/')[1] || '';
  let score = 0;
  if (cPlatApi === rPlatApi) score += 16;
  if (getOS(cPlatApi) === getOS(rPlatApi)) score += 8;
  if (getGfxApi(cPlatApi) === getGfxApi(rPlatApi)) score += 4;
  if (cDevice.toLowerCase() === rDevice.toLowerCase()) score += 2;
  if (isSoftwareRenderer(candidate) === isSoftwareRenderer(requested)) score += 1;
  return score;
}

function pickBestGoldPlatform(platforms, currentPlatform) {
  if (!platforms || platforms.length === 0) return currentPlatform;
  // Exact match
  const exact = platforms.find(p => p.platform === currentPlatform);
  if (exact) return exact.platform;
  let best = null, bestScore = -1;
  for (const p of platforms) {
    const score = scoreFallback(p.platform, currentPlatform);
    if (score > bestScore) { bestScore = score; best = p.platform; }
  }
  return best || platforms[0].platform;
}

function enc(s) { return encodeURIComponent(s); }
function css(s) { return s.replace(/[^a-zA-Z0-9]/g, '_'); }
function esc(s) { return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;'); }
function loadImg(url) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = 'anonymous';
    img.onload = () => resolve(img);
    img.onerror = reject;
    img.src = url;
  });
}
function drawToCanvas(canvas, img) {
  canvas.width = img.naturalWidth;
  canvas.height = img.naturalHeight;
  canvas.getContext('2d').drawImage(img, 0, 0);
}
function naturalSort(a, b) { return a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' }); }

// === CI Modal ===
let ciRuns = [];
let selectedCiRun = null;

async function loadCiRuns() {
  document.getElementById('ciLoading').style.display = 'block';
  document.getElementById('ciRunsList').innerHTML = '';
  try {
    // Check gh status first
    const statusRes = await fetch('/api/ci/status');
    const status = await statusRes.json();
    if (!status.available) {
      document.getElementById('ciLoading').innerHTML = `<span style="color:#e57373">⚠ ${status.error}</span>`;
      return;
    }
    const res = await fetch('/api/ci/runs?limit=30');
    const allRuns = await res.json();
    // Deduplicate by SHA — keep one per commit, use the CI workflow (name "CI")
    const seen = new Map();
    for (const run of allRuns) {
      const sha = run.head_sha ?? '';
      const name = run.name ?? '';
      if (!seen.has(sha) || name === 'CI')
        seen.set(sha, run);
    }
    ciRuns = [...seen.values()].slice(0, 15);
    console.log('CI runs:', ciRuns);
    renderCiRuns();
  } catch (e) {
    document.getElementById('ciLoading').textContent = 'Failed to load runs. Is gh CLI authenticated?';
  }
}

function renderCiRuns() {
  document.getElementById('ciLoading').style.display = 'none';
  const list = document.getElementById('ciRunsList');
  list.innerHTML = ciRuns.map(run => {
    const id = run.id ?? run.Id;
    const branch = run.head_branch ?? run.HeadBranch ?? '';
    const sha = (run.head_sha ?? run.HeadSha ?? '').substring(0, 7);
    const date = run.created_at ?? run.CreatedAt ?? '';
    const conclusion = run.conclusion ?? run.Conclusion ?? '';
    const ago = timeAgo(date);
    const wfName = run.name ?? '';
    const statusIcon = conclusion === 'success' ? '✓' : conclusion === 'failure' ? '✗' : conclusion === 'cancelled' ? '⊘' : '○';
    return `<div class="ci-run ${selectedCiRun === id ? 'selected' : ''}" onclick="selectCiRun(${id})">
      <div><b>#${id}</b> ${esc(branch)} <span class="meta">${sha}</span></div>
      <div><span class="meta">${esc(wfName)}</span> <span class="meta">${ago}</span> ${statusIcon}</div>
    </div>`;
  }).join('');
}

let ciArtifacts = [];
let selectedCiArtifacts = new Set();

async function selectCiRun(id) {
  selectedCiRun = id;
  document.getElementById('ciRunId').value = String(id);
  renderCiRuns();

  // Load artifacts for this run
  const listEl = document.getElementById('ciArtifactsList');
  listEl.innerHTML = '<span class="spinner"></span> Loading artifacts...';
  try {
    const res = await fetch(`/api/ci/artifacts?runId=${id}`);
    ciArtifacts = await res.json();
    // Auto-select test-related artifacts
    selectedCiArtifacts = new Set(
      ciArtifacts
        .filter(a => {
          const name = (a.name ?? a.Name ?? '').toLowerCase();
          return name.includes('test-artifacts') && !(a.expired ?? a.Expired ?? false);
        })
        .map(a => a.name ?? a.Name)
    );
    renderCiArtifacts();
  } catch (e) {
    listEl.innerHTML = `Failed to load artifacts: ${e.message}`;
    console.error('Artifact load error:', e);
  }
  document.getElementById('ciDownloadBtn').disabled = selectedCiArtifacts.size === 0;
}

function renderCiArtifacts() {
  const listEl = document.getElementById('ciArtifactsList');
  const testArtifacts = ciArtifacts.filter(a => (a.name ?? '').startsWith('test-artifacts'));
  if (testArtifacts.length === 0) {
    listEl.innerHTML = 'No test artifacts found for this run';
    document.getElementById('ciDownloadBtn').disabled = true;
    return;
  }
  listEl.innerHTML = testArtifacts.map(a => {
    const name = a.name ?? a.Name;
    const size = a.size_in_bytes ?? a.SizeInBytes ?? 0;
    const sizeMB = (size / 1048576).toFixed(1);
    const checked = selectedCiArtifacts.has(name);
    const expired = a.expired ?? a.Expired ?? false;
    return `<label style="display:block; margin:2px 0; ${expired ? 'text-decoration:line-through; color:#666' : ''}">
      <input type="checkbox" ${checked ? 'checked' : ''} ${expired ? 'disabled' : ''}
        onchange="toggleCiArtifact('${esc(name)}', this.checked)">
      ${esc(name)} <span class="meta">(${sizeMB} MB)</span>
      ${expired ? ' <span style="color:#e57373">expired</span>' : ''}
    </label>`;
  }).join('');
  document.getElementById('ciDownloadBtn').disabled = selectedCiArtifacts.size === 0;
}

function toggleCiArtifact(name, checked) {
  if (checked) selectedCiArtifacts.add(name);
  else selectedCiArtifacts.delete(name);
  document.getElementById('ciDownloadBtn').disabled = selectedCiArtifacts.size === 0;
}

function timeAgo(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  const now = new Date();
  const diff = (now - d) / 1000;
  if (diff < 60) return 'just now';
  if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
  if (diff < 86400) return Math.floor(diff / 3600) + 'h ago';
  return Math.floor(diff / 86400) + 'd ago';
}

// === Synchronized Zoom/Pan ===
const zoomState = {}; // {groupId: {scale, panX, panY}}

function initZoomGroup(groupId) {
  zoomState[groupId] = { scale: 1, panX: 0, panY: 0 };
  const group = document.getElementById(`zoomgroup-${groupId}`);
  if (!group) return;

  const containers = group.querySelectorAll('.zoom-container');

  containers.forEach(container => {
    // Wheel zoom (scroll anywhere over the image area)
    container.addEventListener('wheel', (e) => {
      e.preventDefault();
      const state = zoomState[groupId];
      const rect = container.getBoundingClientRect();
      const mx = e.clientX - rect.left;
      const my = e.clientY - rect.top;

      const oldScale = state.scale;
      const delta = e.deltaY > 0 ? 0.8 : 1.25;
      state.scale = Math.max(0.5, Math.min(20, state.scale * delta));

      // Zoom toward mouse position
      state.panX = mx - (mx - state.panX) * (state.scale / oldScale);
      state.panY = my - (my - state.panY) * (state.scale / oldScale);

      applyZoom(groupId);
    }, { passive: false });

    // Drag pan
    let dragging = false, startX, startY, startPanX, startPanY;
    container.addEventListener('mousedown', (e) => {
      if (e.button !== 0) return;
      dragging = true;
      startX = e.clientX;
      startY = e.clientY;
      const state = zoomState[groupId];
      startPanX = state.panX;
      startPanY = state.panY;
      container.classList.add('dragging');
      e.preventDefault();
    });
    window.addEventListener('mousemove', (e) => {
      if (!dragging) return;
      const state = zoomState[groupId];
      state.panX = startPanX + (e.clientX - startX);
      state.panY = startPanY + (e.clientY - startY);
      applyZoom(groupId);
    });
    window.addEventListener('mouseup', () => {
      if (dragging) {
        dragging = false;
        container.classList.remove('dragging');
      }
    });
  });
}

function applyZoom(groupId) {
  const state = zoomState[groupId];
  const group = document.getElementById(`zoomgroup-${groupId}`);
  if (!group) return;
  group.querySelectorAll('.zoom-inner').forEach(inner => {
    inner.style.transform = `translate(${state.panX}px, ${state.panY}px) scale(${state.scale})`;
  });
}

function resetZoom(groupId) {
  zoomState[groupId] = { scale: 1, panX: 0, panY: 0 };
  applyZoom(groupId);
}

// === Pixel Inspector ===
const piZoomSize = 7; // 7x7 pixel grid
const piScale = 12;   // each pixel drawn at 12x12

document.addEventListener('mousemove', (e) => {
  const img = e.target.closest('img.thumb, canvas.thumb, .image-box img, .image-box canvas');
  if (!img) { document.getElementById('pixelInspector').style.display = 'none'; return; }

  const rect = img.getBoundingClientRect();
  const scaleX = (img.naturalWidth || img.width) / rect.width;
  const scaleY = (img.naturalHeight || img.height) / rect.height;
  const px = Math.floor((e.clientX - rect.left) * scaleX);
  const py = Math.floor((e.clientY - rect.top) * scaleY);

  if (px < 0 || py < 0 || px >= (img.naturalWidth || img.width) || py >= (img.naturalHeight || img.height)) {
    document.getElementById('pixelInspector').style.display = 'none';
    return;
  }

  // Find which detail panel this image belongs to
  const detail = img.closest('.detail') || img.closest('td');
  if (!detail) return;

  // Collect all images in the same row/detail (gold + sources)
  const allImages = detail.closest('tr')?.parentElement?.querySelectorAll('img, canvas') || [];
  // Filter to full-size images in the same detail panel, or thumbnails in the same row
  const relatedImages = [];
  const detailEl = img.closest('.detail');
  if (detailEl) {
    detailEl.querySelectorAll('img, canvas').forEach(el => relatedImages.push(el));
  } else {
    // Thumbnail mode — find images in sibling tds
    const row = img.closest('tr');
    if (row) row.querySelectorAll('img.thumb, canvas.thumb').forEach(el => relatedImages.push(el));
  }

  // Build inspector content
  let html = '';
  // Collect full-size entries in order [left, right, diff]; delta is shown only on the diff entry,
  // computed as right − left (so the third column reports the inter-image difference).
  let leftRGBA = null, rightRGBA = null, entryIdx = 0;
  for (const ri of relatedImages) {
    const lblEl = ri.closest('.image-box')?.querySelector('.lbl');
    const sel = lblEl?.querySelector('select');
    const label = sel ? sel.options[sel.selectedIndex]?.text?.split(' (')[0] || '' : lblEl?.textContent || '';
    const w = ri.naturalWidth || ri.width;
    const h = ri.naturalHeight || ri.height;
    if (w === 0 || h === 0) continue;

    // Draw zoomed region
    const canvas = document.createElement('canvas');
    canvas.width = piZoomSize * piScale;
    canvas.height = piZoomSize * piScale;
    const ctx = canvas.getContext('2d');

    // Get pixel data from the image
    const tmpCanvas = new OffscreenCanvas(w, h);
    const tmpCtx = tmpCanvas.getContext('2d');
    tmpCtx.drawImage(ri, 0, 0);

    const half = Math.floor(piZoomSize / 2);

    // Draw zoomed pixels, clamping to image bounds
    ctx.fillStyle = '#111';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    let cr = 0, cg = 0, cb = 0, ca = 0;
    for (let dy = -half; dy <= half; dy++) {
      for (let dx = -half; dx <= half; dx++) {
        const sx = px + dx, sy = py + dy;
        if (sx >= 0 && sx < w && sy >= 0 && sy < h) {
          const pxData = tmpCtx.getImageData(sx, sy, 1, 1).data;
          ctx.fillStyle = `rgb(${pxData[0]},${pxData[1]},${pxData[2]})`;
          ctx.fillRect((dx + half) * piScale, (dy + half) * piScale, piScale, piScale);
          if (dx === 0 && dy === 0) { cr = pxData[0]; cg = pxData[1]; cb = pxData[2]; ca = pxData[3]; }
        }
      }
    }
    // Draw center crosshair
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 1;
    ctx.strokeRect(half * piScale, half * piScale, piScale, piScale);

    const r = cr, g = cg, b = cb, a = ca;

    // Capture left/right RGBA so the diff entry can report right − left.
    if (entryIdx === 0) leftRGBA = { r, g, b, a };
    else if (entryIdx === 1) rightRGBA = { r, g, b, a };

    // Body rows: columns 1 and 2 show absolute RGB; column 3 shows Δ(right − left) only.
    let bodyHtml;
    if (entryIdx >= 2 && leftRGBA && rightRGBA) {
      const dR = rightRGBA.r - leftRGBA.r;
      const dG = rightRGBA.g - leftRGBA.g;
      const dB = rightRGBA.b - leftRGBA.b;
      const dA = rightRGBA.a - leftRGBA.a;
      const fmt = (v) => (v > 0 ? '+' : '') + v;
      const cls = (v) => v !== 0 ? 'pi-delta-nz' : '';
      bodyHtml =
        `<div class="pi-rgb">&nbsp;</div>` + // spacer to align with the #hex row on columns 1/2
        `<div class="pi-rgb"><span class="${cls(dR)}">ΔR: ${fmt(dR)}</span></div>` +
        `<div class="pi-rgb"><span class="${cls(dG)}">ΔG: ${fmt(dG)}</span></div>` +
        `<div class="pi-rgb"><span class="${cls(dB)}">ΔB: ${fmt(dB)}</span></div>` +
        `<div class="pi-rgb"><span class="${cls(dA)}">ΔA: ${fmt(dA)}</span></div>`;
    } else {
      bodyHtml =
        `<div class="pi-rgb">#${r.toString(16).padStart(2,'0')}${g.toString(16).padStart(2,'0')}${b.toString(16).padStart(2,'0')}${a<255?a.toString(16).padStart(2,'0'):''}</div>` +
        `<div class="pi-rgb">R: ${String(r).padStart(3)} (${(r/255).toFixed(3)})</div>` +
        `<div class="pi-rgb">G: ${String(g).padStart(3)} (${(g/255).toFixed(3)})</div>` +
        `<div class="pi-rgb">B: ${String(b).padStart(3)} (${(b/255).toFixed(3)})</div>` +
        `<div class="pi-rgb">A: ${String(a).padStart(3)} (${(a/255).toFixed(3)})</div>`;
    }

    html += `<div class="pi-entry">
      <div class="pi-label">${esc(label)}</div>
      <img class="pi-zoom" src="${canvas.toDataURL()}" width="${piZoomSize * piScale}" height="${piZoomSize * piScale}">
      <div class="pi-coords">X:${px} Y:${py}</div>
      ${bodyHtml}
    </div>`;
    entryIdx++;
  }

  const inspector = document.getElementById('pixelInspector');
  document.getElementById('piContent').innerHTML = html;
  inspector.style.display = 'block';
});

document.addEventListener('mouseleave', () => {
  document.getElementById('pixelInspector').style.display = 'none';
});

// === Keyboard Navigation ===
let kbFocusKey = null; // persists across re-renders

document.addEventListener('keydown', (e) => {
  if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT' || e.target.tagName === 'TEXTAREA') return;
  const rows = [...document.querySelectorAll('tr.suite-row, tr.row')];
  if (rows.length === 0) return;

  const focusedIdx = kbFocusKey != null ? rows.findIndex(r => r.dataset.kbKey === kbFocusKey) : -1;

  if (e.key === 'ArrowDown' || e.key === 'j') {
    e.preventDefault();
    const next = Math.min(focusedIdx + 1, rows.length - 1);
    kbSetFocus(rows, next);
  } else if (e.key === 'ArrowUp' || e.key === 'k') {
    e.preventDefault();
    const prev = focusedIdx <= 0 ? 0 : focusedIdx - 1;
    kbSetFocus(rows, prev);
  } else if (e.key === 'ArrowRight' || e.key === 'l') {
    e.preventDefault();
    if (focusedIdx >= 0) kbExpand(rows[focusedIdx]);
  } else if (e.key === 'ArrowLeft' || e.key === 'h') {
    e.preventDefault();
    if (focusedIdx >= 0) kbCollapse(rows[focusedIdx]);
  } else if (e.key === ' ') {
    e.preventDefault();
    if (focusedIdx >= 0) {
      const cb = rows[focusedIdx].querySelector('input[type=checkbox]');
      if (cb) cb.click();
    }
  }
});

function kbSetFocus(rows, idx) {
  rows.forEach(r => r.classList.remove('kb-focus'));
  if (idx >= 0 && idx < rows.length) {
    rows[idx].classList.add('kb-focus');
    rows[idx].scrollIntoView({ block: 'nearest' });
    const key = rows[idx].dataset.kbKey || null;
    kbFocusKey = key;
    // Drive the detail pane when focus lands on a test row (not the suite header).
    if (rows[idx].classList.contains('row')) {
      focusedKey = key;
      renderDetailPane(key);
    }
    saveState();
  }
}

function kbExpand(row) {
  const key = row.dataset.kbKey;
  if (!key) return;
  if (row.classList.contains('suite-row')) {
    if (!collapsedSuites.has(key)) {
      // Already expanded — move to first child
      const rows = [...document.querySelectorAll('tr.suite-row, tr.row')];
      const idx = rows.indexOf(row);
      if (idx >= 0 && idx + 1 < rows.length && rows[idx + 1].classList.contains('row')) {
        kbSetFocus(rows, idx + 1);
        return;
      }
    }
    collapsedSuites.delete(key);
    render();
  }
}

function kbCollapse(row) {
  const key = row.dataset.kbKey;
  if (!key) return;
  if (row.classList.contains('suite-row')) {
    if (collapsedSuites.has(key)) return; // already collapsed, nowhere to go
    collapsedSuites.add(key);
  } else {
    // Test row — move to parent suite
    const rows = [...document.querySelectorAll('tr.suite-row, tr.row')];
    const idx = rows.indexOf(row);
    for (let i = idx - 1; i >= 0; i--) {
      if (rows[i].classList.contains('suite-row')) {
        kbSetFocus(rows, i);
        return;
      }
    }
    return;
  }
  render();
}

// Restore kb focus after render
const _origRender2 = render;
render = function() {
  _origRender2();
  if (kbFocusKey) {
    const row = document.querySelector(`[data-kb-key="${CSS.escape(kbFocusKey)}"]`);
    if (row) row.classList.add('kb-focus');
  }
};

// === Persistence ===
function saveState() {
  try {
    localStorage.setItem('compareGold', JSON.stringify({
      platform: currentPlatform,
      selectedKey: focusedKey,
      collapsedSuites: [...collapsedSuites],
      statusFilter: document.getElementById('statusFilter')?.value || '',
      sort: document.getElementById('sortSelect')?.value || 'name',
      search: document.getElementById('searchFilter')?.value || '',
      savedSources: sourceDefs,
      detailPaneH: document.documentElement.style.getPropertyValue('--detail-pane-h') || '',
    }));
  } catch {}
}

function restoreState() {
  try {
    const data = JSON.parse(localStorage.getItem('compareGold') || '{}');
    if (data.platform) currentPlatform = data.platform;
    if (data.selectedKey) { focusedKey = data.selectedKey; kbFocusKey = data.selectedKey; }
    if (data.collapsedSuites) data.collapsedSuites.forEach(k => collapsedSuites.add(k));
    if (data.statusFilter !== undefined) {
      const sel = document.getElementById('statusFilter');
      if (sel) sel.value = data.statusFilter;
    }
    if (data.sort) {
      const sel = document.getElementById('sortSelect');
      if (sel) sel.value = data.sort;
    }
    if (data.search) {
      const el = document.getElementById('searchFilter');
      if (el) el.value = data.search;
    }
    if (data.savedSources) savedSourceDefs = data.savedSources;
    if (data.detailPaneH) document.documentElement.style.setProperty('--detail-pane-h', data.detailPaneH);
  } catch {}
}

let savedSourceDefs = null;
async function restoreSources() {
  if (!savedSourceDefs || savedSourceDefs.length === 0) return;
  for (const def of savedSourceDefs) {
    try {
      let res, src;
      if (def.type === 'local') {
        res = await fetch('/api/sources/add-local', { method: 'POST' });
      } else if (def.type === 'ci') {
        res = await fetch('/api/sources/add-ci', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ runId: def.runId, artifactName: def.artifactName, label: def.label })
        });
      }
      if (res && res.ok) {
        src = await res.json();
        if (!sources.find(s => s.id === src.id)) {
          sources.push(src);
          sourceDefs.push(def);
        }
      }
    } catch {}
  }
  savedSourceDefs = null;
}

// Hook render to auto-save
const _origRender = render;
render = function() { _origRender(); saveState(); };

// === Detail pane resize ===
// Hard floor: keep images usable; updatePaneBounds() recomputes ceiling from loaded image aspect.
const PANE_MIN_H = 160;
let paneMaxH = null;

function updatePaneBounds(refImg) {
  const pane = document.getElementById('detailPane');
  if (!pane) return;
  const container = pane.querySelector('.zoom-container');
  if (!refImg || !container) { paneMaxH = null; return; }
  const aw = refImg.naturalWidth || refImg.width;
  const ah = refImg.naturalHeight || refImg.height;
  if (!aw || !ah) { paneMaxH = null; return; }
  // Image-box width at full pane width. Add non-image overhead (handle, padding, label, footer, margins).
  const boxW = container.offsetWidth;
  const maxImgH = Math.ceil(boxW * ah / aw);
  const overhead = pane.offsetHeight - container.offsetHeight;
  const winCap = window.innerHeight - 120;
  paneMaxH = Math.min(winCap, Math.max(PANE_MIN_H, maxImgH + overhead));
  // Clamp current height if it exceeds the new bounds.
  const cur = pane.offsetHeight;
  const clamped = Math.max(PANE_MIN_H, Math.min(paneMaxH, cur));
  if (clamped !== cur) {
    document.documentElement.style.setProperty('--detail-pane-h', clamped + 'px');
    saveState();
  }
}

(function initPaneResize() {
  const handle = document.getElementById('detailPaneHandle');
  if (!handle) return;
  handle.addEventListener('mousedown', (e) => {
    e.preventDefault();
    handle.classList.add('dragging');
    const startY = e.clientY;
    const startH = document.getElementById('detailPane').offsetHeight;
    const onMove = (ev) => {
      const dy = startY - ev.clientY;
      const cap = paneMaxH != null ? paneMaxH : (window.innerHeight - 120);
      const h = Math.max(PANE_MIN_H, Math.min(cap, startH + dy));
      document.documentElement.style.setProperty('--detail-pane-h', h + 'px');
    };
    const onUp = () => {
      handle.classList.remove('dragging');
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
      saveState();
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  });
})();

// === Start ===
restoreState();
init().then(async () => {
  // Restore platform selection after init populates the dropdown
  if (currentPlatform) {
    const sel = document.getElementById('platformSelect');
    if (sel && [...sel.options].some(o => o.value === currentPlatform)) {
      sel.value = currentPlatform;
    }
  }
  // Restore saved sources, or auto-add local
  if (savedSourceDefs && savedSourceDefs.length > 0) {
    await restoreSources();
    await reload();
  } else {
    await addLocalSource().catch(() => {});
  }
  // Re-hydrate the detail pane with the restored selection (if it still exists).
  if (focusedKey) {
    const row = document.querySelector(`tr.row[data-kb-key="${CSS.escape(focusedKey)}"]`);
    if (row) {
      row.classList.add('kb-focus');
      row.scrollIntoView({ block: 'nearest' });
      renderDetailPane(focusedKey);
    } else {
      focusedKey = null;
      renderDetailPane(null);
    }
  } else {
    renderDetailPane(null);
  }
});
