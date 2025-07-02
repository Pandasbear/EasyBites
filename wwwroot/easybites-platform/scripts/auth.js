// auth.js – front-end form handlers calling backend API

importScripts = (() => {
    document.addEventListener('DOMContentLoaded', () => {
      const registerForm = document.getElementById('registerForm');
      if (registerForm) {
        registerForm.addEventListener('submit', async (e) => {
          e.preventDefault();
          const data = Object.fromEntries(new FormData(registerForm));
          try {
            await EasyBites.api('/api/auth/register', {
              method: 'POST',
              body: JSON.stringify({
                firstName: data.firstName,
                lastName: data.lastName,
                email: data.email,
                username: data.username,
                password: data.password,
                confirmPassword: data.confirmPassword,
                cookingLevel: data.cookingLevel,
                terms: data.terms === 'on'
              })
            });
            EasyBites.toast('Registration successful!');
            window.location.href = 'login.html';
          } catch (err) {
            EasyBites.toast(`Error: ${err.message}`);
          }
        });
      }
  
      const loginForm = document.getElementById('loginForm');
      if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
          e.preventDefault();
          const data = Object.fromEntries(new FormData(loginForm));
          try {
            await EasyBites.api('/api/auth/login', {
              method: 'POST',
              body: JSON.stringify({
                loginEmail: data.loginEmail,
                loginPassword: data.loginPassword
              })
            });
            EasyBites.toast('Logged in!');
            window.location.href = 'index.html';
          } catch (err) {
            EasyBites.toast(`Login failed: ${err.message}`);
          }
        });
      }
  
      const adminLoginForm = document.getElementById('adminLoginForm');
      if (adminLoginForm) {
        adminLoginForm.addEventListener('submit', async (e) => {
          e.preventDefault();
          const data = Object.fromEntries(new FormData(adminLoginForm));
          try {
            await EasyBites.api('/api/auth/admin-login', {
              method: 'POST',
              body: JSON.stringify({
                adminUsername: data.adminUsername,
                adminPassword: data.adminPassword,
                adminCode: data.adminCode
              })
            });
            EasyBites.toast('Admin login success');
            window.location.href = 'admin-dashboard.html';
          } catch (err) {
            EasyBites.toast(`Admin login failed: ${err.message}`);
          }
        });
      }
    });
  })(); 