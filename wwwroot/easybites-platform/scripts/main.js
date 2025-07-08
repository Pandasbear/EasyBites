// Basic main.js for EasyBites – handles mobile nav toggle and helper utilities

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

    // Show the toast
    setTimeout(() => {
      toast.classList.add('show');
    }, 100); // Small delay to allow CSS transition

    // Hide and remove the toast after duration
    setTimeout(() => {
      toast.classList.remove('show');
      toast.addEventListener('transitionend', () => toast.remove());
    }, duration);
  };

  // Enhanced JSON fetch helper with better error handling
  window.EasyBites.api = async (url, options = {}) => {
    
    const opts = Object.assign({
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
      expectedStatusCodes: [], // New option to specify status codes that should not trigger an error
    }, options);
    
    try {
      const res = await fetch(url, opts);
      
      console.log(`[API] Response status: ${res.status} ${res.statusText}`, {
        url: url,
        status: res.status,
        statusText: res.statusText,
        headers: Object.fromEntries(res.headers.entries())
      });
      
      // Check if the status is not OK and not in the list of expected non-error status codes
      if (!res.ok && !opts.expectedStatusCodes.includes(res.status)) {
        let errorMessage = `Request failed (${res.status} ${res.statusText})`;
        let errorDetails = null;
        
        try {
          const contentType = res.headers.get('content-type') || '';
          if (contentType.includes('application/json')) {
            errorDetails = await res.json();
            // For validation errors, errorDetails might contain a 'errors' property
            errorMessage = errorDetails.title || errorDetails.message || errorDetails.errors || errorMessage;
          } else {
            const textResponse = await res.text();
            if (textResponse) {
              errorMessage = textResponse;
            }
          }
        } catch (parseError) {
          console.warn('[API] Could not parse error response:', parseError);
        }
        
        console.error(`[API] Request failed:`, {
          url: url,
          status: res.status,
          statusText: res.statusText,
          errorMessage: errorMessage,
          errorDetails: errorDetails
        });
        
        // Create enhanced error object
        const error = new Error(typeof errorMessage === 'string' ? errorMessage : JSON.stringify(errorMessage)); // Ensure message is string for Error constructor
        error.status = res.status;
        error.statusText = res.statusText;
        error.response = errorDetails; 
        error.url = url;
        
        throw error;
      }
      
      // If it's not OK but is an expected status code, or it is OK, proceed without throwing
      const ct = res.headers.get('content-type') || '';
      const result = ct.includes('application/json') ? await res.json() : await res.text();
      
      console.log(`[API] Success response:`, result);
      return result;
      
    } catch (networkError) {
      console.error(`[API] Network or parse error:`, networkError);
      
      if (networkError.name === 'TypeError' && networkError.message.includes('fetch')) {
        throw new Error('Network error - unable to connect to server');
      }
      
      throw networkError;
    }
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