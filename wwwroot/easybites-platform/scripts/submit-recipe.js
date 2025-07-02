// submit-recipe.js – handles submission form

(function () {
    document.addEventListener('DOMContentLoaded', () => {
      const recipeForm = document.getElementById('recipeForm');
      if (!recipeForm) return;
  
      recipeForm.addEventListener('submit', async (e) => {
        e.preventDefault();
  
        const fd = new FormData(recipeForm);
        const getList = (name) => fd.getAll(name).filter(v => v && v.trim());
  
        const body = {
          recipeName: fd.get('recipeName'),
          recipeDescription: fd.get('recipeDescription'),
          category: fd.get('category'),
          difficulty: fd.get('difficulty'),
          prepTime: parseInt(fd.get('prepTime') || '0', 10),
          cookTime: parseInt(fd.get('cookTime') || '0', 10),
          servings: parseInt(fd.get('servings') || '0', 10),
          ingredients: getList('ingredients[]'),
          instructions: getList('instructions[]'),
          tips: fd.get('tips') || null,
          nutritionInfo: fd.get('nutritionInfo') || null,
          dietaryOptions: getList('dietaryOptions[]'),
          author: 'guest'
        };
  
        try {
          await EasyBites.api('/api/recipes', {
            method: 'POST',
            body: JSON.stringify(body)
          });
          EasyBites.toast('Recipe submitted! Awaiting approval.');
          recipeForm.reset();
        } catch (err) {
          EasyBites.toast(`Submission failed: ${err.message}`);
        }
      });
    });
  })(); 