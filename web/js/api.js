/* ============================================================
   Tu Marca - API Client Module
   Minimal fetch wrapper with JWT authentication
   ============================================================ */

const Api = (() => {
  const BASE_URL = '/api';

  function headers(extra) {
    const h = { 'Content-Type': 'application/json' };
    if (extra) {
      Object.assign(h, extra);
    }
    const authHeader = Auth.getAuthHeader();
    if (authHeader) {
      h['Authorization'] = authHeader;
    }
    return h;
  }

  async function request(method, path, body) {
    const url = BASE_URL + path;
    const opts = { method, headers: headers() };
    if (body !== undefined && body !== null) {
      opts.body = JSON.stringify(body);
    }

    let response;
    try {
      response = await fetch(url, opts);
    } catch (err) {
      throw new Error('Error de red. Verifica tu conexion.');
    }

    if (response.status === 401) {
      Auth.clearSession();
      window.location.href = '/login.html';
      throw new Error('Sesion expirada');
    }

    let data = null;
    const ct = response.headers.get('content-type');
    if (ct && ct.includes('application/json')) {
      data = await response.json();
    }

    if (!response.ok) {
      const msg = (data && data.message) ? data.message : 'Error del servidor (' + response.status + ')';
      throw new Error(msg);
    }

    return data;
  }

  function get(path) { return request('GET', path); }
  function post(path, body) { return request('POST', path, body); }

  /* --- Endpoints --- */

  function getDashboardStats() {
    return get('/dashboard/stats');
  }

  function getMe() {
    return get('/auth/me');
  }

  return {
    get,
    post,
    getDashboardStats,
    getMe
  };
})();
