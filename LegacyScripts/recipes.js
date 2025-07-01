// Recipe browsing and filtering functionality
document.addEventListener("DOMContentLoaded", () => {
  initializeRecipeFilters()
  initializePagination()
  initializeFavorites()
})

const EasyBites = {
  debounce: (func, wait) => {
    let timeout
    return function (...args) {
      
      clearTimeout(timeout)
      timeout = setTimeout(() => func.apply(this, args), wait)
    }
  },
  filterRecipes: (searchTerm, category, difficulty, time) => {
    // Implement filtering logic here
  },
  showToast: (message, type) => {
    // Implement toast logic here
  },
  getFromLocalStorage: (key) => JSON.parse(localStorage.getItem(key)),
  saveToLocalStorage: (key, value) => {
    localStorage.setItem(key, JSON.stringify(value))
  },
}

function initializeRecipeFilters() {
  const searchInput = document.getElementById("searchInput")
  const categoryFilter = document.getElementById("categoryFilter")
  const difficultyFilter = document.getElementById("difficultyFilter")
  const timeFilter = document.getElementById("timeFilter")

  if (searchInput) {
    searchInput.addEventListener("input", EasyBites.debounce(applyFilters, 300))
  }

  if (categoryFilter) {
    categoryFilter.addEventListener("change", applyFilters)
  }

  if (difficultyFilter) {
    difficultyFilter.addEventListener("change", applyFilters)
  }

  if (timeFilter) {
    timeFilter.addEventListener("change", applyFilters)
  }
}

function applyFilters() {
  const searchTerm = document.getElementById("searchInput")?.value.toLowerCase() || ""
  const category = document.getElementById("categoryFilter")?.value || ""
  const difficulty = document.getElementById("difficultyFilter")?.value || ""
  const time = document.getElementById("timeFilter")?.value || ""

  EasyBites.filterRecipes(searchTerm, category, difficulty, time)
  updateResultsCount()
}

function updateResultsCount() {
  const visibleCards = document.querySelectorAll(".recipe-card:not([style*='display: none'])")
  const totalCards = document.querySelectorAll(".recipe-card")

  // Update pagination info if it exists
  const paginationInfo = document.querySelector(".pagination-info")
  if (paginationInfo) {
    paginationInfo.textContent = `Showing ${visibleCards.length} of ${totalCards.length} recipes`
  }
}

function initializePagination() {
  const prevBtn = document.querySelector(".pagination-btn:first-child")
  const nextBtn = document.querySelector(".pagination-btn:last-child")

  if (prevBtn) {
    prevBtn.addEventListener("click", () => {
      // Implement pagination logic
      EasyBites.showToast("Previous page functionality would be implemented here", "info")
    })
  }

  if (nextBtn) {
    nextBtn.addEventListener("click", () => {
      // Implement pagination logic
      EasyBites.showToast("Next page functionality would be implemented here", "info")
    })
  }
}

function initializeFavorites() {
  const favoriteButtons = document.querySelectorAll(".save-recipe-btn")

  favoriteButtons.forEach((btn) => {
    btn.addEventListener("click", function () {
      const recipeCard = this.closest(".recipe-card") || this.closest(".recipe-detail")
      const recipeId = recipeCard?.dataset.id || Math.random().toString(36).substr(2, 9)

      toggleFavorite(recipeId, this)
    })
  })
}

function toggleFavorite(recipeId, button) {
  let favorites = EasyBites.getFromLocalStorage("favorites") || []

  if (favorites.includes(recipeId)) {
    favorites = favorites.filter((id) => id !== recipeId)
    button.textContent = "❤️"
    button.style.color = "#6c757d"
    EasyBites.showToast("Removed from favorites", "info")
  } else {
    favorites.push(recipeId)
    button.textContent = "❤️"
    button.style.color = "#e74c3c"
    EasyBites.showToast("Added to favorites", "success")
  }

  EasyBites.saveToLocalStorage("favorites", favorites)
}
