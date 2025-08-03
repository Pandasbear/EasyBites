// main.js

  const hamburger = document.querySelector('.hamburger');
  const navMenu = document.querySelector('.nav-menu');
  if (hamburger && navMenu) {
    hamburger.addEventListener('click', () => {
      hamburger.classList.toggle('active');
      navMenu.classList.toggle('active');
    });
  }

  window.EasyBites = window.EasyBites || {};

  window.EasyBites.formatDate = (dateString) => {
    if (!dateString) return 'Date not available';
    
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime()) || date.getFullYear() < 1900) {
        return 'Date not available';
      }
      return date.toLocaleDateString();
    } catch (error) {
      return 'Date not available';
    }
  };
  window.EasyBites.toast = (message, type = 'info', duration = 3000) => {
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
      const newContainer = document.createElement('div');
      newContainer.id = 'toast-container';
      document.body.appendChild(newContainer);
      toastContainer = newContainer;
    }

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;

    toastContainer.appendChild(toast);

    setTimeout(() => {
      toast.classList.add('show');
    }, 100);

    setTimeout(() => {
      toast.classList.remove('show');
      toast.addEventListener('transitionend', () => toast.remove());
    }, duration);
  };

  window.EasyBites.api = async (url, options = {}) => {
    const opts = Object.assign({
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
      expectedStatusCodes: [],
    }, options);
    
    try {
      const res = await fetch(url, opts);
      
      if (!res.ok && !opts.expectedStatusCodes.includes(res.status)) {
        let msg = `Request failed (${res.status} ${res.statusText})`;
        let details = null;
        
        try {
          const contentType = res.headers.get('content-type') || '';
          if (contentType.includes('application/json')) {
            details = await res.json();
            msg = details.title || details.message || details.errors || msg;
          } else {
            const textResponse = await res.text();
            if (textResponse) {
              msg = textResponse;
            }
          }
        } catch (parseError) {}
        
        const error = new Error(typeof msg === 'string' ? msg : JSON.stringify(msg));
        error.status = res.status;
        error.statusText = res.statusText;
        error.response = details; 
        error.url = url;
        
        throw error;
      }
      
      const ct = res.headers.get('content-type') || '';
      const result = ct.includes('application/json') ? await res.json() : await res.text();
      return result;
      
    } catch (networkError) {
      if (networkError.name === 'TypeError' && networkError.message.includes('fetch')) {
        throw new Error('Network error - unable to connect to server');
      }
      throw networkError;
    }
  };

  document.addEventListener('DOMContentLoaded', async () => {
    let user = null;
    try {
      user = await window.EasyBites.api('/api/auth/me');
    } catch {
      // guest user
    }

    if (navMenu) {
      const loginLink = navMenu.querySelector('a[href="login.html"]');
      const registerLink = navMenu.querySelector('a[href="register.html"]');
      const shareLink = navMenu.querySelector('a[href="submit-recipe.html"]');

      if (user) {
        if (shareLink) shareLink.style.display = '';

        // Login → View Account
        if (loginLink) {
          loginLink.setAttribute('href', 'account.html');
          loginLink.textContent = 'View Account';
        }

        // Add Admin Dashboard link ONLY for admin users with admin sessions
        if (user.isAdmin && user.isAdminSession) {
          // Check if admin link already exists
          let adminLink = navMenu.querySelector('a[href="admin-dashboard.html"]');
          if (!adminLink) {
            // Create admin dashboard link
            const adminLi = document.createElement('li');
            adminLink = document.createElement('a');
            adminLink.href = 'admin-dashboard.html';
            adminLink.textContent = '🔐 Admin Dashboard';
            adminLink.className = 'nav-link';
            adminLi.appendChild(adminLink);
            
            // Insert before the View Account link
            if (loginLink && loginLink.parentElement) {
              navMenu.insertBefore(adminLi, loginLink.parentElement);
            }
          }
        } else {
          // Remove admin link if user doesn't have admin session
          const adminLink = navMenu.querySelector('a[href="admin-dashboard.html"]');
          if (adminLink && adminLink.parentElement) {
            adminLink.parentElement.remove();
          }
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
        
        // Remove admin link if it exists (for when user logs out)
        const adminLink = navMenu.querySelector('a[href="admin-dashboard.html"]');
        if (adminLink && adminLink.parentElement) {
          adminLink.parentElement.remove();
        }
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