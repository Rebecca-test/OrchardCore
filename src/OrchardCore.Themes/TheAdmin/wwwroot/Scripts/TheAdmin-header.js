/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

// We add some classes to the body tag to restore the sidebar to the state is was before reload.
// That state was saved to localstorage by userPreferencesPersistor.js
// We need to apply the classes BEFORE the page is rendered. 
// That is why we use a MutationObserver instead of document.Ready().
var observer = new MutationObserver(function (mutations) {
  for (var i = 0; i < mutations.length; i++) {
    for (var j = 0; j < mutations[i].addedNodes.length; j++) {
      if (mutations[i].addedNodes[j].tagName == 'BODY') {
        var body = mutations[i].addedNodes[j];
        var adminPreferences = JSON.parse(localStorage.getItem('adminPreferences'));

        if (adminPreferences != null) {
          if (adminPreferences.leftSidebarCompact == true) {
            body.className += ' left-sidebar-compact';
          }

          isCompactExplicit = adminPreferences.isCompactExplicit;

          if (adminPreferences.darkMode == true) {
            body.className += ' darkmode';
            document.getElementById("btn-darkmode").children[0].classList.add('fa-sun');
          }

          darkMode = adminPreferences.darkMode;
        } else {
          body.className += ' no-admin-preferences';
        } // we're done: 


        observer.disconnect();
      }

      ;
    }
  }
});
observer.observe(document.documentElement, {
  childList: true,
  subtree: true
});
var darkMode = darkMode === undefined ? false : darkMode;
var adminPreferences = JSON.parse(localStorage.getItem('adminPreferences'));
var persistedDarkMode = adminPreferences === null || adminPreferences === void 0 ? void 0 : adminPreferences.darkMode;

if (typeof persistedDarkMode !== 'undefined') {
  darkMode = persistedDarkMode;
}

if (document.getElementById('admin-darkmode')) {
  // Automatically sets darkmode based on OS preferences
  if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    if (typeof persistedDarkMode === 'undefined') {
      document.getElementById('admin-darkmode').setAttribute('media', 'all');
      document.getElementById('admin-default').setAttribute('media', 'not all');
    }
  }

  if (darkMode) {
    document.getElementById('admin-darkmode').setAttribute('media', 'all');
    document.getElementById('admin-default').setAttribute('media', 'not all');
  }
}