// submit-recipe.js – handles submission form

(function () {
    document.addEventListener('DOMContentLoaded', async () => {
      const recipeForm = document.getElementById('recipeForm');
      if (!recipeForm) return;

      // Get recipe ID from URL if editing
      const urlParams = new URLSearchParams(window.location.search);
      const recipeId = urlParams.get('id');

      // New elements for image generation
      const recipeNameInput = document.getElementById('recipeName');
      const recipeDescriptionTextarea = document.getElementById('recipeDescription');
      const generateImageBtn = document.getElementById('generateImageBtn');
      const recipeImageUrlInput = document.getElementById('recipeImageUrl');
      const imagePreviewDiv = document.getElementById('imagePreview');
      const imagePreviewImg = imagePreviewDiv.querySelector('img');
      const removeImageBtn = document.getElementById('removeImageBtn');

      // Save as Draft button
      const saveDraftBtn = document.getElementById('saveDraftBtn');

      // File input for direct upload
      const recipeImageFileInput = document.getElementById('recipeImageFile');

      // Ingredient and Instruction elements
      const ingredientsList = document.getElementById('ingredientsList');
      const addIngredientBtn = document.getElementById('addIngredient');
      const instructionsList = document.getElementById('instructionsList');
      const addInstructionBtn = document.getElementById('addInstruction');

      // Event listeners for image generation button state
      recipeNameInput.addEventListener('input', updateImageGenerationButtonState);
      recipeDescriptionTextarea.addEventListener('input', updateImageGenerationButtonState);
      generateImageBtn.addEventListener('click', handleGenerateImage);
      removeImageBtn.addEventListener('click', handleRemoveImage);

      // Event listener for manual file input
      recipeImageFileInput.addEventListener('change', handleManualImageUpload);

      // Event listener for Save as Draft button
      saveDraftBtn.addEventListener('click', (e) => handleFormSubmission(e, true, recipeId));

      // Event listeners for ingredients and instructions
      addIngredientBtn.addEventListener('click', () => addIngredientField());
      addInstructionBtn.addEventListener('click', () => addInstructionField());
      ingredientsList.addEventListener('click', handleRemoveItem);
      instructionsList.addEventListener('click', handleRemoveItem);

      // Populate form if recipeId is present (editing mode)
      if (recipeId) {
          try {
              const recipe = await EasyBites.api(`/api/recipes/${recipeId}`);
              if (recipe) {
                  populateForm(recipe);
                  EasyBites.toast('Editing existing recipe', 'info');
              } else {
                  EasyBites.toast('Recipe not found for editing.', 'error');
              }
          } catch (err) {
              console.error('Error loading recipe for editing:', err);
              EasyBites.toast(`Failed to load recipe for editing: ${err.message}`, 'error');
          }
      }

      // Initial state check
      updateImageGenerationButtonState();

      // Function to update the state of the image generation button
      function updateImageGenerationButtonState() {
          const nameFilled = recipeNameInput.value.trim() !== '';
          const descriptionFilled = recipeDescriptionTextarea.value.trim() !== '';
          const imageUrlPresent = recipeImageUrlInput.value.trim() !== '';

          // Enable generate button only if name and description are filled AND no image is currently set
          const canGenerate = nameFilled && descriptionFilled && !imageUrlPresent;
          generateImageBtn.disabled = !canGenerate;
          if (canGenerate) {
              generateImageBtn.classList.add('btn-highlight'); // Add class for orange highlight
          } else {
              generateImageBtn.classList.remove('btn-highlight'); // Remove class
          }
      }

      // Function to handle manual image upload
      function handleManualImageUpload(event) {
          const file = event.target.files[0];
          if (file) {
              const reader = new FileReader();
              reader.onload = (e) => {
                  const imageUrl = e.target.result;
                  recipeImageUrlInput.value = imageUrl; // Store data URL
                  imagePreviewImg.src = imageUrl;
                  imagePreviewDiv.style.display = 'block';
                  EasyBites.toast('Image uploaded successfully!', 'success');
                  updateImageGenerationButtonState(); // Update button state
              };
              reader.readAsDataURL(file);
          } else {
              // If user cancels file selection, treat as no image
              handleRemoveImage();
          }
      }

      // Function to add an ingredient input field
      function addIngredientField(ingredientText = '') {
          const ingredientItem = document.createElement('div');
          ingredientItem.className = 'ingredient-item';
          ingredientItem.innerHTML = `
              <input type="text" placeholder="e.g., 2 cups all-purpose flour" name="ingredients[]" required value="${escapeHtml(ingredientText)}">
              <button type="button" class="remove-ingredient" title="Remove ingredient">×</button>
          `;
          ingredientsList.appendChild(ingredientItem);
      }

      // Function to add an instruction textarea
      function addInstructionField(instructionText = '') {
          const instructionItems = instructionsList.querySelectorAll('.instruction-item');
          const newStepNumber = instructionItems.length + 1;
          const instructionItem = document.createElement('div');
          instructionItem.className = 'instruction-item';
          instructionItem.innerHTML = `
              <div class="step-number">${newStepNumber}</div>
              <textarea placeholder="Describe step ${newStepNumber}..." name="instructions[]" rows="2" required>${escapeHtml(instructionText)}</textarea>
              <button type="button" class="remove-instruction" title="Remove step">×</button>
          `;
          instructionsList.appendChild(instructionItem);
      }

      // Function to handle removing an item (ingredient or instruction)
      function handleRemoveItem(event) {
          if (event.target.classList.contains('remove-ingredient') || event.target.classList.contains('remove-instruction')) {
              event.target.closest('.ingredient-item, .instruction-item').remove();
              // Re-number instructions after removal
              instructionsList.querySelectorAll('.instruction-item').forEach((item, index) => {
                  item.querySelector('.step-number').textContent = index + 1;
                  item.querySelector('textarea').placeholder = `Describe step ${index + 1}...`;
              });
          }
      }

      // Function to populate the form with recipe data for editing
      function populateForm(recipe) {
          recipeNameInput.value = recipe.name || '';
          recipeDescriptionTextarea.value = recipe.description || '';
          document.getElementById('category').value = recipe.category || '';
          document.getElementById('difficulty').value = recipe.difficulty || '';
          document.getElementById('prepTime').value = recipe.prepTime || '';
          document.getElementById('cookTime').value = recipe.cookTime || '';
          document.getElementById('servings').value = recipe.servings || '';
          document.getElementById('tips').value = recipe.tips || '';
          document.getElementById('nutritionInfo').value = recipe.nutritionInfo || '';

          // Clear existing dynamic fields
          ingredientsList.innerHTML = '';
          instructionsList.innerHTML = '';

          // Populate ingredients
          if (recipe.ingredients && recipe.ingredients.length > 0) {
              recipe.ingredients.forEach(ingredient => addIngredientField(ingredient));
          } else {
              addIngredientField(); // Add one empty field if none exist
          }

          // Populate instructions
          if (recipe.instructions && recipe.instructions.length > 0) {
              recipe.instructions.forEach(instruction => addInstructionField(instruction));
          } else {
              addInstructionField(); // Add one empty field if none exist
          }

          // Populate dietary options
          if (recipe.dietaryOptions) {
              recipe.dietaryOptions.forEach(option => {
                  const checkbox = document.querySelector(`input[name="dietaryOptions[]"][value="${option}"]`);
                  if (checkbox) checkbox.checked = true;
              });
          }

          // Populate image
          if (recipe.imageUrl) {
              recipeImageUrlInput.value = recipe.imageUrl;
              imagePreviewImg.src = recipe.imageUrl;
              imagePreviewDiv.style.display = 'block';
          }
      }


      // Function to handle image generation
      async function handleGenerateImage() {
          const recipeName = recipeNameInput.value.trim();
          const recipeDescription = recipeDescriptionTextarea.value.trim();

          if (!recipeName || !recipeDescription) {
              EasyBites.toast('Please enter both recipe name and description to generate an image.', 'warning');
              return;
          }

          // Clear any manually uploaded file before generating AI image
          if (recipeImageFileInput) {
              recipeImageFileInput.value = ''; // Clear the file input
          }

          generateImageBtn.disabled = true;
          generateImageBtn.textContent = 'Generating...';
          imagePreviewDiv.style.display = 'none'; // Hide previous preview
          imagePreviewImg.src = '';

          EasyBites.toast('Generating recipe image, this may take a moment...', 'info');

          try {
              const result = await EasyBites.api('/api/recipes/temp-image-gen', {
                  method: 'POST',
                  body: JSON.stringify({ recipeName, recipeDescription })
              });

              if (result && result.imageUrl) {
                  recipeImageUrlInput.value = result.imageUrl;
                  imagePreviewImg.src = result.imageUrl;
                  imagePreviewDiv.style.display = 'block';
                  EasyBites.toast('Recipe image generated successfully!', 'success');
              } else {
                  EasyBites.toast('Failed to generate image. Please try again.', 'error');
              }
          } catch (error) {
              console.error('Error generating image:', error);
              EasyBites.toast(`Error generating image: ${error.message || 'Unknown error'}`, 'error');
          } finally {
              generateImageBtn.disabled = false;
              generateImageBtn.textContent = 'Generate Recipe Image';
              updateImageGenerationButtonState(); // Update button state after generation
          }
      }

      // Function to handle removing the generated image
      function handleRemoveImage() {
          recipeImageUrlInput.value = '';
          imagePreviewImg.src = '';
          imagePreviewDiv.style.display = 'none';
          // Also clear the file input if it was used for manual upload
          if (recipeImageFileInput) {
              recipeImageFileInput.value = '';
          }
          EasyBites.toast('Recipe image removed.', 'info');
          updateImageGenerationButtonState(); // Re-evaluate button state
      }

      // Universal form submission handler
      async function handleFormSubmission(e, isDraft = false, currentRecipeId = null) {
        e.preventDefault();
  
        // Clear previous error messages
        clearAllErrors();

        const fd = new FormData(recipeForm);
        const getList = (name) => fd.getAll(name).filter(v => v && v.trim());

        // Get the image URL from the hidden input field
        const imageUrl = recipeImageUrlInput.value;

        const body = {
          id: currentRecipeId, // Include ID for updates
          recipeName: fd.get('recipeName'),
          recipeDescription: fd.get('recipeDescription'),
          category: fd.get('category'),
          difficulty: fd.get('difficulty'),
          prepTime: fd.get('prepTime').trim() === '' ? null : parseInt(fd.get('prepTime'), 10),
          cookTime: fd.get('cookTime').trim() === '' ? null : parseInt(fd.get('cookTime'), 10),
          servings: fd.get('servings').trim() === '' ? null : parseInt(fd.get('servings'), 10),
          ingredients: getList('ingredients[]'),
          instructions: getList('instructions[]'),
          tips: fd.get('tips') || null,
          nutritionInfo: fd.get('nutritionInfo') || null,
          dietaryOptions: getList('dietaryOptions[]'),
          author: 'guest',
          imageUrl: imageUrl, // Explicitly include the image URL
          isDraft: isDraft // Set the isDraft flag
        };

        try {
          const method = currentRecipeId ? 'PUT' : 'POST';
          const url = currentRecipeId ? `/api/recipes/${currentRecipeId}` : '/api/recipes/submit';

          await EasyBites.api(url, {
            method: method,
            body: JSON.stringify(body)
          });
          EasyBites.toast(`Recipe ${isDraft ? 'saved as draft' : 'submitted'} successfully!`);
          recipeForm.reset();
          handleRemoveImage(); // Clear image preview and reset states after submission
          
          // Redirect to account page after successful submission/save
          window.location.href = 'account.html';

        } catch (err) {
          console.error('Submission error:', err);
          let errorMessage = 'Submission failed. Please check your inputs.';

          if (err.response && err.response.errors) {
              // Handle validation errors from the backend
              errorMessage = err.response.title || 'Validation failed. Please correct the highlighted fields.';
              displayValidationErrors(err.response.errors);
          } else if (err.message) {
              // Handle other API errors
              errorMessage = `Submission failed: ${err.message}`;
          }

          EasyBites.toast(errorMessage, 'error');
        }
      }

      function clearAllErrors() {
          document.querySelectorAll('.error-message').forEach(span => {
              span.textContent = '';
          });
          document.querySelectorAll('.form-group input, .form-group textarea, .form-group select').forEach(input => {
              input.classList.remove('error');
          });
      }

      function displayValidationErrors(errors) {
          for (const key in errors) {
              if (errors.hasOwnProperty(key)) {
                  // The key from backend might be 'Category', 'CookTime', etc.
                  // Need to map them to frontend IDs (category, cookTime, etc.)
                  const frontendId = key.charAt(0).toLowerCase() + key.slice(1);
                  const errorSpan = document.getElementById(`${frontendId}Error`);
                  const inputField = document.getElementById(frontendId); // Also add error class to input

                  if (errorSpan) {
                      errorSpan.textContent = errors[key].join(', '); // Join multiple error messages
                  }
                  if (inputField) {
                      inputField.classList.add('error'); // Add a class for visual error indication
                  }
              }
          }
      }

      // Update the main form submission listener to use the new handler
      recipeForm.addEventListener('submit', (e) => handleFormSubmission(e, false, recipeId));

      function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;'
        };
        return text.replace(/[&<>"]/g, function(m) { return map[m]; });
      }

    });
  })(); 