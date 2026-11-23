const App = {
  currentUser: null,
  progressChart: null,
  popularChart: null,

  async init() {
    const savedId = ApiClient.getUserId();
    if (!savedId) return;

    try {
      this.currentUser = await ApiClient.get(`/api/profile/${savedId}`);
      await this.showDashboard();
    } catch {
      this.logout(false);
    }
  },

  async login() {
    try {
      const email = document.getElementById('loginEmail').value.trim();
      const password = document.getElementById('loginPass').value;
      this.currentUser = await ApiClient.post('/api/auth/login', { email, password });
      ApiClient.setUserId(this.currentUser.id);
      await this.showDashboard();
    } catch (err) {
      alert(err.message);
    }
  },

  async register() {
    try {
      const payload = {
        email: document.getElementById('regEmail').value.trim(),
        password: document.getElementById('regPass').value,
        gender: parseInt(document.getElementById('regGender').value),
        birthDate: document.getElementById('regBirth').value,
        heightCm: parseFloat(document.getElementById('regHeight').value),
        weightKg: parseFloat(document.getElementById('regWeight').value)
      };
      this.currentUser = await ApiClient.post('/api/auth/register', payload);
      ApiClient.setUserId(this.currentUser.id);
      await this.showDashboard();
    } catch (err) {
      alert(err.message);
    }
  },

  logout(reload = true) {
    ApiClient.clearSession();
    if (reload) location.reload();
  },

  logout(reload = true) {
    localStorage.removeItem('fitnessUserId');
    if (reload) location.reload();
  },

  async showDashboard() {
    document.getElementById('authSection').classList.add('d-none');
    document.getElementById('dashboardSection').classList.remove('d-none');
    document.getElementById('btnLogout').classList.remove('d-none');
    document.getElementById('navUserEmail').textContent = this.currentUser.email;

    const adminBtn = document.getElementById('btnAdminPanel');
    if (this.currentUser.role === 'Admin') {
      adminBtn.classList.remove('d-none');
    } else {
      adminBtn.classList.add('d-none');
    }

    await this.refreshAll();
  },

  async refreshAll() {
    const uid = this.currentUser.id;
    const user = await ApiClient.get(`/api/profile/${uid}`);
    const p = user.profile;

    document.getElementById('profileSummary').innerHTML = `
      <div>Зріст: <strong>${p.heightCm} см</strong> | Вага: <strong>${p.weightKg} кг</strong></div>
      <div>ІМТ: <span class="badge bg-info text-dark">${p.bmi}</span></div>
    `;

    const workouts = await ApiClient.get('/api/workouts');
    const types = ['Кардіо', 'Силове', 'Йога'];
    document.getElementById('workoutSelect').innerHTML = workouts
      .map(w => `<option value="${w.id}">${w.title} (${types[w.type]})</option>`)
      .join('');

    const [programs, sessions] = await Promise.all([
      ApiClient.get(`/api/programs/${uid}`),
      ApiClient.get(`/api/sessions/${uid}`)
    ]);

    let programsHtml = programs.length
      ? programs.map(pr => `<div class="border-top pt-1 mt-1"><strong>${pr.title}</strong> (${pr.daysPerWeek} дн/тиж)</div>`).join('')
      : '<span class="text-muted">Ще немає згенерованих програм</span>';

    if (sessions.length) {
      programsHtml += `<div class="mt-2 pt-2 border-top"><span class="text-muted fw-bold">Останні тренування:</span>` +
        sessions.slice(0, 4).map(s =>
          `<div class="d-flex justify-content-between mt-1">
            <span>✅ ${s.workoutTemplate?.title || 'Тренування'} (${s.durationMinutes} хв)</span>
            <strong>${s.caloriesBurned} ккал</strong>
          </div>`).join('') + `</div>`;
    }
    document.getElementById('programsBox').innerHTML = programsHtml;

    const [challenges, achievements, leaderboard, reminders] = await Promise.all([
      ApiClient.get('/api/challenges'),
      ApiClient.get(`/api/achievements/${uid}`),
      ApiClient.get('/api/leaderboard'),
      ApiClient.get(`/api/reminders/${uid}`)
    ]);

    document.getElementById('challengesList').innerHTML = challenges.map(c => {
      const part = c.participants?.find(x => x.userId === uid);
      return `<div class="border-bottom pb-2 mb-2">
        <div class="fw-bold">${c.title}</div>
        <div class="text-muted">Ціль: ${c.targetCalories} ккал</div>
        ${part
          ? `<span class="badge bg-success">Прогрес: ${part.currentCalories} ккал</span>`
          : `<button class="btn btn-outline-primary btn-sm mt-1" onclick="App.joinChallenge('${c.id}')">Приєднатися</button>`}
      </div>`;
    }).join('');

    document.getElementById('achievementsList').innerHTML = achievements.length
      ? achievements.map(a => `<span class="badge bg-warning text-dark me-1 mb-1">🏅 ${a.achievement.name}</span>`).join('')
      : '<span class="small text-muted">Виконайте тренування для нагород</span>';

    document.getElementById('leaderboardList').innerHTML = leaderboard
      .map(l => `<li>${l.email} — <strong>${l.totalCalories} ккал</strong></li>`)
      .join('');

    document.getElementById('remindersList').innerHTML = reminders
      .map(r => `<li>⏰ ${r.scheduledTime} (${r.channel === 0 ? 'Email' : 'WebPush'})</li>`)
      .join('');

    await this.drawCharts(uid);
  },

  async logWeight() {
    const weightKg = parseFloat(document.getElementById('newWeightInput').value);
    if (!weightKg) return;
    await ApiClient.post(`/api/profile/${this.currentUser.id}/weight`, { weightKg });
    document.getElementById('newWeightInput').value = '';
    this.refreshAll();
  },

  async generateProgram() {
    const goal = parseInt(document.getElementById('goalSelect').value);
    await ApiClient.post('/api/programs/generate', { userId: this.currentUser.id, goal });
    this.refreshAll();
  },

  async completeWorkout() {
    const workoutTemplateId = document.getElementById('workoutSelect').value;
    const durationMinutes = parseInt(document.getElementById('durationInput').value);
    const data = await ApiClient.post('/api/sessions', {
      userId: this.currentUser.id,
      workoutTemplateId,
      durationMinutes
    });
    document.getElementById('lastSessionMsg').textContent = `Зараховано! Спалено ${data.caloriesBurned} ккал`;
    this.refreshAll();
  },

  async joinChallenge(id) {
    await ApiClient.post(`/api/challenges/${id}/join`, { userId: this.currentUser.id });
    this.refreshAll();
  },

  async addReminder() {
    const scheduledTime = document.getElementById('remTime').value;
    const channel = parseInt(document.getElementById('remChannel').value);
    await ApiClient.post('/api/reminders', {
      userId: this.currentUser.id,
      dayOfWeek: new Date().getDay(),
      scheduledTime,
      channel
    });
    this.refreshAll();
  },

  async drawCharts(uid) {
    const [prog, pop] = await Promise.all([
      ApiClient.get(`/api/reports/progress/${uid}`),
      ApiClient.get('/api/reports/popular')
    ]);

    if (this.progressChart) this.progressChart.destroy();
    const allDates = [...new Set([
      ...prog.weightHistory.map(x => x.date),
      ...prog.calorieHistory.map(x => x.date)
    ])].sort();

    this.progressChart = new Chart(document.getElementById('progressChart'), {
      type: 'line',
      data: {
        labels: allDates,
        datasets: [
          {
            label: 'Вага (кг)',
            data: prog.weightHistory.map(x => ({ x: x.date, y: x.weightKg })),
            borderColor: '#0d6efd',
            yAxisID: 'y',
            tension: 0.2
          },
          {
            label: 'Спалені калорії (ккал)',
            data: prog.calorieHistory.map(x => ({ x: x.date, y: x.caloriesBurned })),
            borderColor: '#198754',
            yAxisID: 'y1',
            tension: 0.2
          }
        ]
      },
      options: {
        scales: {
          y: { type: 'linear', position: 'left', title: { display: true, text: 'Кг' } },
          y1: { type: 'linear', position: 'right', grid: { drawOnChartArea: false }, title: { display: true, text: 'Ккал' } }
        }
      }
    });

    if (this.popularChart) this.popularChart.destroy();
    this.popularChart = new Chart(document.getElementById('popularChart'), {
      type: 'doughnut',
      data: {
        labels: pop.map(x => x.workoutTitle),
        datasets: [{ data: pop.map(x => x.sessionsCount) }]
      }
    });
  }
};

window.addEventListener('DOMContentLoaded', () => App.init());