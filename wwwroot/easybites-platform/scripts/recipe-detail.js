// recipe-detail.js - Script for the recipe detail page with step-by-step experience

(function() {
    document.addEventListener('DOMContentLoaded', async () => {
        // Get recipe ID from URL
        const urlParams = new URLSearchParams(window.location.search);
        const recipeId = urlParams.get('id');
        
        if (!recipeId) {
            window.location.href = 'recipes.html';
            return;
        }
        
        // Check if user is logged in
        let currentUser = null;
        try {
            const userResponse = await EasyBites.api('/api/auth/me');
            if (userResponse) {
                currentUser = userResponse;
                // Update navigation based on logged in state
                updateNavigation(true);
            } else {
                window.location.href = 'recipes.html';
                return;
            }
        } catch (err) {
            console.log('User not logged in');
            window.location.href = 'recipes.html';
            return;
        }
        
        // Load recipe details
        try {
            const recipe = await EasyBites.api(`/api/recipes/${recipeId}`);
            renderRecipeDetails(recipe);
            
            // Set up save button
            const saveButton = document.querySelector('.save-recipe-btn');
            if (saveButton) {
                // Check if recipe is saved
                try {
                    const isSavedResponse = await EasyBites.api(`/api/recipes/saved/${recipeId}`);
                    if (isSavedResponse.isSaved) {
                        saveButton.classList.add('saved');
                        saveButton.textContent = '❤️';
                        saveButton.title = 'Remove from favorites';
                    }
                } catch (err) {
                    console.error('Failed to check if recipe is saved:', err);
                }
                
                // Add click handler
                saveButton.addEventListener('click', async (e) => {
                    e.preventDefault();
                    
                    const isSaved = saveButton.classList.contains('saved');
                    
                    try {
                        if (isSaved) {
                            // Unsave recipe
                            await EasyBites.api(`/api/recipes/saved/${recipeId}`, { method: 'DELETE' });
                            saveButton.classList.remove('saved');
                            saveButton.textContent = '🤍';
                            saveButton.title = 'Save to favorites';
                            EasyBites.toast('Recipe removed from favorites');
                        } else {
                            // Save recipe
                            await EasyBites.api('/api/recipes/saved', {
                                method: 'POST',
                                body: JSON.stringify({ recipeId })
                            });
                            saveButton.classList.add('saved');
                            saveButton.textContent = '❤️';
                            saveButton.title = 'Remove from favorites';
                            EasyBites.toast('Recipe saved to favorites');
                        }
                    } catch (err) {
                        console.error('Failed to save/unsave recipe:', err);
                        EasyBites.toast('Failed to update favorites');
                    }
                });
            }
            
            // Initialize step-by-step experience
            initializeStepByStep();
        } catch (err) {
            console.error('Failed to load recipe:', err);
            EasyBites.toast('Failed to load recipe details');
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
    
    // Render recipe details
    function renderRecipeDetails(recipe) {
        // Update page title
        document.title = `${recipe.name} - EasyBites`;
        
        // Update breadcrumb
        const breadcrumb = document.querySelector('.breadcrumb span');
        if (breadcrumb) {
            breadcrumb.textContent = recipe.name;
        }
        
        // Update recipe image
        const recipeImage = document.querySelector('.recipe-image img');
        if (recipeImage) {
            recipeImage.src = recipe.imageUrl || 'https://via.placeholder.com/800x600?text=Recipe+Image';
            recipeImage.alt = recipe.name;
        }
        
        // Update recipe header info
        const recipeTitle = document.querySelector('.recipe-info h1');
        if (recipeTitle) {
            recipeTitle.textContent = recipe.name;
        }
        
        const recipeDescription = document.querySelector('.recipe-description');
        if (recipeDescription) {
            recipeDescription.textContent = recipe.description || '';
        }
        
        // Update meta information
        const metaItems = document.querySelectorAll('.meta-item');
        if (metaItems.length >= 4) {
            // Prep Time
            const prepTimeValue = metaItems[0].querySelector('.meta-value');
            if (prepTimeValue) {
                prepTimeValue.textContent = `${recipe.prepTime} minutes`;
            }
            
            // Cook Time
            const cookTimeValue = metaItems[1].querySelector('.meta-value');
            if (cookTimeValue) {
                cookTimeValue.textContent = `${recipe.cookTime} minutes`;
            }
            
            // Servings
            const servingsValue = metaItems[2].querySelector('.meta-value');
            if (servingsValue) {
                servingsValue.textContent = `${recipe.servings} people`;
            }
            
            // Difficulty
            const difficultyValue = metaItems[3].querySelector('.meta-value');
            if (difficultyValue) {
                const difficultyClass = recipe.difficulty ? recipe.difficulty.toLowerCase() : 'medium';
                const difficultyIcon = difficultyClass === 'easy' ? '🟢' : difficultyClass === 'medium' ? '🟡' : '🔴';
                
                difficultyValue.className = `meta-value difficulty-${difficultyClass}`;
                difficultyValue.textContent = `${difficultyIcon} ${recipe.difficulty}`;
            }
        }
        
        // Update author info
        const authorName = document.querySelector('.author-info h3');
        if (authorName) {
            authorName.textContent = recipe.author || 'Anonymous';
        }
        
        const authorAvatar = document.querySelector('.author-avatar');
        if (authorAvatar && recipe.author) {
            // Set first letter of author name as avatar
            authorAvatar.textContent = recipe.author.charAt(0).toUpperCase();
        }
        
        // Render ingredients
        const ingredientsList = document.querySelector('.ingredients-list');
        if (ingredientsList && Array.isArray(recipe.ingredients)) {
            ingredientsList.innerHTML = recipe.ingredients.map((ingredient, index) => `
                <li>
                    <input type="checkbox" id="ingredient${index}" class="ingredient-checkbox">
                    <label for="ingredient${index}">${ingredient}</label>
                </li>
            `).join('');
        }
        
        // Render instructions
        const instructionsList = document.querySelector('.instructions-list');
        if (instructionsList && Array.isArray(recipe.instructions)) {
            instructionsList.innerHTML = recipe.instructions.map((instruction, index) => `
                <li class="instruction-step" data-step="${index + 1}">
                    <div class="step-number">${index + 1}</div>
                    <div class="step-content">
                        <p>${instruction}</p>
                    </div>
                </li>
            `).join('');
        }
        
        // Render tips if available
        const tipsSection = document.querySelector('.tips-section');
        if (tipsSection) {
            if (recipe.tips) {
                const tipsGrid = tipsSection.querySelector('.tips-grid');
                if (tipsGrid) {
                    tipsGrid.innerHTML = `
                        <div class="tip-card">
                            <h4>💡 Chef's Tip</h4>
                            <p>${recipe.tips}</p>
                        </div>
                    `;
                }
            } else {
                tipsSection.style.display = 'none';
            }
        }
    }
    
    // Initialize step-by-step experience
    function initializeStepByStep() {
        const instructionSteps = document.querySelectorAll('.instruction-step');
        if (instructionSteps.length === 0) return;
        
        // Initially hide all steps except the first one
        instructionSteps.forEach((step, index) => {
            if (index > 0) {
                step.classList.add('locked');
            } else {
                step.classList.add('active');
            }
        });
        
        // Add progress bar
        const instructionsSection = document.querySelector('.instructions-section');
        if (instructionsSection) {
            // Create progress bar
            const progressBar = document.createElement('div');
            progressBar.className = 'recipe-progress';
            progressBar.innerHTML = `
                <div class="progress-bar">
                    <div class="progress-fill" style="width: ${100 / instructionSteps.length}%"></div>
                </div>
                <div class="progress-text">Step 1 of ${instructionSteps.length}</div>
            `;
            
            // Create navigation buttons
            const navButtons = document.createElement('div');
            navButtons.className = 'step-navigation';
            navButtons.innerHTML = `
                <button class="btn btn-outline step-prev" disabled>← Previous</button>
                <button class="btn btn-primary step-next">Next →</button>
            `;
            
            // Add elements to DOM
            instructionsSection.insertBefore(progressBar, instructionsSection.querySelector('.instructions-list'));
            instructionsSection.appendChild(navButtons);
            
            // Set up navigation functionality
            let currentStep = 0;
            const prevButton = navButtons.querySelector('.step-prev');
            const nextButton = navButtons.querySelector('.step-next');
            
            // Next button handler
            nextButton.addEventListener('click', () => {
                if (currentStep < instructionSteps.length - 1) {
                    // Mark current step as completed
                    instructionSteps[currentStep].classList.remove('active');
                    instructionSteps[currentStep].classList.add('completed');
                    
                    // Move to next step
                    currentStep++;
                    instructionSteps[currentStep].classList.remove('locked');
                    instructionSteps[currentStep].classList.add('active');
                    
                    // Update progress bar
                    const progressFill = progressBar.querySelector('.progress-fill');
                    if (progressFill) {
                        progressFill.style.width = `${((currentStep + 1) / instructionSteps.length) * 100}%`;
                    }
                    
                    // Update progress text
                    const progressText = progressBar.querySelector('.progress-text');
                    if (progressText) {
                        progressText.textContent = `Step ${currentStep + 1} of ${instructionSteps.length}`;
                    }
                    
                    // Update button states
                    prevButton.disabled = false;
                    if (currentStep === instructionSteps.length - 1) {
                        nextButton.textContent = 'Finish';
                    }
                    
                    // Scroll to active step
                    instructionSteps[currentStep].scrollIntoView({ behavior: 'smooth', block: 'center' });
                } else {
                    // Mark last step as completed
                    instructionSteps[currentStep].classList.remove('active');
                    instructionSteps[currentStep].classList.add('completed');
                    
                    // Show completion message
                    const completionMessage = document.createElement('div');
                    completionMessage.className = 'completion-message';
                    completionMessage.innerHTML = `
                        <div class="completion-content">
                            <h2>🎉 Recipe Completed!</h2>
                            <p>Congratulations! You've successfully completed this recipe.</p>
                            <div class="completion-actions">
                                <a href="recipes.html" class="btn btn-primary">Find More Recipes</a>
                            </div>
                        </div>
                    `;
                    instructionsSection.appendChild(completionMessage);
                    
                    // Hide navigation buttons
                    navButtons.style.display = 'none';
                }
            });
            
            // Previous button handler
            prevButton.addEventListener('click', () => {
                if (currentStep > 0) {
                    // Restore current step to locked
                    instructionSteps[currentStep].classList.remove('active');
                    instructionSteps[currentStep].classList.add('locked');
                    
                    // Move to previous step
                    currentStep--;
                    instructionSteps[currentStep].classList.remove('completed');
                    instructionSteps[currentStep].classList.add('active');
                    
                    // Update progress bar
                    const progressFill = progressBar.querySelector('.progress-fill');
                    if (progressFill) {
                        progressFill.style.width = `${((currentStep + 1) / instructionSteps.length) * 100}%`;
                    }
                    
                    // Update progress text
                    const progressText = progressBar.querySelector('.progress-text');
                    if (progressText) {
                        progressText.textContent = `Step ${currentStep + 1} of ${instructionSteps.length}`;
                    }
                    
                    // Update button states
                    nextButton.textContent = 'Next →';
                    if (currentStep === 0) {
                        prevButton.disabled = true;
                    }
                    
                    // Scroll to active step
                    instructionSteps[currentStep].scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            });
        }
    }
})();
