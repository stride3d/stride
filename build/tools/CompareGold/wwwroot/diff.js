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

  for (let i = 0; i < gd.length; i += 4) {
    const dr = Math.abs(gd[i] - ld[i]);
    const dg = Math.abs(gd[i + 1] - ld[i + 1]);
    const db = Math.abs(gd[i + 2] - ld[i + 2]);
    const pixMax = Math.max(dr, dg, db);

    if (pixMax > maxDiff) maxDiff = pixMax;
    sumSq += dr * dr + dg * dg + db * db;
    if (pixMax > threshold) diffPixels++;

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

  return { maxDiff, psnr, mse, diffPixels, totalPixels: total, histogram: hist, width: w, height: h };
}

function formatStats(stats) {
  const pct = (100 * stats.diffPixels / stats.totalPixels).toFixed(2);
  return `<span>${stats.width}×${stats.height}</span>
    <span>Diff pixels: <b>${stats.diffPixels}/${shortNum(stats.totalPixels)}</b> (${pct}%)</span>
    <span>Max diff: <b>${stats.maxDiff}</b></span>
    <span>PSNR: <b>${stats.psnr === Infinity ? '∞' : stats.psnr.toFixed(1)} dB</b></span>
    <br><span class="histogram">
      <span style="color:#44f">[1-2]:${stats.histogram[1]}</span>  <span style="color:#0f0">[3-5]:${stats.histogram[2]}</span>  <span style="color:#ff0">[6-15]:${stats.histogram[3]}</span>  <span style="color:#f00">[16+]:${stats.histogram[4]}</span>
    </span>`;
}

function shortNum(n) { return n >= 1000 ? (n / 1000).toFixed(0) + 'k' : n; }
