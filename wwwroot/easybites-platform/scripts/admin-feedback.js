// Admin Feedback Management Functionality
let currentPage = 1;
let currentStatus = '';
let currentType = '';
let currentRating = '';
let currentFeedbackId = null;

document.addEventListener('DOMContentLoaded', function() {
    initializeFeedbackManagement();
});

function initializeFeedbackManagement() {
    console.log('[Admin Feedback] Initializing feedback management...');
    setupEventListeners();
    loadFeedback();
}

function setupEventListeners() {
    const statusFilter = document.getElementById('statusFilter');
    const typeFilter = document.getElementById('typeFilter');
    const ratingFilter = document.getElementById('ratingFilter');
    const refreshBtn = document.getElementById('refreshBtn');
    const prevPageBtn = document.getElementById('prevPageBtn');
    const nextPageBtn = document.getElementById('nextPageBtn');
    
    // Main modal controls
    const feedbackModal = document.getElementById('feedbackModal');
    const responseModal = document.getElementById('responseModal');
    const closeBtn = feedbackModal.querySelector('.close');
    const modalCloseBtn = feedbackModal.querySelector('.modal-close');
    const markReviewedBtn = document.getElementById('markReviewedBtn');
    const respondBtn = document.getElementById('respondBtn');
    
    // Response modal controls
    const responseCloseBtn = responseModal.querySelector('.close');
    const responseModalCloseBtn = responseModal.querySelector('.modal-close');
    const sendResponseBtn = document.getElementById('sendResponseBtn');
    
    statusFilter.addEventListener('change', function() {
        currentStatus = this.value;
        currentPage = 1;
        loadFeedback();
    });
    
    typeFilter.addEventListener('change', function() {
        currentType = this.value;
        currentPage = 1;
        loadFeedback();
    });
    
    ratingFilter.addEventListener('change', function() {
        console.log('[Admin Feedback] Rating filter changed from', currentRating, 'to', this.value);
        currentRating = this.value;
        currentPage = 1;
        console.log('[Admin Feedback] Updated currentRating to:', currentRating);
        loadFeedback();
    });
    
    refreshBtn.addEventListener('click', () => { currentPage = 1; loadFeedback(); });
    
    prevPageBtn.addEventListener('click', function() {
        if (currentPage > 1) { currentPage--; loadFeedback(); }
    });
    nextPageBtn.addEventListener('click', function() {
        currentPage++; loadFeedback();
    });
    
    // Modal events
    closeBtn.addEventListener('click', closeFeedbackModal);
    modalCloseBtn.addEventListener('click', closeFeedbackModal);
    responseCloseBtn.addEventListener('click', closeResponseModal);
    responseModalCloseBtn.addEventListener('click', closeResponseModal);
    
    window.addEventListener('click', function(e) {
        if (e.target === feedbackModal) closeFeedbackModal();
        if (e.target === responseModal) closeResponseModal();
    });
    
    markReviewedBtn.addEventListener('click', () => updateFeedbackStatus('reviewed'));
    respondBtn.addEventListener('click', showResponseModal);
    sendResponseBtn.addEventListener('click', sendResponse);
}

async function loadFeedback() {
    console.log('[Admin Feedback] Loading feedback...', { page: currentPage, status: currentStatus, type: currentType, rating: currentRating });
    
    try {
        // First, let's check if we have a session
        const cookies = document.cookie;
        console.log('[Admin Feedback] Current cookies:', cookies);
        
        const params = new URLSearchParams({
            page: currentPage,
            limit: 20
        });
        
        if (currentStatus) {
            params.append('status', currentStatus);
            console.log('[Admin Feedback] Added status parameter:', currentStatus);
        }
        if (currentType) {
            params.append('type', currentType);
            console.log('[Admin Feedback] Added type parameter:', currentType);
        }
        if (currentRating) {
            params.append('rating', currentRating);
            console.log('[Admin Feedback] Added rating parameter:', currentRating);
        }
        
        console.log('[Admin Feedback] Final URL parameters:', params.toString());
        console.log('[Admin Feedback] Making API call to:', `/api/admin/feedback?${params}`);
        
        const feedback = await EasyBites.api(`/api/admin/feedback?${params}`);
        
        console.log('[Admin Feedback] Feedback loaded successfully:', feedback);
        
        displayFeedback(feedback);
        updatePaginationControls(feedback);
        
    } catch (error) {
        console.error('[Admin Feedback] Error loading feedback:', error);
        console.error('[Admin Feedback] Error details:', {
            message: error.message,
            status: error.status,
            response: error.response
        });
        
        // Show more specific error messages
        let errorMessage = 'Failed to load feedback';
        
        if (error.message && error.message.includes('Unauthorized')) {
            errorMessage = 'Not logged in as admin. Please log in first.';
            
            // Redirect to admin login after a delay
            setTimeout(() => {
                window.location.href = 'admin-login.html';
            }, 2000);
        } else if (error.message && error.message.includes('session expired')) {
            errorMessage = 'Admin session expired. Redirecting to login...';
            setTimeout(() => {
                window.location.href = 'admin-login.html';
            }, 1500);
        }
        
        showNotification(errorMessage, 'error');

        document.getElementById('feedbackTableBody').innerHTML = 
            `<tr><td colspan="7" class="error-row">${errorMessage}</td></tr>`;
    }
}

function displayFeedback(feedbackList) {
    const tbody = document.getElementById('feedbackTableBody');
    
    if (feedbackList.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="no-data-row">No feedback found</td></tr>';
        return;
    }
    
    tbody.innerHTML = feedbackList.map(feedback => {
        const statusClass = getStatusClass(feedback.status);
        const submittedDate = EasyBites.formatDate(feedback.submittedAt || feedback.createdAt);
        const rating = feedback.rating ? '⭐'.repeat(feedback.rating) : 'N/A';
        
        return `
            <tr>
                <td>
                    <div class="user-info">
                        <strong>${escapeHtml(feedback.name || 'Anonymous')}</strong>
                        <small>${escapeHtml(feedback.email || 'No email')}</small>
                    </div>
                </td>
                <td>
                    <span class="type-badge">${escapeHtml(feedback.type || 'other')}</span>
                </td>
                <td>
                    <div class="subject-text">
                        ${escapeHtml(feedback.subject || 'No subject')}
                    </div>
                </td>
                <td>${rating}</td>
                <td>
                    <span class="status-badge ${statusClass}">${feedback.status || 'new'}</span>
                </td>
                <td>${submittedDate}</td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-sm btn-primary" onclick="viewFeedback('${feedback.id}')">View</button>
                        ${feedback.status === 'new' ? 
                            `<button class="btn btn-sm btn-info" onclick="quickReview('${feedback.id}')">Review</button>` : ''}
                        ${feedback.status !== 'responded' ? 
                            `<button class="btn btn-sm btn-success" onclick="quickRespond('${feedback.id}')">Respond</button>` : ''}
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

function updatePaginationControls(feedback) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');
    
    prevBtn.disabled = currentPage <= 1;
    nextBtn.disabled = feedback.length < 20;
    pageInfo.textContent = `Page ${currentPage}`;
}

function getStatusClass(status) {
    switch (status) {
        case 'new': return 'status-pending';
        case 'reviewed': return 'status-warning';
        case 'responded': return 'status-approved';
        default: return 'status-pending';
    }
}

async function viewFeedback(feedbackId) {
    console.log('[Admin Feedback] Viewing feedback:', feedbackId);
    currentFeedbackId = feedbackId;
    
    try {
        const modalBody = document.getElementById('feedbackModalBody');
        modalBody.innerHTML = `<div class="loading">Loading feedback details...</div>`;
        showFeedbackModal();
        
        // Find the feedback from our current list
        const allParams = new URLSearchParams({
            page: 1,
            limit: 100  // Get more records to find the one we want
        });
        
        if (currentStatus) allParams.append('status', currentStatus);
        if (currentType) allParams.append('type', currentType);
        if (currentRating) allParams.append('rating', currentRating);
        
        const allFeedback = await EasyBites.api(`/api/admin/feedback?${allParams}`);
        const feedback = allFeedback.find(f => f.id === feedbackId);
        
        if (!feedback) {
            modalBody.innerHTML = `<div class="error">Feedback not found</div>`;
            return;
        }

        const submittedDate = EasyBites.formatDate(feedback.submittedAt || feedback.createdAt);
        const rating = feedback.rating ? '⭐'.repeat(feedback.rating) : 'No rating';
        
        modalBody.innerHTML = `
            <div class="feedback-details">
                <div class="feedback-header">
                    <h3>${escapeHtml(feedback.subject || 'No subject')}</h3>
                    <span class="status-badge ${getStatusClass(feedback.status)}">${feedback.status || 'new'}</span>
                </div>
                
                <div class="feedback-meta">
                    <p><strong>From:</strong> ${escapeHtml(feedback.name || 'Anonymous')} ${feedback.email ? `(${escapeHtml(feedback.email)})` : ''}</p>
                    <p><strong>Type:</strong> ${escapeHtml(feedback.type || 'other')}</p>
                    <p><strong>Rating:</strong> ${rating}</p>
                    <p><strong>Submitted:</strong> ${submittedDate}</p>
                </div>
                
                <div class="feedback-content">
                    <h4>Message:</h4>
                    <div class="feedback-message">${escapeHtml(feedback.message || 'No message')}</div>
                </div>
                
                ${feedback.adminResponse ? `
                <div class="feedback-response">
                    <h4>Admin Response:</h4>
                    <div class="admin-response">${escapeHtml(feedback.adminResponse)}</div>
                </div>
                ` : ''}
            </div>
        `;
        
    } catch (error) {
        console.error('[Admin Feedback] Error viewing feedback:', error);
        showNotification('Failed to load feedback details', 'error');
        document.getElementById('feedbackModalBody').innerHTML = `<div class="error">Failed to load feedback details</div>`;
    }
}

async function quickReview(feedbackId) {
    await updateFeedbackStatusDirect(feedbackId, 'reviewed');
}

async function quickRespond(feedbackId) {
    currentFeedbackId = feedbackId;
    showResponseModal();
}

async function updateFeedbackStatusDirect(feedbackId, status, adminResponse = null) {
    try {
        const body = { Status: status };
        if (adminResponse) {
            body.AdminResponse = adminResponse;
        }
        
        const response = await fetch(`/api/admin/feedback/${feedbackId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(body)
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to update feedback');
        }
        
        showNotification(`Feedback ${status} successfully`, 'success');
        loadFeedback();
        
    } catch (error) {
        console.error('[Admin Feedback] Error updating feedback:', error);
        showNotification('Failed to update feedback', 'error');
    }
}

async function updateFeedbackStatus(status) {
    if (!currentFeedbackId) return;
    await updateFeedbackStatusDirect(currentFeedbackId, status);
    closeFeedbackModal();
}

async function sendResponse() {
    if (!currentFeedbackId) return;
    
    const responseText = document.getElementById('responseText').value.trim();
    if (!responseText) {
        showNotification('Please enter a response', 'error');
        return;
    }
    
    try {
        await updateFeedbackStatusDirect(currentFeedbackId, 'responded', responseText);
        closeResponseModal();
        closeFeedbackModal();
        showNotification('Response sent successfully', 'success');
    } catch (error) {
        showNotification('Failed to send response', 'error');
    }
}

function showFeedbackModal() {
    document.getElementById('feedbackModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeFeedbackModal() {
    document.getElementById('feedbackModal').style.display = 'none';
    document.body.style.overflow = 'auto';
    currentFeedbackId = null;
}

function showResponseModal() {
    document.getElementById('responseText').value = '';
    document.getElementById('responseModal').style.display = 'block';
}

function closeResponseModal() {
    document.getElementById('responseModal').style.display = 'none';
}

function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
}

function showNotification(message, type = 'info') {
    if (typeof showAdminNotification === 'function') {
        showAdminNotification(message, type);
    } else {
        EasyBites.toast(message, type); // Replaced alert with EasyBites.toast
    }
}