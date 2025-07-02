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
    const ct = res.headers.get('content-type') || '';
    return ct.includes('application/json') ? res.json() : res.text();
  };

  // =====================
  // AUTH-BASED UI HANDLING
  // =====================
  document.addEventListener('DOMContentLoaded', async () => {
    let user = null;
    try {
      user = await window.EasyBites.api('/api/auth/me');
    } catch {
      /* guest */
    }

    if (navMenu) {
      const loginLink = navMenu.querySelector('a[href="login.html"]');
      const registerLink = navMenu.querySelector('a[href="register.html"]');
      const shareLink = navMenu.querySelector('a[href="submit-recipe.html"]');

      if (user) {
        // Show share recipe link
        if (shareLink) shareLink.style.display = '';

        // Login → View Account
        if (loginLink) {
          loginLink.setAttribute('href', 'account.html');
          loginLink.textContent = 'View Account';
        }

        // Register → Sign Out (orange button remains)
        if (registerLink) {
          registerLink.textContent = 'Sign Out';
          registerLink.setAttribute('href', '#');
          registerLink.classList.add('btn-primary');
          registerLink.addEventListener('click', async (e) => {
            e.preventDefault();
            try {
              await window.EasyBites.api('/api/auth/logout', { method: 'POST' });
            } catch {}
            window.location.href = 'index.html';
          });
        }
      } else {
        // Hide share recipe link
        if (shareLink) shareLink.style.display = 'none';
      }

      // Update hero secondary button on home page
      const heroSecondary = document.querySelector('.hero-buttons .btn-secondary');
      if (heroSecondary) {
        if (user) {
          heroSecondary.textContent = 'Share Recipe';
          heroSecondary.setAttribute('href', 'submit-recipe.html');
        } else {
          heroSecondary.textContent = 'Join Community';
          heroSecondary.setAttribute('href', 'register.html');
        }
      }
    }
  });
})(); 