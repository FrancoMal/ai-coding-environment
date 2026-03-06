/* ============================================================
   Tu Marca - Dashboard SPA
   Minimal template: sidebar, dashboard, configuracion
   ============================================================ */

const App = (() => {
  /* ==========================================================
     STATE
     ========================================================== */
  let currentView = 'dashboard';

  /* ==========================================================
     SVG ICONS
     ========================================================== */
  const Icons = {
    brand: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>',
    dashboard: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>',
    settings: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/></svg>',
    refresh: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15"/></svg>',
    users: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>',
    status: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
    logout: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>',
    user: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>',
    code: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg>',
    check: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
    terminal: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="4 17 10 11 4 5"/><line x1="12" y1="19" x2="20" y2="19"/></svg>'
  };

  /* ==========================================================
     TOAST NOTIFICATIONS
     ========================================================== */
  function showToast(message, type) {
    type = type || 'info';
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = 'toast toast-' + type;
    toast.textContent = message;
    container.appendChild(toast);
    requestAnimationFrame(() => toast.classList.add('visible'));
    setTimeout(() => {
      toast.classList.remove('visible');
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  }

  /* ==========================================================
     RENDER SHELL
     ========================================================== */
  function renderShell() {
    const user = Auth.getUser() || { username: 'usuario', role: 'user' };
    const app = document.getElementById('app');

    app.innerHTML = ''
      + '<aside class="sidebar" id="sidebar">'
      +   '<div class="sidebar-brand">'
      +     '<span class="sidebar-brand-icon">' + Icons.brand + '</span>'
      +     '<span class="sidebar-brand-text">Tu Marca</span>'
      +   '</div>'
      +   '<nav class="sidebar-nav" role="navigation" aria-label="Menu principal">'
      +     '<a href="#" class="nav-item active" data-view="dashboard">'
      +       '<span class="nav-icon">' + Icons.dashboard + '</span>'
      +       '<span class="nav-label">Dashboard</span>'
      +     '</a>'
      +     '<a href="#" class="nav-item" data-view="config">'
      +       '<span class="nav-icon">' + Icons.settings + '</span>'
      +       '<span class="nav-label">Configuracion</span>'
      +     '</a>'
      +   '</nav>'
      +   '<div class="sidebar-user">'
      +     '<div class="sidebar-user-avatar">' + Icons.user + '</div>'
      +     '<div class="sidebar-user-info">'
      +       '<span class="sidebar-user-name">' + escapeHtml(user.username) + '</span>'
      +       '<span class="sidebar-user-role">' + escapeHtml(user.role) + '</span>'
      +     '</div>'
      +     '<button class="sidebar-logout-btn" id="logoutBtn" title="Cerrar sesion">'
      +       Icons.logout
      +     '</button>'
      +   '</div>'
      + '</aside>'
      + '<main class="main-wrapper">'
      +   '<header class="topbar">'
      +     '<div class="topbar-left">'
      +       '<button class="topbar-menu-btn" id="menuToggle" aria-label="Abrir menu">'
      +         '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="18" x2="21" y2="18"/></svg>'
      +       '</button>'
      +       '<h1 class="topbar-title">Tu Marca</h1>'
      +     '</div>'
      +     '<div class="topbar-actions">'
      +       '<button class="topbar-btn" id="refreshBtn" title="Actualizar">'
      +         Icons.refresh
      +       '</button>'
      +     '</div>'
      +   '</header>'
      +   '<div class="content" id="content"></div>'
      + '</main>'
      + '<div class="sidebar-overlay" id="sidebarOverlay"></div>'
      + '<div class="toast-container" id="toastContainer"></div>';

    bindShellEvents();
    navigateTo('dashboard');
  }

  /* ==========================================================
     SHELL EVENTS
     ========================================================== */
  function bindShellEvents() {
    // Navigation
    document.querySelectorAll('.nav-item[data-view]').forEach(function(item) {
      item.addEventListener('click', function(e) {
        e.preventDefault();
        const view = this.getAttribute('data-view');
        navigateTo(view);
        closeMobileMenu();
      });
    });

    // Logout
    document.getElementById('logoutBtn').addEventListener('click', function() {
      Auth.logout();
    });

    // Refresh
    document.getElementById('refreshBtn').addEventListener('click', function() {
      navigateTo(currentView);
      showToast('Datos actualizados', 'success');
    });

    // Mobile menu toggle
    var menuToggle = document.getElementById('menuToggle');
    var overlay = document.getElementById('sidebarOverlay');
    if (menuToggle) {
      menuToggle.addEventListener('click', toggleMobileMenu);
    }
    if (overlay) {
      overlay.addEventListener('click', closeMobileMenu);
    }
  }

  function toggleMobileMenu() {
    document.getElementById('sidebar').classList.toggle('open');
    document.getElementById('sidebarOverlay').classList.toggle('visible');
  }

  function closeMobileMenu() {
    document.getElementById('sidebar').classList.remove('open');
    document.getElementById('sidebarOverlay').classList.remove('visible');
  }

  /* ==========================================================
     NAVIGATION
     ========================================================== */
  function navigateTo(view) {
    currentView = view;

    // Update active nav item
    document.querySelectorAll('.nav-item').forEach(function(item) {
      item.classList.toggle('active', item.getAttribute('data-view') === view);
    });

    // Render the view
    if (view === 'dashboard') {
      renderDashboard();
    } else if (view === 'config') {
      renderConfig();
    }
  }

  /* ==========================================================
     DASHBOARD VIEW
     ========================================================== */
  function renderDashboard() {
    const user = Auth.getUser() || { username: 'usuario' };
    const content = document.getElementById('content');

    content.innerHTML = ''
      + '<div class="view-header">'
      +   '<h2>Bienvenido, ' + escapeHtml(user.username) + '</h2>'
      +   '<p class="view-subtitle">Panel de control</p>'
      + '</div>'
      + '<div class="stats-grid">'
      +   '<div class="stat-card stat-card--primary">'
      +     '<div class="stat-card-icon">' + Icons.users + '</div>'
      +     '<div class="stat-card-body">'
      +       '<span class="stat-card-value" id="statUsers">--</span>'
      +       '<span class="stat-card-label">Usuarios activos</span>'
      +     '</div>'
      +   '</div>'
      +   '<div class="stat-card stat-card--info">'
      +     '<div class="stat-card-icon">' + Icons.check + '</div>'
      +     '<div class="stat-card-body">'
      +       '<span class="stat-card-value" id="statStatus">--</span>'
      +       '<span class="stat-card-label">Estado del sistema</span>'
      +     '</div>'
      +   '</div>'
      + '</div>'
      + '<div class="template-message">'
      +   '<div class="template-message-icon">' + Icons.code + '</div>'
      +   '<h3>Este es tu template base</h3>'
      +   '<p>Usa <strong>Claude Code</strong> para personalizarlo y agregar las funcionalidades que necesites. '
      +   'Podes agregar nuevas paginas, conectar APIs, crear formularios y mucho mas.</p>'
      +   '<div class="template-suggestions">'
      +     '<span class="suggestion-tag">Agregar tablas de datos</span>'
      +     '<span class="suggestion-tag">Conectar una API externa</span>'
      +     '<span class="suggestion-tag">Crear formularios CRUD</span>'
      +     '<span class="suggestion-tag">Agregar graficos</span>'
      +     '<span class="suggestion-tag">Implementar notificaciones</span>'
      +   '</div>'
      + '</div>';

    loadDashboardStats();
  }

  async function loadDashboardStats() {
    try {
      const stats = await Api.getDashboardStats();
      var usersEl = document.getElementById('statUsers');
      var statusEl = document.getElementById('statStatus');
      if (usersEl && stats.totalUsers !== undefined) {
        usersEl.textContent = stats.totalUsers;
      }
      if (statusEl && stats.systemStatus) {
        statusEl.textContent = stats.systemStatus;
      }
    } catch (err) {
      // If API is unavailable, show placeholder values
      var usersEl = document.getElementById('statUsers');
      var statusEl = document.getElementById('statStatus');
      if (usersEl) usersEl.textContent = '1';
      if (statusEl) statusEl.textContent = 'Activo';
    }
  }

  /* ==========================================================
     CONFIGURACION VIEW
     ========================================================== */
  function renderConfig() {
    const user = Auth.getUser() || { username: 'usuario', role: 'user' };
    const content = document.getElementById('content');

    content.innerHTML = ''
      + '<div class="view-header">'
      +   '<h2>Configuracion</h2>'
      +   '<p class="view-subtitle">Informacion del sistema</p>'
      + '</div>'
      + '<div class="config-grid">'
      +   '<div class="card">'
      +     '<div class="card-header">'
      +       '<h3>Informacion del sistema</h3>'
      +     '</div>'
      +     '<div class="card-body">'
      +       '<table class="info-table">'
      +         '<tr><td class="info-label">Version</td><td class="info-value">1.0.0</td></tr>'
      +         '<tr><td class="info-label">Entorno</td><td class="info-value">Produccion</td></tr>'
      +         '<tr><td class="info-label">Nombre de la app</td><td class="info-value">Tu Marca</td></tr>'
      +         '<tr><td class="info-label">Framework</td><td class="info-value">Vanilla JS</td></tr>'
      +       '</table>'
      +     '</div>'
      +   '</div>'
      +   '<div class="card">'
      +     '<div class="card-header">'
      +       '<h3>Usuario actual</h3>'
      +     '</div>'
      +     '<div class="card-body">'
      +       '<table class="info-table">'
      +         '<tr><td class="info-label">Nombre</td><td class="info-value">' + escapeHtml(user.username) + '</td></tr>'
      +         '<tr><td class="info-label">Rol</td><td class="info-value">' + escapeHtml(user.role) + '</td></tr>'
      +         '<tr><td class="info-label">Sesion</td><td class="info-value"><span class="status-badge status-active">Activa</span></td></tr>'
      +       '</table>'
      +     '</div>'
      +   '</div>'
      + '</div>';
  }

  /* ==========================================================
     HELPERS
     ========================================================== */
  function escapeHtml(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
  }

  /* ==========================================================
     INIT
     ========================================================== */
  function init() {
    if (!Auth.requireAuth()) return;
    renderShell();
  }

  // Boot
  document.addEventListener('DOMContentLoaded', init);

  return { init, navigateTo, showToast };
})();
