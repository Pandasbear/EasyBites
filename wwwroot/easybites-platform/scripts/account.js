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

    // Update page title with user's name
    const accountTitle = document.getElementById('accountTitle');
    if (accountTitle && user) {
      const userName = user.firstName ? `${user.firstName}${user.lastName ? ' ' + user.lastName : ''}` : user.username || 'User';
      accountTitle.textContent = `Welcome ${userName}`;
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

      // Modal triggers
      const btnChangePassword = document.getElementById('btnChangePassword');
      const btnDeleteAccount = document.getElementById('btnDeleteAccount');
      const passwordModal = document.getElementById('passwordModal');
      const deleteModal = document.getElementById('deleteModal');

      function openModal(modal) {
        modal?.classList.remove('hidden');
      }
      function closeModal(modal) {
        modal?.classList.add('hidden');
      }

      btnChangePassword?.addEventListener('click', () => openModal(passwordModal));
      btnDeleteAccount?.addEventListener('click', () => openModal(deleteModal));

      // Close buttons
      document.querySelectorAll('.close-modal').forEach(btn => {
        btn.addEventListener('click', (e) => {
          const id = btn.getAttribute('data-close');
          const m = document.getElementById(id);
          closeModal(m);
        });
      });

      // Close modal when clicking outside content
      document.querySelectorAll('.modal').forEach(modalEl => {
        modalEl.addEventListener('click', (e) => {
          if (e.target === modalEl) {
            closeModal(modalEl);
          }
        });
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
          // Close modal on success
          const passwordModal = document.getElementById('passwordModal');
          passwordModal && passwordModal.classList.add('hidden');
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
          EasyBites.toast('Account deleted'); 
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
        console.log('Fetching my recipes for user:', user.id);
        const recipes = await EasyBites.api(`/api/recipes/user/${user.id}`);
        console.log('My recipes response:', recipes);
        renderRecipes(recipes, grid);
      } catch (err) {
        console.error('Error loading user recipes:', err);
        
        // Check if it's a database schema issue
        if (err.message && err.message.includes('column recipes.user_id does not exist')) {
          grid.innerHTML = `
            <div class="error-message">
              <p>Database setup required. Please add the missing user_id column to the recipes table.</p>
              <p>Run the SQL script in <code>fix_database_schema.sql</code> in your database.</p>
            </div>`;
        } else {
          grid.innerHTML = `<p class="error-message">Failed to load your recipes: ${err.message}</p>`;
        }
      }
    }

    // Load saved recipes
    async function loadSavedRecipes() {
      const grid = document.getElementById('savedRecipesGrid');
      if (!grid) return;
      try {
        console.log('Fetching saved recipes for authenticated user');
        const recipes = await EasyBites.api('/api/recipes/saved');
        console.log('Saved recipes response:', recipes);
        renderRecipes(recipes, grid);
      } catch (err) {
        console.error('Error loading saved recipes:', err);
        grid.innerHTML = `<p class="error-message">Failed to load saved recipes: ${err.message}</p>`;
      }
    }

    // Re-use simple card renderer from recipes.js style
    function renderRecipes(recipes, grid) {
      grid.innerHTML = '';
      if (!recipes || recipes.length === 0) {
        // Determine context based on grid id and show proper banner
        let bannerHtml = '';
        if (grid.id === 'myRecipesGrid') {
          bannerHtml = `
            <div class="empty-state-banner">
              <div class="banner-icon">👨‍🍳</div>
              <h3>No Recipes Yet</h3>
              <p>You haven't shared any recipes with the community yet. Start cooking up something amazing!</p>
              <a href="submit-recipe.html" class="btn btn-primary">Share Your First Recipe</a>
            </div>`;
        } else if (grid.id === 'savedRecipesGrid') {
          bannerHtml = `
            <div class="empty-state-banner">
              <div class="banner-icon">🔖</div>
              <h3>No Saved Recipes</h3>
              <p>You haven't saved any recipes yet. Discover delicious recipes and save your favorites!</p>
              <a href="recipes.html" class="btn btn-primary">Browse Recipes</a>
            </div>`;
        }
        grid.innerHTML = bannerHtml;
        return;
      }
      recipes.forEach(r => {
        const card = document.createElement('div');
        card.className = 'recipe-card';
        
        const isOwnRecipe = grid.id === 'myRecipesGrid';
        
        let actionButtons = '';
        if (isOwnRecipe) {
          // Always include Edit button for own recipes
          actionButtons += `<button class="btn btn-sm btn-primary edit-recipe-btn" data-recipe-id="${r.id}">Edit</button>`;

          // If it's a draft, include Publish button
          if (r.isDraft) {
            actionButtons += `<button class="btn btn-sm btn-success publish-recipe-btn" data-recipe-id="${r.id}">Publish</button>`;
          }

          // Only show the Generate Image button if the recipe does NOT already have an image
          if (!r.imageUrl) {
            actionButtons += `<button class="btn btn-sm btn-primary generate-image-btn" data-recipe-id="${r.id}" title="Generate AI Image">🎨 Generate Image</button>`;
          }

          // For published recipes (or pending review), allow viewing
          if (!r.isDraft) {
            actionButtons += `<a href="recipe-detail.html?id=${r.id}" class="btn btn-outline btn-sm">View</a>`;
          }
          
          // Add delete button for all recipes
          actionButtons += `<button class="btn btn-sm btn-danger delete-recipe-btn" data-recipe-id="${r.id}">Delete</button>`;
        } else if (grid.id === 'savedRecipesGrid') {
          // Saved recipes: provide a View button to open the recipe details page
          actionButtons += `<a href="recipe-detail.html?id=${r.id}" class="btn btn-outline btn-sm">View</a>`;
        }

        card.innerHTML = `
          <div class="recipe-image">
            <img src="${r.imageUrl || 'https://via.placeholder.com/300x200?text=Recipe'}" alt="${r.name}">
          </div>
          <div class="recipe-info">
            <h3>${r.name}</h3>
            <p class="recipe-description">${r.description || ''}</p>
            <div class="recipe-status-draft">
              ${r.isDraft ? '<span class="status-badge status-pending">Draft</span>' : ''}
            </div>
            <div class="recipe-actions-bottom">
              ${actionButtons}
            </div>
          </div>`;
        grid.appendChild(card);

        // Add event listeners for new buttons
        if (isOwnRecipe) {
          const editBtn = card.querySelector('.edit-recipe-btn');
          const publishBtn = card.querySelector('.publish-recipe-btn');
          const generateBtn = card.querySelector('.generate-image-btn');
          const deleteBtn = card.querySelector('.delete-recipe-btn');
          
          if (editBtn) {
            editBtn.addEventListener('click', () => editRecipe(r.id));
          }
          if (publishBtn) {
            publishBtn.addEventListener('click', () => publishRecipe(r.id, card));
          }
          if (generateBtn) {
            generateBtn.addEventListener('click', () => generateRecipeImage(r.id, card));
          }
          if (deleteBtn) {
            deleteBtn.addEventListener('click', () => deleteRecipe(r.id, card));
          }
        }
      });
    }

    // Function to handle editing a recipe
    function editRecipe(recipeId) {
      window.location.href = `submit-recipe.html?id=${recipeId}`;
    }

    // Function to handle publishing a recipe (from draft)
    async function publishRecipe(recipeId, cardElement) {
      if (!confirm('Are you sure you want to publish this recipe? It will be sent for admin review.')) {
        return;
      }
      const button = cardElement.querySelector('.publish-recipe-btn');
      const originalText = button.textContent;

      try {
        button.disabled = true;
        button.textContent = 'Publishing...';

        await EasyBites.api(`/api/recipes/${recipeId}/publish`, {
          method: 'PUT'
        });

        EasyBites.toast('Recipe published successfully and sent for review!', 'success');
        loadUserRecipes(); 
      } catch (err) {
        console.error('Failed to publish recipe:', err);
        EasyBites.toast(`Failed to publish recipe: ${err.message}`, 'error');
      } finally {
        button.disabled = false;
        button.textContent = originalText;
      }
    }

    // Generate image for recipe
    async function generateRecipeImage(recipeId, cardElement) {
      const button = cardElement.querySelector('.generate-image-btn');
      const originalText = button.textContent;
      
      try {
        button.disabled = true;
        button.textContent = '⏳ Generating Image...';
        
        const result = await EasyBites.api(`/api/recipes/${recipeId}/generate-image`, {
          method: 'POST'
        });
        
        if (result.success) {
          // Update the image in the card
          const img = cardElement.querySelector('img');
          img.src = result.imageUrl;
          
          // Update button to regenerate
          button.className = 'btn btn-sm btn-secondary regenerate-image-btn';
          button.textContent = '🎨 Regenerate Image';
          button.title = 'Regenerate AI Image';
          button.onclick = () => regenerateRecipeImage(recipeId, cardElement);
          
          EasyBites.toast('AI image generated successfully! 🎨');
        } else {
          throw new Error(result.details || 'Failed to generate image');
        }
      } catch (err) {
        console.error('Failed to generate image:', err);
        EasyBites.toast(`Failed to generate image: ${err.message}`);
        button.textContent = originalText;
      } finally {
        button.disabled = false;
        button.textContent = originalText;
      }
    }

    // Regenerate image for recipe
    async function regenerateRecipeImage(recipeId, cardElement) {
      if (!confirm('Are you sure you want to regenerate the AI image? This will replace the current image.')) {
        return;
      }
      
      const button = cardElement.querySelector('.regenerate-image-btn');
      const originalText = button.textContent;
      
      try {
        button.disabled = true;
        button.textContent = '⏳ Regenerating Image...';
        
        const result = await EasyBites.api(`/api/recipes/${recipeId}/regenerate-image`, {
          method: 'POST'
        });
        
        if (result.success) {
          // Update the image in the card
          const img = cardElement.querySelector('img');
          img.src = result.imageUrl + '?v=' + Date.now(); // Cache bust
          
          EasyBites.toast('AI image regenerated successfully! 🎨');
        } else {
          throw new Error(result.details || 'Failed to regenerate image');
        }
      } catch (err) {
        console.error('Failed to regenerate image:', err);
        EasyBites.toast(`Failed to regenerate image: ${err.message}`);
      } finally {
        button.disabled = false;
        button.textContent = originalText;
      }
    }

    // Function to handle deleting a recipe
    async function deleteRecipe(recipeId, cardElement) {
      if (!confirm('Are you sure you want to delete this recipe? This action cannot be undone.')) {
        return;
      }
      
      try {
        const response = await EasyBites.api(`/api/recipes/${recipeId}`, {
          method: 'DELETE'
        });
        
        // Remove card from DOM
        cardElement.remove();
        
        // Show success message
        EasyBites.toast('Recipe deleted successfully', 'success');
        
        // If this was the last recipe, show empty state banner
        const grid = document.getElementById('myRecipesGrid');
        if (grid && !grid.hasChildNodes()) {
          grid.innerHTML = `
            <div class="empty-state-banner">
              <div class="banner-icon">👨‍🍳</div>
              <h3>No Recipes Yet</h3>
              <p>You haven't shared any recipes with the community yet. Start cooking up something amazing!</p>
              <a href="submit-recipe.html" class="btn btn-primary">Share Your First Recipe</a>
            </div>`;
        }
      } catch (err) {
        console.error('Failed to delete recipe:', err);
        EasyBites.toast(`Failed to delete recipe: ${err.message}`, 'error');
      }
    }

    // Setup navigation
    setupAccountNavigation();
    
    await Promise.all([loadUserRecipes(), loadSavedRecipes(), loadUserReports(), loadUserFeedback()]);
  });

  // Setup account navigation
  function setupAccountNavigation() {
    const navButtons = document.querySelectorAll('.nav-btn');
    const sections = document.querySelectorAll('.account-section');
    
    navButtons.forEach(btn => {
      btn.addEventListener('click', () => {
        const targetSection = btn.getAttribute('data-section');
        
        // Remove active class from all buttons
        navButtons.forEach(b => b.classList.remove('active'));
        // Add active class to clicked button
        btn.classList.add('active');
        
        // Hide all sections
        sections.forEach(section => {
          section.style.display = 'none';
        });
        
        // Show target section
        const targetElement = document.getElementById(targetSection + 'Section');
        if (targetElement) {
          targetElement.style.display = 'block';
        }
      });
    });
    
    // Show profile section by default
    sections.forEach((section, index) => {
      section.style.display = index === 0 ? 'block' : 'none';
    });
  }

  // Load user reports
  async function loadUserReports() {
    const container = document.getElementById('myReportsList');
    if (!container) return;
    
    try {
      const reports = await EasyBites.api('/api/reports/my-reports');
      renderReports(reports, container);
    } catch (err) {
      console.error('Error loading user reports:', err);
      container.innerHTML = `<p class="error-message">Failed to load reports: ${err.message}</p>`;
    }
  }

  // Load user feedback
  async function loadUserFeedback() {
    const container = document.getElementById('myFeedbackList');
    if (!container) return;
    
    try {
      const feedback = await EasyBites.api('/api/feedback/user');
      renderFeedback(feedback, container);
    } catch (err) {
      console.error('Error loading user feedback:', err);
      container.innerHTML = `<p class="error-message">Failed to load feedback: ${err.message}</p>`;
    }
  }

  // Render reports
  function renderReports(reports, container) {
    container.innerHTML = '';
    
    if (!reports || reports.length === 0) {
      container.innerHTML = `
        <div class="empty-state text-center">
          <p>You haven't submitted any reports yet.</p>
          <p>Reports help keep our community safe and improve the platform.</p>
        </div>`;
      return;
    }
    
    reports.forEach(report => {
      const reportCard = document.createElement('div');
      reportCard.className = 'report-card';
      
      const statusClass = report.status === 'resolved' ? 'status-resolved' : 
                         report.status === 'pending' ? 'status-pending' : 'status-reviewing';
      
      const adminResponse = report.adminNotes ? 
        `<div class="admin-response">
          <h5>Admin Response:</h5>
          <p>${escapeHtml(report.adminNotes)}</p>
          <small>Reviewed on ${window.EasyBites && window.EasyBites.formatDate ? window.EasyBites.formatDate(report.reviewedAt) : 'Date not available'}</small>
        </div>` : '';
      
      reportCard.innerHTML = `
        <div class="report-header">
          <span class="report-type">${escapeHtml(report.reportType.replace('_', ' '))}</span>
          <span class="status-badge ${statusClass}">${report.status}</span>
        </div>
        <div class="report-content">
          <p><strong>Description:</strong> ${escapeHtml(report.description)}</p>
          <small>Submitted on ${window.EasyBites && window.EasyBites.formatDate ? window.EasyBites.formatDate(report.createdAt) : 'Date not available'}</small>
        </div>
        ${adminResponse}
      `;
      
      container.appendChild(reportCard);
    });
  }

  // Render feedback
  function renderFeedback(feedbackList, container) {
    container.innerHTML = '';
    
    if (!feedbackList || feedbackList.length === 0) {
      container.innerHTML = `
        <div class="empty-state text-center">
          <p>You haven't submitted any feedback yet.</p>
          <p>Your feedback helps us improve EasyBites!</p>
        </div>`;
      return;
    }
    
    feedbackList.forEach(feedback => {
      const feedbackCard = document.createElement('div');
      feedbackCard.className = 'feedback-card';
      
      const ratingDisplay = feedback.rating ? 
        `<div class="rating-display">
          <span class="rating-stars">${'★'.repeat(feedback.rating)}${'☆'.repeat(5 - feedback.rating)}</span>
          <span class="rating-text">(${feedback.rating}/5)</span>
        </div>` : '';
      
      const statusDisplay = feedback.status ? 
        `<span class="feedback-status status-${feedback.status.toLowerCase()}">${escapeHtml(feedback.status.charAt(0).toUpperCase() + feedback.status.slice(1))}</span>` : '';
      
      const adminResponse = feedback.adminResponse ? 
        `<div class="admin-response">
          <h5>Admin Response:</h5>
          <p>${escapeHtml(feedback.adminResponse)}</p>
          <small>Responded on ${window.EasyBites && window.EasyBites.formatDate ? window.EasyBites.formatDate(feedback.reviewedAt) : 'Date not available'}</small>
        </div>` : '';
      
      feedbackCard.innerHTML = `
        <div class="feedback-header">
          <span class="feedback-type">${escapeHtml(feedback.type.replace('_', ' '))}</span>
          ${statusDisplay}
          ${ratingDisplay}
        </div>
        <div class="feedback-content">
          <h4>${escapeHtml(feedback.subject)}</h4>
          <p>${escapeHtml(feedback.message)}</p>
          <small>Submitted on ${window.EasyBites && window.EasyBites.formatDate ? window.EasyBites.formatDate(feedback.submittedAt) : 'Date not available'}</small>
        </div>
        ${adminResponse}
      `;
      
      container.appendChild(feedbackCard);
    });
  }

  // Helper function for HTML escaping
  function escapeHtml(text) {
    if (!text) return '';
    const map = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
  }
})();