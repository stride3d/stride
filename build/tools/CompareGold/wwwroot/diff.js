// Pixel comparison utilities

function computeImageDiff(goldImg, sourceImg, canvas) {
  const w = goldImg.naturalWidth, h = goldImg.naturalHeight;
  canvas.width = w;
  canvas.height = h;
  const ctx = canvas.getContext('2d');

  const gc = new OffscreenCanvas(w, h);
  const gctx = gc.getContext('2d');
  gctx.drawImage(goldImg, 0, 0);
  const gd = gctx.getImageData(0, 0, w, h).data;

  const lc = new OffscreenCanvas(w, h);
  const lctx = lc.getContext('2d');
  lctx.drawImage(sourceImg, 0, 0);
  const ld = lctx.getImageData(0, 0, w, h).data;

  const diffData = ctx.createImageData(w, h);
  const dd = diffData.data;

  let maxDiff = 0, sumSq = 0, diffPixels = 0;
  const total = w * h;
  const threshold = 2;
  const hist = [0, 0, 0, 0, 0]; // [0], [1-2], [3-5], [6-15], [16+]
  const pixelDiffs = new Int32Array(256); // per-value diff counts for threshold checking

  for (let i = 0; i < gd.length; i += 4) {
    const dr = Math.abs(gd[i] - ld[i]);
    const dg = Math.abs(gd[i + 1] - ld[i + 1]);
    const db = Math.abs(gd[i + 2] - ld[i + 2]);
    const pixMax = Math.max(dr, dg, db);

    if (pixMax > maxDiff) maxDiff = pixMax;
    sumSq += dr * dr + dg * dg + db * db;
    if (pixMax > threshold) diffPixels++;
    pixelDiffs[pixMax]++;

    if (pixMax === 0) hist[0]++;
    else if (pixMax <= 2) hist[1]++;
    else if (pixMax <= 5) hist[2]++;
    else if (pixMax <= 15) hist[3]++;
    else hist[4]++;

    // Color-coded diff by severity bucket:
    // [0]: black, [1-2]: blue, [3-5]: green, [6-15]: yellow, [16+]: red
    let r, g, b;
    if (pixMax === 0)       { r = 0;   g = 0;   b = 0;   }
    else if (pixMax <= 2)   { r = 0;   g = 0;   b = 255; }  // blue — precision noise
    else if (pixMax <= 5)   { r = 0;   g = 255; b = 0;   }  // green — minor
    else if (pixMax <= 15)  { r = 255; g = 255; b = 0;   }  // yellow — noticeable
    else                    { r = 255; g = 0;   b = 0;   }  // red — significant
    dd[i] = r; dd[i + 1] = g; dd[i + 2] = b; dd[i + 3] = 255;
  }
  ctx.putImageData(diffData, 0, 0);

  const mse = sumSq / (total * 3);
  const psnr = mse > 0 ? 10 * Math.log10(255 * 255 / mse) : Infinity;

  return { maxDiff, psnr, mse, diffPixels, totalPixels: total, histogram: hist, pixelDiffs, width: w, height: h };
}

function formatStats(stats, thresholdResult) {
  const pct = (100 * stats.diffPixels / stats.totalPixels).toFixed(2);
  let html = `<span>${stats.width}×${stats.height}</span>
    <span>Diff pixels: <b>${stats.diffPixels}/${shortNum(stats.totalPixels)}</b> (${pct}%)</span>
    <span>Max diff: <b>${stats.maxDiff}</b></span>
    <span>PSNR: <b>${stats.psnr === Infinity ? '∞' : stats.psnr.toFixed(1)} dB</b></span>
    <br><span class="histogram">
      <span style="color:#44f">[1-2]:${stats.histogram[1]}</span>  <span style="color:#0f0">[3-5]:${stats.histogram[2]}</span>  <span style="color:#ff0">[6-15]:${stats.histogram[3]}</span>  <span style="color:#f00">[16+]:${stats.histogram[4]}</span>
    </span>`;
  if (thresholdResult) {
    const icon = thresholdResult.passed ? '✓' : '✗';
    const cls = thresholdResult.passed ? 'color:#66bb6a' : 'color:#ef5350';
    html += `<br><span style="${cls}"><b>${icon} Threshold:</b></span> ${formatThresholdResult(thresholdResult)}`;
  }
  return html;
}

function shortNum(n) { return n >= 1000 ? (n / 1000).toFixed(0) + 'k' : n; }

// === Threshold checking ===

// Parse an allow bucket key like "1-2", "16+", "3"
function parseAllowKey(key) {
  if (key.endsWith('+')) return { min: parseInt(key), max: 255 };
  const parts = key.split('-');
  if (parts.length === 2) return { min: parseInt(parts[0]), max: parseInt(parts[1]) };
  const v = parseInt(key);
  return { min: v, max: v };
}

// Resolve the best matching threshold rule for an image
// rules: array from thresholds.jsonc, imageName/platform/api/device: current context
function resolveThreshold(rules, imageName, platform, api, device) {
  if (!rules || rules.length === 0) return null;
  let best = null, bestScore = -1;
  for (const rule of rules) {
    if (rule.image && rule.image !== 'default' && rule.image.toLowerCase() !== imageName.toLowerCase()) continue;
    if (rule.platform && rule.platform.toLowerCase() !== (platform || '').toLowerCase()) continue;
    if (rule.api && rule.api.toLowerCase() !== (api || '').toLowerCase()) continue;
    if (rule.device && rule.device.toLowerCase() !== (device || '').toLowerCase()) continue;
    let score = 0;
    if (rule.image && rule.image !== 'default') score += 1;
    if (rule.platform) score += 2;
    if (rule.api) score += 4;
    if (rule.device) score += 8;
    if (rule.image === 'default') score = 0;
    if (score > bestScore || (score === bestScore && rule.allow)) {
      bestScore = score;
      best = rule;
    }
  }
  return best?.allow || null;
}

// Check pixelDiffs against allow buckets. Returns { passed, details[] }
// Only explicitly specified ranges are checked. Unspecified ranges are not limited.
function checkThreshold(pixelDiffs, allow) {
  if (!allow) {
    // Default: 3+: 0
    let count = 0;
    for (let d = 3; d < pixelDiffs.length; d++) count += pixelDiffs[d];
    return { passed: count === 0, details: [{ range: '3+', count, limit: 0, passed: count === 0 }] };
  }
  const details = [];
  let allPassed = true;
  for (const [key, limit] of Object.entries(allow)) {
    const { min, max } = parseAllowKey(key);
    let count = 0;
    for (let d = Math.max(min, 0); d <= Math.min(max, 255); d++) count += pixelDiffs[d];
    const passed = count <= limit;
    if (!passed) allPassed = false;
    details.push({ range: key, count, limit, passed });
  }
  return { passed: allPassed, details };
}

// Format threshold result as compact text for table cells
function formatThresholdBrief(result) {
  if (!result || !result.details) return '';
  return result.details.map(d => `[${d.range}]:${d.count}/${d.limit}`).join(' ');
}

// Format threshold result as HTML
function formatThresholdResult(result) {
  if (!result) return '';
  return result.details.map(d => {
    const cls = d.passed ? 'color:#66bb6a' : 'color:#ef5350;font-weight:bold';
    return `<span style="${cls}">[${d.range}]: ${d.count}/${d.limit}</span>`;
  }).join('  ');
}
