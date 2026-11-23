const ApiClient = {
  getUserId() {
    // Очищуємо старий localStorage, щоб після білду не було автовходу
    localStorage.removeItem('fitnessUserId');
    localStorage.removeItem('fitnessUserRole');
    return sessionStorage.getItem('fitnessUserId') || '';
  },

  setUserId(id) {
    sessionStorage.setItem('fitnessUserId', id);
  },

  clearSession() {
    localStorage.removeItem('fitnessUserId');
    localStorage.removeItem('fitnessUserRole');
    sessionStorage.removeItem('fitnessUserId');
  },

  async request(url, options = {}) {
    const headers = {
      'Content-Type': 'application/json',
      ...(this.getUserId() ? { 'X-User-Id': this.getUserId() } : {}),
      ...options.headers
    };

    const response = await fetch(url, { ...options, headers });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || `HTTP помилка ${response.status}`);
    }

    const contentType = response.headers.get('content-type');
    return contentType && contentType.includes('application/json')
      ? response.json()
      : null;
  },

  get(url) {
    return this.request(url, { method: 'GET' });
  },

  post(url, body) {
    return this.request(url, { method: 'POST', body: JSON.stringify(body) });
  },

  delete(url) {
    return this.request(url, { method: 'DELETE' });
  }
};