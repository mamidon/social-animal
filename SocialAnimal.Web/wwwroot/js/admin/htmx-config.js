/**
 * HTMX Configuration for Social Animal Admin Portal
 */

document.addEventListener('DOMContentLoaded', function() {
    
    // Configure HTMX defaults
    htmx.config.globalViewTransitions = true;
    htmx.config.requestTimeout = 10000; // 10 seconds
    htmx.config.scrollBehavior = 'smooth';
    
    // Add CSRF token to all requests
    document.addEventListener('htmx:configRequest', function(evt) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            evt.detail.headers['RequestVerificationToken'] = token.value;
        }
    });
    
    // Show loading indicators
    document.addEventListener('htmx:beforeRequest', function(evt) {
        // Add loading class to trigger element
        evt.target.classList.add('htmx-loading');
        
        // Show global loading indicator if present
        const loader = document.getElementById('global-loader');
        if (loader) {
            loader.style.display = 'block';
        }
    });
    
    // Hide loading indicators
    document.addEventListener('htmx:afterRequest', function(evt) {
        // Remove loading class from trigger element
        evt.target.classList.remove('htmx-loading');
        
        // Hide global loading indicator
        const loader = document.getElementById('global-loader');
        if (loader) {
            loader.style.display = 'none';
        }
    });
    
    // Handle errors
    document.addEventListener('htmx:responseError', function(evt) {
        console.error('HTMX Request Error:', evt.detail);
        
        // Show error message
        const errorContainer = document.getElementById('error-container');
        if (errorContainer) {
            errorContainer.innerHTML = '<div class="alert alert-danger">Request failed. Please try again.</div>';
            errorContainer.style.display = 'block';
        }
    });
    
    // Handle successful requests
    document.addEventListener('htmx:afterSwap', function(evt) {
        // Re-initialize any JavaScript components in the swapped content
        initializeComponents(evt.target);
        
        // Auto-hide success messages after 5 seconds
        const alerts = evt.target.querySelectorAll('.alert-success[data-auto-hide="true"]');
        alerts.forEach(alert => {
            setTimeout(() => {
                alert.style.opacity = '0';
                setTimeout(() => alert.remove(), 300);
            }, 5000);
        });
    });
    
    // Handle form validation errors
    document.addEventListener('htmx:afterSettle', function(evt) {
        // Focus on first error field if present
        const firstError = evt.target.querySelector('.is-invalid');
        if (firstError) {
            firstError.focus();
        }
    });
    
});

/**
 * Initialize JavaScript components in a container
 */
function initializeComponents(container = document) {
    // Initialize Bootstrap components
    const tooltips = container.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltips.forEach(tooltip => {
        new bootstrap.Tooltip(tooltip);
    });
    
    const popovers = container.querySelectorAll('[data-bs-toggle="popover"]');
    popovers.forEach(popover => {
        new bootstrap.Popover(popover);
    });
    
    // Initialize any custom components here
}

/**
 * Utility function to make HTMX requests programmatically
 */
function htmxRequest(method, url, data = null, target = null) {
    const options = {
        method: method.toUpperCase(),
        url: url,
        swap: 'innerHTML'
    };
    
    if (target) {
        options.target = target;
    }
    
    if (data) {
        options.values = data;
    }
    
    return htmx.ajax(options.method, options.url, {
        target: options.target,
        swap: options.swap,
        values: options.values
    });
}

/**
 * Show a toast notification
 */
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toast-container');
    if (!toastContainer) return;
    
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center text-bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                    data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    const bootstrapToast = new bootstrap.Toast(toast);
    bootstrapToast.show();
    
    // Remove from DOM after it's hidden
    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}