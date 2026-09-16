(() => {
  const options = document.getElementById('selling-options'), add = document.getElementById('add-selling-option');
  function renumber() {
    [...options.children].forEach((row, index) => {
      row.querySelector('legend').textContent = `Selling option ${index + 1}`;
      row.querySelectorAll('[name]').forEach(input => { input.name = input.name.replace(/Options\[[^\]]*\]/, `Options[${index}]`); });
    });
    add.disabled = options.children.length >= 25;
  }
  add.onclick = () => {
    if (options.children.length >= 25) return;
    options.append(document.getElementById('selling-option-template').content.cloneNode(true));
    renumber(); options.lastElementChild.querySelector('input:not([type=hidden])').focus();
  };
  options.addEventListener('click', event => {
    if (event.target.matches('.remove-option')) { event.target.closest('fieldset').remove(); renumber(); add.focus(); }
  });
  renumber();
})();
