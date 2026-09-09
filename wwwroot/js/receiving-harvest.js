(() => {
    const mode = document.getElementById('harvestSourceMode');
    const payload = document.getElementById('NewReceiving_HarvestPayload');
    const preview = document.getElementById('harvestSourcePreview');
    const status = document.getElementById('harvestScanStatus');
    const video = document.getElementById('harvestVideo');
    const stopButton = document.getElementById('stopHarvestCamera');
    const startButton = document.getElementById('startHarvestCamera');
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d', { willReadFrequently: true });
    let stream, frame, cameraRequest = 0;
    function stop() {
        cameraRequest++;
        cancelAnimationFrame(frame);
        stream?.getTracks().forEach(track => track.stop());
        stream = null; video.srcObject = null; video.hidden = true;
        stopButton.hidden = true; startButton.disabled = false;
    }
    function showSource() {
        try {
            const text = payload.value.trim();
            if (!text.startsWith('AGRILOCO-HARVEST:1:')) throw new Error();
            const data = JSON.parse(text.slice('AGRILOCO-HARVEST:1:'.length));
            if (!data.Farm || !data.Lot || !data.Crop || !data.Date || !(data.Quantity > 0) || !data.Unit) throw new Error();
            preview.textContent = `${data.Farm} · ${data.Address || ''} · ${data.Lot} · ${data.Crop} / ${data.Variety || ''} · ${data.Row || ''} · Harvested ${data.Date} · ${data.Quantity} ${data.Unit}`;
            return true;
        } catch { preview.textContent = payload.value ? 'This is not a supported Agriloco harvest code. Scan a harvest label or paste its transfer text.' : ''; return false; }
    }
    function decode(source, width, height) {
        // Bound image memory; QR labels should fill most of the image.
        const scale = Math.min(1, 1600 / Math.max(width, height));
        canvas.width = Math.round(width * scale); canvas.height = Math.round(height * scale);
        context.drawImage(source, 0, 0, canvas.width, canvas.height);
        const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
        return jsQR(pixels.data, pixels.width, pixels.height)?.data;
    }
    function accept(text) {
        payload.value = text; showSource();
        status.textContent = 'QR read. Review the source details, then select your inventory item and received quantity.';
    }
    function tick() {
        if (!stream) return;
        if (video.readyState >= 2 && video.videoWidth) {
            const text = decode(video, video.videoWidth, video.videoHeight);
            if (text) { stop(); accept(text); return; }
        }
        frame = requestAnimationFrame(tick);
    }
    startButton.addEventListener('click', async () => {
        stop(); const request = cameraRequest; startButton.disabled = true;
        try {
            if (!navigator.mediaDevices?.getUserMedia) throw new Error();
            const nextStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
            if (request !== cameraRequest) { nextStream.getTracks().forEach(track => track.stop()); return; }
            stream = nextStream; video.srcObject = stream; video.hidden = false;
            stopButton.hidden = false; await video.play();
            status.textContent = 'Point the camera at the whole QR code.'; tick();
        } catch { stop(); status.textContent = 'Camera unavailable. Allow camera access on HTTPS or localhost, or upload a QR image / paste transfer text below.'; }
    });
    stopButton.addEventListener('click', () => { stop(); status.textContent = 'Camera stopped.'; });
    document.getElementById('harvestQrFile').addEventListener('change', async event => {
        stop(); const file = event.target.files[0]; if (!file) return;
        if (file.size > 15 * 1024 * 1024) { status.textContent = 'Choose an image smaller than 15 MB.'; return; }
        const url = URL.createObjectURL(file);
        try {
            const image = new Image(); image.src = url; await image.decode();
            const text = decode(image, image.naturalWidth, image.naturalHeight);
            if (text) accept(text);
            else status.textContent = 'No QR found. Use a clear image with the complete code and white border.';
        } catch { status.textContent = 'Unable to read this image. Try a PNG or JPEG of the QR label.'; }
        finally { URL.revokeObjectURL(url); }
    });
    function updateMode() {
        stop();
        document.getElementById('ownHarvestPanel').hidden = mode.value !== 'own';
        document.getElementById('qrHarvestPanel').hidden = mode.value !== 'qr';
        document.getElementById('NewReceiving_HarvestId').required = mode.value === 'own';
        payload.required = mode.value === 'qr';
        document.getElementById('NewReceiving_SupplierSearch').required = mode.value === 'manual';
    }
    mode.addEventListener('change', updateMode);
    payload.addEventListener('input', showSource);
    window.addEventListener('pagehide', stop);
    document.addEventListener('visibilitychange', () => { if (document.hidden) stop(); });
    updateMode(); showSource();
})();
