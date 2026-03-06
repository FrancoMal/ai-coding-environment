/* ============================================================
   Tu Marca - Authentication Module
   JWT token management, login/logout, session checks
   ============================================================ */

const Auth = (() => {
  const TOKEN_KEY = 'tm_token';
  const USER_KEY = 'tm_user';
  const EXPIRY_KEY = 'tm_expires';

  function saveSession(data) {
    localStorage.setItem(TOKEN_KEY, data.token);
    localStorage.setItem(USER_KEY, JSON.stringify({
      username: data.username,
      role: data.role
    }));
    if (data.expiresAt) {
      localStorage.setItem(EXPIRY_KEY, data.expiresAt);
    }
  }

  function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(EXPIRY_KEY);
  }

  function getToken() {
    return localStorage.getItem(TOKEN_KEY);
  }

  function getUser() {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }

  function isTokenValid() {
    const token = getToken();
    if (!token) return false;
    const expiresAt = localStorage.getItem(EXPIRY_KEY);
    if (!expiresAt) return true;
    return new Date(expiresAt) > new Date();
  }

  function getAuthHeader() {
    const token = getToken();
    return token ? 'Bearer ' + token : '';
  }

  function isAdmin() {
    const user = getUser();
    return user && user.role === 'admin';
  }

  async function login(username, password) {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
      const body = await response.json().catch(() => ({}));
      throw new Error(body.message || 'Error al iniciar sesion');
    }

    const data = await response.json();
    saveSession(data);
    return data;
  }

  function logout() {
    clearSession();
    window.location.href = '/login.html';
  }

  function requireAuth() {
    if (!isTokenValid()) {
      clearSession();
      window.location.href = '/login.html';
      return false;
    }
    return true;
  }

  function redirectIfAuthenticated() {
    if (isTokenValid()) {
      window.location.href = '/index.html';
      return true;
    }
    return false;
  }

  return {
    saveSession,
    clearSession,
    getToken,
    getUser,
    isTokenValid,
    getAuthHeader,
    isAdmin,
    login,
    logout,
    requireAuth,
    redirectIfAuthenticated
  };
})();
