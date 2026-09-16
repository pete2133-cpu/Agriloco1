/* Public, same-origin viewer. Never reads editor tokens or calls write endpoints. */
(() => {
  'use strict';
  const $ = id => document.getElementById(id), NS = 'http://www.w3.org/2000/svg';
  const map = $('map'), surface = $('surface'), image = $('basemap'), svg = $('geometry');
  let farm, published, items = new Map(), features = [], mode = 'farm', loading = false;
  let width = 1, height = 1, scale = 1, minScale = 1, maxScale = 12, x = 0, y = 0, ready = false;
  let imageKey = '', selectedId = null, watchId = null, gps = null, imageVersion = 0;
  const pointers = new Map();
  let moved = 0, downFeature = null, interacted = false, viewportWidth = 0, viewportHeight = 0;
  const ids = new URLSearchParams(location.search).getAll('farmId');
  const farmId = ids.length === 1 && /^[1-9]\d*$/.test(ids[0]) && Number(ids[0]) <= 2147483647 ? Number(ids[0]) : null;
  const color = {available:'#00B8D9', soon:'#FFB000', unavailable:'#D81B60', unknown:'#8b9891'};
  const category = status => {
    const s = (status || '').trim().toLowerCase();
    if (['available','peak season','limited availability','late season'].includes(s)) return 'available';
    if (['coming soon','growing','not yet available'].includes(s)) return 'soon';
    if (['unavailable','out of season','end of season','sold out'].includes(s)) return 'unavailable';
    return 'unknown';
  };
  function notice(text, retry = false) {
    $('notice-text').textContent = text; $('notice').hidden = !text; $('retry').hidden = !retry;
  }
  function panel(open) {
    $('filters').classList.toggle('open', open); $('filters').inert = !open;
    $('viewer').classList.toggle('panel-open', open);
    for (const id of ['menu','panel-handle']) $(id).setAttribute('aria-expanded', String(open));
    $('menu').setAttribute('aria-label', open ? 'Close map filters' : 'Open map filters');
    if (!open && $('filters').contains(document.activeElement)) $('panel-handle').focus();
  }
  $('menu').onclick = $('panel-handle').onclick = () => panel(!$('filters').classList.contains('open'));
  $('close-panel').onclick = () => panel(false);
  // Initialize once for every viewport; later updates retain the visitor's choice.
  panel(true);
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !$('notify-dialog').open) { panel(false); $('feature-card').hidden = true; } });
  function ancestor(id, target) {
    const seen = new Set();
    while (id != null && !seen.has(id)) {
      if (id === target) return true;
      seen.add(id); id = items.get(id)?.parentId;
    }
    return false;
  }
  const isPathway = f => items.get(f.farmDefinitionId)?.type === 'Pathway';
  function featureMode(f) {
    const item = items.get(f.farmDefinitionId);
    const kind = `${f.purpose || ''} ${f.subtype || ''} ${item?.type || ''}`.toLowerCase();
    if (/market|farmstore|farm store|shop/.test(kind)) return 'market';
    if (!item && !/farmdefinition|crop|row/.test(kind)) return 'other';
    return 'farm';
  }
  function matches(f) {
    if (!f.isPublic || !f.isActive || featureMode(f) !== mode) return false;
    // Infrastructure stays visible independently of crop selection and availability.
    if (isPathway(f)) return true;
    const crop = Number($('crop').value), variety = Number($('variety').value);
    if (mode === 'farm' && ((crop && !ancestor(f.farmDefinitionId, crop)) ||
        (variety && !ancestor(f.farmDefinitionId, variety)))) return false;
    return !$('available').checked || category(items.get(f.farmDefinitionId)?.status || f.status) === 'available';
  }
  const sorted = list => list.sort((a,b) => (a.sortOrder || 0) - (b.sortOrder || 0) || a.name.localeCompare(b.name));
  function options(select, rows, label, previous, alphabetical = false) {
    select.replaceChildren(new Option(label, ''));
    const ordered = alphabetical ? rows.sort((a,b) => a.name.localeCompare(b.name, 'en', {sensitivity:'base'})) : sorted(rows);
    for (const row of ordered) select.add(new Option(row.name, row.id));
    select.value = rows.some(r => String(r.id) === previous) ? previous : '';
  }
  function varieties(previous = '') {
    const crop = Number($('crop').value);
    options($('variety'), [...items.values()].filter(i => i.isPublic && i.type !== 'Pathway' && i.parentId === crop), 'All varieties', previous, true);
    $('variety').disabled = !crop || mode !== 'farm';
  }
  $('crop').onchange = () => { varieties(); render(); };
  $('variety').onchange = $('available').onchange = () => render();
  let marketItems = [], marketCategory = '', marketLoading = false;
  function renderMarket() {
    const categories = new Map(marketItems.map(i => [String(i.categoryKey ?? i.categoryId), i.categoryName]));
    if (!categories.has(marketCategory)) marketCategory = '';
    $('market-categories').replaceChildren();
    for (const [id, name] of [['', 'All'], ...categories]) {
      const button = document.createElement('button');
      button.type = 'button'; button.textContent = name;
      button.setAttribute('aria-pressed', String(id === marketCategory));
      button.onclick = () => { marketCategory = id; renderMarket(); };
      $('market-categories').append(button);
    }
    $('market-categories').hidden = !marketItems.length;
    $('market-items').replaceChildren();
    for (const item of marketItems.filter(i => !marketCategory || String(i.categoryKey ?? i.categoryId) === marketCategory)) {
      const row = document.createElement('article'), copy = document.createElement('div');
      row.className = 'market-item';
      if (item.imageUrl) {
        const picture = document.createElement('img'); picture.className = 'market-item-image';
        picture.alt = item.name; picture.loading = 'lazy'; picture.src = item.imageUrl;
        picture.onerror = () => picture.remove(); row.append(picture);
      }
      const title = document.createElement('h2'); title.textContent = item.name; copy.append(title);
      if (item.subtitle) { const sub = document.createElement('p'); sub.textContent = item.subtitle; copy.append(sub); }
      if (item.description) { const description = document.createElement('p'); description.textContent = item.description; copy.append(description); }
      for (const option of item.sellingOptions || []) {
        const line = document.createElement('p'); line.className = 'market-option';
        const measurement = [option.sellQuantity == null || (Number(option.sellQuantity) === 1 && option.sellUnit) ? '' : Number(option.sellQuantity).toLocaleString(), option.sellUnit || ''].filter(Boolean).join(' ');
        const unit = [measurement, option.packageType === 'loose' && measurement ? '' : option.packageType || ''].filter(Boolean).join(' ');
        let price = '';
        if (option.price != null) {
          try { price = new Intl.NumberFormat('en-CA', {style:'currency',currency:option.currency}).format(option.price); }
          catch { price = `${option.currency || ''} ${option.price}`.trim(); }
        }
        line.textContent = [option.name, price ? price + (unit ? ' / ' + unit : '') : unit].filter(Boolean).join(' · ');
        if (line.textContent) copy.append(line);
      }
      const badge = document.createElement('span'); badge.className = 'market-status';
      const dot = document.createElement('i'); dot.style.backgroundColor = color[category(item.status)];
      badge.append(dot, document.createTextNode(item.status || 'Availability not specified'));
      row.append(copy, badge); $('market-items').append(row);
    }
    $('market-message').textContent = marketItems.length ? '' : 'No items in market';
  }
  async function loadMarket() {
    if (marketLoading) return;
    if (!farmId) { $('market-message').textContent = 'Open Market with a valid farm link.'; return; }
    marketLoading = true; $('market-retry').hidden = true;
    if (!marketItems.length) $('market-message').textContent = 'Loading market…';
    try {
      const data = await get('market');
      if (data.farmId !== farmId) throw new Error('farm');
      marketItems = data.items || []; renderMarket();
    } catch {
      $('market-message').textContent = 'Market could not be refreshed. Please try again.';
      $('market-retry').hidden = false;
    } finally { marketLoading = false; }
  }
  $('market-retry').onclick = loadMarket;
  setInterval(() => { if (!document.hidden && mode === 'market') loadMarket(); }, 30000);
  document.querySelectorAll('[data-mode]').forEach(button => button.onclick = () => {
    mode = button.dataset.mode;
    $('viewer').classList.toggle('market-open', mode === 'market');
    $('market-view').hidden = mode !== 'market';
    map.inert = mode === 'market';
    document.querySelectorAll('[data-mode]').forEach(b => b.setAttribute('aria-pressed', String(b === button)));
    $('crop').disabled = mode !== 'farm'; $('variety').disabled = mode !== 'farm' || !$('crop').value;
    if (mode === 'market') loadMarket();
    else { selectedId = null; render(); }
  });
  let notificationIds = [], notificationBusy = false, confirmationTimer;
  const signup = $('notify-dialog');
  function closeSignup() {
    signup.close();
  }
  signup.addEventListener('close', () => {
    $('notify-open').setAttribute('aria-expanded', 'false');
    document.querySelectorAll('[data-mode]').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.mode === mode)));
    $('notify-open').focus();
  });
  $('notify-close').onclick = closeSignup;
  $('notify-open').onclick = () => {
    const crop = items.get(Number($('crop').value)), variety = items.get(Number($('variety').value));
    const eligible = i => i?.isPublic && i.type !== 'Pathway';
    let choices = [];
    if (eligible(crop) && crop.type === 'Product') {
      choices = variety && eligible(variety) && variety.parentId === crop.id ? [variety] :
        [...items.values()].filter(i => eligible(i) && i.parentId === crop.id);
      if (!choices.length && crop.type === 'Product') choices = [crop];
    }
    choices.sort((a,b) => a.name.localeCompare(b.name, 'en', {sensitivity:'base'}));
    notificationIds = choices.map(i => i.id);
    $('notify-selection-title').textContent = crop ? crop.name + (!variety && choices.some(i => i.parentId === crop.id) ? ' (All varieties)' : '') : 'Choose a crop first';
    $('notify-varieties').replaceChildren();
    for (const item of choices) { const li = document.createElement('li'); li.textContent = item.name; $('notify-varieties').append(li); }
    $('notify-error').textContent = choices.length ? '' : 'No products are available for notifications with this selection.';
    $('notify-error').hidden = choices.length > 0;
    $('notify-submit').disabled = notificationBusy || !choices.length;
    $('notify-open').setAttribute('aria-expanded', 'true');
    document.querySelectorAll('[data-mode]').forEach(b => b.setAttribute('aria-pressed', 'false'));
    signup.showModal();
  };
  $('notify-form').onsubmit = async e => {
    e.preventDefault();
    if (notificationBusy || !notificationIds.length) return;
    if (!$('notify-email').checkValidity()) {
      $('notify-error').textContent = 'Please enter a valid email address.';
      $('notify-error').hidden = false; $('notify-email').focus(); return;
    }
    notificationBusy = true; $('notify-submit').disabled = true; $('notify-submit').textContent = 'Signing up...';
    $('notify-error').hidden = true;
    const ids = [...notificationIds];
    let failureMessage = "We couldn't complete your signup. Please try again.";
    try {
      const response = await fetch('/api/AvailabilitySubscriptions/batch', {
        method:'POST', credentials:'omit', headers:{'Content-Type':'application/json'},
        body:JSON.stringify({farmId, farmDefinitionIds:ids, email:$('notify-email').value.trim()}),
        signal:AbortSignal.timeout(30000)
      });
      if (!response.ok) {
        if (response.status === 400) {
          const result = await response.json().catch(() => ({}));
          if (result.code === 'invalid_email') failureMessage = 'Please enter a valid email address.';
          if (result.code === 'no_products') failureMessage = 'No products are available for notifications with this selection.';
        }
        throw new Error('signup');
      }
      closeSignup();
      document.querySelector('[data-mode="farm"]').click();
      $('notify-email').value = '';
      clearTimeout(confirmationTimer); $('notify-success').hidden = false;
      confirmationTimer = setTimeout(() => { $('notify-success').hidden = true; }, 5000);
    } catch {
      $('notify-error').textContent = failureMessage;
      $('notify-error').hidden = false;
    } finally {
      notificationBusy = false; $('notify-submit').disabled = !notificationIds.length; $('notify-submit').textContent = 'Notify Me';
    }
  };
  async function get(route) {
    const response = await fetch(`/api/UnityMap/${route}?farmId=${farmId}`, {
      credentials:'omit', cache:'no-store', signal:AbortSignal.timeout(20000), headers:{Accept:'application/json'}
    });
    if (!response.ok) throw new Error(response.status === 404 ? 'This farm could not be found.' : 'The farm map could not be loaded.');
    return response.json();
  }
  async function load() {
    if (!farmId) { notice('Open this map with a valid farm link, such as /viewer/?farmId=1.'); return; }
    if (loading) return;
    loading = true; $('retry').hidden = true;
    try {
      const [nextFarm, nextMap] = await Promise.all([get('farm'), get('published')]);
      if (nextFarm.farmId !== farmId || nextMap.farmId !== farmId) throw new Error('The server returned a different farm.');
      farm = nextFarm; published = nextMap;
      drawGps();
      items = new Map((farm.items || []).map(i => [i.id, i]));
      const previousCrop = $('crop').value, previousVariety = $('variety').value;
      options($('crop'), [...items.values()].filter(i => i.isPublic && i.type !== 'Pathway' && i.parentId == null), 'All crops', previousCrop);
      varieties(previousVariety);
      document.title = `${farm.farmName || 'Farm'} · Agriloco`;
      const nextImageKey = `${farm.mapImageUrl}|${farm.mapImageUploadedAt}`;
      if (!farm.mapImageUrl) throw new Error('This farm has not uploaded a basemap yet.');
      if (imageKey !== nextImageKey || !ready) {
        const url = new URL(farm.mapImageUrl.replace(/^(?!https?:|\/)/, '/'), location.origin);
        if (!['https:','http:'].includes(url.protocol) || (location.protocol === 'https:' && url.protocol !== 'https:'))
          throw new Error('The farm image must use a secure image URL.');
        if (farm.mapImageUploadedAt) url.searchParams.set('v', farm.mapImageUploadedAt);
        const version = ++imageVersion;
        await new Promise((resolve, reject) => {
          image.onload = () => {
            if (version !== imageVersion) return;
            width = image.naturalWidth; height = image.naturalHeight;
            surface.style.width = `${width}px`; surface.style.height = `${height}px`;
            svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
            image.alt = `${farm.farmName} basemap`;
            surface.style.visibility = 'visible'; ready = true; imageKey = nextImageKey;
            recenter(true); resolve();
          };
          image.onerror = () => reject(new Error('The farm basemap could not be downloaded.'));
          image.src = url.href;
        });
      }
      render();
    } catch (error) { notice(`${error.message || 'Connection interrupted.'}${ready ? ' Showing the last loaded map.' : ''}`, true); }
    finally { loading = false; }
  }
  $('retry').onclick = load;
  function element(tag, attrs, parent = svg) {
    const node = document.createElementNS(NS, tag);
    for (const [key,value] of Object.entries(attrs)) node.setAttribute(key, value);
    parent.append(node); return node;
  }
  function labelText(f) {
    if (f.showLabel === false) return '';
    return (f.labelSourceFarmDefinitionId ? items.get(f.labelSourceFarmDefinitionId)?.name : f.customLabel?.trim()) || f.displayPath?.split(' > ').pop() || items.get(f.farmDefinitionId)?.name || '';
  }
  function midpoint(points) {
    const lengths = points.slice(1).map((p,i) => Math.hypot(p.x-points[i].x,p.y-points[i].y));
    let half = lengths.reduce((a,b) => a+b,0)/2;
    for (let i=0;i<lengths.length;i++) {
      if (half <= lengths[i]) { const t = lengths[i] ? half/lengths[i] : 0; return {x:points[i].x+(points[i+1].x-points[i].x)*t,y:points[i].y+(points[i+1].y-points[i].y)*t}; }
      half -= lengths[i];
    }
    return points[0];
  }
  function render() {
    if (!ready) return;
    svg.replaceChildren(); features = [];
    const defs = element('defs', {});
    const haloFilter = element('filter', {id:'pathway-halo', filterUnits:'userSpaceOnUse',
      x:-32, y:-32, width:width+64, height:height+64}, defs);
    element('feGaussianBlur', {'data-pathway-blur':'', stdDeviation:1}, haloFilter);
    // Explicit paint layers keep paths below assets regardless of saved feature order.
    const shadows = element('g', {'data-layer':'pathway-shadows', 'pointer-events':'none'});
    const pathways = element('g', {'data-layer':'pathways'});
    const assets = element('g', {'data-layer':'assets'});
    const labels = element('g', {'data-layer':'labels'});
    let unsupported = 0;
    for (const f of published.features || []) {
      if (!matches(f)) continue;
      const points = (f.points || []).filter(p => Number.isFinite(p.x) && Number.isFinite(p.y))
        .map(p => ({x:p.x*width, y:(1-p.y)*height})); // Unity's normalized origin is bottom-left.
      const type = (f.geometryType || '').toLowerCase();
      if (!points.length || !['line','polyline','row','polygon','point'].includes(type)) { unsupported++; continue; }
      const pathway = isPathway(f);
      const status = pathway ? 'Pathway' : items.get(f.farmDefinitionId)?.status || f.status;
      const tint = color[category(status)], lineWidth = Math.max(2,Math.min(14,Number(f.lineWidth) || 3));
      const group = element('g', {class:'map-feature', 'data-feature-id':f.id, tabindex:'0', role:'button', 'aria-label':`${labelText(f)} — ${status || 'Availability not set'}`}, pathway ? pathways : assets);
      let labelGroup = null;
      const pointsString = points.map(p => `${p.x},${p.y}`).join(' ');
      if (pathway) {
        // Same saved ordered points; no closure, resampling, or availability styling.
        for (const [stroke, screenWidth, halo] of [['#505050',11,true],['#f5edc4',4,false]]) {
          element('polyline', {class:'pathway-stroke', points:pointsString, fill:'none', stroke,
            'data-screen-width':screenWidth, 'stroke-linecap':'round', 'stroke-linejoin':'round',
            filter:halo ? 'url(#pathway-halo)' : 'none',
            opacity:Math.max(.25,Math.min(1,f.opacity ?? 1))*(halo ? .6 : 1)}, halo ? shadows : group);
        }
      } else if (type === 'point') {
        const point = element('g', {class:'map-label', 'data-x':points[0].x, 'data-y':points[0].y},group);
        element('circle',{r:7,fill:tint,stroke:'white','stroke-width':2},point);
      } else {
        const tag = type === 'polygon' ? 'polygon' : 'polyline';
        element(tag,{class:'shape',points:pointsString,fill:'none',stroke:'#11291b90','stroke-width':lineWidth+4},group);
        element(tag,{class:'shape',points:pointsString,fill:type === 'polygon' ? tint : 'none','fill-opacity':.2,stroke:tint,'stroke-width':lineWidth,opacity:Math.max(.25,Math.min(1,f.opacity ?? 1))},group);
      }
      if (f.showLabel === true && labelText(f)) {
        labelGroup = element('g', {class:'map-feature', 'data-feature-id':f.id}, labels);
        let layout = null;
        try { const parsed = JSON.parse(f.labelLayoutJson || 'null'); if (parsed?.version === 1) layout = parsed; } catch { /* Legacy defaults for absent/invalid metadata. */ }
        const row = ['line','row','polyline'].includes(type);
        const savedAnchor = (f.labelPosition || 'center').toLowerCase();
        const automatic = row && (layout ? !layout.manual : !['left','right'].includes(savedAnchor));
        const position = automatic ? 'right' : savedAnchor;
        const ends = [points[0], points.at(-1)];
        const anchor = position === 'right' ? ends.reduce((a,b)=>a.x>=b.x?a:b) :
          position === 'left' ? points.reduce((a,b)=>a.x<=b.x?a:b) : midpoint(points);
        const finite = (value, fallback) => Number.isFinite(value) ? value : fallback;
        // All label dimensions are in basemap units and inherit the same pan/zoom as geometry.
        // Manual offsets use Unity normalized coordinates: positive Y is up.
        const dx = automatic || !layout ? (position === 'right' ? 4 : 0) : finite(layout.offsetX,0)*width;
        const dy = automatic || !layout ? 0 : -finite(layout.offsetY,0)*height;
        const label = element('g',{class:'published-label',stroke:'none',transform:
          `translate(${anchor.x+dx},${anchor.y+dy-(position==='center'?18:0)})`},labelGroup);
        const palette = {'white':'#fff','black':'#191919','red':'#cc3333','yellow':'#f2cc1a',
          'green':'#268c40','blue':'#2659bf','grey':'#666','gray':'#666','dark slate':'#1f293d'};
        const fontSize = automatic ? Math.min(Number(f.labelFontSize)||9,9) : Number(f.labelFontSize)||9;
        const text = element('text',{'text-anchor':position==='right'?'start':position==='left'?'end':'middle',
          'dominant-baseline':'central',fill:palette[(f.labelTextColour||'white').toLowerCase()]||'#fff',
          'font-size':fontSize,'font-weight':500},label);
        text.textContent = labelText(f);
        const box = text.getBBox(), padX = Math.max(0,finite(layout?.paddingX,6)), padY = Math.max(0,finite(layout?.paddingY,2));
        // Keep the bubble edge, rather than text centre, at the endpoint gap.
        const shift = position==='right' ? padX/2-box.x : position==='left' ? -padX/2-(box.x+box.width) : 0;
        text.setAttribute('x',shift);
        const background = element('rect',{x:box.x+shift-padX/2,y:box.y-padY/2,
          width:box.width+padX,height:box.height+padY,rx:1,
          fill:palette[(layout?.backgroundColour||'dark slate').toLowerCase()]||'#1f293d'},label);
        label.insertBefore(background,text);
      }
      group.onclick = () => { if (moved < 6) showFeature(f); };
      group.onkeydown = e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); e.stopPropagation(); showFeature(f); } };
      if (labelGroup) labelGroup.onclick = group.onclick;
      features.push(f);
    }
    notice(features.length ? '' : unsupported ? 'This map contains geometry that cannot be displayed yet.' :
      mode !== 'farm' ? `No ${mode === 'market' ? 'market' : 'other'} locations have been published.` :
      (published.features || []).length ? 'No mapped features match these filters.' : 'This farm has not published any map features yet.');
    if (selectedId != null) {
      const current = features.find(f => f.id === selectedId);
      if (current) showFeature(current); else { selectedId = null; $('feature-card').hidden = true; }
    }
    drawGps(); transform();
  }
  function showFeature(f) {
    selectedId = f.id;
    const thumbnail = $('feature-image'), imageUrl = items.get(f.farmDefinitionId)?.imageUrl;
    if (imageUrl) {
      if (thumbnail.getAttribute('src') !== imageUrl) { thumbnail.hidden = false; thumbnail.src = imageUrl; }
    } else { thumbnail.hidden = true; thumbnail.removeAttribute('src'); }
    $('feature-name').textContent = labelText(f) || 'Map feature';
    $('feature-name').hidden = f.showLabel === false;
    $('feature-status').textContent = isPathway(f) ? 'Pathway' : items.get(f.farmDefinitionId)?.status || f.status || 'Availability not set';
    $('feature-path').textContent = f.displayPath || '';
    $('feature-path').hidden = f.showLabel === false;
    $('feature-card').hidden = false; $('location-card').hidden = true;
  }
  $('close-feature').onclick = () => { selectedId = null; $('feature-card').hidden = true; };
  $('feature-image').onerror = () => { $('feature-image').hidden = true; };
  function limits() {
    minScale = Math.min(map.clientWidth/width,map.clientHeight/height);
    maxScale = Math.max(map.clientWidth/width,map.clientHeight/height)*10;
  }
  function transform() {
    const w = width*scale, h = height*scale;
    x = w < map.clientWidth ? (map.clientWidth-w)/2 : Math.min(0,Math.max(map.clientWidth-w,x));
    y = h < map.clientHeight ? (map.clientHeight-h)/2 : Math.min(0,Math.max(map.clientHeight-h,y));
    surface.style.transform = `translate(${x}px,${y}px) scale(${scale})`;
    // Counter the CSS surface scale so pathway widths, dashes and gaps stay in screen pixels.
    svg.querySelectorAll('.pathway-stroke').forEach(stroke => {
      stroke.setAttribute('stroke-width', Number(stroke.dataset.screenWidth)/scale);
      stroke.setAttribute('stroke-dasharray', `${14/scale} ${8/scale}`);
    });
    svg.querySelector('[data-pathway-blur]')?.setAttribute('stdDeviation', 1/scale);
    svg.querySelectorAll('.map-label').forEach(label => label.setAttribute('transform',`translate(${label.dataset.x},${label.dataset.y}) scale(${1/scale})`));
  }
  function recenter(cover = false) {
    if (!ready) return;
    limits(); scale = cover ? Math.max(map.clientWidth/width,map.clientHeight/height) : minScale;
    x = (map.clientWidth-width*scale)/2; y = (map.clientHeight-height*scale)/2;
    // Start near the actual published rows when portrait cropping is needed.
    const points = (published?.features || []).filter(f => f.isPublic && f.isActive).flatMap(f => f.points || [])
      .filter(p => Number.isFinite(p.x) && Number.isFinite(p.y));
    if (cover && points.length) {
      x = map.clientWidth/2 - (Math.min(...points.map(p=>p.x))+Math.max(...points.map(p=>p.x)))/2*width*scale;
      y = map.clientHeight/2 - (1-(Math.min(...points.map(p=>p.y))+Math.max(...points.map(p=>p.y)))/2)*height*scale;
    }
    transform();
  }
  function zoom(factor,cx = map.clientWidth/2,cy = map.clientHeight/2) {
    if (!ready) return;
    interacted = true;
    const next = Math.max(minScale,Math.min(maxScale,scale*factor)), ratio = next/scale;
    x = cx-(cx-x)*ratio; y = cy-(cy-y)*ratio; scale = next; transform();
  }
  $('zoom-in').onclick = () => zoom(1.35); $('zoom-out').onclick = () => zoom(1/1.35); $('fit').onclick = () => recenter();
  map.addEventListener('wheel',e => { if (!ready) return; e.preventDefault(); const r=map.getBoundingClientRect(); zoom(Math.exp(-Math.max(-150,Math.min(150,e.deltaY*(e.deltaMode === 1 ? 16 : 1)))*.004),e.clientX-r.left,e.clientY-r.top); },{passive:false});
  const center = values => ({x:(values[0].x+values[1].x)/2,y:(values[0].y+values[1].y)/2});
  const distance = values => Math.hypot(values[0].x-values[1].x,values[0].y-values[1].y);
  map.onpointerdown = e => {
    if (!ready || e.button > 0 || pointers.size >= 2) return;
    if (!pointers.size) { moved=0; downFeature=e.target.closest('[data-feature-id]')?.dataset.featureId; }
    interacted = true;
    pointers.set(e.pointerId,{x:e.clientX,y:e.clientY});
    if (pointers.size === 2) { moved = Math.max(6,moved); downFeature = null; }
    map.setPointerCapture(e.pointerId);
  };
  map.onpointermove = e => {
    if (!pointers.has(e.pointerId)) return;
    const before=[...pointers.values()], old=pointers.get(e.pointerId);
    moved += Math.hypot(e.clientX-old.x,e.clientY-old.y);
    pointers.set(e.pointerId,{x:e.clientX,y:e.clientY}); const after=[...pointers.values()];
    if (after.length === 1) { x+=e.clientX-old.x; y+=e.clientY-old.y; }
    else {
      const a=center(before), b=center(after), r=map.getBoundingClientRect();
      const next = Math.max(minScale,Math.min(maxScale,scale*distance(after)/Math.max(1,distance(before))));
      const ratio = next/scale;
      // Apply pinch scale and midpoint movement together, then constrain once.
      x = b.x-r.left-(a.x-r.left-x)*ratio;
      y = b.y-r.top-(a.y-r.top-y)*ratio;
      scale = next;
    }
    transform();
  };
  map.onpointerup = e => {
    if (pointers.size === 1 && moved < 6 && downFeature != null) {
      const feature=features.find(f=>String(f.id)===downFeature); if(feature)showFeature(feature);
    }
    pointers.delete(e.pointerId); downFeature=null;
  };
  map.onpointercancel = map.onlostpointercapture = e => { pointers.delete(e.pointerId); downFeature=null; };
  map.onkeydown = e => {
    if (e.target !== map) return;
    if (['+','=','-','ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)) e.preventDefault();
    if (e.key === '+' || e.key === '=') zoom(1.2); else if (e.key === '-') zoom(1/1.2);
    else { if(e.key==='ArrowLeft') x+=40; if(e.key==='ArrowRight') x-=40; if(e.key==='ArrowUp') y+=40; if(e.key==='ArrowDown') y-=40; transform(); }
  };
  new ResizeObserver(() => {
    document.getElementById('viewer').style.setProperty('--panel-height', document.getElementById('filters').offsetHeight + 'px');
  }).observe(document.getElementById('filters'));
  new ResizeObserver(() => {
    if (ready) {
      const cx=(viewportWidth/2-x)/scale,cy=(viewportHeight/2-y)/scale;
      limits();
      if (!interacted) recenter(true);
      else { scale=Math.max(minScale,Math.min(maxScale,scale)); x=map.clientWidth/2-cx*scale; y=map.clientHeight/2-cy*scale; transform(); }
    }
    viewportWidth=map.clientWidth; viewportHeight=map.clientHeight;
  }).observe(map);

  // Same LocalEquirectangularAffine projection as Shared/GeoCalibration.cs.
  // Only calibration coefficients are fetched; visitor coordinates never leave this browser.
  let calibration = null, calibrationMessage = 'Loading GPS calibration\u2026', calibrationLoading = false, gpsGeneration = 0;
  async function loadCalibration() {
    if (!farmId || calibrationLoading) return;
    calibrationLoading = true;
    try {
      const url = `/api/UnityMap/georeference?farmId=${farmId}`;
      const response = await fetch(url,{credentials:'omit',cache:'no-store',signal:AbortSignal.timeout(20000)});
      if (!response.ok) throw new Error(`GPS calibration unavailable (GET ${url}, HTTP ${response.status}).`);
      const data = await response.json(), t = data.transform;
      if (data.farmId !== farmId) throw new Error('GPS calibration belongs to a different farm.');
      if (!t) { calibration=null; calibrationMessage=data.status || 'This farm is not georeferenced.'; }
      else {
        if (t.version!==1 || t.projection!=='LocalEquirectangularAffine' ||
            ![t.originLatitude,t.originLongitude,t.earthRadiusMeters,...(t.x||[]),...(t.y||[])].every(Number.isFinite) ||
            t.x?.length!==3 || t.y?.length!==3 || t.earthRadiusMeters<=0 ||
            Math.abs(t.originLatitude)>85 || Math.abs(t.originLongitude)>180 ||
            Math.abs(t.x[1]*t.y[2]-t.x[2]*t.y[1])<1e-15)
          throw new Error('GPS calibration is invalid or unsupported.');
        calibration=data; calibrationMessage='';
      }
    } catch(error) { calibration=null; calibrationMessage=error.message || 'GPS calibration could not be loaded. Try again.'; }
    finally { calibrationLoading=false; drawGps(); if(ready)transform(); }
  }
  function currentCalibration() {
    if (!calibration || !ready || imageKey!==`${farm?.mapImageUrl}|${farm?.mapImageUploadedAt}` || calibration.mapImageUrl!==farm?.mapImageUrl ||
        calibration.mapImageUploadedAt!==farm?.mapImageUploadedAt) return null;
    return calibration.transform;
  }
  function gpsPoint() {
    const t=currentCalibration(); if(!gps || !t)return null;
    const radians=Math.PI/180;
    const east=t.earthRadiusMeters*Math.cos(t.originLatitude*radians)*(((gps.longitude-t.originLongitude+180)%360+360)%360-180)*radians;
    const north=t.earthRadiusMeters*(gps.latitude-t.originLatitude)*radians;
    return {x:(t.x[0]+t.x[1]*east+t.x[2]*north)*width,y:(1-t.y[0]-t.y[1]*east-t.y[2]*north)*height};
  }
  const onMap = p => p && p.x>=0 && p.x<=width && p.y>=0 && p.y<=height;
  function gpsMessage(text = '') {
    $('location-text').textContent = text;
    $('location-card').hidden = !text;
  }
  function drawGps() {
    svg.querySelector('#user-position')?.remove();
    if(!gps)return;
    const p=gpsPoint(), t=currentCalibration();
    gpsMessage(!p ? calibrationMessage || 'GPS calibration does not match the loaded basemap.' :
      !onMap(p) ? 'Your location is outside this farm image. Your position has not been moved onto the farm.' :
      gps.accuracy>30 ? 'Poor GPS accuracy: move outdoors and wait for a better fix.' : '');
    if(!onMap(p))return;
    const g=element('g',{id:'user-position','aria-label':'You Are Here','pointer-events':'none'});
    // A metre-radius circle transformed through the affine basis becomes an ellipse.
    // Longitude's local metre scale is corrected at the reported latitude.
    const eastScale=Math.cos(t.originLatitude*Math.PI/180)/Math.cos(gps.latitude*Math.PI/180);
    element('circle',{r:gps.accuracy,class:'user-accuracy',transform:`matrix(${width*t.x[1]*eastScale} ${-height*t.y[1]*eastScale} ${width*t.x[2]} ${-height*t.y[2]} ${p.x} ${p.y})`},g);
    const dot=element('g',{class:'map-label','data-x':p.x,'data-y':p.y},g);
    element('circle',{r:7,class:'user-marker'},dot);
    element('title',{},dot).textContent='You Are Here';
  }
  function stopGps() {
    ++gpsGeneration;
    if(watchId!==null)navigator.geolocation?.clearWatch(watchId);
    watchId=null; gps=null; drawGps(); $('locate').setAttribute('aria-pressed','false');
    $('stop-location').hidden=true; gpsMessage();
  }
  $('locate').onclick = () => {
    $('feature-card').hidden=true;
    if (!navigator.geolocation || !window.isSecureContext) { gpsMessage('Location requires HTTPS and a browser with geolocation support.'); return; }
    if (!currentCalibration()) { gpsMessage(calibrationMessage || 'GPS calibration does not match the loaded basemap. Try again after it reloads.'); loadCalibration(); return; }
    if (watchId!==null) { const p=gpsPoint(); if(onMap(p)){x=map.clientWidth/2-p.x*scale;y=map.clientHeight/2-p.y*scale;transform();} return; }
    gpsMessage();
    $('stop-location').hidden=false;
    const generation=++gpsGeneration;
    $('locate').setAttribute('aria-pressed','true');
    // Call synchronously from the user's button press, never on page load.
    try { watchId=navigator.geolocation.watchPosition(position => {
      if(generation!==gpsGeneration)return;
      const c=position.coords;
      if(![c.latitude,c.longitude,c.accuracy].every(Number.isFinite)||Math.abs(c.latitude)>=90||Math.abs(c.longitude)>180||c.accuracy<0){
        gps=null;drawGps();gpsMessage('The device returned an invalid GPS fix. Waiting for another reading.');return;
      }
      gps={latitude:c.latitude,longitude:c.longitude,accuracy:c.accuracy}; drawGps();transform();
    },error=>{
      if(generation!==gpsGeneration)return;
      gps=null;drawGps();
      if(error.code===1)stopGps();
      gpsMessage(error.code===1 ? 'Location permission was denied. Allow location in browser settings, then press GPS again.' :
        error.code===3 ? 'Location timed out. Move outdoors; tracking will retry. You can also stop and restart GPS.' :
        'Location is unavailable. Move outdoors or enable device location. Waiting for a GPS fix.');
    },{enableHighAccuracy:true,maximumAge:1000,timeout:15000}); }
    catch(error){stopGps();gpsMessage('Could not start browser location. Check device location and browser permissions.');}
  };
  $('dismiss-location-error').onclick=()=>gpsMessage();
  $('stop-location').onclick=()=>{stopGps();$('location-card').hidden=true;};
  window.addEventListener('pagehide',()=>{stopGps();$('location-card').hidden=true;});
  loadCalibration();
  setInterval(()=>{if(!document.hidden)loadCalibration();},30000);
  load();
  setInterval(()=>{if(!document.hidden && !pointers.size)load();},30000);
})();

