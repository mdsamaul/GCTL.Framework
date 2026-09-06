// Theme, Sidebar and Live Clock Manager

document.addEventListener('DOMContentLoaded', () => {
    // -----------------------------------------------------------------
    // Theme Management (Light Mode)
    // -----------------------------------------------------------------
    document.documentElement.setAttribute('data-theme', 'light');

    // -----------------------------------------------------------------
    // Collapsible Sidebar Management (3-bar Hamburger Toggle)
    // -----------------------------------------------------------------
    const sidebar = document.getElementById('app-sidebar');
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const appWrapper = document.getElementById('app-wrapper');

    const getStoredSidebarState = () => localStorage.getItem('sidebar-collapsed') === 'true';
    const setStoredSidebarState = collapsed => localStorage.setItem('sidebar-collapsed', collapsed);

    const applySidebarState = (collapsed) => {
        if (collapsed) {
            sidebar?.classList.add('collapsed');
            appWrapper?.classList.add('sidebar-collapsed');
        } else {
            sidebar?.classList.remove('collapsed');
            appWrapper?.classList.remove('sidebar-collapsed');
        }
    };

    // Initialize sidebar state from localStorage (default to expanded)
    const isSidebarCollapsed = getStoredSidebarState();
    applySidebarState(isSidebarCollapsed);

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', (e) => {
            e.preventDefault();
            const collapsed = !sidebar.classList.contains('collapsed');
            setStoredSidebarState(collapsed);
            applySidebarState(collapsed);
        });
    }

    // -----------------------------------------------------------------
    // Multi-Level Flyout Menu Interaction (Collapsed Rail Mode)
    // -----------------------------------------------------------------
    document.addEventListener('click', (e) => {
        const sidebarEl = document.getElementById('app-sidebar');
        if (!sidebarEl) return;
        const isCollapsed = sidebarEl.classList.contains('collapsed');

        // Level 1: Rail item click in collapsed mode
        const railLink = e.target.closest('.rail-item.has-flyout > .rail-link');
        if (railLink && isCollapsed) {
            e.preventDefault();
            e.stopPropagation();
            const parentItem = railLink.closest('.rail-item');
            const wasOpen = parentItem.classList.contains('open');

            // Close other top-level flyouts
            sidebarEl.querySelectorAll('.rail-item.has-flyout').forEach(item => {
                if (item !== parentItem) {
                    item.classList.remove('open');
                    item.querySelectorAll('.flyout-item.has-nested').forEach(nested => nested.classList.remove('open'));
                }
            });

            if (wasOpen) {
                parentItem.classList.remove('open');
            } else {
                parentItem.classList.add('open');
            }
            return;
        }

        // Level 2: Nested flyout item click in collapsed mode
        const nestedLink = e.target.closest('.flyout-item.has-nested > .flyout-link');
        if (nestedLink && isCollapsed) {
            e.preventDefault();
            e.stopPropagation();
            const parentItem = nestedLink.closest('.flyout-item');
            const wasOpen = parentItem.classList.contains('open');

            const siblings = parentItem.parentElement ? parentItem.parentElement.querySelectorAll(':scope > .flyout-item.has-nested') : [];
            siblings.forEach(sibling => {
                if (sibling !== parentItem) {
                    sibling.classList.remove('open');
                }
            });

            if (wasOpen) {
                parentItem.classList.remove('open');
            } else {
                parentItem.classList.add('open');
            }
            return;
        }

        // Click outside sidebar closes all open flyouts
        if (!e.target.closest('#app-sidebar')) {
            sidebarEl.querySelectorAll('.rail-item.has-flyout.open').forEach(item => item.classList.remove('open'));
            sidebarEl.querySelectorAll('.flyout-item.has-nested.open').forEach(item => item.classList.remove('open'));
        }
    });

    // -----------------------------------------------------------------
    // Live Clocks (Dashboard vs. Other Pages Topnav Center)
    // -----------------------------------------------------------------
    const dashboardDateEl = document.getElementById('dashboard-date');
    const dashboardTimeEl = document.getElementById('dashboard-time');
    const topnavClockEl = document.getElementById('topnav-clock');

    const updateClocks = () => {
        const now = new Date();

        // 1. Dashboard specific Date & Time
        if (dashboardDateEl || dashboardTimeEl) {
            if (dashboardDateEl) {
                const dateOptions = { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' };
                dashboardDateEl.textContent = now.toLocaleDateString('en-US', dateOptions);
            }
            if (dashboardTimeEl) {
                const timeOptions = { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true };
                dashboardTimeEl.textContent = now.toLocaleTimeString('en-US', timeOptions);
            }
        }

        // 2. Other Pages Topnav Center Clock: "Sun, Sep 6, 2026, 09:35:52 AM"
        if (topnavClockEl) {
            const topnavOptions = {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            };
            topnavClockEl.textContent = now.toLocaleDateString('en-US', topnavOptions);
        }
    };

    updateClocks();
    setInterval(updateClocks, 1000);

    // -----------------------------------------------------------------
    // Change Password Modal Handler
    // -----------------------------------------------------------------
    const btnChangePassword = document.getElementById('btnChangePassword');
    const changePasswordModalEl = document.getElementById('changePasswordModal');

    if (btnChangePassword && changePasswordModalEl) {
        btnChangePassword.addEventListener('click', (e) => {
            e.preventDefault();
            const modal = bootstrap.Modal.getOrCreateInstance(changePasswordModalEl);
            modal.show();
        });

        const btnSavePassword = document.getElementById('btnSavePassword');
        if (btnSavePassword) {
            btnSavePassword.addEventListener('click', () => {
                const currentPass = document.getElementById('currentPassword')?.value;
                const newPass = document.getElementById('newPassword')?.value;
                const confirmPass = document.getElementById('confirmPassword')?.value;

                if (!currentPass || !newPass || !confirmPass) {
                    if (typeof toastr !== 'undefined') {
                        toastr.warning('Please fill in all password fields.');
                    } else {
                        alert('Please fill in all password fields.');
                    }
                    return;
                }

                if (newPass !== confirmPass) {
                    if (typeof toastr !== 'undefined') {
                        toastr.error('New password and confirmation do not match.');
                    } else {
                        alert('New password and confirmation do not match.');
                    }
                    return;
                }

                // Simulate password update
                if (typeof toastr !== 'undefined') {
                    toastr.success('Password changed successfully.');
                } else {
                    alert('Password changed successfully.');
                }

                document.getElementById('changePasswordForm')?.reset();
                const modal = bootstrap.Modal.getInstance(changePasswordModalEl);
                modal?.hide();
            });
        }
    }
});


