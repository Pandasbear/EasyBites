// Admin Dashboard Security and Functionality
document.addEventListener('DOMContentLoaded', async function() {
    console.log('[Admin] Checking admin access...');
    
    try {
        // Check if user is authenticated and has admin access
        const response = await fetch('/api/auth/me');
        
        if (!response.ok) {
            console.log('[Admin] User not authenticated, redirecting to admin login');
            redirectToAdminLogin();
            return;
        }
        
        const user = await response.json();
        console.log('[Admin] User data:', user);
        
        // Check if user is admin AND has an admin session
        if (!user.isAdmin || !user.isAdminSession) {
            console.log(`[Admin] Access denied. isAdmin: ${user.isAdmin}, isAdminSession: ${user.isAdminSession}`);
            redirectToAdminLogin('You must log in through the admin portal to access this area.');
            return;
        }
        
        console.log('[Admin] Access granted for admin user');
        
        // Update admin user display
        updateAdminUserDisplay(user);
        
        // Set up logout functionality
        setupAdminLogout();
        
        // Initialize dashboard data if we're on the dashboard page
        if (window.location.pathname.includes('admin-dashboard.html')) {
            await initializeDashboard();
        }
        
    } catch (error) {
        console.error('[Admin] Error checking admin access:', error);
        redirectToAdminLogin('Error verifying admin access. Please log in again.');
    }
});

function redirectToAdminLogin(message = null) {
    if (message) {
        // Store message to show after redirect
        sessionStorage.setItem('adminLoginMessage', message);
    }
    window.location.href = 'admin-login.html';
}

function updateAdminUserDisplay(user) {
    const adminUserSpan = document.querySelector('.admin-user span');
    if (adminUserSpan) {
        adminUserSpan.textContent = `Welcome, ${user.firstName} ${user.lastName}`;
    }
}

function setupAdminLogout() {
    const logoutBtn = document.querySelector('.logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async function(e) {
            e.preventDefault();
            
            try {
                const response = await fetch('/api/auth/logout', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                
                if (response.ok) {
                    console.log('[Admin] Logout successful');
                    sessionStorage.setItem('adminLoginMessage', 'You have been logged out successfully.');
                    window.location.href = 'admin-login.html';
                } else {
                    console.error('[Admin] Logout failed');
                    EasyBites.toast('Logout failed. Please try again.', 'error'); 
                }
            } catch (error) {
                console.error('[Admin] Logout error:', error);
                EasyBites.toast('Logout error. Please try again.', 'error'); 
            }
        });
    }
}

async function initializeDashboard() {
    console.log('[Admin] Initializing dashboard...');
    
    try {
        // Load real statistics and data
        await Promise.all([
            loadDashboardStats(),
            loadPopularCategories(),
            loadRecentActivity(),
            loadPendingActions()
        ]);
        
        console.log('[Admin] Dashboard initialized successfully');
    } catch (error) {
        console.error('[Admin] Error initializing dashboard:', error);
    }
}

async function loadDashboardStats() {
    try {
        console.log('[Admin] Loading dashboard statistics...');
        const stats = await EasyBites.api('/api/admin/dashboard/stats');
        
        // Update the dashboard stats display
        updateDashboardStats(stats);
        
    } catch (error) {
        console.error('[Admin] Error loading dashboard stats:', error);
        showAdminNotification('Failed to load dashboard statistics', 'error');
    }
}

async function loadPopularCategories() {
    try {
        console.log('[Admin] Loading popular categories...');
        const categories = await EasyBites.api('/api/admin/dashboard/popular-categories');
        
        // Update the popular categories display
        updatePopularCategories(categories);
        
    } catch (error) {
        console.error('[Admin] Error loading popular categories:', error);
        showAdminNotification('Failed to load popular categories', 'error');
    }
}

async function loadRecentActivity() {
    try {
        console.log('[Admin] Loading recent activity...');
        const activities = await EasyBites.api('/api/admin/dashboard/activities');
        
        // Update the recent activity display
        await updateRecentActivity(activities);
        
    } catch (error) {
        console.error('[Admin] Error loading recent activity:', error);
        showAdminNotification('Failed to load recent activities', 'error');
    }
}

async function loadPendingActions() {
    try {
        console.log('[Admin] Loading pending actions...');
        const pendingData = await EasyBites.api('/api/admin/dashboard/pending-actions');
        
        // Update the pending actions display
        updatePendingActions(pendingData);
        
    } catch (error) {
        console.error('[Admin] Error loading pending actions:', error);
        showAdminNotification('Failed to load pending actions', 'error');
    }
}

// Functions to update the dashboard UI with real data
function updateDashboardStats(stats) {
    try {
        // Update total users
        const totalUsersEl = document.getElementById('totalUsers');
        if (totalUsersEl) {
            totalUsersEl.textContent = stats.totalUsers.toLocaleString();
        }
        const totalUsersChangeEl = document.getElementById('totalUsersChange');
        if (totalUsersChangeEl) {
            totalUsersChangeEl.textContent = '+12% this month'; 
            totalUsersChangeEl.className = 'stat-change positive';
        }

        // Update total recipes
        const totalRecipesEl = document.getElementById('totalRecipes');
        if (totalRecipesEl) {
            totalRecipesEl.textContent = stats.totalRecipes.toLocaleString();
        }
        const totalRecipesChangeEl = document.getElementById('totalRecipesChange');
        if (totalRecipesChangeEl) {
            totalRecipesChangeEl.textContent = '+8% this month'; 
            totalRecipesChangeEl.className = 'stat-change positive';
        }

        // Update pending reviews (recipes)
        const pendingReviewsEl = document.getElementById('pendingReviews');
        if (pendingReviewsEl) {
            pendingReviewsEl.textContent = stats.pendingRecipes.toLocaleString();
        }
        const pendingReviewsChangeEl = document.getElementById('pendingReviewsChange');
        if (pendingReviewsChangeEl) {
            pendingReviewsChangeEl.textContent = 'Needs attention'; 
            pendingReviewsChangeEl.className = 'stat-change neutral';
        }

        // Update average rating
        const avgRatingEl = document.getElementById('avgRating');
        if (avgRatingEl) {
            avgRatingEl.textContent = stats.avgRating.toFixed(1);
        }
        const avgRatingChangeEl = document.getElementById('avgRatingChange');
        if (avgRatingChangeEl) {
            avgRatingChangeEl.textContent = '+0.2 this month'; 
            avgRatingChangeEl.className = 'stat-change positive';
        }

        console.log('[Admin] Dashboard stats updated successfully');
    } catch (error) {
        console.error('[Admin] Error updating dashboard stats:', error);
    }
}

function updatePopularCategories(categories) {
    try {
        const categoryStatsContainer = document.getElementById('popularCategoriesList');
        if (!categoryStatsContainer) return;

        // Calculate max count for percentage bars
        const maxCount = Math.max(...categories.map(c => c.count), 1);

        // Clear existing content
        categoryStatsContainer.innerHTML = '';

        // Add each category
        categories.forEach(category => {
            const percentage = (category.count / maxCount) * 100;
            
            const categoryItem = document.createElement('div');
            categoryItem.className = 'category-item';
            categoryItem.innerHTML = `
                <span class="category-name">${escapeHtml(category.name)}</span>
                <div class="category-bar">
                    <div class="category-fill" style="width: ${percentage}%"></div>
                </div>
                <span class="category-count">${category.count}</span>
            `;
            
            categoryStatsContainer.appendChild(categoryItem);
        });

        console.log('[Admin] Popular categories updated successfully');
    } catch (error) {
        console.error('[Admin] Error updating popular categories:', error);
    }
}

async function updateRecentActivity(activities) {
    try {
        const activityListContainer = document.querySelector('#recentActivityList');
        if (!activityListContainer) return;

        // Clear existing content
        activityListContainer.innerHTML = '';

        if (activities.length === 0) {
            activityListContainer.innerHTML = '<p class="no-activities">No recent activities</p>';
            return;
        }

        // Fetch all unique usernames needed for activities in parallel
        const userIds = [...new Set(activities.map(a => a.userId).filter(id => id))];
        const usernameMap = new Map();

        if (userIds.length > 0) {
            const usernamePromises = userIds.map(async id => {
                try {
                    const user = await EasyBites.api(`/api/admin/users/${id}/username`);
                    usernameMap.set(id, user.username);
                } catch (err) {
                    console.error(`[Admin] Failed to fetch username for ID ${id}:`, err);
                    usernameMap.set(id, 'Unknown User'); 
                }
            });
            await Promise.all(usernamePromises);
        }

        activities.forEach(activity => {
            const activityItem = document.createElement('div');
            activityItem.className = 'activity-item';

            let icon = '📝'; 
            let contentHtml = '';
            let targetLink = '#'; 

            // Determine content and icon based on action_type
            switch (activity.actionType) {
                case 'recipe_submitted':
                    icon = '🍳';
                    contentHtml = `<strong>${usernameMap.get(activity.userId) || 'A user'}</strong> submitted a new recipe`;
                    if (activity.targetId) {
                        targetLink = `admin-recipes.html?id=${activity.targetId}`;
                    }
                    break;
                case 'user_registered':
                    icon = '👤';
                    contentHtml = `<strong>${usernameMap.get(activity.userId) || 'A new user'}</strong> registered as a new user`;
                    if (activity.targetId) {
                        targetLink = `admin-users.html?id=${activity.targetId}`;
                    }
                    break;
                case 'recipe_rated':
                    icon = '⭐';
                    contentHtml = `<strong>${usernameMap.get(activity.userId) || 'A user'}</strong> left a rating`;
                    if (activity.targetId) {
                        targetLink = `admin-recipes.html?id=${activity.targetId}`;
                    }
                    if (activity.details && activity.details.score) {
                        contentHtml += ` (${activity.details.score}-star)`;
                    }
                    break;
                case 'recipe_reported':
                case 'user_reported':
                    icon = '🚫';
                    contentHtml = `A ${activity.targetType || 'item'} was flagged for review`;
                    if (activity.details && activity.details.reportedName) {
                        contentHtml = `${activity.details.reportedName} was flagged for review`;
                    }
                    if (activity.targetId) {
                        targetLink = `admin-reports.html?id=${activity.targetId}`;
                    }
                    break;
                case 'user_suspended':
                    icon = '⚠️';
                    contentHtml = `<strong>${activity.details?.target_email || 'A user'}</strong> was suspended`;
                    if (activity.targetId) {
                        targetLink = `admin-users.html?id=${activity.targetId}`;
                    }
                    break;
                case 'user_activated':
                    icon = '✅';
                    contentHtml = `<strong>${activity.details?.target_email || 'A user'}</strong> was activated`;
                    if (activity.targetId) {
                        targetLink = `admin-users.html?id=${activity.targetId}`;
                    }
                    break;
                case 'user_banned':
                    icon = '⛔';
                    contentHtml = `<strong>${activity.details?.target_email || 'A user'}</strong> was permanently banned`;
                    if (activity.targetId) {
                        targetLink = `admin-users.html?id=${activity.targetId}`;
                    }
                    break;
                case 'feedback_submitted':
                    icon = '💬';
                    contentHtml = `<strong>${activity.details?.name || 'A user'}</strong> submitted feedback`;
                    if (activity.targetId) {
                        targetLink = `admin-feedback.html?id=${activity.targetId}`;
                    }
                    break;
                case 'recipe_approved':
                    icon = '✔️';
                    contentHtml = `Recipe <strong>"${activity.details?.recipe_name || 'N/A'}"</strong> was approved`;
                    if (activity.targetId) {
                        targetLink = `admin-recipes.html?id=${activity.targetId}`;
                    }
                    break;
                case 'recipe_rejected':
                    icon = '❌';
                    contentHtml = `Recipe <strong>"${activity.details?.recipe_name || 'N/A'}"</strong> was rejected`;
                    if (activity.targetId) {
                        targetLink = `admin-recipes.html?id=${activity.targetId}`;
                    }
                    break;
                case 'recipe_image_generated':
                    icon = '🖼️';
                    contentHtml = `Image generated for recipe <strong>"${activity.details?.recipeName || 'N/A'}"</strong>`;
                    if (activity.targetId) {
                        targetLink = `admin-recipes.html?id=${activity.targetId}`;
                    }
                    break;
                case 'feedback_reviewed':
                    icon = '👀';
                    contentHtml = `Feedback reviewed by <strong>${activity.details?.admin_email || 'an admin'}</strong>`;
                    if (activity.targetId) {
                        targetLink = `admin-feedback.html?id=${activity.targetId}`;
                    }
                    break;
                case 'report_reviewed':
                    icon = '✅';
                    contentHtml = `Report reviewed by <strong>${activity.details?.admin_email || 'an admin'}</strong> (Status: ${activity.details?.status || 'N/A'})`;
                    if (activity.targetId) {
                        targetLink = `admin-reports.html?id=${activity.targetId}`;
                    }
                    break;
                default:
                    // Fallback for unhandled activity types
                    icon = '📝';
                    contentHtml = `An activity of type '${activity.actionType}' occurred`;
                    if (activity.targetType && activity.targetId) {
                        targetLink = `admin-${activity.targetType}s.html?id=${activity.targetId}`;
                    }
                    break;
            }

            activityItem.innerHTML = `
                <div class="activity-icon">${icon}</div>
                <div class="activity-content">
                    <p><a href="${targetLink}">${contentHtml}</a></p>
                    <span class="activity-time">${formatTimeAgo(activity.createdAt)}</span>
                </div>
            `;
            activityListContainer.appendChild(activityItem);
        });

        console.log('[Admin] Recent activity updated successfully');
    } catch (error) {
        console.error('[Admin] Error updating recent activity:', error);
    }
}

function updatePendingActions(pendingData) {
    try {
        const pendingRecipesCountEl = document.getElementById('pendingRecipesCount');
        if (pendingRecipesCountEl) {
            pendingRecipesCountEl.textContent = pendingData.pendingRecipes.toLocaleString();
        }

        const flaggedUsersCountEl = document.getElementById('flaggedUsersCount');
        if (flaggedUsersCountEl) {
            flaggedUsersCountEl.textContent = pendingData.flaggedUsers.toLocaleString();
        }

        const unreadFeedbackCountEl = document.getElementById('unreadFeedbackCount');
        if (unreadFeedbackCountEl) {
            unreadFeedbackCountEl.textContent = pendingData.unreadFeedback.toLocaleString();
        }

        console.log('[Admin] Pending actions updated successfully');
    } catch (error) {
        console.error('[Admin] Error updating pending actions:', error);
    }
}

// Helper to format time ago (e.g., "2 hours ago")
function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    let interval = seconds / 31536000;
    if (interval > 1) return Math.floor(interval) + ' years ago';
    interval = seconds / 2592000;
    if (interval > 1) return Math.floor(interval) + ' months ago';
    interval = seconds / 86400;
    if (interval > 1) return Math.floor(interval) + ' days ago';
    interval = seconds / 3600;
    if (interval > 1) return Math.floor(interval) + ' hours ago';
    interval = seconds / 60;
    if (interval > 1) return Math.floor(interval) + ' minutes ago';
    return Math.floor(seconds) + ' seconds ago';
}

function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;'
    };
    return text.replace(/[&<>"]'/g, function(m) { return map[m]; });
}

// Generic notification function (can be improved with a toast library)
function showAdminNotification(message, type = 'info') {
    let notificationContainer = document.getElementById('adminNotificationContainer');
    if (!notificationContainer) {
        // Create a simple notification container if it doesn't exist
        const body = document.body;
        const div = document.createElement('div');
        div.id = 'adminNotificationContainer';
        div.style.position = 'fixed';
        div.style.top = '20px';
        div.style.right = '20px';
        div.style.zIndex = '10000';
        body.appendChild(div);
        notificationContainer = div;
    }

    const notification = document.createElement('div');
    notification.className = `toast toast-${type} show`;
    notification.textContent = message;
    notificationContainer.appendChild(notification);

    setTimeout(() => {
        notification.classList.remove('show');
        notification.classList.add('hide');
        notification.addEventListener('transitionend', () => notification.remove());
    }, 5000);
}

// Add a global EasyBites.api if it doesn't exist (for direct calls from other admin scripts)
if (!window.EasyBites) {
    window.EasyBites = {};
}

if (!EasyBites.api) {
    EasyBites.api = async function(url, options = {}) {
        const response = await fetch(url, {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            }
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: response.statusText }));
            throw new Error(errorData.error || errorData.message || 'API request failed');
        }

        return response.json();
    };
}

// Add CSS for admin notifications
const adminNotificationStyles = `
    .admin-notification {
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 15px 20px;
        border-radius: 5px;
        color: white;
        font-weight: 500;
        z-index: 10000;
        animation: slideIn 0.3s ease-out;
    }
    
    .admin-notification.info {
        background-color: #3498db;
    }
    
    .admin-notification.success {
        background-color: #2ecc71;
    }
    
    .admin-notification.warning {
        background-color: #f39c12;
    }
    
    .admin-notification.error {
        background-color: #e74c3c;
    }
    
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
`;

// Inject CSS
const styleSheet = document.createElement('style');
styleSheet.textContent = adminNotificationStyles;
document.head.appendChild(styleSheet); 