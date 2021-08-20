export default class Toaster {

  constructor(el) {
    this.el = el;
    this.originalDisplay = el.style.display;
    this.el.hidden = false;
    this.close();
    this.textEl = document.createElement('span');
    this.el.appendChild(this.textEl);
    const closeEl = document.createElement('span');
    closeEl.className = 'close';
    closeEl.innerHTML = '&times;';
    closeEl.addEventListener('click', this.close.bind(this));
    this.el.appendChild(closeEl);
  }

  close() {
    this.el.style.display = 'none';
  }

  warn(msg) {
    this._showMsg(msg, 'warn');
  }

  info(msg) {
    this._showMsg(msg, 'info');
  }

  error(msg) {
    this._showMsg(msg, 'error');
  }

  success(msg) {
    this._showMsg(msg, 'success');
  }

  _showMsg(msg, className) {
    this._removeAllClasses();
    this.el.classList.add(className);
    this.textEl.textContent = msg;
    this.el.style.display = this.originalDisplay;
  }

  _removeAllClasses() {
    this.el.classList.remove('warn', 'info', 'error', 'success');
  }

}