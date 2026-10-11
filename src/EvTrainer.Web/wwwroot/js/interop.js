// The only hand-written JS of the app: storage, file download, theme and language (contracts/ui-shell.md).
window.evt = {
    storageGet(key) {
        try {
            return window.localStorage.getItem(key);
        } catch {
            return null;
        }
    },

    storageSet(key, value) {
        try {
            window.localStorage.setItem(key, value);
            return true;
        } catch {
            return false;
        }
    },

    storageRemove(key) {
        try {
            window.localStorage.removeItem(key);
        } catch {
            // Storage blocked: nothing to remove.
        }
    },

    downloadText(fileName, text) {
        const blob = new Blob([text], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },

    setTheme(theme) {
        if (theme === 'light' || theme === 'dark') {
            document.documentElement.setAttribute('data-theme', theme);
        } else {
            document.documentElement.removeAttribute('data-theme');
        }
    },

    // Sync (spec 008): tell .NET when the page is visible again, so coming back to the app syncs.
    onVisible(ref) {
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                ref.invokeMethodAsync('OnVisibleAsync').catch(() => { });
            }
        });
    },

    setLanguage(language) {
        document.documentElement.setAttribute('lang', language);
    },
};
