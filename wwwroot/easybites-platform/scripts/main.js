// Basic main.js for EasyBites – handles mobile nav toggle and helper utilities

(function () {
  // Hamburger toggle
  const hamburger = document.querySelector('.hamburger');
  const navMenu = document.querySelector('.nav-menu');
  if (hamburger && navMenu) {
    hamburger.addEventListener('click', () => {
      hamburger.classList.toggle('active');
      navMenu.classList.toggle('active');
    });
  }

  // Simple toast helper
  window.EasyBites = window.EasyBites || {};
  window.EasyBites.toast = (msg) => alert(msg);

  // Generic JSON fetch helper
  window.EasyBites.api = async (url, options = {}) => {
    const opts = Object.assign({
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
    }, options);
    const res = await fetch(url, opts);
    if (!res.ok) {
      const txt = await res.text();
      throw new Error(txt || `Request failed (${res.status})`);
    }
    return res.json();
  };
})(); 