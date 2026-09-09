(() => {
    let stopActive = () => {};
    document.querySelectorAll('.receipt-source').forEach(form => {
        const get = name => form.querySelector(`[data-${name}]`);
        const mode = get('source-mode'), payload = get('qr-payload'), preview = get('qr-preview');
        const status = get('qr-status'), video = get('qr-video'), confirm = get('confirm-receipt');
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d', { willReadFrequently: true });
        let stream, frame, request = 0, lastScan = 0;
        function stop() {
            request++; cancelAnimationFrame(frame);
            stream?.getTracks().forEach(track => track.stop());
            stream = null; video.srcObject = null; video.hidden = true;
            get('stop-camera').hidden = true; get('start-camera').disabled = false;
        }
        function review() {
            try {
                const value = payload.value.trim(), prefix = 'AGRILOCO-RECEIPT:1:';
                if (!value.startsWith(prefix)) throw new Error();
                const data = JSON.parse(value.slice(prefix.length));
                if (!data.Receiver || !data.Lot || !data.Item || !data.Date || !(data.Quantity > 0) || !data.Unit) throw new Error();
                let text = `${data.Receiver} · ${data.Lot} · Received ${data.Date} from ${data.Supplier} · ${data.Item} · ${data.Quantity} ${data.Unit} · Total ${data.TotalQuantity} ${data.BaseUnit}`;
                if (data.Harvest) {
                    const h = data.Harvest;
                    text += ` | Harvest: ${h.Farm} · ${h.Lot} · ${h.Crop} / ${h.Variety} · ${h.Row} · ${h.Date} · ${h.Quantity} ${h.Unit}`;
                } else text += ' | Harvest source not recorded.';
                preview.textContent = text;
            } catch { preview.textContent = payload.value ? 'Not a supported receiving QR. Use a receiving label, rather than a harvest-only label.' : ''; }
        }
        function accept(text) { payload.value = text; confirm.checked = false; review(); status.textContent = 'QR read. Review and confirm the source, then save it.'; }
        function decode(image, width, height) {
            const scale = Math.min(1, 1600 / Math.max(width, height));
            canvas.width = Math.round(width * scale); canvas.height = Math.round(height * scale);
            context.drawImage(image, 0, 0, canvas.width, canvas.height);
            const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
            return jsQR(pixels.data, pixels.width, pixels.height)?.data;
        }
        function tick(time) {
            if (!stream) return;
            if (time - lastScan > 150 && video.readyState >= 2 && video.videoWidth) {
                lastScan = time;
                const text = decode(video, video.videoWidth, video.videoHeight);
                if (text) { stop(); accept(text); return; }
            }
            frame = requestAnimationFrame(tick);
        }
        get('start-camera').addEventListener('click', async () => {
            stopActive(); stop(); stopActive = stop;
            const current = request; get('start-camera').disabled = true;
            try {
                const next = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
                if (current !== request) { next.getTracks().forEach(track => track.stop()); return; }
                stream = next; video.srcObject = stream; video.hidden = false; get('stop-camera').hidden = false;
                await video.play(); status.textContent = 'Point the camera at the complete receiving QR code.'; frame = requestAnimationFrame(tick);
            } catch { stop(); status.textContent = 'Camera unavailable. Allow camera access on HTTPS or localhost, upload a QR image, or paste transfer text.'; }
        });
        get('stop-camera').addEventListener('click', () => { stop(); status.textContent = 'Camera stopped.'; });
        get('qr-file').addEventListener('change', async event => {
            stop(); const file = event.target.files[0]; if (!file) return;
            if (file.size > 15 * 1024 * 1024) { status.textContent = 'Choose an image smaller than 15 MB.'; return; }
            const url = URL.createObjectURL(file);
            try {
                const image = new Image(); image.src = url; await image.decode();
                const text = decode(image, image.naturalWidth, image.naturalHeight);
                if (text) accept(text); else status.textContent = 'No QR found. Use a clear image with the complete code and white border.';
            } catch { status.textContent = 'Unable to read the image. Try a PNG or JPEG.'; }
            finally { URL.revokeObjectURL(url); }
        });
        payload.addEventListener('input', () => { confirm.checked = false; review(); });
        function update() {
            stop(); get('local-panel').hidden = mode.value !== 'local'; get('qr-panel').hidden = mode.value !== 'qr';
            get('local-lot').required = mode.value === 'local'; payload.required = mode.value === 'qr'; confirm.required = mode.value === 'qr';
        }
        function localPreview() {
            const selected = get('local-lot').selectedOptions[0];
            get('local-preview').textContent = selected?.value ? selected.textContent + ' | ' + selected.dataset.harvestSummary : '';
        }
        get('local-lot').addEventListener('change', localPreview);
        localPreview();
        mode.addEventListener('change', update);
        form.closest('details').addEventListener('toggle', event => { if (!event.target.open) stop(); });
        update(); review();
    });
    window.addEventListener('pagehide', () => stopActive());
    document.addEventListener('visibilitychange', () => { if (document.hidden) stopActive(); });
})();
