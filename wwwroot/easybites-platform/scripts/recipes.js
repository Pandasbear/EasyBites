// recipes.js – fetch recipes from backend and render list (basic)

(function () {
  document.addEventListener('DOMContentLoaded', async () => {
    const grid = document.getElementById('recipesGrid');
    if (!grid) return;

    try {
      const recipes = await EasyBites.api('/api/recipes');
      if (!Array.isArray(recipes) || recipes.length === 0) return;

      grid.innerHTML = recipes.map(r => `
        <article class="recipe-card" data-category="${r.category}" data-difficulty="${r.difficulty}" data-time="${r.prepTime + r.cookTime}">
          <img src="/placeholder.svg?height=200&width=300" alt="${r.name}">
          <div class="recipe-info">
            <h3><a href="recipe-detail.html?id=${r.id}">${r.name}</a></h3>
            <p>${r.description}</p>
            <div class="recipe-meta">
              <span class="time">⏱️ ${r.prepTime + r.cookTime} min</span>
              <span class="difficulty">${r.difficulty}</span>
              <span class="category">${r.category}</span>
            </div>
            <div class="recipe-author"><span>By: ${r.author}</span></div>
          </div>
        </article>
      `).join('');
    } catch (err) {
      console.error(err);
    }
  });
})(); 