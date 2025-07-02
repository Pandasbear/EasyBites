// account.js – handles profile, password, delete, and recipe lists
(function () {
  document.addEventListener('DOMContentLoaded', async () => {
    let user = null;
    try {
      user = await EasyBites.api('/api/auth/me');
    } catch {
      // Not logged in, redirect
      window.location.href = 'login.html';
      return;
    }

    // Pre-fill profile form
    const profileForm = document.getElementById('profileForm');
    if (profileForm && user) {
      profileForm.firstName.value = user.firstName || '';
      profileForm.lastName.value = user.lastName || '';
      profileForm.username.value = user.username || '';
      profileForm.cookingLevel.value = user.cookingLevel || 'Beginner';
      profileForm.favoriteCuisine.value = user.favoriteCuisine || '';
      profileForm.location.value = user.location || '';
      profileForm.bio.value = user.bio || '';

      profileForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fd = new FormData(profileForm);
        const body = Object.fromEntries(fd.entries());
        try {
          await EasyBites.api('/api/auth/profile', {
            method: 'PUT',
            body: JSON.stringify(body)
          });
          EasyBites.toast('Profile updated successfully');
        } catch (err) {
          EasyBites.toast(`Update failed: ${err.message}`);
        }
      });
    }

    // Password change
    const passwordForm = document.getElementById('passwordForm');
    if (passwordForm) {
      passwordForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fd = new FormData(passwordForm);
        const body = {
          currentPassword: fd.get('currentPassword'),
          newPassword: fd.get('newPassword'),
          confirmPassword: fd.get('confirmPassword')
        };
        try {
          await EasyBites.api('/api/auth/password', {
            method: 'PUT',
            body: JSON.stringify(body)
          });
          EasyBites.toast('Password updated');
          passwordForm.reset();
        } catch (err) {
          EasyBites.toast(`Password update failed: ${err.message}`);
        }
      });
    }

    // Delete account
    const deleteForm = document.getElementById('deleteForm');
    if (deleteForm) {
      deleteForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!confirm('Are you sure you want to delete your account? This action cannot be undone.')) return;
        const fd = new FormData(deleteForm);
        try {
          await EasyBites.api('/api/auth/delete-account', {
            method: 'POST',
            body: JSON.stringify({ password: fd.get('deletePassword') })
          });
          alert('Account deleted');
          window.location.href = 'index.html';
        } catch (err) {
          EasyBites.toast(`Delete failed: ${err.message}`);
        }
      });
    }

    // Load user recipes
    async function loadUserRecipes() {
      const grid = document.getElementById('myRecipesGrid');
      if (!grid) return;
      try {
        const recipes = await EasyBites.api(`/api/recipes/user/${user.id}`);
        renderRecipes(recipes, grid);
      } catch {
        grid.innerHTML = '<p class="error-message">Failed to load your recipes.</p>';
      }
    }

    // Load saved recipes
    async function loadSavedRecipes() {
      const grid = document.getElementById('savedRecipesGrid');
      if (!grid) return;
      try {
        const recipes = await EasyBites.api('/api/recipes/saved');
        renderRecipes(recipes, grid);
      } catch {
        grid.innerHTML = '<p class="error-message">Failed to load saved recipes.</p>';
      }
    }

    // Re-use simple card renderer from recipes.js style
    function renderRecipes(recipes, grid) {
      grid.innerHTML = '';
      if (!recipes || recipes.length === 0) {
        grid.innerHTML = '<p>No recipes found.</p>';
        return;
      }
      recipes.forEach(r => {
        const card = document.createElement('div');
        card.className = 'recipe-card';
        card.innerHTML = `
          <div class="recipe-image">
            <img src="${r.imageUrl || 'https://via.placeholder.com/300x200?text=Recipe'}" alt="${r.name}">
          </div>
          <div class="recipe-info">
            <h3>${r.name}</h3>
            <p class="recipe-description">${r.description || ''}</p>
            <a href="recipe-detail.html?id=${r.id}" class="btn btn-outline btn-sm">View</a>
          </div>`;
        grid.appendChild(card);
      });
    }

    await Promise.all([loadUserRecipes(), loadSavedRecipes()]);
  });
})(); 