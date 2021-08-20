export function showSuccessSlide(slides) {
  // We're expecting there to be just two of them even 
  // though the system is primed for more if needed.
  slides[0].className = 'hidden';
  slides[1].className = 'present-forward';
}

export function setLoading(element, isLoading) {
  showModal(element, isLoading);
}

export function showModal(element, show) {
  element.style.display = show ? 'block' : '';
  if (show) 
    document.body.setAttribute('data-overlay', true);
  else 
    document.body.removeAttribute('data-overlay');
}

export function removeNodesFromElement(element) {
  while (element.firstChild) {
    element.removeChild(element.firstChild);
  }
}

export function addHtmlOption(select, text, document, value, { type, callback } = null) {
  const opt = document.createElement('option');
  opt.textContent = text;
  if (value !== undefined) opt.value = value;
  if (type) opt.addEventListener(type, callback);
  select.appendChild(opt);
}