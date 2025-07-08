// home.js – Fetch and display popular recipes on the landing page
(function () {
  document.addEventListener('DOMContentLoaded', async () => {
    const grid = document.getElementById('popularRecipesGrid');
    if (!grid) return;

    try {
      // Fetch approved, non-draft recipes
      const recipes = await EasyBites.api('/api/recipes');
      if (!recipes || recipes.length === 0) {
        grid.innerHTML = '<p class="error-message">No recipes yet – check back soon!</p>';
        return;
      }

      // Pick the first 3 recipes (could be random/shuffled later)
      const popular = recipes.slice(0, 3);
      grid.innerHTML = '';

      popular.forEach(r => {
        const card = document.createElement('article');
        card.className = 'recipe-card';
        card.innerHTML = `
          <img src="${r.imageUrl || 'https://via.placeholder.com/300x200?text=Recipe'}" alt="${r.name}">
          <div class="recipe-info">
            <h3>${r.name}</h3>
            <p>${r.description || ''}</p>
            <div class="recipe-meta">
              <span class="time">⏱️ ${(r.totalTime || (r.prepTime + r.cookTime) || '--')} min</span>
              <span class="difficulty">${difficultyEmoji(r.difficulty)}</span>
            </div>
          </div>`;
        // Make card clickable to detail page
        card.addEventListener('click', () => {
          window.location.href = `recipe-detail.html?id=${r.id}`;
        });
        grid.appendChild(card);
      });
    } catch (err) {
      console.error('[home.js] Failed to load recipes:', err);
      grid.innerHTML = `<p class="error-message">Failed to load recipes. Please try again later.</p>`;
    }
  });

  function difficultyEmoji(diff) {
    switch ((diff || '').toLowerCase()) {
      case 'easy':
        return '🟢 Easy';
      case 'medium':
        return '🟡 Medium';
      case 'hard':
        return '🔴 Hard';
      default:
        return diff || 'Easy';
    }
  }
})(); 