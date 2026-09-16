// Presentation only. Original Razor forms, values and submission handlers are untouched.
(() => {
 document.addEventListener('error', event => {
  if (event.target instanceof HTMLImageElement && event.target.classList.contains('definition-thumbnail')) event.target.hidden = true;
 }, true);
 const buttons = document.querySelectorAll('.dash-menu-toggle');
 function navigation(open) {
  document.body.classList.toggle('navigation-open',open);
  buttons.forEach(b => b.setAttribute('aria-expanded',String(open)));
 }
 buttons.forEach(b => b.onclick=()=>navigation(!document.body.classList.contains('navigation-open')));
 document.addEventListener('keydown',e=>{if(e.key==='Escape')navigation(false)});
 const rows=[...document.querySelectorAll('.definition-row')], collapsed=new Set();
 function refresh() {
  const hiddenParents=[];
  rows.forEach(row=>{
   const depth=Number(row.dataset.depth);
   while(hiddenParents.length && hiddenParents.at(-1)>=depth)hiddenParents.pop();
   row.hidden=hiddenParents.length>0;
   if(collapsed.has(row.id))hiddenParents.push(depth);
  });
 }
 document.querySelectorAll('.tree-toggle').forEach(button=>button.onclick=()=>{
  const id='definition-'+button.dataset.definition;
  if(collapsed.has(id))collapsed.delete(id);else collapsed.add(id);
  const open=!collapsed.has(id), name=button.closest('.definition-row').querySelector('.definition-name').textContent.trim();
  button.setAttribute('aria-expanded',String(open));button.setAttribute('aria-label',(open?'Collapse ':'Expand ')+name+' children');
  button.textContent=open?'Collapse ⌃':'Expand ⌄';refresh();
 });
 document.querySelectorAll('.crop-sections a').forEach(link=>link.addEventListener('click',()=>{
  const row=document.querySelector(link.hash);if(!row)return;
  // Section links never change selections, data, or posted form values.
  navigation(false);
 }));
})();
