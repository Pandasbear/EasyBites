// Authentication JavaScript
document.addEventListener("DOMContentLoaded", () => {
  initializeAuthForms()
})

function initializeAuthForms() {
  // Registration form
  const registerForm = document.getElementById("registerForm")
  if (registerForm) {
    registerForm.addEventListener("submit", handleRegistration)
  }

  // Login form
  const loginForm = document.getElementById("loginForm")
  if (loginForm) {
    loginForm.addEventListener("submit", handleLogin)
  }

  // Admin login form
  const adminLoginForm = document.getElementById("adminLoginForm")
  if (adminLoginForm) {
    adminLoginForm.addEventListener("submit", handleAdminLogin)
  }

  // Real-time validation
  setupRealTimeValidation()
}

function handleRegistration(e) {
  e.preventDefault()

  const formData = new FormData(e.target)
  const data = Object.fromEntries(formData)

  // Clear previous errors
  window.EasyBites.clearAllErrors("registerForm")

  let isValid = true

  // Validate required fields
  if (!data.firstName.trim()) {
    window.EasyBites.showError("firstName", "First name is required")
    isValid = false
  }

  if (!data.lastName.trim()) {
    window.EasyBites.showError("lastName", "Last name is required")
    isValid = false
  }

  if (!data.email.trim()) {
    window.EasyBites.showError("email", "Email is required")
    isValid = false
  } else if (!window.EasyBites.validateEmail(data.email)) {
    window.EasyBites.showError("email", "Please enter a valid email address")
    isValid = false
  }

  if (!data.username.trim()) {
    window.EasyBites.showError("username", "Username is required")
    isValid = false
  } else if (data.username.length < 3) {
    window.EasyBites.showError("username", "Username must be at least 3 characters")
    isValid = false
  }

  if (!data.password) {
    window.EasyBites.showError("password", "Password is required")
    isValid = false
  } else if (!window.EasyBites.validatePassword(data.password)) {
    window.EasyBites.showError("password", "Password must be at least 8 characters with letters and numbers")
    isValid = false
  }

  if (!data.confirmPassword) {
    window.EasyBites.showError("confirmPassword", "Please confirm your password")
    isValid = false
  } else if (data.password !== data.confirmPassword) {
    window.EasyBites.showError("confirmPassword", "Passwords do not match")
    isValid = false
  }

  if (!data.terms) {
    window.EasyBites.showError("terms", "You must agree to the terms and conditions")
    isValid = false
  }

  if (isValid) {
    // Show loading state
    const submitBtn = e.target.querySelector('button[type="submit"]')
    submitBtn.classList.add("loading")
    submitBtn.disabled = true

    // Simulate API call
    setTimeout(() => {
      // Save user data to localStorage (in real app, this would be sent to server)
      const userData = {
        id: Date.now(),
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        username: data.username,
        cookingLevel: data.cookingLevel,
        registeredAt: new Date().toISOString(),
      }

      window.EasyBites.saveToLocalStorage("currentUser", userData)
      window.EasyBites.showToast("Account created successfully! Welcome to EasyBites!", "success")

      // Redirect to home page
      setTimeout(() => {
        window.location.href = "index.html"
      }, 1500)
    }, 2000)
  }
}

function handleLogin(e) {
  e.preventDefault()

  const formData = new FormData(e.target)
  const data = Object.fromEntries(formData)

  // Clear previous errors
  window.EasyBites.clearAllErrors("loginForm")

  let isValid = true

  if (!data.loginEmail.trim()) {
    window.EasyBites.showError("loginEmail", "Email or username is required")
    isValid = false
  }

  if (!data.loginPassword) {
    window.EasyBites.showError("loginPassword", "Password is required")
    isValid = false
  }

  if (isValid) {
    const submitBtn = e.target.querySelector('button[type="submit"]')
    submitBtn.classList.add("loading")
    submitBtn.disabled = true

    // Simulate login
    setTimeout(() => {
      // In a real app, this would validate against server
      const userData = {
        id: 1,
        firstName: "John",
        lastName: "Doe",
        email: data.loginEmail,
        username: "johndoe",
        cookingLevel: "intermediate",
      }

      window.EasyBites.saveToLocalStorage("currentUser", userData)
      window.EasyBites.showToast("Welcome back!", "success")

      setTimeout(() => {
        window.location.href = "index.html"
      }, 1500)
    }, 2000)
  }
}

function handleAdminLogin(e) {
  e.preventDefault()

  const formData = new FormData(e.target)
  const data = Object.fromEntries(formData)

  window.EasyBites.clearAllErrors("adminLoginForm")

  let isValid = true

  if (!data.adminUsername.trim()) {
    window.EasyBites.showError("adminUsername", "Admin username is required")
    isValid = false
  }

  if (!data.adminPassword) {
    window.EasyBites.showError("adminPassword", "Admin password is required")
    isValid = false
  }

  if (!data.adminCode.trim()) {
    window.EasyBites.showError("adminCode", "Security code is required")
    isValid = false
  } else if (data.adminCode.length !== 6) {
    window.EasyBites.showError("adminCode", "Security code must be 6 digits")
    isValid = false
  }

  if (isValid) {
    const submitBtn = e.target.querySelector('button[type="submit"]')
    submitBtn.classList.add("loading")
    submitBtn.disabled = true

    // Simulate admin login
    setTimeout(() => {
      // Check credentials (in real app, validate against server)
      if (data.adminUsername === "admin" && data.adminPassword === "admin123" && data.adminCode === "123456") {
        window.EasyBites.saveToLocalStorage("adminUser", { username: data.adminUsername, role: "admin" })
        window.EasyBites.showToast("Admin access granted", "success")

        setTimeout(() => {
          window.location.href = "admin-dashboard.html"
        }, 1500)
      } else {
        submitBtn.classList.remove("loading")
        submitBtn.disabled = false
        window.EasyBites.showToast("Invalid credentials", "error")
      }
    }, 2000)
  }
}

function setupRealTimeValidation() {
  // Email validation
  const emailField = document.getElementById("email")
  if (emailField) {
    emailField.addEventListener("blur", function () {
      if (this.value && !window.EasyBites.validateEmail(this.value)) {
        window.EasyBites.showError("email", "Please enter a valid email address")
      } else {
        window.EasyBites.clearError("email")
      }
    })
  }

  // Password validation
  const passwordField = document.getElementById("password")
  if (passwordField) {
    passwordField.addEventListener("input", function () {
      if (this.value && !window.EasyBites.validatePassword(this.value)) {
        window.EasyBites.showError("password", "Password must be at least 8 characters with letters and numbers")
      } else {
        window.EasyBites.clearError("password")
      }
    })
  }

  // Confirm password validation
  const confirmPasswordField = document.getElementById("confirmPassword")
  if (confirmPasswordField && passwordField) {
    confirmPasswordField.addEventListener("input", function () {
      if (this.value && this.value !== passwordField.value) {
        window.EasyBites.showError("confirmPassword", "Passwords do not match")
      } else {
        window.EasyBites.clearError("confirmPassword")
      }
    })
  }
}
