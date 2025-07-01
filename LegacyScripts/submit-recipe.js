// Recipe submission functionality
document.addEventListener("DOMContentLoaded", () => {
  initializeRecipeForm()
  initializeDynamicLists()
  initializeFileUpload()
})

function initializeRecipeForm() {
  const recipeForm = document.getElementById("recipeForm")
  if (recipeForm) {
    recipeForm.addEventListener("submit", handleRecipeSubmission)
  }
}

function initializeDynamicLists() {
  // Add ingredient functionality
  const addIngredientBtn = document.getElementById("addIngredient")
  if (addIngredientBtn) {
    addIngredientBtn.addEventListener("click", addIngredientField)
  }

  // Add instruction functionality
  const addInstructionBtn = document.getElementById("addInstruction")
  if (addInstructionBtn) {
    addInstructionBtn.addEventListener("click", addInstructionField)
  }

  // Initialize remove buttons for existing items
  initializeRemoveButtons()
}

function addIngredientField() {
  const ingredientsList = document.getElementById("ingredientsList")
  const newIngredient = document.createElement("div")
  newIngredient.className = "ingredient-item"
  newIngredient.innerHTML = `
    <input type="text" placeholder="e.g., 1 cup sugar" name="ingredients[]" required>
    <button type="button" class="remove-ingredient" title="Remove ingredient">×</button>
  `

  ingredientsList.appendChild(newIngredient)

  // Add event listener to remove button
  const removeBtn = newIngredient.querySelector(".remove-ingredient")
  removeBtn.addEventListener("click", () => {
    newIngredient.remove()
  })

  // Focus on the new input
  newIngredient.querySelector("input").focus()
}

function addInstructionField() {
  const instructionsList = document.getElementById("instructionsList")
  const stepNumber = instructionsList.children.length + 1

  const newInstruction = document.createElement("div")
  newInstruction.className = "instruction-item"
  newInstruction.innerHTML = `
    <div class="step-number">${stepNumber}</div>
    <textarea placeholder="Describe step ${stepNumber}..." name="instructions[]" rows="2" required></textarea>
    <button type="button" class="remove-instruction" title="Remove step">×</button>
  `

  instructionsList.appendChild(newInstruction)

  // Add event listener to remove button
  const removeBtn = newInstruction.querySelector(".remove-instruction")
  removeBtn.addEventListener("click", () => {
    newInstruction.remove()
    updateStepNumbers()
  })

  // Focus on the new textarea
  newInstruction.querySelector("textarea").focus()
}

function initializeRemoveButtons() {
  // Remove ingredient buttons
  document.addEventListener("click", (e) => {
    if (e.target.classList.contains("remove-ingredient")) {
      const ingredientsList = document.getElementById("ingredientsList")
      if (ingredientsList.children.length > 1) {
        e.target.closest(".ingredient-item").remove()
      } else {
        window.alert("At least one ingredient is required")
      }
    }

    if (e.target.classList.contains("remove-instruction")) {
      const instructionsList = document.getElementById("instructionsList")
      if (instructionsList.children.length > 1) {
        e.target.closest(".instruction-item").remove()
        updateStepNumbers()
      } else {
        window.alert("At least one instruction step is required")
      }
    }
  })
}

function updateStepNumbers() {
  const instructionItems = document.querySelectorAll(".instruction-item")
  instructionItems.forEach((item, index) => {
    const stepNumber = item.querySelector(".step-number")
    const textarea = item.querySelector("textarea")

    stepNumber.textContent = index + 1
    textarea.placeholder = `Describe step ${index + 1}...`
  })
}

function initializeFileUpload() {
  const fileInput = document.getElementById("recipeImage")
  const fileLabel = document.querySelector(".file-upload-label")

  if (fileInput && fileLabel) {
    fileInput.addEventListener("change", function () {
      const file = this.files[0]
      const uploadText = fileLabel.querySelector(".upload-text")

      if (file) {
        // Validate file size (5MB max)
        if (file.size > 5 * 1024 * 1024) {
          window.alert("File size must be less than 5MB")
          this.value = ""
          uploadText.textContent = "Choose Image"
          return
        }

        // Validate file type
        const allowedTypes = ["image/jpeg", "image/png", "image/gif"]
        if (!allowedTypes.includes(file.type)) {
          window.alert("Only JPG, PNG, and GIF files are allowed")
          this.value = ""
          uploadText.textContent = "Choose Image"
          return
        }

        uploadText.textContent = file.name

        // Preview image (optional)
        const reader = new FileReader()
        reader.onload = (e) => {
          // Could add image preview here
        }
        reader.readAsDataURL(file)
      } else {
        uploadText.textContent = "Choose Image"
      }
    })
  }
}

function handleRecipeSubmission(e) {
  e.preventDefault()

  const formData = new FormData(e.target)

  // Clear previous errors
  // EasyBites.clearAllErrors("recipeForm")

  let isValid = true

  // Validate basic information
  if (!formData.get("recipeName").trim()) {
    window.alert("Recipe name is required")
    isValid = false
  }

  if (!formData.get("recipeDescription").trim()) {
    window.alert("Recipe description is required")
    isValid = false
  }

  if (!formData.get("category")) {
    window.alert("Please select a category")
    isValid = false
  }

  if (!formData.get("difficulty")) {
    window.alert("Please select difficulty level")
    isValid = false
  }

  const prepTime = Number.parseInt(formData.get("prepTime"))
  if (!prepTime || prepTime < 1) {
    window.alert("Prep time must be at least 1 minute")
    isValid = false
  }

  const cookTime = Number.parseInt(formData.get("cookTime"))
  if (!cookTime || cookTime < 1) {
    window.alert("Cook time must be at least 1 minute")
    isValid = false
  }

  const servings = Number.parseInt(formData.get("servings"))
  if (!servings || servings < 1) {
    window.alert("Servings must be at least 1")
    isValid = false
  }

  // Validate ingredients
  const ingredients = formData.getAll("ingredients[]").filter((ing) => ing.trim())
  if (ingredients.length === 0) {
    window.alert("At least one ingredient is required")
    isValid = false
  }

  // Validate instructions
  const instructions = formData.getAll("instructions[]").filter((inst) => inst.trim())
  if (instructions.length === 0) {
    window.alert("At least one instruction step is required")
    isValid = false
  }

  if (isValid) {
    const submitBtn = e.target.querySelector('button[type="submit"]')
    submitBtn.classList.add("loading")
    submitBtn.disabled = true

    // Simulate recipe submission
    setTimeout(() => {
      const recipeData = {
        id: Date.now(),
        name: formData.get("recipeName"),
        description: formData.get("recipeDescription"),
        category: formData.get("category"),
        difficulty: formData.get("difficulty"),
        prepTime: prepTime,
        cookTime: cookTime,
        servings: servings,
        ingredients: ingredients,
        instructions: instructions,
        tips: formData.get("tips"),
        nutritionInfo: formData.get("nutritionInfo"),
        dietaryOptions: formData.getAll("dietaryOptions[]"),
        author: "Current User", // Would get from logged in user
        submittedAt: new Date().toISOString(),
        status: "pending", // Would be pending admin approval
      }

      // Save to localStorage (in real app, send to server)
      const recipes = JSON.parse(localStorage.getItem("submittedRecipes")) || []
      recipes.push(recipeData)
      localStorage.setItem("submittedRecipes", JSON.stringify(recipes))

      window.alert("Recipe submitted successfully! It will be reviewed by our team.")

      // Reset form
      setTimeout(() => {
        e.target.reset()
        // Reset dynamic lists to initial state
        const ingredientsList = document.getElementById("ingredientsList")
        const instructionsList = document.getElementById("instructionsList")

        // Keep only first ingredient and instruction
        while (ingredientsList.children.length > 1) {
          ingredientsList.removeChild(ingredientsList.lastChild)
        }
        while (instructionsList.children.length > 1) {
          instructionsList.removeChild(instructionsList.lastChild)
        }

        submitBtn.classList.remove("loading")
        submitBtn.disabled = false

        // Scroll to top
        window.scrollTo({ top: 0, behavior: "smooth" })
      }, 2000)
    }, 3000)
  }
}
