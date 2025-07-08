// recipe-detail.js - Handles the detailed view of a single recipe
let currentRecipe = null;
let currentUser = null;
let recipeProgress = null; // To store user's progress for this recipe

document.addEventListener('DOMContentLoaded', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const recipeId = urlParams.get('id');
    
    if (!recipeId) {
        document.querySelector('main').innerHTML = '<p class="error-message">Recipe ID not provided.</p>';
        return;
    }

    try {
        // Fetch user and recipe in parallel
        const [userResponse, recipeResponse] = await Promise.all([
            EasyBites.api('/api/auth/me').catch(err => {
                console.warn('User not logged in or session expired:', err);
                return null; // Allow recipe loading even if user is not logged in
            }),
            EasyBites.api(`/api/recipes/${recipeId}`)
        ]);

        currentUser = userResponse;
        currentRecipe = recipeResponse;
        
        if (!currentRecipe) {
            document.querySelector('main').innerHTML = '<p class="error-message">Recipe not found.</p>';
            return;
        }

        // If user is logged in, fetch their progress for this recipe
        if (currentUser) {
            try {
                recipeProgress = await EasyBites.api(`/api/recipes/progress/${recipeId}`);
                console.log('Fetched recipe progress:', recipeProgress);
            } catch (err) {
                console.warn('No existing progress for this recipe, or error fetching:', err);
                recipeProgress = { // Initialize with default values if no progress found
                    currentInstructionStep: 0,
                    checkedIngredients: []
                };
            }
        } else {
            // If not logged in, treat progress as non-existent
            recipeProgress = { currentInstructionStep: 0, checkedIngredients: [] };
        }

        renderRecipeDetails(currentRecipe);
        updateNavigation(!!currentUser);
        initializeStepByStep();
        setupIngredientCheckboxes();
        setupProgressNavigation();

    } catch (err) {
        console.error('Failed to load recipe or user data:', err);
        document.querySelector('main').innerHTML = `<p class="error-message">Failed to load recipe: ${err.message || 'Unknown error'}</p>`;
    }
});

// Update navigation based on login state
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
        window.location.href = 'recipes.html';
    } catch (err) {
        console.error('Logout failed:', err);
        EasyBites.toast('Logout failed');
    }
}

function renderRecipeDetails(recipe) {
    document.title = `${recipe.name} - EasyBites`;

    // Breadcrumb
    const breadcrumbSpan = document.querySelector('.breadcrumb span');
    if (breadcrumbSpan) {
        breadcrumbSpan.textContent = recipe.name;
    }

    // Recipe Image
    const recipeImageElement = document.querySelector('.recipe-image img');
    if (recipeImageElement) {
        recipeImageElement.src = recipe.imageUrl || '/placeholder.svg?height=800&width=600&text=Recipe+Image';
        recipeImageElement.alt = recipe.name;
    }

    // Save Recipe Button
    const saveRecipeBtn = document.querySelector('.save-recipe-btn');
    if (saveRecipeBtn && currentUser) {
        saveRecipeBtn.setAttribute('data-id', recipe.id);
        // Check if recipe is already saved by the user
        EasyBites.api(`/api/recipes/saved/${recipe.id}`)
        .then(status => {
            if (status && status.isSaved) {
                saveRecipeBtn.classList.add('saved');
                saveRecipeBtn.textContent = '❤️';
                saveRecipeBtn.title = 'Remove from favorites';
            } else {
                saveRecipeBtn.classList.remove('saved');
                saveRecipeBtn.textContent = '🤍';
                saveRecipeBtn.title = 'Save to favorites';
            }
        }).catch(err => {
            // Unexpected error
            console.error('Failed to get save status:', err);
            // Fallback to unsaved state in case of unexpected error
            saveRecipeBtn.classList.remove('saved');
            saveRecipeBtn.textContent = '🤍';
            saveRecipeBtn.title = 'Save to favorites';
        });
        saveRecipeBtn.addEventListener('click', handleSaveRecipe);
    } else if (saveRecipeBtn) {
        // Hide save button if user is not logged in
        saveRecipeBtn.style.display = 'none';
    }

    // Recipe Info
    document.querySelector('.recipe-info h1').textContent = recipe.name;
    document.querySelector('.recipe-description').textContent = recipe.description || 'No description provided.';

    // Meta Grid
    document.querySelector('.meta-item:nth-child(1) .meta-value').textContent = `${recipe.prepTime || '--'} minutes`;
    document.querySelector('.meta-item:nth-child(2) .meta-value').textContent = `${recipe.cookTime || '--'} minutes`;
    document.querySelector('.meta-item:nth-child(3) .meta-value').textContent = `${recipe.servings || '--'} people`;
    const difficultyElement = document.querySelector('.meta-item:nth-child(4) .meta-value');
    difficultyElement.textContent = recipe.difficulty || 'Medium';
    difficultyElement.className = `meta-value difficulty-${(recipe.difficulty || 'medium').toLowerCase()}`;

    // Author Info
    document.querySelector('.author-info h3').textContent = recipe.author || 'Unknown Chef';
    const authorAvatar = document.querySelector('.author-avatar');
    if (authorAvatar) {
        authorAvatar.textContent = (recipe.author ? recipe.author[0] : 'U').toUpperCase();
    }

    // Ingredients List
    const ingredientsList = document.querySelector('.ingredients-list');
    ingredientsList.innerHTML = ''; // Clear existing placeholders
    if (recipe.ingredients && recipe.ingredients.length > 0) {
        recipe.ingredients.forEach((ingredient, index) => {
            const li = document.createElement('li');
            const isChecked = recipeProgress.checkedIngredients.includes(index);
            li.innerHTML = `
                <input type="checkbox" id="ingredient${index}" class="ingredient-checkbox" data-index="${index}" ${isChecked ? 'checked' : ''}>
                <label for="ingredient${index}">${escapeHtml(ingredient)}</label>
            `;
            ingredientsList.appendChild(li);
        });
    } else {
        ingredientsList.innerHTML = '<p>No ingredients listed.</p>';
    }

    // Instructions List
    const instructionsList = document.getElementById('instructionsList');
    instructionsList.innerHTML = ''; // Clear existing placeholders
    if (recipe.instructions && recipe.instructions.length > 0) {
        recipe.instructions.forEach((instruction, index) => {
            const li = document.createElement('li');
            li.className = 'instruction-step';
            li.setAttribute('data-step', index + 1);
            li.innerHTML = `
                <div class="step-number">${index + 1}</div>
                <div class="step-content">
                    <p>${escapeHtml(instruction)}</p>
                </div>
            `;
            instructionsList.appendChild(li);
        });
    } else {
        instructionsList.innerHTML = '<p>No instructions provided.</p>';
    }
    
    // Tips Section
    const tipsGrid = document.querySelector('.tips-grid');
    tipsGrid.innerHTML = ''; // Clear existing placeholders
    if (recipe.tips) {
        const tipCard = document.createElement('div');
        tipCard.className = 'tip-card';
        tipCard.innerHTML = `
            <h4>💡 Chef's Tip</h4>
            <p>${escapeHtml(recipe.tips)}</p>
        `;
        tipsGrid.appendChild(tipCard);
    } else {
        tipsGrid.innerHTML = '<p>No tips available.</p>';
    }

    // Initial render of instruction progress
    updateInstructionProgressUI();
}

// Handle saving/unsaving a recipe
async function handleSaveRecipe(e) {
    e.preventDefault();
    e.stopPropagation();

    if (!currentUser) {
        console.warn('Attempted to save/unsave recipe without authentication. Showing login prompt.');
        showLoginPrompt(e); // Defined in main.js, or local if needed
        return;
    }

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
            EasyBites.toast('Recipe removed from favorites');
        } else {
            // Save recipe
            await EasyBites.api('/api/recipes/saved', {
                method: 'POST',
                body: JSON.stringify({ recipeId: recipeId }),
                expectedStatusCodes: [409] // Expect 409 if recipe is already saved
            });
            button.classList.add('saved');
            button.textContent = '❤️';
            button.title = 'Remove from favorites';
            EasyBites.toast('Recipe saved to favorites');
        }
    } catch (err) {
        console.error('Failed to save/unsave recipe:', err);
        // Check if the error is due to the recipe already being saved (409 Conflict)
        if (err.status === 409) {
            EasyBites.toast('Recipe is already in your favorites.');
            button.classList.add('saved'); // Ensure button shows as saved
            button.textContent = '❤️';
            button.title = 'Remove from favorites';
        } else {
            EasyBites.toast('Failed to update favorites: ' + (err.message || ''));
        }
    }
}

// Show login prompt modal (can be reused from recipes.js or defined here)
function showLoginPrompt(e) {
    // Re-use logic from main.js or recipes.js if available
    // For now, a simple toast
    EasyBites.toast('Please log in to save recipes and track your progress!', 'info'); // Replaced alert with EasyBites.toast
    window.location.href = 'login.html';
}

// --- New Functions for Recipe Progress ---

// Initializes the step-by-step functionality
function initializeStepByStep() {
    const totalSteps = currentRecipe.instructions ? currentRecipe.instructions.length : 0;
    document.getElementById('totalStepsDisplay').textContent = totalSteps;
    updateInstructionProgressUI();
}

// Updates the UI based on current instruction step
function updateInstructionProgressUI() {
    const instructions = document.querySelectorAll('.instruction-step');
    const totalSteps = instructions.length;
    const currentStep = recipeProgress.currentInstructionStep; // Use the stored progress

    instructions.forEach((stepEl, index) => {
        const stepNumber = parseInt(stepEl.getAttribute('data-step'));
        if (stepNumber === currentStep) {
            stepEl.classList.add('active');
            stepEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } else {
            stepEl.classList.remove('active');
        }

        if (stepNumber < currentStep) {
            stepEl.classList.add('completed');
        } else {
            stepEl.classList.remove('completed');
        }
    });

    // Update progress bar
    const progressBarFill = document.querySelector('.progress-fill');
    const currentStepDisplay = document.getElementById('currentStepDisplay');
    const progressPercentage = totalSteps > 0 ? (currentStep / totalSteps) * 100 : 0;
    progressBarFill.style.width = `${progressPercentage}%`;
    currentStepDisplay.textContent = `Step ${currentStep}`;

    // Update navigation buttons
    document.getElementById('prevStepBtn').disabled = currentStep <= 1;
    document.getElementById('nextStepBtn').disabled = currentStep >= totalSteps;
}

// Sets up event listeners for instruction navigation buttons
function setupProgressNavigation() {
    document.getElementById('prevStepBtn').addEventListener('click', () => navigateInstruction(-1));
    document.getElementById('nextStepBtn').addEventListener('click', () => navigateInstruction(1));
}

// Navigates instructions and updates progress
async function navigateInstruction(direction) {
    if (!currentUser) {
        showLoginPrompt();
        return; // Prevent navigating if not logged in
    }

    const totalSteps = currentRecipe.instructions ? currentRecipe.instructions.length : 0;
    let newStep = recipeProgress.currentInstructionStep + direction;

    // Ensure newStep is within bounds
    newStep = Math.max(0, Math.min(newStep, totalSteps));

    if (newStep === recipeProgress.currentInstructionStep) {
        return; // No change needed
    }

    recipeProgress.currentInstructionStep = newStep;
    updateInstructionProgressUI();
    await saveRecipeProgress();
}

// Sets up event listeners for ingredient checkboxes
function setupIngredientCheckboxes() {
    const checkboxes = document.querySelectorAll('.ingredient-checkbox');
    checkboxes.forEach(checkbox => {
        checkbox.addEventListener('change', handleIngredientCheck);
    });
}

// Handles ingredient checkbox change
async function handleIngredientCheck(e) {
    if (!currentUser) {
        showLoginPrompt();
        e.target.checked = !e.target.checked; // Revert checkbox state
        return; // Prevent update if not logged in
    }

    const index = parseInt(e.target.getAttribute('data-index'));
    if (e.target.checked) {
        if (!recipeProgress.checkedIngredients.includes(index)) {
            recipeProgress.checkedIngredients.push(index);
        }
    } else {
        const i = recipeProgress.checkedIngredients.indexOf(index);
        if (i > -1) {
            recipeProgress.checkedIngredients.splice(i, 1);
        }
    }
    await saveRecipeProgress();
}

// Saves the current recipe progress to the backend
async function saveRecipeProgress() {
    if (!currentUser) {
        console.warn('Not logged in. Cannot save recipe progress.');
        return;
    }
    try {
        // POST if new, PUT if updating existing progress
        const method = recipeProgress.id ? 'PUT' : 'POST';
        const url = recipeProgress.id ? `/api/recipes/progress/${recipeProgress.id}` : '/api/recipes/progress';

        const body = {
            RecipeId: currentRecipe.id,
            CurrentInstructionStep: recipeProgress.currentInstructionStep,
            CheckedIngredients: recipeProgress.checkedIngredients
        };

        const result = await EasyBites.api(url, {
            method: method,
            body: JSON.stringify(body)
        });

        // Update recipeProgress ID if it was a new creation
        if (method === 'POST' && result && result.id) {
            recipeProgress.id = result.id;
        }
        console.log('Recipe progress saved:', result);
    } catch (err) {
        console.error('Failed to save recipe progress:', err);
        EasyBites.toast('Failed to save progress: ' + (err.message || ''));
    }
}

// Helper for HTML escaping
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"]'/g, function(m) { return map[m]; });
}
