const AdminApp = {
  types: ['Cardio', 'Strength', 'Yoga'],
  roles: ['User', 'Admin'],

  showBanner(message, type = 'danger') {
    const banner = document.getElementById('adminStatusBanner');
    if (!banner) return;
    banner.innerHTML = `
      <div class="alert alert-${type} d-flex justify-content-between align-items-center shadow-sm">
        <span>${message}</span>
        <a href="/index.html" class="btn btn-dark btn-sm">На сторінку входу</a>
      </div>`;
  },

  async init() {
    const userId = ApiClient.getUserId();
    if (!userId) {
      this.showBanner('Сесія не знайдена. Увійдіть під адміністратором на головній сторінці.');
      return;
    }

    try {
      const users = await ApiClient.get('/api/admin/users');
      this.renderUsers(users);
      await this.loadCatalogs();
    } catch (err) {
      this.showBanner(`Сервер відхилив доступ: ${err.message}`);
    }
  },

  renderUsers(users) {
    const currentId = ApiClient.getUserId();

    document.getElementById('usersTable').innerHTML = users.map(u => {
      const roleLabel = typeof u.role === 'number' ? this.roles[u.role] : u.role;
      const isAdmin = roleLabel === 'Admin';
      const isSelfOrRoot = u.id === currentId || u.email === 'admin@fitness.local';

      const roleBadge = isAdmin
        ? `<span class="badge bg-warning text-dark">Admin</span>`
        : `<span class="badge bg-secondary">User</span>`;

      let actionsHtml = '';
      if (isSelfOrRoot) {
        actionsHtml = `<span class="text-muted fst-italic">Поточний / Головний</span>`;
      } else {
        const roleBtn = !isAdmin
          ? `<button class="btn btn-outline-warning btn-sm py-0 me-1" title="Надати права адміністратора" onclick="AdminApp.promoteUser('${u.id}', '${u.email}')">⬆️ Адмін</button>`
          : `<button class="btn btn-outline-secondary btn-sm py-0 me-1" title="Понизити до звичайного користувача" onclick="AdminApp.demoteUser('${u.id}', '${u.email}')">⬇️ Юзер</button>`;
        const deleteBtn = `<button class="btn btn-outline-danger btn-sm py-0" title="Видалити користувача" onclick="AdminApp.deleteUser('${u.id}', '${u.email}')">🗑️</button>`;
        actionsHtml = roleBtn + deleteBtn;
      }

      return `<tr>
        <td>${u.email}</td>
        <td>${roleBadge}</td>
        <td>${u.weight ?? 0} кг</td>
        <td class="text-end">${actionsHtml}</td>
      </tr>`;
    }).join('');
  },

  async promoteUser(id, email) {
    if (!confirm(`Надати права адміністратора користувачу ${email}?`)) return;
    try {
      await ApiClient.put(`/api/admin/users/${id}/promote`);
      await this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async demoteUser(id, email) {
    if (!confirm(`Понизити ${email} до звичайного користувача (User)?`)) return;
    try {
      await ApiClient.put(`/api/admin/users/${id}/demote`);
      await this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async deleteUser(id, email) {
    if (!confirm(`Видалити користувача ${email} та всі його тренування?`)) return;
    try {
      await ApiClient.delete(`/api/admin/users/${id}`);
      await this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async loadCatalogs() {
    const [exercises, workouts] = await Promise.all([
      ApiClient.get('/api/exercises'),
      ApiClient.get('/api/workouts')
    ]);

    document.getElementById('exList').innerHTML = exercises.map(e =>
      `<li class="list-group-item d-flex justify-content-between align-items-center px-0">
        <span>${e.name} (${this.types[e.category] ?? e.category}, MET ${e.baseMetValue})</span>
        <button class="btn btn-sm btn-outline-danger py-0" onclick="AdminApp.delEx('${e.id}')">×</button>
      </li>`).join('');

    document.getElementById('wtList').innerHTML = workouts.map(w =>
      `<li class="list-group-item d-flex justify-content-between align-items-center px-0">
        <span>${w.title} (${this.types[w.type] ?? w.type})</span>
        <button class="btn btn-sm btn-outline-danger py-0" onclick="AdminApp.delWt('${w.id}')">×</button>
      </li>`).join('');
  },

  async reloadAll() {
    const users = await ApiClient.get('/api/admin/users');
    this.renderUsers(users);
    await this.loadCatalogs();
  },

  async addExercise() {
    try {
      const baseMetValue = parseFloat(document.getElementById('exMet').value);
      if (isNaN(baseMetValue) || baseMetValue <= 0) return alert('MET має бути більшим за 0');

      await ApiClient.post('/api/admin/exercises', {
        name: document.getElementById('exName').value.trim(),
        category: parseInt(document.getElementById('exCat').value),
        baseMetValue,
        description: document.getElementById('exDesc').value.trim()
      });
      document.getElementById('exName').value = '';
      this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async delEx(id) {
    try {
      await ApiClient.delete(`/api/admin/exercises/${id}`);
      this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async addWorkout() {
    try {
      await ApiClient.post('/api/admin/workouts', {
        title: document.getElementById('wtTitle').value.trim(),
        type: parseInt(document.getElementById('wtType').value),
        difficulty: parseInt(document.getElementById('wtDiff').value)
      });
      document.getElementById('wtTitle').value = '';
      this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async delWt(id) {
    try {
      await ApiClient.delete(`/api/admin/workouts/${id}`);
      this.reloadAll();
    } catch (err) {
      alert(err.message);
    }
  },

  async addChallenge() {
    try {
      const targetCalories = parseFloat(document.getElementById('chCal').value);
      const durationDays = parseInt(document.getElementById('chDays').value);
      if (isNaN(targetCalories) || targetCalories <= 0 || isNaN(durationDays) || durationDays <= 0) {
        return alert('Значення мають бути більшими за 0');
      }

      await ApiClient.post('/api/admin/challenges', {
        title: document.getElementById('chTitle').value.trim(),
        targetCalories,
        durationDays
      });
      alert('Челендж створено!');
      document.getElementById('chTitle').value = '';
    } catch (err) {
      alert(err.message);
    }
  },

  async loadAvgReport() {
    try {
      const from = document.getElementById('repFrom').value;
      const to = document.getElementById('repTo').value;
      const data = await ApiClient.get(`/api/reports/average-activity?from=${from}&to=${to}`);
      document.getElementById('avgResult').innerHTML =
        `Середній час на користувача: <strong>${data.avgMinutesPerUser} хв</strong> | Середні витрати: <strong>${data.avgCaloriesPerUser} ккал</strong>`;
    } catch (err) {
      document.getElementById('avgResult').textContent = err.message;
    }
  }
};

window.addEventListener('DOMContentLoaded', () => AdminApp.init());