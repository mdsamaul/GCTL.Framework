// Theme and Sidebar Manager

document.addEventListener('DOMContentLoaded', () => {
    // -----------------------------------------------------------------
    // Theme Management (Light / Dark / System)
    // -----------------------------------------------------------------
    const themeSelectButtons = document.querySelectorAll('[data-theme-value]');
    const activeThemeIcon = document.getElementById('active-theme-icon');

    const getStoredTheme = () => localStorage.getItem('theme') || 'system';
    const setStoredTheme = theme => localStorage.setItem('theme', theme);

    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme();
        if (storedTheme !== 'system') {
            return storedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    };

    const setTheme = theme => {
        if (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-theme', 'dark');
        } else if (theme === 'system') {
            document.documentElement.setAttribute('data-theme', 'light');
        } else {
            document.documentElement.setAttribute('data-theme', theme);
        }
        
        updateThemeUI(theme);
    };

    const updateThemeUI = (theme) => {
        // Remove active class from all buttons
        themeSelectButtons.forEach(btn => {
            btn.classList.remove('active');
            if (btn.getAttribute('data-theme-value') === theme) {
                btn.classList.add('active');
            }
        });

        // Update the top-nav theme toggle icon
        if (activeThemeIcon) {
            activeThemeIcon.className = ''; // Reset
            if (theme === 'dark') {
                activeThemeIcon.className = 'bi bi-moon-stars-fill';
            } else if (theme === 'light') {
                activeThemeIcon.className = 'bi bi-sun-fill';
            } else {
                activeThemeIcon.className = 'bi bi-laptop-fill';
            }
        }
    };

    // Initialize theme
    setTheme(getStoredTheme());

    // Listen for theme buttons clicks
    themeSelectButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const theme = btn.getAttribute('data-theme-value');
            setStoredTheme(theme);
            setTheme(theme);
        });
    });

    // Listen for OS theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const storedTheme = getStoredTheme();
        if (storedTheme === 'system') {
            setTheme('system');
        }
    });

    // -----------------------------------------------------------------
    // Collapsible Sidebar Management
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

    // Initialize sidebar state
    const isSidebarCollapsed = getStoredSidebarState();
    applySidebarState(isSidebarCollapsed);

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', () => {
            const collapsed = !sidebar.classList.contains('collapsed');
            setStoredSidebarState(collapsed);
            applySidebarState(collapsed);
        });
    }

    // -----------------------------------------------------------------
    // Live Clock (Center of Topnav)
    // -----------------------------------------------------------------
    const clockElement = document.getElementById('topnav-clock');
    if (clockElement) {
        const updateClock = () => {
            const now = new Date();
            const options = { 
                weekday: 'short', 
                year: 'numeric', 
                month: 'short', 
                day: 'numeric',
                hour: '2-digit', 
                minute: '2-digit', 
                second: '2-digit',
                hour12: true 
            };
            clockElement.textContent = now.toLocaleDateString('en-US', options);
        };
        updateClock();
        setInterval(updateClock, 1000);
    }

    // -----------------------------------------------------------------
    // Sidebar Accordion (Same-Level Auto Collapse / Shrink)
    // -----------------------------------------------------------------
    document.addEventListener('show.bs.collapse', (e) => {
        const targetCollapse = e.target;
        if (!targetCollapse.closest('.sidebar-menu')) return;

        // Get the immediate parent <ul> containing this menu item
        const parentUl = targetCollapse.closest('ul.sidebar-menu');
        if (!parentUl) return;

        // Find all sibling menu items at the exact same level
        const siblingItems = Array.from(parentUl.children).filter(li => li.classList.contains('menu-item'));
        siblingItems.forEach(li => {
            const siblingCollapse = li.querySelector(':scope > .collapse.show');
            if (siblingCollapse && siblingCollapse !== targetCollapse) {
                const bsCollapse = bootstrap.Collapse.getInstance(siblingCollapse) || new bootstrap.Collapse(siblingCollapse, { toggle: false });
                bsCollapse.hide();
            }
        });
    });
});

