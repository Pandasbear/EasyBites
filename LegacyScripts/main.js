// Main JavaScript functionality for EasyBites

// DOM Content Loaded
document.addEventListener("DOMContentLoaded", () => {
  initializeNavigation()
  initializeSearch()
  initializeModals()
})

// Navigation functionality
function initializeNavigation() {
  const hamburger = document.querySelector(".hamburger")
  const navMenu = document.querySelector(".nav-menu")

  if (hamburger && navMenu) {
    hamburger.addEventListener("click", () => {
      navMenu.classList.toggle("active")
      hamburger.classList.toggle("active")
    })
  }

  // Close mobile menu when clicking on a link
  const navLinks = document.querySelectorAll(".nav-link")
  navLinks.forEach((link) => {
    link.addEventListener("click", () => {
      navMenu.classList.remove("active")
      hamburger.classList.remove("active")
    })
  })
}

// Search functionality
function initializeSearch() {
  const searchInput = document.getElementById("searchInput")
  const searchBtn = document.querySelector(".search-btn")

  if (searchInput && searchBtn) {
    searchBtn.addEventListener("click", performSearch)
    searchInput.addEventListener("keypress", (e) => {
      if (e.key === "Enter") {
        performSearch()
      }
    })
  }
}

function performSearch() {
  const searchInput = document.getElementById("searchInput")
  const searchTerm = searchInput.value.trim().toLowerCase()

  if (searchTerm) {
    filterRecipes(searchTerm)
  }
}

// Recipe filtering
function filterRecipes(searchTerm = "", category = "", difficulty = "", time = "") {
  const recipeCards = document.querySelectorAll(".recipe-card")

  recipeCards.forEach((card) => {
    const title = card.querySelector("h3").textContent.toLowerCase()
    const cardCategory = card.dataset.category || ""
    const cardDifficulty = card.dataset.difficulty || ""
    const cardTime = Number.parseInt(card.dataset.time) || 0

    let showCard = true

    // Search term filter
    if (searchTerm && !title.includes(searchTerm)) {
      showCard = false
    }

    // Category filter
    if (category && cardCategory !== category) {
      showCard = false
    }

    // Difficulty filter
    if (difficulty && cardDifficulty !== difficulty) {
      showCard = false
    }

    // Time filter
    if (time) {
      if (time === "15" && cardTime > 15) showCard = false
      if (time === "30" && cardTime > 30) showCard = false
      if (time === "60" && cardTime > 60) showCard = false
      if (time === "60+" && cardTime <= 60) showCard = false
    }

    card.style.display = showCard ? "block" : "none"
  })
}

// Modal functionality
function initializeModals() {
  // Generic modal close functionality
  const modals = document.querySelectorAll(".modal")
  const modalCloses = document.querySelectorAll(".modal-close, .modal-overlay")

  modalCloses.forEach((close) => {
    close.addEventListener("click", function (e) {
      if (e.target === this) {
        closeModal(this.closest(".modal"))
      }
    })
  })

  // Close modal with Escape key
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
      const openModal = document.querySelector(".modal.active")
      if (openModal) {
        closeModal(openModal)
      }
    }
  })
}

function openModal(modalId) {
  const modal = document.getElementById(modalId)
  if (modal) {
    modal.classList.add("active")
    document.body.style.overflow = "hidden"
  }
}

function closeModal(modal) {
  if (modal) {
    modal.classList.remove("active")
    document.body.style.overflow = ""
  }
}

// Form validation utilities
function validateEmail(email) {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

function validatePassword(password) {
  // At least 8 characters, contains letters and numbers
  const passwordRegex = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{8,}$/
  return passwordRegex.test(password)
}

function showError(fieldId, message) {
  const field = document.getElementById(fieldId)
  const errorElement = document.getElementById(fieldId + "Error")

  if (field && errorElement) {
    field.classList.add("error")
    errorElement.textContent = message
    errorElement.style.display = "block"
  }
}

function clearError(fieldId) {
  const field = document.getElementById(fieldId)
  const errorElement = document.getElementById(fieldId + "Error")

  if (field && errorElement) {
    field.classList.remove("error")
    errorElement.textContent = ""
    errorElement.style.display = "none"
  }
}

function clearAllErrors(formId) {
  const form = document.getElementById(formId)
  if (form) {
    const errorElements = form.querySelectorAll(".error-message")
    const errorFields = form.querySelectorAll(".error")

    errorElements.forEach((error) => {
      error.textContent = ""
      error.style.display = "none"
    })

    errorFields.forEach((field) => {
      field.classList.remove("error")
    })
  }
}

// Toast notifications
function showToast(message, type = "info") {
  const toast = document.createElement("div")
  toast.className = `toast toast-${type}`
  toast.textContent = message

  document.body.appendChild(toast)

  // Trigger animation
  setTimeout(() => toast.classList.add("show"), 100)

  // Remove toast after 3 seconds
  setTimeout(() => {
    toast.classList.remove("show")
    setTimeout(() => document.body.removeChild(toast), 300)
  }, 3000)
}

// Local storage utilities
function saveToLocalStorage(key, data) {
  try {
    localStorage.setItem(key, JSON.stringify(data))
    return true
  } catch (error) {
    console.error("Error saving to localStorage:", error)
    return false
  }
}

function getFromLocalStorage(key) {
  try {
    const data = localStorage.getItem(key)
    return data ? JSON.parse(data) : null
  } catch (error) {
    console.error("Error reading from localStorage:", error)
    return null
  }
}

function removeFromLocalStorage(key) {
  try {
    localStorage.removeItem(key)
    return true
  } catch (error) {
    console.error("Error removing from localStorage:", error)
    return false
  }
}

// Utility functions
function debounce(func, wait) {
  let timeout
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout)
      func(...args)
    }
    clearTimeout(timeout)
    timeout = setTimeout(later, wait)
  }
}

function formatTime(minutes) {
  if (minutes < 60) {
    return `${minutes} min`
  } else {
    const hours = Math.floor(minutes / 60)
    const remainingMinutes = minutes % 60
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`
  }
}

function capitalizeFirst(str) {
  return str.charAt(0).toUpperCase() + str.slice(1)
}

// Export functions for use in other scripts
window.EasyBites = {
  openModal,
  closeModal,
  validateEmail,
  validatePassword,
  showError,
  clearError,
  clearAllErrors,
  showToast,
  saveToLocalStorage,
  getFromLocalStorage,
  removeFromLocalStorage,
  filterRecipes,
  formatTime,
  capitalizeFirst,
}
