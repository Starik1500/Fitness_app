const App = {
  currentUser: null,
  isDemo: false,
  demoData: null,
  progressChart: null,
  popularChart: null,

  createTemplateDemoData() {
    return {
      email: 'tour.guest@demo.local',
      heightCm: 178,
      weightKg: 76.5,
      workouts: [
        { id: 'demo-w1', title: 'Ранкова пробіжка (Шаблон)', type: 0, met: 8.5 },
        { id: 'demo-w2', title: 'Силовий фулбоді (Шаблон)', type: 1, met: 6.0 },
        { id: 'demo-w3', title: 'Вечірня йога та розтяжка (Шаблон)', type: 2, met: 3.2 }
      ],
      programs: [
        { title: 'Базовий план: Схуднення та тонус (Шаблон)', startDate: '2026-11-23', endDate: '2026-12-21', workoutsCount: 4 }
      ],
      sessions: [
        { workoutTemplate: { title: 'Ранкова пробіжка' }, durationMinutes: 35, caloriesBurned: 380 },
        { workoutTemplate: { title: 'Силовий фулбоді' }, durationMinutes: 45, caloriesBurned: 345 }
      ],
      weightHistory: [
        { date: '2026-11-01', weightKg: 78.2 },
        { date: '2026-11-08', weightKg: 77.4 },
        { date: '2026-11-15', weightKg: 76.9 },
        { date: '2026-11-22', weightKg: 76.5 }
      ],
      calorieHistory: [
        { date: '2026-11-01', caloriesBurned: 310 },
        { date: '2026-11-08', caloriesBurned: 420 },
        { date: '2026-11-15', caloriesBurned: 345 },
        { date: '2026-11-22', caloriesBurned: 380 }
      ],
      popular: [
        { title: 'Ранкова пробіжка', sessionsCount: 14 },
        { title: 'Силовий фулбоді', sessionsCount: 11 },
        { title: 'Йога та мобільність', sessionsCount: 8 }
      ],
      challenges: [
        { id: 'demo-c1', title: 'Осінній марафон 3000 ккал', targetCalories: 3000, isJoined: true, currentCalories: 1455 },
        { id: 'demo-c2', title: 'Тиждень кардіо-витривалості', targetCalories: 1500, isJoined: false, currentCalories: 0 }
      ],
      achievements: ['Перший крок (1 тренування)', 'Втягнувся (5 тренувань)'],
      leaderboard: [
        { name: 'oleksandr.k', totalCalories: 4250 },
        { name: 'iryna.m', totalCalories: 3180 },
        { name: 'tour.guest (Ви у турі)', totalCalories: 1455 }
      ],
      reminders: [
        { cronExpression: 'Щодня о 18:30', channel: 0 },
        { cronExpression: 'Пн, Ср, Пт о 08:00', channel: 1 }
      ]
    };
  },

  clearAuthInputs() {
    ['loginEmail', 'loginPass', 'regEmail', 'regPass', 'regBirth', 'regHeight', 'regWeight'].forEach(id => {
      const el = document.getElementById(id);
      if (el) el.value = '';
    });
  },

  async init() {
    this.clearAuthInputs();
    setTimeout(() => this.clearAuthInputs(), 120);

    const savedId = ApiClient.getUserId();
    if (!savedId) return;

    try {
      this.currentUser = await ApiClient.get(`/api/profile/${savedId}`);
      this.isDemo = false;
      await this.showDashboard();
    } catch {
      this.logout(false);
    }
  },

  async startDemoTour() {
    this.isDemo = true;
    this.currentUser = null;
    this.demoData = this.createTemplateDemoData();
    await this.showDashboard();
  },

  async login() {
    try {
      const email = document.getElementById('loginEmail').value.trim();
      const password = document.getElementById('loginPass').value;
      this.currentUser = await ApiClient.post('/api/auth/login', { email, password });
      this.isDemo = false;
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
      this.isDemo = false;
      ApiClient.setUserId(this.currentUser.id);
      await this.showDashboard();
    } catch (err) {
      alert(err.message);
    }
  },

  logout(reload = true) {
    ApiClient.clearSession();
    this.isDemo = false;
    this.currentUser = null;
    if (reload) location.reload();
  },

  async showDashboard() {
    document.getElementById('authSection').classList.add('d-none');
    document.getElementById('dashboardSection').classList.remove('d-none');
    document.getElementById('btnLogout').classList.remove('d-none');

    const demoBanner = document.getElementById('demoModeBanner');
    const tourCallouts = document.querySelectorAll('.tour-callout');
    const adminBtn = document.getElementById('btnAdminPanel');

    if (this.isDemo) {
      document.getElementById('navUserEmail').textContent = 'Режим демо-туру (Шаблон)';
      demoBanner.classList.remove('d-none');
      tourCallouts.forEach(el => el.classList.remove('d-none'));
      adminBtn.classList.add('d-none');
      this.renderDemoDashboard();
      return;
    }

    demoBanner.classList.add('d-none');
    tourCallouts.forEach(el => el.classList.add('d-none'));
    document.getElementById('navUserEmail').textContent = this.currentUser.email;

    const isAdmin = this.currentUser.role === 1 || this.currentUser.role === 'Admin';
    if (isAdmin) {
      adminBtn.classList.remove('d-none');
    } else {
      adminBtn.classList.add('d-none');
    }

    await this.refreshAll();
  },

  renderDemoDashboard() {
    const d = this.demoData;
    const hMeters = d.heightCm / 100;
    const bmi = (d.weightKg / (hMeters * hMeters)).toFixed(1);

    document.getElementById('profileSummary').innerHTML = `
      <div>Зріст: <strong>${d.heightCm} см</strong> | Вага: <strong>${d.weightKg} кг</strong></div>
      <div>ІМТ: <span class="badge bg-info text-dark">${bmi}</span></div>
    `;

    const types = ['Кардіо', 'Силове', 'Йога'];
    document.getElementById('workoutSelect').innerHTML = d.workouts
      .map(w => `<option value="${w.id}">${w.title} (${types[w.type]})</option>`)
      .join('');

    let programsHtml = d.programs
      .map(pr => `<div class="border-top pt-1 mt-1"><strong>${pr.title}</strong> (${pr.workoutsCount} трен.)</div>`)
      .join('');

    programsHtml += `<div class="mt-2 pt-2 border-top"><span class="text-muted fw-bold">Останні тренування (приклад):</span>` +
      d.sessions.slice(0, 4).map(s =>
        `<div class="d-flex justify-content-between mt-1">
          <span>✅ ${s.workoutTemplate.title} (${s.durationMinutes} хв)</span>
          <strong>${s.caloriesBurned} ккал</strong>
        </div>`).join('') + `</div>`;
    document.getElementById('programsBox').innerHTML = programsHtml;

    document.getElementById('challengesList').innerHTML = d.challenges.map(c => `
      <div class="border-bottom pb-2 mb-2">
        <div class="fw-bold">${c.title}</div>
        <div class="text-muted">Ціль: ${c.targetCalories} ккал</div>
        ${c.isJoined
          ? `<span class="badge bg-success">Прогрес: ${c.currentCalories} ккал</span>`
          : `<button class="btn btn-outline-primary btn-sm mt-1" onclick="App.joinChallenge('${c.id}')">Приєднатися (тест)</button>`}
      </div>`).join('');

    document.getElementById('achievementsList').innerHTML = d.achievements
      .map(name => `<span class="badge bg-warning text-dark me-1 mb-1">🏅 ${name}</span>`)
      .join('');

    document.getElementById('leaderboardList').innerHTML = d.leaderboard
      .map(l => `<li>${l.name} — <strong>${l.totalCalories} ккал</strong></li>`)
      .join('');

    document.getElementById('remindersList').innerHTML = d.reminders
      .map(r => `<li>⏰ ${r.cronExpression} (${r.channel === 0 ? 'Email' : 'WebPush'})</li>`)
      .join('');

    this.renderCharts(d.weightHistory, d.calorieHistory, d.popular);
  },

  async refreshAll() {
    const uid = this.currentUser.id;
    const userData = await ApiClient.get(`/api/profile/${uid}`);
    const p = userData.profile;

    document.getElementById('profileSummary').innerHTML = `
      <div>Зріст: <strong>${p.heightCm} см</strong> | Вага: <strong>${p.currentWeightKg} кг</strong></div>
      <div>ІМТ: <span class="badge bg-info text-dark">${userData.bmi}</span></div>
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
      ? programs.map(pr => `<div class="border-top pt-1 mt-1"><strong>${pr.title}</strong> (${pr.startDate} — ${pr.endDate})</div>`).join('')
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
      ApiClient.get(`/api/social/challenges/${uid}`),
      ApiClient.get(`/api/social/achievements/${uid}`),
      ApiClient.get('/api/social/leaderboard'),
      ApiClient.get(`/api/reminders/${uid}`)
    ]);

    document.getElementById('challengesList').innerHTML = challenges.map(c => `
      <div class="border-bottom pb-2 mb-2">
        <div class="fw-bold">${c.title}</div>
        <div class="text-muted">Ціль: ${c.targetCalories} ккал</div>
        ${c.isJoined
          ? `<span class="badge bg-success">Прогрес: ${c.currentCalories} ккал</span>`
          : `<button class="btn btn-outline-primary btn-sm mt-1" onclick="App.joinChallenge('${c.id}')">Приєднатися</button>`}
      </div>`).join('');

    document.getElementById('achievementsList').innerHTML = achievements.length
      ? achievements.map(a => `<span class="badge bg-warning text-dark me-1 mb-1">🏅 ${a.title}</span>`).join('')
      : '<span class="small text-muted">Виконайте тренування для нагород</span>';

    document.getElementById('leaderboardList').innerHTML = leaderboard
      .map(l => `<li>${l.name} — <strong>${l.totalCalories} ккал</strong></li>`)
      .join('');

    document.getElementById('remindersList').innerHTML = reminders
      .map(r => `<li>⏰ ${r.cronExpression} (${r.channel === 0 ? 'Email' : 'WebPush'})</li>`)
      .join('');

    const [prog, pop] = await Promise.all([
      ApiClient.get(`/api/reports/progress/${uid}`),
      ApiClient.get('/api/reports/popular-workouts')
    ]);
    this.renderCharts(prog.weightHistory, prog.calorieHistory, pop);
  },

  async logWeight() {
    const weightKg = parseFloat(document.getElementById('newWeightInput').value);
    if (!weightKg) return;
    document.getElementById('newWeightInput').value = '';

    if (this.isDemo) {
      this.demoData.weightKg = weightKg;
      const today = new Date().toISOString().slice(0, 10);
      this.demoData.weightHistory.push({ date: today, weightKg });
      this.renderDemoDashboard();
      return;
    }

    await ApiClient.post(`/api/profile/${this.currentUser.id}/weight?weightKg=${weightKg}`, {});
    this.refreshAll();
  },

  async generateProgram() {
    const goal = parseInt(document.getElementById('goalSelect').value);

    if (this.isDemo) {
      const names = ['Схуднення (Шаблон)', 'Набір м’язової маси (Шаблон)', 'Витривалість (Шаблон)'];
      this.demoData.programs.unshift({ title: `Персональна програма: ${names[goal]}`, workoutsCount: 3 });
      this.renderDemoDashboard();
      return;
    }

    await ApiClient.post(`/api/programs/generate?userId=${this.currentUser.id}&goal=${goal}`, {});
    this.refreshAll();
  },

  async completeWorkout() {
    const workoutTemplateId = document.getElementById('workoutSelect').value;
    const durationMinutes = parseInt(document.getElementById('durationInput').value) || 30;

    if (this.isDemo) {
      const w = this.demoData.workouts.find(x => x.id === workoutTemplateId) || this.demoData.workouts[0];
      const burned = Math.round(w.met * this.demoData.weightKg * (durationMinutes / 60));
      this.demoData.sessions.unshift({
        workoutTemplate: { title: w.title },
        durationMinutes,
        caloriesBurned: burned
      });
      const today = new Date().toISOString().slice(0, 10);
      this.demoData.calorieHistory.push({ date: today, caloriesBurned: burned });
      this.demoData.challenges.forEach(c => { if (c.isJoined) c.currentCalories += burned; });
      document.getElementById('lastSessionMsg').textContent = `[Тур] Симуляція: розраховано ${burned} ккал!`;
      this.renderDemoDashboard();
      return;
    }

    const data = await ApiClient.post(
      `/api/sessions?userId=${this.currentUser.id}&templateId=${workoutTemplateId}&durationMinutes=${durationMinutes}`,
      {}
    );
    document.getElementById('lastSessionMsg').textContent = `Зараховано! Спалено ${data.caloriesBurned} ккал`;
    this.refreshAll();
  },

  async joinChallenge(id) {
    if (this.isDemo) {
      const ch = this.demoData.challenges.find(c => c.id === id);
      if (ch) ch.isJoined = true;
      this.renderDemoDashboard();
      return;
    }

    await ApiClient.post(`/api/social/challenges/${id}/join?userId=${this.currentUser.id}`, {});
    this.refreshAll();
  },

  async addReminder() {
    const scheduledTime = document.getElementById('remTime').value;
    const channel = parseInt(document.getElementById('remChannel').value);

    if (this.isDemo) {
      this.demoData.reminders.push({ cronExpression: `Щодня о ${scheduledTime}`, channel });
      this.renderDemoDashboard();
      return;
    }

    await ApiClient.post('/api/reminders', {
      userId: this.currentUser.id,
      cronExpression: `Щодня о ${scheduledTime}`,
      channel
    });
    this.refreshAll();
  },

  renderCharts(weightHistory, calorieHistory, popular) {
    if (this.progressChart) this.progressChart.destroy();
    const allDates = [...new Set([
      ...weightHistory.map(x => x.date),
      ...calorieHistory.map(x => x.date)
    ])].sort();

    this.progressChart = new Chart(document.getElementById('progressChart'), {
      type: 'line',
      data: {
        labels: allDates,
        datasets: [
          {
            label: 'Вага (кг)',
            data: weightHistory.map(x => ({ x: x.date, y: x.weightKg })),
            borderColor: '#0d6efd',
            yAxisID: 'y',
            tension: 0.2
          },
          {
            label: 'Спалені калорії (ккал)',
            data: calorieHistory.map(x => ({ x: x.date, y: x.caloriesBurned })),
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
        labels: popular.map(x => x.title),
        datasets: [{ data: popular.map(x => x.sessionsCount) }]
      }
    });
  }
};

window.addEventListener('DOMContentLoaded', () => App.init());