// feedback.js - Handles the feedback form submission and star rating functionality
document.addEventListener('DOMContentLoaded', function() {
    initializeFeedback();
});

function initializeFeedback() {
    setupStarRating();
    setupFormSubmission();
}

// Initialize star rating functionality
function setupStarRating() {
    const stars = document.querySelectorAll('.star-rating .star');
    const ratingText = document.querySelector('.rating-text');
    let selectedRating = 0;
    
    stars.forEach(star => {
        star.addEventListener('click', function() {
            const rating = parseInt(this.getAttribute('data-rating'));
            selectedRating = rating;
            
            // Update visual state
            stars.forEach(s => {
                const starRating = parseInt(s.getAttribute('data-rating'));
                if (starRating <= rating) {
                    s.classList.add('selected');
                } else {
                    s.classList.remove('selected');
                }
            });
            
            // Update rating text
            ratingText.textContent = `${rating} star${rating > 1 ? 's' : ''}`;
        });
        
        // Hover effects
        star.addEventListener('mouseenter', function() {
            const rating = parseInt(this.getAttribute('data-rating'));
            
            stars.forEach(s => {
                const starRating = parseInt(s.getAttribute('data-rating'));
                if (starRating <= rating) {
                    s.classList.add('hover');
                }
            });
        });
        
        star.addEventListener('mouseleave', function() {
            stars.forEach(s => s.classList.remove('hover'));
        });
    });
}

// Handle form submission
function setupFormSubmission() {
    const feedbackForm = document.getElementById('feedbackForm');
    if (!feedbackForm) return;
    
    feedbackForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        // Clear previous error messages
        clearErrors();
        
        // Validate form
        if (!validateForm()) {
            return;
        }
        
        // Get form data
        const formData = {
            Name: feedbackForm.feedbackName.value.trim(),
            Email: feedbackForm.feedbackEmail.value.trim(),
            Type: feedbackForm.feedbackType.value,
            Subject: feedbackForm.feedbackSubject.value.trim(),
            Message: feedbackForm.feedbackMessage.value.trim(),
            Rating: getSelectedRating()
        };
        
        // Disable form while submitting
        const submitBtn = feedbackForm.querySelector('button[type="submit"]');
        const originalBtnText = submitBtn.textContent;
        submitBtn.disabled = true;
        submitBtn.textContent = 'Submitting...';
        
        try {
            // Submit feedback to the API
            const response = await EasyBites.api('/api/feedback/submit', {
                method: 'POST',
                body: JSON.stringify(formData)
            });
            
            // Show success message
            showSuccessMessage();
            
            // Reset the form
            feedbackForm.reset();
            resetStarRating();
            
        } catch (error) {
            console.error('Error submitting feedback:', error);
            
            // Show error message
            EasyBites.toast('Failed to submit feedback: ' + (error.message || 'Unknown error'), 'error');
            
        } finally {
            // Re-enable form
            submitBtn.disabled = false;
            submitBtn.textContent = originalBtnText;
        }
    });
}

// Form validation
function validateForm() {
    const feedbackForm = document.getElementById('feedbackForm');
    let isValid = true;
    
    // Check feedback type
    if (!feedbackForm.feedbackType.value) {
        showError('feedbackTypeError', 'Please select a feedback type');
        isValid = false;
    }
    
    // Check subject
    if (!feedbackForm.feedbackSubject.value.trim()) {
        showError('feedbackSubjectError', 'Please enter a subject');
        isValid = false;
    }
    
    // Check message
    if (!feedbackForm.feedbackMessage.value.trim()) {
        showError('feedbackMessageError', 'Please enter your feedback message');
        isValid = false;
    } else if (feedbackForm.feedbackMessage.value.trim().length < 10) {
        showError('feedbackMessageError', 'Please provide more detailed feedback (at least 10 characters)');
        isValid = false;
    }
    
    // Check email format if provided
    const email = feedbackForm.feedbackEmail.value.trim();
    if (email && !isValidEmail(email)) {
        showError('feedbackEmail', 'Please enter a valid email address');
        isValid = false;
    }
    
    return isValid;
}

// Email validation helper
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// Get the selected rating
function getSelectedRating() {
    const selectedStars = document.querySelectorAll('.star-rating .star.selected');
    return selectedStars.length;
}

// Reset star rating
function resetStarRating() {
    document.querySelectorAll('.star-rating .star').forEach(star => {
        star.classList.remove('selected');
    });
    document.querySelector('.rating-text').textContent = 'Click to rate';
}

// Show form error message
function showError(elementId, message) {
    const errorElement = document.getElementById(elementId);
    if (errorElement) {
        errorElement.textContent = message;
        errorElement.style.display = 'block';
    }
}

// Clear all error messages
function clearErrors() {
    const errorElements = document.querySelectorAll('.error-message');
    errorElements.forEach(element => {
        element.textContent = '';
        element.style.display = 'none';
    });
}

// Show success message after form submission
function showSuccessMessage() {
    // Create success message element
    const successMessage = document.createElement('div');
    successMessage.className = 'feedback-success';
    successMessage.innerHTML = `
        <h3>Thank You for Your Feedback!</h3>
        <p>We appreciate you taking the time to share your thoughts with us.</p>
        <p>Your feedback has been submitted successfully.</p>
        <button class="btn btn-primary mt-2" id="newFeedbackBtn">Submit Another Feedback</button>
    `;
    
    // Replace form with success message
    const formSection = document.querySelector('.feedback-form-section');
    formSection.innerHTML = '';
    formSection.appendChild(successMessage);
    
    // Add event listener to "Submit Another" button
    document.getElementById('newFeedbackBtn').addEventListener('click', function() {
        window.location.reload();
    });
    
    // Scroll to top of form section
    formSection.scrollIntoView({ behavior: 'smooth' });
} 