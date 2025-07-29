// Admin User Management Functionality
let currentPage = 1;
let currentStatus = '';
let currentSearch = '';
let currentUserId = null;
let totalUsers = 0;

document.addEventListener('DOMContentLoaded', function() {
    initializeUserManagement();
});

function initializeUserManagement() {
    console.log('[Admin Users] Initializing user management...');
    setupEventListeners();
    loadUsers();
}

function setupEventListeners() {
    const statusFilter = document.getElementById('statusFilter');
    const refreshBtn = document.getElementById('refreshBtn');
    const createUserBtn = document.getElementById('createUserBtn');
    const searchBtn = document.getElementById('searchBtn');
    const searchInput = document.getElementById('searchInput');
    const prevPageBtn = document.getElementById('prevPageBtn');
    const nextPageBtn = document.getElementById('nextPageBtn');
    
    // Modal controls
    const userModal = document.getElementById('userModal');
    const suspendModal = document.getElementById('suspendModal');
    const banModal = document.getElementById('banModal');
    const createUserModal = document.getElementById('createUserModal');
    
    const userCloseBtn = userModal.querySelector('.close');
    const userModalCloseBtn = userModal.querySelector('.modal-close');
    
    const suspendCloseBtn = suspendModal.querySelector('.close');
    const suspendModalCloseBtn = suspendModal.querySelector('.modal-close');
    const confirmSuspendBtn = document.getElementById('confirmSuspendBtn');
    
    const banCloseBtn = banModal.querySelector('.close');
    const banModalCloseBtn = banModal.querySelector('.modal-close');
    const confirmBanBtn = document.getElementById('confirmBanBtn');
    
    // Create User Modal controls
    const createUserCloseBtn = createUserModal.querySelector('.close');
    const createUserModalCloseBtn = createUserModal.querySelector('.modal-close');
    const createUserSubmit = document.getElementById('createUserSubmit');
    const createUserForm = document.getElementById('createUserForm');
    
    statusFilter.addEventListener('change', function() {
        currentStatus = this.value;
        currentPage = 1;
        loadUsers();
    });
    
    refreshBtn.addEventListener('click', () => { 
        currentPage = 1; 
        currentSearch = '';
        currentStatus = '';
        statusFilter.value = '';
        searchInput.value = '';
        loadUsers(); 
    });
    createUserBtn.addEventListener('click', showCreateUserModal);
    searchBtn.addEventListener('click', performSearch);
    searchInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') performSearch();
    });
    
    // Auto-search when user clears the input
    searchInput.addEventListener('input', function(e) {
        if (e.target.value.trim() === '' && currentSearch !== '') {
            currentSearch = '';
            currentPage = 1;
            loadUsers();
        }
    });
    
    prevPageBtn.addEventListener('click', function() {
        if (currentPage > 1) { currentPage--; loadUsers(); }
    });
    nextPageBtn.addEventListener('click', function() {
        currentPage++; loadUsers();
    });
    
    // User Modal events
    userCloseBtn.addEventListener('click', closeUserModal);
    userModalCloseBtn.addEventListener('click', closeUserModal);
    
    // Suspend Modal events
    suspendCloseBtn.addEventListener('click', closeSuspendModal);
    suspendModalCloseBtn.addEventListener('click', closeSuspendModal);
    confirmSuspendBtn.addEventListener('click', confirmSuspendUser);
    
    // Ban Modal events
    banCloseBtn.addEventListener('click', closeBanModal);
    banModalCloseBtn.addEventListener('click', closeBanModal);
    confirmBanBtn.addEventListener('click', confirmBanUser);
    
    // Create User Modal events
    createUserCloseBtn.addEventListener('click', closeCreateUserModal);
    createUserModalCloseBtn.addEventListener('click', closeCreateUserModal);
    createUserSubmit.addEventListener('click', handleCreateUser);
    createUserForm.addEventListener('submit', function(e) {
        e.preventDefault();
        handleCreateUser();
    });
    
    // Close modals when clicking outside
    window.addEventListener('click', function(e) {
        if (e.target === userModal) closeUserModal();
        if (e.target === suspendModal) closeSuspendModal();
        if (e.target === banModal) closeBanModal();
        if (e.target === createUserModal) closeCreateUserModal();
    });
}

async function loadUsers() {
    console.log('[Admin Users] Loading users...', { page: currentPage, status: currentStatus, search: currentSearch });
    
    // Show loading state
    document.getElementById('usersTableBody').innerHTML = 
        '<tr><td colspan="7" class="loading-row">Loading users...</td></tr>';
    
    try {
        const params = new URLSearchParams({
            page: currentPage,
            limit: 20
        });
        
        if (currentStatus) {
            params.append('status', currentStatus);
        }
        
        if (currentSearch) {
            params.append('search', currentSearch);
        }
        
        const users = await EasyBites.api(`/api/admin/users?${params}`);
        console.log('[Admin Users] Users loaded:', users);
        
        // Store total count from the first user if available
        if (users.length > 0 && users[0].totalCount !== undefined) {
            totalUsers = users[0].totalCount;
        } else {
            totalUsers = users.length;
        }
        
        displayUsers(users);
        updatePaginationControls(users);
        
    } catch (error) {
        console.error('[Admin Users] Error loading users:', error);
        showNotification('Failed to load users', 'error');

        // Detect session/auth issues and redirect to admin login for better DX
        if (error.message && /(unauthorized|not authenticated|session expired)/i.test(error.message)) {
            showNotification('Session expired. Redirecting to admin login…', 'warning');
            setTimeout(() => window.location.href = 'admin-login.html', 1500);
        }

        document.getElementById('usersTableBody').innerHTML = 
            '<tr><td colspan="7" class="error-row">Error loading users</td></tr>';
    }
}

function displayUsers(users) {
    const tbody = document.getElementById('usersTableBody');
    
    if (users.length === 0) {
        let message = 'No users found';
        if (currentSearch) {
            message = `No users found matching "${currentSearch}"`;
        } else if (currentStatus) {
            message = `No users found with status "${currentStatus}"`;
        }
        tbody.innerHTML = `<tr><td colspan="7" class="no-data-row">${message}</td></tr>`;
        return;
    }
    
    tbody.innerHTML = users.map(user => {
        // Convert boolean active to status string for display
        const accountStatus = user.active ? 'Active' : 'Inactive';
        const statusClass = getStatusClass(accountStatus);
        const joinedDate = EasyBites.formatDate(user.createdAt || new Date().toISOString());
        const lastLogin = user.lastLogin ? EasyBites.formatDate(user.lastLogin) : 'Never';
        
        return `
            <tr>
                <td>
                    <div class="user-info">
                        <strong>${escapeHtml(user.firstName || 'Unknown')} ${escapeHtml(user.lastName || '')}</strong>
                        <small>ID: ${user.id}</small>
                    </div>
                </td>
                <td>${escapeHtml(user.email)}</td>
                <td>
                    <span class="admin-badge ${user.isAdmin ? 'admin-yes' : 'admin-no'}">
                        ${user.isAdmin ? 'Yes' : 'No'}
                    </span>
                </td>
                <td>
                    <span class="status-badge ${statusClass}">${accountStatus}</span>
                </td>
                <td>${lastLogin}</td>
                <td>${joinedDate}</td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-sm btn-primary" onclick="viewUser('${user.id}')">View</button>
                        ${user.active ? 
                            `<button class="btn btn-sm btn-warning" onclick="quickSuspend('${user.id}')">Suspend</button>` : ''}
                        <button class="btn btn-sm btn-danger" onclick="quickBan('${user.id}')">Ban</button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

function updatePaginationControls(users) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');
    
    const limit = 20;
    const totalPages = Math.ceil(totalUsers / limit);
    
    prevBtn.disabled = currentPage <= 1;
    nextBtn.disabled = currentPage >= totalPages || users.length < limit;
    
    if (totalUsers > 0) {
        pageInfo.textContent = `Page ${currentPage} of ${totalPages} (${totalUsers} total users)`;
    } else {
        pageInfo.textContent = `Page ${currentPage}`;
    }
}

function getStatusClass(status) {
    switch (status?.toLowerCase()) {
        case 'active': return 'status-approved';
        case 'inactive': return 'status-warning';
        default: return 'status-approved';
    }
}

async function viewUser(userId) {
    console.log('[Admin Users] Viewing user:', userId);
    currentUserId = userId;
    
    try {
        const modalBody = document.getElementById('userModalBody');
        modalBody.innerHTML = `<div class="loading">Loading user details...</div>`;
        showUserModal();
        
        // Get the user data from our current loaded users
        const params = new URLSearchParams({
            page: 1,
            limit: 100  
        });
        
        if (currentStatus) params.append('status', currentStatus);
        if (currentSearch) params.append('search', currentSearch);
        
        const users = await EasyBites.api(`/api/admin/users?${params}`);
        const user = users.find(u => u.id === userId);
        
        if (!user) {
            modalBody.innerHTML = `<div class="error">User not found</div>`;
            return;
        }

        const joinedDate = EasyBites.formatDate(user.createdAt || new Date().toISOString());
        const lastLogin = user.lastLogin ? EasyBites.formatDate(user.lastLogin) : 'Never';
        const accountStatus = user.active ? 'Active' : 'Inactive';
        
        modalBody.innerHTML = `
            <div class="user-details">
                <div class="user-summary">
                    <h4>${escapeHtml(user.firstName || 'Unknown')} ${escapeHtml(user.lastName || '')}</h4>
                    <p><strong>User ID:</strong> ${user.id}</p>
                    <p><strong>Email:</strong> ${escapeHtml(user.email)}</p>
                    <p><strong>Username:</strong> ${escapeHtml(user.username || 'Not set')}</p>
                    <p><strong>Account Status:</strong> 
                        <span class="status-badge ${getStatusClass(accountStatus)}">${accountStatus}</span>
                    </p>
                    <p><strong>Admin Rights:</strong> 
                        <span class="admin-badge ${user.isAdmin ? 'admin-yes' : 'admin-no'}">
                            ${user.isAdmin ? 'Yes' : 'No'}
                        </span>
                    </p>
                    <p><strong>Joined:</strong> ${joinedDate}</p>
                    <p><strong>Last Login:</strong> ${lastLogin}</p>
                </div>
                
                <div class="user-actions-section">
                    <h4>Available Actions</h4>
                    <div class="action-buttons">
                        ${user.active ? 
                            `<button class="btn btn-warning" onclick="showSuspendModal('${user.id}')">Suspend Account</button>` : 
                            `<button class="btn btn-success" onclick="quickUnsuspend('${user.id}')">Unsuspend Account</button>`}
                        <button class="btn btn-danger" onclick="showBanModal('${user.id}')">Ban Account</button>
                    </div>
                </div>
            </div>
        `;
        
    } catch (error) {
        console.error('[Admin Users] Error viewing user:', error);
        showNotification('Failed to load user details', 'error');
        document.getElementById('userModalBody').innerHTML = `<div class="error">Failed to load user details</div>`;
    }
}



async function quickSuspend(userId) {
    showSuspendModal(userId);
}

async function quickBan(userId) {
    showBanModal(userId);
}

async function quickUnsuspend(userId) {
    if (confirm('Are you sure you want to unsuspend this user? Their account will be reactivated.')) {
        await updateUserStatusDirect(userId, 'activate');
    }
}

function showSuspendModal(userId) {
    console.log('[Admin Users] Showing suspend modal for user:', userId);
    currentUserId = userId;
    
    // Get user data and populate summary
    const params = new URLSearchParams({
        page: 1,
        limit: 100
    });
    
    if (currentStatus) params.append('status', currentStatus);
    if (currentSearch) params.append('search', currentSearch);
    
    EasyBites.api(`/api/admin/users?${params}`)
        .then(users => {
            const user = users.find(u => u.id === userId);
            if (user) {
                document.getElementById('suspendUserSummary').innerHTML = `
                    <h4>User to Suspend</h4>
                    <p><strong>Name:</strong> ${escapeHtml(user.firstName || 'Unknown')} ${escapeHtml(user.lastName || '')}</p>
                    <p><strong>Email:</strong> ${escapeHtml(user.email)}</p>
                    <p><strong>Current Status:</strong> ${user.active ? 'Active' : 'Inactive'}</p>
                `;
            }
        })
        .catch(error => {
            console.error('Error loading user for suspend modal:', error);
        });
    
    document.getElementById('suspendModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function showBanModal(userId) {
    console.log('[Admin Users] Showing ban modal for user:', userId);
    currentUserId = userId;
    
    // Clear previous ban reason
    document.getElementById('banReasonText').value = '';
    
    // Get user data and populate summary
    const params = new URLSearchParams({
        page: 1,
        limit: 100
    });
    
    if (currentStatus) params.append('status', currentStatus);
    if (currentSearch) params.append('search', currentSearch);
    
    EasyBites.api(`/api/admin/users?${params}`)
        .then(users => {
            const user = users.find(u => u.id === userId);
            if (user) {
                document.getElementById('banUserSummary').innerHTML = `
                    <h4>User to Ban</h4>
                    <p><strong>Name:</strong> ${escapeHtml(user.firstName || 'Unknown')} ${escapeHtml(user.lastName || '')}</p>
                    <p><strong>Email:</strong> ${escapeHtml(user.email)}</p>
                    <p><strong>Current Status:</strong> ${user.active ? 'Active' : 'Inactive'}</p>
                    <p><strong>Member Since:</strong> ${new Date(user.createdAt || Date.now()).toLocaleDateString()}</p>
                `;
            }
        })
        .catch(error => {
            console.error('Error loading user for ban modal:', error);
        });
    
    document.getElementById('banModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

async function confirmSuspendUser() {
    if (!currentUserId) return;
    
    try {
        const confirmBtn = document.getElementById('confirmSuspendBtn');
        confirmBtn.disabled = true;
        confirmBtn.textContent = 'Suspending...';
        
        await updateUserStatusDirect(currentUserId, 'suspend');
        
        closeSuspendModal();
        closeUserModal();
        showNotification('User has been suspended successfully', 'success');
        
    } catch (error) {
        console.error('[Admin Users] Error suspending user:', error);
        showNotification('Failed to suspend user', 'error');
    } finally {
        const confirmBtn = document.getElementById('confirmSuspendBtn');
        confirmBtn.disabled = false;
        confirmBtn.textContent = 'Yes, Suspend User';
    }
}

async function confirmBanUser() {
    if (!currentUserId) return;
    
    const banReason = document.getElementById('banReasonText').value.trim();
    if (!banReason) {
        showNotification('Please provide a reason for the ban', 'error');
        document.getElementById('banReasonText').focus();
        return;
    }
    
    try {
        const confirmBtn = document.getElementById('confirmBanBtn');
        confirmBtn.disabled = true;
        confirmBtn.textContent = 'Banning...';
        
        // Include ban reason in the request
        await updateUserStatusDirect(currentUserId, 'ban', banReason);
        
        closeBanModal();
        closeUserModal();
        showNotification('User has been banned permanently', 'success');
        
    } catch (error) {
        console.error('[Admin Users] Error banning user:', error);
        showNotification('Failed to ban user', 'error');
    } finally {
        const confirmBtn = document.getElementById('confirmBanBtn');
        confirmBtn.disabled = false;
        confirmBtn.textContent = 'Yes, Ban User Permanently';
    }
}

function showUserModal() {
    document.getElementById('userModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeUserModal() {
    document.getElementById('userModal').style.display = 'none';
    document.body.style.overflow = 'auto';
    currentUserId = null;
}

function closeSuspendModal() {
    document.getElementById('suspendModal').style.display = 'none';
    document.body.style.overflow = 'auto';
}

function closeBanModal() {
    document.getElementById('banModal').style.display = 'none';
    document.body.style.overflow = 'auto';
}

async function updateUserStatusDirect(userId, action, reason = null) {
    try {
        const body = { Action: action }; 
        if (reason) {
            body.Reason = reason; 
        }
        
        const response = await fetch(`/api/admin/users/${userId}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(body)
        });
        
        if (!response.ok) {
            const errorData = await response.text();
            throw new Error(`Failed to update user status: ${errorData}`);
        }
        
        // Show action-specific success messages
        let message = '';
        switch (action) {
            case 'ban':
                message = 'User has been banned permanently';
                break;
            case 'suspend':
                message = 'User has been suspended successfully';
                break;
            case 'activate':
                message = 'User has been activated successfully';
                break;
            default:
                message = `User status updated successfully`;
        }
        
        showNotification(message, 'success');
        
        // Force reload users to show updated status
        await loadUsers();
        
    } catch (error) {
        console.error('[Admin Users] Error updating user status:', error);
        showNotification('Failed to update user status', 'error');
    }
}

function performSearch() {
    const searchTerm = document.getElementById('searchInput').value.trim();
    console.log('[Admin Users] Performing search:', searchTerm);
    
    // Disable search button during search
    const searchBtn = document.getElementById('searchBtn');
    const originalText = searchBtn.textContent;
    searchBtn.disabled = true;
    searchBtn.textContent = 'Searching...';
    
    currentSearch = searchTerm;
    currentPage = 1; 
    
    loadUsers().finally(() => {
        // Re-enable search button
        searchBtn.disabled = false;
        searchBtn.textContent = originalText;
    });
}

function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
}

function showCreateUserModal() {
    console.log('[Admin Users] Showing create user modal');
    
    // Clear form
    document.getElementById('createUserForm').reset();
    
    // Show modal
    document.getElementById('createUserModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
    
    // Focus on first input
    setTimeout(() => {
        document.getElementById('newFirstName').focus();
    }, 100);
}

function closeCreateUserModal() {
    document.getElementById('createUserModal').style.display = 'none';
    document.body.style.overflow = 'auto';
}

async function handleCreateUser() {
    console.log('[Admin Users] Handling create user');
    
    const form = document.getElementById('createUserForm');
    const formData = new FormData(form);
    
    // Validate required fields
    const firstName = formData.get('firstName')?.trim();
    const lastName = formData.get('lastName')?.trim();
    const username = formData.get('username')?.trim();
    const email = formData.get('email')?.trim();
    const password = formData.get('password')?.trim();
    const role = formData.get('role');
    
    if (!firstName || !lastName || !username || !email || !password || !role) {
        showNotification('Please fill in all required fields', 'error');
        return;
    }
    
    if (password.length < 6) {
        showNotification('Password must be at least 6 characters long', 'error');
        return;
    }
    
    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showNotification('Please enter a valid email address', 'error');
        return;
    }
    
    try {
        const createBtn = document.getElementById('createUserSubmit');
        createBtn.disabled = true;
        createBtn.textContent = 'Creating User...';
        
        // Prepare user data
        const userData = {
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            isAdmin: role === 'admin',
            bio: formData.get('bio')?.trim() || '',
            sendWelcomeEmail: formData.get('sendWelcomeEmail') === 'on'
        };
        
        console.log('[Admin Users] Creating user with data:', userData);
        
        // Create user via API
        const response = await fetch('/api/admin/users', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userData)
        });
        
        if (!response.ok) {
            const errorData = await response.text();
            throw new Error(`Failed to create user: ${errorData}`);
        }
        
        const result = await response.json();
        console.log('[Admin Users] User created successfully:', result);
        
        // Close modal and refresh users list
        closeCreateUserModal();
        showNotification(`User "${firstName} ${lastName}" created successfully!`, 'success');
        
        // Reload users to show the new user
        currentPage = 1; 
        await loadUsers();
        
    } catch (error) {
        console.error('[Admin Users] Error creating user:', error);
        showNotification(error.message || 'Failed to create user', 'error');
    } finally {
        const createBtn = document.getElementById('createUserSubmit');
        createBtn.disabled = false;
        createBtn.textContent = 'Create User';
    }
}

function showNotification(message, type = 'info') {
    // Use the same notification system as admin.js
    if (typeof showAdminNotification === 'function') {
        showAdminNotification(message, type); 
    } else {
        EasyBites.toast(message, type); 
    }
}