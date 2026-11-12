/**
 * Accounts Tab Module
 */
'use strict';

window.DashboardAccounts = {
    currentSub: 'cb',

    initSubnav() {
        const subnav = document.getElementById('subnav');
        if (subnav && !subnav._hasBoundEvents) {
            subnav._hasBoundEvents = true;
            subnav.onclick = e => {
                const b = e.target.closest('button');
                if (!b) return;
                const sub = b.dataset.s;
                this.switchSub(sub);
            };
        }
    },

    switchSub(subId, filter = null) {
        if (!subId) return;
        const normalized = (subId === 'summary') ? 'cb' : subId;
        this.currentSub = normalized;

        sessionStorage.setItem('dashboard_accounts_subtab', normalized);
        if (window.history && window.history.replaceState) {
            window.history.replaceState(null, '', '#accounts/' + normalized);
        }

        d$$('#subnav button').forEach(x => {
            const s = x.dataset.s;
            x.classList.toggle('on', s === normalized || (s === 'cb' && normalized === 'summary'));
        });

        d$$('#p-accounts .sub').forEach(s => {
            s.classList.toggle('on', s.id === 's-' + normalized || (s.id === 's-cb' && normalized === 'summary'));
        });

        this.loadCurrentSub(filter);
    },

    loadCurrentSub(filter = null) {
        const activeFilter = filter || (window.DashboardApp ? window.DashboardApp.getFilterState() : {});
        if (this.currentSub === 'cb' || this.currentSub === 'summary') {
            if (window.DashboardCashBank) {
                window.DashboardCashBank.load(activeFilter);
            }
        }
    },

    async load(filter) {
        this.initSubnav();

        // Restore active sub-tab from URL hash or sessionStorage
        let targetSub = 'cb';
        const hash = (window.location.hash || '').replace('#', '').trim();
        if (hash.startsWith('accounts/')) {
            targetSub = hash.substring('accounts/'.length);
        } else if (hash.startsWith('accounts-')) {
            targetSub = hash.substring('accounts-'.length);
        } else {
            const stored = sessionStorage.getItem('dashboard_accounts_subtab');
            if (stored) targetSub = stored;
        }

        this.switchSub(targetSub, filter);
    }
};
