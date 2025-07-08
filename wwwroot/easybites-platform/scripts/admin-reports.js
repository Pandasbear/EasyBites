// Admin Reports Management Functionality
let currentPage = 1;
let currentStatus = '';
let currentType = '';
let currentReportId = null;

document.addEventListener('DOMContentLoaded', function() {
    initializeReportsManagement();
});

function initializeReportsManagement() {
    console.log('[Admin Reports] Initializing reports management...');
    setupEventListeners();
    loadReports();
}

function setupEventListeners() {
    const statusFilter = document.getElementById('statusFilter');
    const typeFilter = document.getElementById('typeFilter');
    const refreshBtn = document.getElementById('refreshBtn');
    const prevPageBtn = document.getElementById('prevPageBtn');
    const nextPageBtn = document.getElementById('nextPageBtn');
    
    // Main modal controls
    const reportModal = document.getElementById('reportModal');
    const notesModal = document.getElementById('notesModal');
    const closeBtn = reportModal.querySelector('.close');
    const modalCloseBtn = reportModal.querySelector('.modal-close');
    const resolveBtn = document.getElementById('resolveBtn');
    const dismissBtn = document.getElementById('dismissBtn');
    const escalateBtn = document.getElementById('escalateBtn');
    
    // Notes modal controls
    const notesCloseBtn = notesModal.querySelector('.close');
    const notesModalCloseBtn = notesModal.querySelector('.modal-close');
    const saveNotesBtn = document.getElementById('saveNotesBtn');
    
    statusFilter.addEventListener('change', function() {
        currentStatus = this.value;
        currentPage = 1;
        loadReports();
    });
    
    typeFilter.addEventListener('change', function() {
        currentType = this.value;
        currentPage = 1;
        loadReports();
    });
    
    refreshBtn.addEventListener('click', function() {
        currentPage = 1;
        loadReports();
    });
    
    prevPageBtn.addEventListener('click', function() {
        if (currentPage > 1) {
            currentPage--;
            loadReports();
        }
    });
    
    nextPageBtn.addEventListener('click', function() {
        currentPage++;
        loadReports();
    });
    
    // Modal events
    closeBtn.addEventListener('click', closeReportModal);
    modalCloseBtn.addEventListener('click', closeReportModal);
    notesCloseBtn.addEventListener('click', closeNotesModal);
    notesModalCloseBtn.addEventListener('click', closeNotesModal);
    
    window.addEventListener('click', function(e) {
        if (e.target === reportModal) closeReportModal();
        if (e.target === notesModal) closeNotesModal();
    });
    
    resolveBtn.addEventListener('click', function() {
        updateReportStatus('resolved');
    });
    
    dismissBtn.addEventListener('click', function() {
        updateReportStatus('dismissed');
    });
    
    escalateBtn.addEventListener('click', function() {
        showNotesModal('escalate');
    });
    
    saveNotesBtn.addEventListener('click', saveAdminNotes);
}

async function loadReports() {
    console.log('[Admin Reports] Loading reports...', { page: currentPage, status: currentStatus, type: currentType });
    
    try {
        const params = new URLSearchParams({
            page: currentPage,
            limit: 20
        });
        
        if (currentStatus) params.append('status', currentStatus);
        if (currentType) params.append('type', currentType);
        
        const reports = await EasyBites.api(`/api/admin/reports?${params}`);
        
        console.log('[Admin Reports] Reports loaded:', reports);
        
        displayReports(reports);
        updatePaginationControls(reports);
        
    } catch (error) {
        console.error('[Admin Reports] Error loading reports:', error);
        
        let errorMessage = 'Failed to load reports';
        
        if (error.message && /(unauthorized|not authenticated|session expired)/i.test(error.message)) {
            errorMessage = 'Session expired. Redirecting to admin login...';
            setTimeout(() => window.location.href = 'admin-login.html', 1500);
        } else if (error.message && error.message.includes('404')) {
            errorMessage = 'Reports endpoint not found';
        }
        
        showNotification(errorMessage, 'error');

        document.getElementById('reportsTableBody').innerHTML = 
            `<tr><td colspan="7" class="error-row">${errorMessage}</td></tr>`;
    }
}

function displayReports(reports) {
    const tbody = document.getElementById('reportsTableBody');
    
    if (reports.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="no-data-row">No reports found</td></tr>';
        return;
    }
    
    tbody.innerHTML = reports.map(report => {
        const statusClass = getStatusClass(report.status);
        const submittedDate = new Date(report.createdAt).toLocaleDateString();
        const priority = getPriority(report.reportType);
        const reportedItem = getReportedItem(report);
        
        return `
            <tr>
                <td>
                    <span class="type-badge ${getTypeClass(report.reportType)}">
                        ${formatReportType(report.reportType)}
                    </span>
                </td>
                <td>
                    <div class="reported-item">
                        ${reportedItem}
                    </div>
                </td>
                <td>
                    ${report.reporterUserId ? `User ID: ${report.reporterUserId}` : 'Anonymous'}
                </td>
                <td>
                    <span class="status-badge ${statusClass}">${report.status || 'pending'}</span>
                </td>
                <td>
                    <span class="priority-badge ${priority.class}">${priority.text}</span>
                </td>
                <td>${submittedDate}</td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-sm btn-primary" onclick="viewReport(${report.id})">View</button>
                        ${report.status === 'pending' ? 
                            `<button class="btn btn-sm btn-success" onclick="quickResolve(${report.id})">Resolve</button>
                             <button class="btn btn-sm btn-warning" onclick="quickDismiss(${report.id})">Dismiss</button>` : ''}
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

function updatePaginationControls(reports) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');
    
    prevBtn.disabled = currentPage <= 1;
    nextBtn.disabled = reports.length < 20;
    pageInfo.textContent = `Page ${currentPage}`;
}

function getStatusClass(status) {
    switch (status) {
        case 'pending': return 'status-pending';
        case 'reviewed': return 'status-warning';
        case 'resolved': return 'status-approved';
        case 'dismissed': return 'status-rejected';
        default: return 'status-pending';
    }
}

function getTypeClass(type) {
    switch (type) {
        case 'inappropriate_content': return 'type-inappropriate';
        case 'spam': return 'type-spam';
        case 'harassment': return 'type-harassment';
        case 'copyright': return 'type-copyright';
        default: return 'type-other';
    }
}

function formatReportType(type) {
    return type.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
}

function getPriority(type) {
    switch (type) {
        case 'harassment':
        case 'inappropriate_content':
            return { text: 'High', class: 'priority-high' };
        case 'spam':
        case 'copyright':
            return { text: 'Medium', class: 'priority-medium' };
        default:
            return { text: 'Low', class: 'priority-low' };
    }
}

function getReportedItem(report) {
    if (report.reportedRecipeId) {
        return `Recipe ID: ${report.reportedRecipeId}`;
    } else if (report.reportedUserId) {
        return `User ID: ${report.reportedUserId}`;
    } else {
        return 'General Report';
    }
}

async function viewReport(reportId) {
    console.log('[Admin Reports] Viewing report:', reportId, 'Type:', typeof reportId);
    currentReportId = reportId;
    
    try {
        const modalBody = document.getElementById('reportModalBody');
        modalBody.innerHTML = `<div class="loading">Loading report details...</div>`;
        showReportModal();
        
        console.log('[Admin Reports] Making API call to:', `/api/admin/reports/${reportId}`);
        
        // Use the dedicated endpoint to get a single report by ID
        const report = await EasyBites.api(`/api/admin/reports/${reportId}`);
        
        console.log('[Admin Reports] API response:', report);
        console.log('[Admin Reports] Report object keys:', Object.keys(report || {}));
        
        if (!report) {
            console.log('[Admin Reports] Report is null or undefined');
            modalBody.innerHTML = `<div class="error">Report not found</div>`;
            return;
        }

        const submittedDate = new Date(report.createdAt).toLocaleString();
        const priority = getPriority(report.reportType);
        const reportedItem = getReportedItem(report);
        
        modalBody.innerHTML = `
            <div class="report-details">
                <div class="report-header">
                    <h3>${formatReportType(report.reportType)}</h3>
                    <span class="status-badge ${getStatusClass(report.status)}">${report.status || 'pending'}</span>
                </div>
                
                <div class="report-meta">
                    <p><strong>Reported Item:</strong> ${reportedItem}</p>
                    <p><strong>Reporter:</strong> ${report.reporterUserId ? `User ID: ${report.reporterUserId}` : 'Anonymous'}</p>
                    <p><strong>Priority:</strong> <span class="priority-badge ${priority.class}">${priority.text}</span></p>
                    <p><strong>Submitted:</strong> ${submittedDate}</p>
                </div>
                
                <div class="report-description">
                    <h4>Description:</h4>
                    <div class="report-text">${escapeHtml(report.description || 'No description provided')}</div>
                </div>
                
                ${report.adminNotes ? `
                <div class="admin-notes">
                    <h4>Admin Notes:</h4>
                    <div class="notes-text">${escapeHtml(report.adminNotes)}</div>
                </div>
                ` : ''}
                
                ${report.reviewedAt ? `
                <div class="review-info">
                    <p><strong>Reviewed:</strong> ${new Date(report.reviewedAt).toLocaleString()}</p>
                    ${report.reviewedByAdminId ? `<p><strong>Reviewed by:</strong> Admin ID: ${report.reviewedByAdminId}</p>` : ''}
                </div>
                ` : ''}
            </div>
        `;
        
        // Get references to the action buttons in the modal footer
        const resolveBtn = document.getElementById('resolveBtn');
        const dismissBtn = document.getElementById('dismissBtn');
        const escalateBtn = document.getElementById('escalateBtn');

        // Determine if the report is already resolved or dismissed
        const isResolvedOrDismissed = report.status === 'resolved' || report.status === 'dismissed';

        // Conditionally hide/show buttons
        if (resolveBtn) resolveBtn.style.display = isResolvedOrDismissed ? 'none' : 'inline-block';
        if (dismissBtn) dismissBtn.style.display = isResolvedOrDismissed ? 'none' : 'inline-block';
        if (escalateBtn) escalateBtn.style.display = isResolvedOrDismissed ? 'none' : 'inline-block';
        
    } catch (error) {
        console.error('[Admin Reports] Error viewing report:', error);
        console.error('[Admin Reports] Error details:', {
            message: error.message,
            stack: error.stack,
            name: error.name,
            status: error.status
        });
        
        let errorMessage = 'Failed to load report details';
        
        if (error.message && error.message.includes('404')) {
            errorMessage = 'Report not found';
        } else if (error.message && /(unauthorized|not authenticated|session expired)/i.test(error.message)) {
            errorMessage = 'Session expired. Please log in again.';
            setTimeout(() => window.location.href = 'admin-login.html', 1500);
        } else if (error.status) {
            errorMessage = `HTTP Error ${error.status}: ${error.message}`;
        }
        
        showNotification(errorMessage, 'error');
        document.getElementById('reportModalBody').innerHTML = `<div class="error">${errorMessage}<br><small>Check console for details</small></div>`;
    }
}

async function quickResolve(reportId) {
    if (confirm('Are you sure you want to resolve this report?')) {
        await updateReportStatusDirect(reportId, 'resolved');
    }
}

async function quickDismiss(reportId) {
    if (confirm('Are you sure you want to dismiss this report?')) {
        await updateReportStatusDirect(reportId, 'dismissed');
    }
}

async function updateReportStatusDirect(reportId, status, adminNotes = null) {
    console.log('[Admin Reports] Updating report status:', { reportId, status, adminNotes });
    
    try {
        const body = { Status: status };
        body.AdminNotes = adminNotes || ''; // Ensure AdminNotes is always a string
        
        console.log('[Admin Reports] Sending PUT request to:', `/api/admin/reports/${reportId}`, body);
        
        const response = await fetch(`/api/admin/reports/${reportId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(body)
        });
        
        console.log('[Admin Reports] Update response status:', response.status);
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('[Admin Reports] Update failed with error:', errorText);
            throw new Error(errorText || 'Failed to update report');
        }
        
        showNotification(`Report ${status} successfully`, 'success');
        loadReports();
        
    } catch (error) {
        console.error('[Admin Reports] Error updating report:', error);
        showNotification('Failed to update report', 'error');
    }
}

async function updateReportStatus(status) {
    if (!currentReportId) return;
    
    if (status === 'resolved' && !confirm('Are you sure you want to resolve this report?')) {
        return;
    }
    if (status === 'dismissed' && !confirm('Are you sure you want to dismiss this report?')) {
        return;
    }
    
    await updateReportStatusDirect(currentReportId, status);
    closeReportModal();
}

function showNotesModal(action = '') {
    document.getElementById('adminNotes').value = '';
    document.getElementById('notesModal').style.display = 'block';
    
    // Store the action for later use
    document.getElementById('saveNotesBtn').dataset.action = action;
}

async function saveAdminNotes() {
    if (!currentReportId) return;
    
    const notes = document.getElementById('adminNotes').value.trim();
    if (!notes) {
        showNotification('Please enter admin notes', 'error');
        return;
    }
    
    const action = document.getElementById('saveNotesBtn').dataset.action;
    
    try {
        if (action === 'escalate') {
            await updateReportStatusDirect(currentReportId, 'reviewed', notes);
            showNotification('Report escalated with notes', 'success');
        } else {
            await updateReportStatusDirect(currentReportId, null, notes);
            showNotification('Admin notes saved', 'success');
        }
        
        closeNotesModal();
        closeReportModal();
    } catch (error) {
        showNotification('Failed to save notes', 'error');
    }
}

function showReportModal() {
    document.getElementById('reportModal').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeReportModal() {
    document.getElementById('reportModal').style.display = 'none';
    document.body.style.overflow = 'auto';
    currentReportId = null;
}

function closeNotesModal() {
    document.getElementById('notesModal').style.display = 'none';
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
        EasyBites.toast(message, type); 
    }
} 