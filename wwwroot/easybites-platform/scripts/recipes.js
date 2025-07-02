// recipes.js - Handler for the recipes browse page

(function() {
    // Initialize variables
    let currentUser = null;
    let savedRecipesIds = [];
    
  document.addEventListener('DOMContentLoaded', async () => {
        const recipesGrid = document.getElementById('recipesGrid');
        if (!recipesGrid) return;

        // Check if user is logged in
        try {
            const userResponse = await EasyBites.api('/api/auth/me');
            if (userResponse) {
                currentUser = userResponse;
                updateNavigation(true);
                
                // Get user's saved recipes
                try {
                    const savedRecipes = await EasyBites.api('/api/recipes/saved');
                    savedRecipesIds = savedRecipes.map(recipe => recipe.id);
                } catch (err) {
                    console.error('Failed to load saved recipes:', err);
                }
            } else {
                updateNavigation(false);
            }
        } catch (err) {
            console.log('User not logged in');
            updateNavigation(false);
        }
        
        // Load recipes
    try {
      const recipes = await EasyBites.api('/api/recipes');
            if (recipes.length > 0) {
                renderRecipes(recipes, recipesGrid);
            } else {
                showNoRecipesMessage(recipesGrid);
            }
        } catch (err) {
            console.error('Failed to load recipes:', err);
            showErrorMessage(recipesGrid);
        }
        
        // Set up search
        setupSearch();
        
        // Set up filters
        setupFilters();
    });
    
    // Function to update navigation based on login state
    function updateNavigation(isLoggedIn) {
        const navMenu = document.querySelector('.nav-menu');
        if (!navMenu) return;
        
        if (isLoggedIn) {
            // Replace Login/Register with Account and Logout
            const loginLink = navMenu.querySelector('a[href="login.html"]');
            const registerLink = navMenu.querySelector('a[href="register.html"]');
            
            if (loginLink) {
                loginLink.setAttribute('href', 'account.html');
                loginLink.textContent = 'My Account';
            }
            
            if (registerLink) {
                registerLink.setAttribute('href', '#');
                registerLink.textContent = 'Logout';
                registerLink.classList.remove('btn-primary');
                registerLink.addEventListener('click', handleLogout);
            }
        }
    }
    
    // Handle logout
    async function handleLogout(e) {
        e.preventDefault();
        try {
            await EasyBites.api('/api/auth/logout', { method: 'POST' });
            window.location.reload();
        } catch (err) {
            console.error('Logout failed:', err);
            EasyBites.toast('Logout failed');
        }
    }
    
    // Render recipes to grid
    function renderRecipes(recipes, grid) {
        grid.innerHTML = '';
        
        recipes.forEach(recipe => {
            const isSaved = savedRecipesIds.includes(recipe.id);
            
            // Create recipe card
            const card = document.createElement('div');
            card.className = 'recipe-card';
            
            // Create heart button if user is logged in
            let saveButton = '';
            if (currentUser) {
                const heartIcon = isSaved ? '❤️' : '🤍';
                const saveTitle = isSaved ? 'Remove from favorites' : 'Save to favorites';
                saveButton = `
                    <button class="save-recipe-btn ${isSaved ? 'saved' : ''}" 
                            data-id="${recipe.id}" 
                            title="${saveTitle}">${heartIcon}</button>
                `;
            }
            
            // Format recipe difficulty with color
            const difficultyClass = recipe.difficulty ? recipe.difficulty.toLowerCase() : 'medium';
            
            // Build card content
            card.innerHTML = `
                <div class="recipe-image">
                    <img src="${recipe.imageUrl || 'https://via.placeholder.com/300x200?text=Recipe'}" alt="${recipe.name}">
                </div>
                <div class="recipe-content">
                    <div class="recipe-header">
                        <h3>${recipe.name}</h3>
                        <div class="recipe-actions">
                            ${saveButton}
                        </div>
                    </div>
            <div class="recipe-meta">
                        <span class="difficulty ${difficultyClass}">${recipe.difficulty || 'Medium'}</span>
                        <span class="prep-time">${recipe.prepTime || '30'} min</span>
                    </div>
                    <p class="recipe-description">${recipe.description || ''}</p>
                    <a href="${currentUser ? `recipe-detail.html?id=${recipe.id}` : '#'}" 
                       class="${currentUser ? '' : 'login-required'}" 
                       data-id="${recipe.id}">View Recipe</a>
                </div>
            `;
            
            // Add card to grid
            grid.appendChild(card);
            
            // Add event handler for save button
            if (currentUser) {
                const saveBtn = card.querySelector('.save-recipe-btn');
                if (saveBtn) {
                    saveBtn.addEventListener('click', handleSaveRecipe);
                }
            } else {
                // Add login prompt for non-logged in users
                const recipeLink = card.querySelector('.login-required');
                if (recipeLink) {
                    recipeLink.addEventListener('click', showLoginPrompt);
                }
            }
        });
    }
    
    // Handle saving/unsaving a recipe
    async function handleSaveRecipe(e) {
        e.preventDefault();
        e.stopPropagation();
        
        const button = e.currentTarget;
        const recipeId = button.getAttribute('data-id');
        const isSaved = button.classList.contains('saved');
        
        try {
            if (isSaved) {
                // Unsave recipe
                await EasyBites.api(`/api/recipes/saved/${recipeId}`, { method: 'DELETE' });
                button.classList.remove('saved');
                button.textContent = '🤍';
                button.title = 'Save to favorites';
                
                // Remove from saved recipes array
                const index = savedRecipesIds.indexOf(recipeId);
                if (index > -1) savedRecipesIds.splice(index, 1);
                
                EasyBites.toast('Recipe removed from favorites');
            } else {
                // Save recipe
                await EasyBites.api('/api/recipes/saved', {
                    method: 'POST',
                    body: JSON.stringify({ recipeId })
                });
                button.classList.add('saved');
                button.textContent = '❤️';
                button.title = 'Remove from favorites';
                
                // Add to saved recipes array
                savedRecipesIds.push(recipeId);
                
                EasyBites.toast('Recipe saved to favorites');
            }
        } catch (err) {
            console.error('Failed to save/unsave recipe:', err);
            EasyBites.toast('Failed to update favorites');
        }
    }
    
    // Show login prompt modal
    function showLoginPrompt(e) {
        e.preventDefault();
        
        // Create modal
        const modal = document.createElement('div');
        modal.className = 'login-modal';
        
        // Create modal content
        modal.innerHTML = `
            <div class="login-modal-content">
                <button class="close-modal">&times;</button>
                <h2>Login Required</h2>
                <p>You need to be logged in to view the full recipe and save your favorites.</p>
                <div class="login-modal-actions">
                    <a href="login.html" class="btn btn-primary">Login</a>
                    <a href="register.html" class="btn btn-outline">Register</a>
                </div>
            </div>
        `;
        
        // Add to body
        document.body.appendChild(modal);
        
        // Add close handler
        const closeBtn = modal.querySelector('.close-modal');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                modal.remove();
            });
        }
        
        // Close when clicking outside
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });
    }
    
    // Setup search functionality
    function setupSearch() {
        const searchForm = document.querySelector('.search-form');
        if (!searchForm) return;
        
        searchForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const searchInput = searchForm.querySelector('input[type="search"]');
            if (!searchInput) return;
            
            const query = searchInput.value.trim();
            if (!query) return;
            
            try {
                const recipes = await EasyBites.api(`/api/recipes/search?q=${encodeURIComponent(query)}`);
                const recipesGrid = document.getElementById('recipesGrid');
                
                if (recipes.length > 0) {
                    renderRecipes(recipes, recipesGrid);
                } else {
                    showNoRecipesMessage(recipesGrid, `No recipes found for "${query}"`);
                }
    } catch (err) {
                console.error('Search failed:', err);
                EasyBites.toast('Search failed');
            }
        });
    }
    
    // Setup filter functionality
    function setupFilters() {
        const filterForm = document.querySelector('.filter-form');
        if (!filterForm) return;
        
        // Get filter elements
        const difficultyFilter = filterForm.querySelector('select[name="difficulty"]');
        const timeFilter = filterForm.querySelector('select[name="time"]');
        
        // Add change event listeners
        [difficultyFilter, timeFilter].forEach(filter => {
            if (filter) {
                filter.addEventListener('change', applyFilters);
            }
        });
    }
    
    // Apply filters to recipes
    async function applyFilters() {
        const filterForm = document.querySelector('.filter-form');
        if (!filterForm) return;
        
        const difficultyFilter = filterForm.querySelector('select[name="difficulty"]');
        const timeFilter = filterForm.querySelector('select[name="time"]');
        
        // Build query string
        const params = new URLSearchParams();
        
        if (difficultyFilter && difficultyFilter.value) {
            params.append('difficulty', difficultyFilter.value);
        }
        
        if (timeFilter && timeFilter.value) {
            params.append('time', timeFilter.value);
        }
        
        const queryString = params.toString();
        
        try {
            const endpoint = queryString ? `/api/recipes/filter?${queryString}` : '/api/recipes';
            const recipes = await EasyBites.api(endpoint);
            
            const recipesGrid = document.getElementById('recipesGrid');
            if (recipes.length > 0) {
                renderRecipes(recipes, recipesGrid);
            } else {
                showNoRecipesMessage(recipesGrid, 'No recipes match your filters');
            }
        } catch (err) {
            console.error('Failed to apply filters:', err);
            EasyBites.toast('Failed to filter recipes');
        }
    }
    
    // Show no recipes message
    function showNoRecipesMessage(container, message = 'No recipes found') {
        container.innerHTML = `
            <div class="no-recipes">
                <h3>${message}</h3>
                <p>Try different search terms or filters.</p>
            </div>
        `;
    }
    
    // Show error message
    function showErrorMessage(container) {
        container.innerHTML = `
            <div class="error-message">
                <h3>Error loading recipes</h3>
                <p>Please try again later.</p>
            </div>
        `;
    }
})(); 