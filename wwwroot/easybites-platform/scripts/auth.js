// auth.js – front-end form handlers calling backend API

document.addEventListener('DOMContentLoaded', () => {
  
  // Check for admin login messages
  const adminLoginMessage = sessionStorage.getItem('adminLoginMessage');
  if (adminLoginMessage && window.location.pathname.includes('admin-login.html')) {
    showAdminMessage(adminLoginMessage);
    sessionStorage.removeItem('adminLoginMessage');
  }
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

// Function to show admin messages
function showAdminMessage(message, type = 'info') {
  const messageDiv = document.createElement('div');
  messageDiv.className = `admin-message ${type}`;
  messageDiv.innerHTML = `
    <span>${message}</span>
    <button onclick="this.parentElement.remove()" class="admin-message-close">×</button>
  `;
  
  const authContainer = document.querySelector('.auth-container');
  if (authContainer) {
    authContainer.insertBefore(messageDiv, authContainer.firstChild);
    
    // Auto-remove after 10 seconds
    setTimeout(() => {
      if (messageDiv.parentElement) {
        messageDiv.remove();
      }
    }, 10000);
  }
}

// Add CSS for admin messages
const adminMessageStyles = `
  .admin-message {
    background-color: #3498db;
    color: white;
    padding: 15px;
    border-radius: 5px;
    margin-bottom: 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    animation: slideDown 0.3s ease-out;
  }
  
  .admin-message.success {
    background-color: #2ecc71;
  }
  
  .admin-message.warning {
    background-color: #f39c12;
  }
  
  .admin-message.error {
    background-color: #e74c3c;
  }
  
  .admin-message-close {
    background: none;
    border: none;
    color: white;
    font-size: 18px;
    cursor: pointer;
    padding: 0;
    margin-left: 10px;
  }
  
  .admin-message-close:hover {
    opacity: 0.7;
  }
  
  @keyframes slideDown {
    from {
      transform: translateY(-100%);
      opacity: 0;
    }
    to {
      transform: translateY(0);
      opacity: 1;
    }
  }
`;

// Inject CSS
const styleSheet = document.createElement('style');
styleSheet.textContent = adminMessageStyles;
document.head.appendChild(styleSheet);
 