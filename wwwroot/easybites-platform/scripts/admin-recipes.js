// Admin Recipe Management Functionality
let currentPage = 1;
let currentStatus = '';
let currentRecipeId = null;

document.addEventListener('DOMContentLoaded', function() {
    initializeRecipeManagement();
});

function initializeRecipeManagement() {
    console.log('[Admin Recipes] Initializing recipe management...');
    
    // Set up event listeners
    setupEventListeners();
    
    // Load initial recipes
    loadRecipes();
}

function setupEventListeners() {
    // Filter controls
    const statusFilter = document.getElementById('statusFilter');
    const refreshBtn = document.getElementById('refreshBtn');
    const searchBtn = document.getElementById('searchBtn');
    const searchInput = document.getElementById('searchInput');
    
    // Pagination controls
    const prevPageBtn = document.getElementById('prevPageBtn');
    const nextPageBtn = document.getElementById('nextPageBtn');
    
    // Modal controls
    const modal = document.getElementById('recipeModal');
    const closeBtn = modal.querySelector('.close');
    const modalCloseBtn = modal.querySelector('.modal-close');
    const approveBtn = document.getElementById('approveBtn');
    const rejectBtn = document.getElementById('rejectBtn');
    const deleteBtn = document.getElementById('deleteBtn');

    // New elements for edit functionality
    const recipeDetailForm = document.getElementById('recipeDetailForm');
    const editRecipeBtn = document.getElementById('editRecipeBtn');
    const saveRecipeBtn = document.getElementById('saveRecipeBtn');
    const cancelEditBtn = document.getElementById('cancelEditBtn');
    const generateImageBtn = document.getElementById('generateImageBtn');
    
    // Event listeners
    statusFilter.addEventListener('change', function() {
        currentStatus = this.value;
        currentPage = 1;
        loadRecipes();
    });
    
    refreshBtn.addEventListener('click', function() {
        currentPage = 1;
        loadRecipes();
    });
    
    searchBtn.addEventListener('click', performSearch);
    searchInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            performSearch();
        }
    });
    
    prevPageBtn.addEventListener('click', function() {
        if (currentPage > 1) {
            currentPage--;
            loadRecipes();
        }
    });
    
    nextPageBtn.addEventListener('click', function() {
        currentPage++;
        loadRecipes();
    });
    
    // Modal event listeners
    closeBtn.addEventListener('click', closeModal);
    modalCloseBtn.addEventListener('click', closeModal);
    window.addEventListener('click', function(e) {
        if (e.target === modal) {
            closeModal();
        }
    });
    
    approveBtn.addEventListener('click', function() {
        updateRecipeStatus('approved');
    });
    
    rejectBtn.addEventListener('click', function() {
        updateRecipeStatus('rejected');
    });
    
    deleteBtn.addEventListener('click', function() {
        if (confirm('Are you sure you want to delete this recipe? This action cannot be undone.')) {
            deleteRecipe();
        }
    });

    // New event listeners for edit mode
    editRecipeBtn.addEventListener('click', toggleEditMode);
    cancelEditBtn.addEventListener('click', () => toggleEditMode(false)); // Pass false to always exit edit mode
    generateImageBtn.addEventListener('click', () => generateRecipeImage(currentRecipeId));
    recipeDetailForm.addEventListener('submit', handleSaveRecipe);
}

async function loadRecipes() {
    console.log('[Admin Recipes] Loading recipes...', { page: currentPage, status: currentStatus });
    
    try {
        const params = new URLSearchParams({
            page: currentPage,
            limit: 20
        });
        
        if (currentStatus) {
            params.append('status', currentStatus);
        }
        
        const recipes = await EasyBites.api(`/api/admin/recipes?${params}`);
        console.log('[Admin Recipes] Recipes loaded:', recipes);
        
        displayRecipes(recipes);
        updatePaginationControls(recipes);
        
    } catch (error) {
        console.error('[Admin Recipes] Error loading recipes:', error);
        showNotification('Failed to load recipes', 'error');

        // Detect session/auth issues and redirect
        if (error.message && /(unauthorized|not authenticated|session expired)/i.test(error.message)) {
            showNotification('Session expired. Redirecting to admin login…', 'warning');
            setTimeout(() => window.location.href = 'admin-login.html', 1500);
        }

        document.getElementById('recipesTableBody').innerHTML = 
            '<tr><td colspan="6" class="error-row">Error loading recipes</td></tr>';
    }
}

function displayRecipes(recipes) {
    const tbody = document.getElementById('recipesTableBody');
    
    if (recipes.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="no-data-row">No recipes found</td></tr>';
        return;
    }
    
    tbody.innerHTML = recipes.map(recipe => {
        const statusClass = getStatusClass(recipe.status);
        const submittedDate = EasyBites.formatDate(recipe.submittedAt);
        
        return `
            <tr>
                <td>
                    <div class="recipe-name">
                        <strong>${escapeHtml(recipe.name)}</strong>
                        <small class="recipe-description">${escapeHtml(recipe.description?.substring(0, 100) || '')}${recipe.description?.length > 100 ? '...' : ''}</small>
                    </div>
                </td>
                <td>${escapeHtml(recipe.author || 'Unknown')}</td>
                <td>
                    <span class="category-badge">${escapeHtml(recipe.category || 'Uncategorized')}</span>
                </td>
                <td>
                    <span class="status-badge ${statusClass}">${recipe.status}</span>
                </td>
                <td>${submittedDate}</td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-sm btn-primary" onclick="viewRecipe('${recipe.id}')">View</button>
                        ${recipe.status === 'pending' ? 
                            `<button class="btn btn-sm btn-success" onclick="quickApprove('${recipe.id}')">Approve</button>
                             <button class="btn btn-sm btn-warning" onclick="quickReject('${recipe.id}')">Reject</button>` : ''}
                        <button class="btn btn-sm btn-danger" onclick="confirmDelete('${recipe.id}')">Delete</button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

function updatePaginationControls(recipes) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');
    
    // Simple pagination (improve based on your API response structure)
    prevBtn.disabled = currentPage <= 1;
    nextBtn.disabled = recipes.length < 20;
    
    pageInfo.textContent = `Page ${currentPage}`;
}

function getStatusClass(status) {
    switch (status) {
        case 'approved': return 'status-approved';
        case 'rejected': return 'status-rejected';
        case 'pending': return 'status-pending';
        default: return 'status-unknown';
    }
}

async function viewRecipe(recipeId) {
    console.log('[Admin Recipes] Viewing recipe:', recipeId);
    currentRecipeId = recipeId;
    
    try {
        const modalBody = document.getElementById('recipeModalBody');
        const loadingOverlay = document.getElementById('recipeFormLoadingOverlay');
        
        // Removed: modalBody.innerHTML = ''; // This was incorrectly clearing the form as well

        if (loadingOverlay) {
            loadingOverlay.style.display = 'flex'; // Show loading overlay
        }
        showModal();
        
        const recipe = await EasyBites.api(`/api/admin/recipes/${recipeId}`);
        console.log('[Admin Recipes] Fetched recipe details:', recipe);

        populateRecipeForm(recipe);
        toggleEditMode(false); // Ensure view mode when opening

        // Add a small delay before hiding the loading overlay
        setTimeout(() => {
            if (loadingOverlay) {
                loadingOverlay.style.display = 'none'; // Hide loading overlay
                console.log('[Admin Recipes] Loading overlay hidden after delay.');
                console.log('[Admin Recipes] Final loading overlay display style:', loadingOverlay.style.display);
            }
        }, 100); // 100ms delay
    } catch (error) {
        console.error('[Admin Recipes] Error viewing recipe:', error);
        let errorMessage = 'Failed to load recipe details';
        if (error.status === 404) {
            errorMessage = 'Recipe not found.';
        } else if (error.message && /(unauthorized|not authenticated|session expired)/i.test(error.message)) {
            errorMessage = 'Session expired. Please log in again.';
            setTimeout(() => window.location.href = 'admin-login.html', 1500);
        }
        showNotification(errorMessage, 'error');

        const loadingOverlay = document.getElementById('recipeFormLoadingOverlay');
        if (loadingOverlay) {
            loadingOverlay.style.display = 'none'; // Hide loading overlay on error
        }
        
        const form = document.getElementById('recipeDetailForm');
        if (form) { // Only append error if form structure is still present
            const errorDiv = document.createElement('div');
            errorDiv.className = "error";
            errorDiv.innerHTML = `${errorMessage}<br><small>Check console for details</small>`;
            form.appendChild(errorDiv); // Append error to form, not modalBody directly
        } else {
            // If form itself is gone, append to modalBody as fallback
            modalBody.innerHTML = `<div class="error">${errorMessage}<br><small>Check console for details</small></div>`;
        }
    }
}

function populateRecipeForm(recipe) {
    const form = document.getElementById('recipeDetailForm');
    if (!form) {
        console.error('[populateRecipeForm] Form element #recipeDetailForm not found.');
        return;
    }

    console.log('[populateRecipeForm] Populating form with recipe:', recipe);

    form.querySelector('#recipeId').value = recipe.id || '';
    console.log('[populateRecipeForm] Set recipeId:', form.querySelector('#recipeId').value);
    form.querySelector('#recipeName').value = recipe.name || '';
    console.log('[populateRecipeForm] Set recipeName:', form.querySelector('#recipeName').value);
    form.querySelector('#recipeDescription').value = recipe.description || '';
    console.log('[populateRecipeForm] Set recipeDescription:', form.querySelector('#recipeDescription').value);
    form.querySelector('#recipeCategory').value = recipe.category || '';
    console.log('[populateRecipeForm] Set recipeCategory:', form.querySelector('#recipeCategory').value);
    form.querySelector('#recipeDifficulty').value = recipe.difficulty || '';
    console.log('[populateRecipeForm] Set recipeDifficulty:', form.querySelector('#recipeDifficulty').value);
    form.querySelector('#recipePrepTime').value = recipe.prepTime || 0;
    console.log('[populateRecipeForm] Set recipePrepTime:', form.querySelector('#recipePrepTime').value);
    form.querySelector('#recipeCookTime').value = recipe.cookTime || 0;
    console.log('[populateRecipeForm] Set recipeCookTime:', form.querySelector('#recipeCookTime').value);
    form.querySelector('#recipeServings').value = recipe.servings || 0;
    console.log('[populateRecipeForm] Set recipeServings:', form.querySelector('#recipeServings').value);
    form.querySelector('#recipeIngredients').value = recipe.ingredients ? recipe.ingredients.join('\n') : '';
    console.log('[populateRecipeForm] Set recipeIngredients:', form.querySelector('#recipeIngredients').value);
    form.querySelector('#recipeInstructions').value = recipe.instructions ? recipe.instructions.join('\n') : '';
    console.log('[populateRecipeForm] Set recipeInstructions:', form.querySelector('#recipeInstructions').value);
    form.querySelector('#recipeTips').value = recipe.tips || '';
    console.log('[populateRecipeForm] Set recipeTips:', form.querySelector('#recipeTips').value);
    form.querySelector('#recipeNutritionInfo').value = recipe.nutritionInfo || '';
    console.log('[populateRecipeForm] Set recipeNutritionInfo:', form.querySelector('#recipeNutritionInfo').value);
    form.querySelector('#recipeDietaryOptions').value = recipe.dietaryOptions ? recipe.dietaryOptions.join(', ') : '';
    console.log('[populateRecipeForm] Set recipeDietaryOptions:', form.querySelector('#recipeDietaryOptions').value);
    form.querySelector('#recipeAuthor').value = recipe.author || '';
    console.log('[populateRecipeForm] Set recipeAuthor:', form.querySelector('#recipeAuthor').value);
    form.querySelector('#recipeStatus').value = recipe.status || 'pending';
    console.log('[populateRecipeForm] Set recipeStatus:', form.querySelector('#recipeStatus').value);
    form.querySelector('#recipeSubmittedAt').value = recipe.submittedAt ? EasyBites.formatDate(recipe.submittedAt) : '';
    console.log('[populateRecipeForm] Set recipeSubmittedAt:', form.querySelector('#recipeSubmittedAt').value);
    form.querySelector('#recipeImageUrl').value = recipe.imageUrl || '';
    console.log('[populateRecipeForm] Set recipeImageUrl:', form.querySelector('#recipeImageUrl').value);

    // Enable/disable image generation button based on image_url presence
    const generateImageBtn = document.getElementById('generateImageBtn');
    if (generateImageBtn) {
        generateImageBtn.textContent = recipe.imageUrl ? 'Regenerate Image' : 'Generate Image';
        generateImageBtn.disabled = false;
        console.log('[populateRecipeForm] Updated generateImageBtn text and disabled status.');
    }
}

function toggleEditMode(enable = true) {
    const form = document.getElementById('recipeDetailForm');
    if (!form) return;

    const inputs = form.querySelectorAll('input, textarea, select');
    const editBtn = document.getElementById('editRecipeBtn');
    const saveBtn = document.getElementById('saveRecipeBtn');
    const cancelBtn = document.getElementById('cancelEditBtn');
    const actionButtons = document.querySelectorAll('#approveBtn, #rejectBtn, #deleteBtn, .modal-close');
    const generateImageBtn = document.getElementById('generateImageBtn');

    inputs.forEach(input => {
        // Keep recipeId and recipeSubmittedAt disabled always
        if (input.id === 'recipeId' || input.id === 'recipeSubmittedAt') {
            input.disabled = true;
        } else if (input.id === 'recipeAuthor') {
            // Explicitly enable/disable author based on edit mode
            input.disabled = !enable;
        } else {
            input.disabled = !enable;
        }
    });

    // Manage button visibility
    editBtn.style.display = enable ? 'none' : 'inline-block';
    saveBtn.style.display = enable ? 'inline-block' : 'none';
    cancelBtn.style.display = enable ? 'inline-block' : 'none';
    generateImageBtn.style.display = enable ? 'inline-block' : 'none'; // Only show in edit mode

    actionButtons.forEach(btn => {
        if (btn.id === 'approveBtn' || btn.id === 'rejectBtn' || btn.id === 'deleteBtn') {
            btn.style.display = enable ? 'none' : 'inline-block'; // Hide recipe action buttons in edit mode
        } else if (btn.classList.contains('modal-close')) {
            btn.style.display = 'inline-block'; // Always show close
        }
    });

    // If entering edit mode, focus on the first editable field
    if (enable) {
        form.querySelector('#recipeName').focus();
    }
}

async function handleSaveRecipe(e) {
    e.preventDefault();
    const form = document.getElementById('recipeDetailForm');
    const recipeId = form.querySelector('#recipeId').value;
    const saveBtn = document.getElementById('saveRecipeBtn');
    const originalText = saveBtn.textContent;

    try {
        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving...';

        const formData = new FormData(form);
        const body = {
            id: recipeId,
            name: formData.get('name'),
            description: formData.get('description'),
            category: formData.get('category'),
            difficulty: formData.get('difficulty'),
            prepTime: parseInt(formData.get('prep_time')),
            cookTime: parseInt(formData.get('cook_time')),
            servings: parseInt(formData.get('servings')),
            ingredients: formData.get('ingredients').split('\n').filter(line => line.trim() !== ''),
            instructions: formData.get('instructions').split('\n').filter(line => line.trim() !== ''),
            tips: formData.get('tips'),
            nutritionInfo: formData.get('nutrition_info'),
            dietaryOptions: formData.get('dietary_options').split(',').map(item => item.trim()).filter(item => item !== ''),
            author: formData.get('author'),
            status: formData.get('status'),
            imageUrl: formData.get('image_url')
        };

        // Client-side validation for required Author field
        if (!body.author || body.author.trim() === '') {
            showNotification('The Author field is required.', 'error');
            return; // Stop form submission
        }

        console.log('[Admin Recipes] Sending update request:', body);

        await EasyBites.api(`/api/admin/recipes/${recipeId}`, {
            method: 'PUT',
            body: JSON.stringify(body)
        });

        showNotification('Recipe updated successfully!', 'success');
        toggleEditMode(false); // Exit edit mode
        loadRecipes(); // Reload list to reflect changes

    } catch (error) {
        console.error('[Admin Recipes] Error saving recipe:', error);
        showNotification(`Failed to save recipe: ${error.message || error.details}`, 'error');
    } finally {
        saveBtn.disabled = false;
        saveBtn.textContent = originalText;
    }
}

async function generateRecipeImage(recipeId) {
    const generateImageBtn = document.getElementById('generateImageBtn');
    const originalText = generateImageBtn.textContent;
    const imageUrlInput = document.getElementById('recipeImageUrl');
    const recipeModalBody = document.getElementById('recipeModalBody');

    try {
        generateImageBtn.disabled = true;
        generateImageBtn.textContent = 'Generating...';

        // Add a loading indicator to the image input field or modal body
        imageUrlInput.classList.add('loading'); 
        recipeModalBody.classList.add('generating-image'); 

        const result = await EasyBites.api(`/api/admin/recipes/${recipeId}/generate-image`, {
            method: 'POST'
        });

        if (result.success) {
            imageUrlInput.value = result.imageUrl;
            showNotification('Image generated successfully!', 'success');
            generateImageBtn.textContent = 'Regenerate Image'; 
        } else {
            throw new Error(result.details || 'Unknown error');
        }
    } catch (error) {
        console.error('[Admin Recipes] Error generating image:', error);
        showNotification(`Failed to generate image: ${error.message || 'Please try again.'}`, 'error');
    } finally {
        generateImageBtn.disabled = false;
        generateImageBtn.textContent = originalText;
        imageUrlInput.classList.remove('loading');
        recipeModalBody.classList.remove('generating-image');
    }
}

async function quickApprove(recipeId) {
    await updateRecipeStatusDirect(recipeId, 'approved');
}

async function quickReject(recipeId) {
    await updateRecipeStatusDirect(recipeId, 'rejected');
}

async function updateRecipeStatusDirect(recipeId, status) {
    try {
        const response = await fetch(`/api/admin/recipes/${recipeId}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status })
        });
        
        if (!response.ok) {
            throw new Error('Failed to update recipe status');
        }
        
        showNotification(`Recipe ${status} successfully`, 'success');
        loadRecipes(); 
        
    } catch (error) {
        console.error('[Admin Recipes] Error updating recipe status:', error);
        showNotification('Failed to update recipe status', 'error');
    }
}

async function updateRecipeStatus(status) {
    if (!currentRecipeId) return;
    
    await updateRecipeStatusDirect(currentRecipeId, status);
    closeModal();
}

async function deleteRecipe() {
    if (!currentRecipeId) return;
    
    try {
        const response = await fetch(`/api/admin/recipes/${currentRecipeId}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) {
            throw new Error('Failed to delete recipe');
        }
        
        showNotification('Recipe deleted successfully', 'success');
        closeModal();
        loadRecipes(); 
        
    } catch (error) {
        console.error('[Admin Recipes] Error deleting recipe:', error);
        showNotification('Failed to delete recipe', 'error');
    }
}

function confirmDelete(recipeId) {
    if (confirm('Are you sure you want to delete this recipe? This action cannot be undone.')) {
        currentRecipeId = recipeId;
        deleteRecipe();
    }
}

function performSearch() {
    const searchTerm = document.getElementById('searchInput').value.trim();
    console.log('[Admin Recipes] Search not implemented yet:', searchTerm);
    showNotification('Search functionality coming soon', 'info');
}

function showModal() {
    document.getElementById('recipeModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeModal() {
    document.getElementById('recipeModal').style.display = 'none';
    document.body.style.overflow = 'auto';
    currentRecipeId = null;
}

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

function showNotification(message, type = 'info') {
    // Use the same notification system as admin.js
    if (typeof showAdminNotification === 'function') {
        showAdminNotification(message, type);
    } else {
        EasyBites.toast(message, type); 
    }
}