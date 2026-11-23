const AdminApp = {
  types: ['Cardio', 'Strength', 'Yoga'],

  async init() {
    const userId = ApiClient.getUserId();
    if (!userId) {
      window.location.replace('/index.html');
      return;
    }

    try {
      const user = await ApiClient.get(`/api/profile/${userId}`);
      if (user.role !== 'Admin') {
        window.location.replace('/index.html');
        return;
      }
      await this.loadData();
    } catch {
      window.location.replace('/index.html');
    }
  },

  async loadData() {
    const [exercises, workouts, users] = await Promise.all([
      ApiClient.get('/api/exercises'),
      ApiClient.get('/api/workouts'),
      ApiClient.get('/api/admin/users')
    ]);

    document.getElementById('exList').innerHTML = exercises.map(e =>
      `<li class="list-group-item d-flex justify-content-between align-items-center px-0">
        <span>${e.name} (${this.types[e.category]}, MET ${e.baseMetValue})</span>
        <button class="btn btn-sm btn-outline-danger py-0" onclick="AdminApp.delEx('${e.id}')">×</button>
      </li>`).join('');

    document.getElementById('wtList').innerHTML = workouts.map(w =>
      `<li class="list-group-item d-flex justify-content-between align-items-center px-0">
        <span>${w.title} (${this.types[w.type]})</span>
        <button class="btn btn-sm btn-outline-danger py-0" onclick="AdminApp.delWt('${w.id}')">×</button>
      </li>`).join('');

    document.getElementById('usersTable').innerHTML = users.map(u =>
      `<tr><td>${u.email}</td><td><span class="badge bg-secondary">${u.role}</span></td><td>${u.weight} кг</td></tr>`).join('');
  },

  async addExercise() {
    await ApiClient.post('/api/admin/exercises', {
      name: document.getElementById('exName').value,
      category: parseInt(document.getElementById('exCat').value),
      baseMetValue: parseFloat(document.getElementById('exMet').value),
      description: document.getElementById('exDesc').value
    });
    document.getElementById('exName').value = '';
    this.loadData();
  },

  async delEx(id) {
    await ApiClient.delete(`/api/admin/exercises/${id}`);
    this.loadData();
  },

  async addWorkout() {
    await ApiClient.post('/api/admin/workouts', {
      title: document.getElementById('wtTitle').value,
      type: parseInt(document.getElementById('wtType').value),
      difficulty: parseInt(document.getElementById('wtDiff').value)
    });
    document.getElementById('wtTitle').value = '';
    this.loadData();
  },

  async delWt(id) {
    await ApiClient.delete(`/api/admin/workouts/${id}`);
    this.loadData();
  },

  async addChallenge() {
    await ApiClient.post('/api/admin/challenges', {
      title: document.getElementById('chTitle').value,
      targetCalories: parseFloat(document.getElementById('chCal').value),
      durationDays: parseInt(document.getElementById('chDays').value)
    });
    alert('Челендж створено!');
    document.getElementById('chTitle').value = '';
  },

  async loadAvgReport() {
    const from = document.getElementById('repFrom').value;
    const to = document.getElementById('repTo').value;
    const data = await ApiClient.get(`/api/reports/average-activity?from=${from}&to=${to}`);
    document.getElementById('avgResult').innerHTML =
      `Середній час на користувача: <strong>${data.avgMinutesPerUser} хв</strong> | Середні витрати: <strong>${data.avgCaloriesPerUser} ккал</strong>`;
  }
};

window.addEventListener('DOMContentLoaded', () => AdminApp.init());