/* ============================================================
   EXPENSE TRACKER — Application Logic (JavaScript)
   Uses localStorage for data persistence (mirrors SQLite)
   ============================================================ */

// =================== DATA LAYER ===================
const DB_KEY_EXPENSES   = 'et_expenses';
const DB_KEY_CATEGORIES = 'et_categories';

const DEFAULT_CATEGORIES = ['Food 🍔', 'Travel 🚗', 'Shopping 🛍️', 'Bills 💡', 'Other 📦'];

/** Get all expenses from localStorage */
function getExpenses() {
  try {
    return JSON.parse(localStorage.getItem(DB_KEY_EXPENSES) || '[]');
  } catch { return []; }
}

/** Save all expenses to localStorage */
function saveExpenses(expenses) {
  localStorage.setItem(DB_KEY_EXPENSES, JSON.stringify(expenses));
}

/** Get all categories from localStorage */
function getCategories() {
  const stored = localStorage.getItem(DB_KEY_CATEGORIES);
  if (!stored) {
    localStorage.setItem(DB_KEY_CATEGORIES, JSON.stringify(DEFAULT_CATEGORIES));
    return [...DEFAULT_CATEGORIES];
  }
  return JSON.parse(stored);
}

/** Save categories to localStorage */
function saveCategories(cats) {
  localStorage.setItem(DB_KEY_CATEGORIES, JSON.stringify(cats));
}

/** Generate a unique ID */
function genId() {
  return Date.now() + Math.floor(Math.random() * 1000);
}

// =================== CHART INSTANCES ===================
let dashPieChartInst    = null;
let barChartInst        = null;
let repPieChartInst     = null;
let lineChartInst       = null;

// Chart.js global defaults
Chart.defaults.color = '#8888aa';
Chart.defaults.font.family = 'Inter';

const PALETTE = [
  '#28c76f', '#00cfe8', '#a78bfa', '#ff9f43', '#ea5455',
  '#fd7e14', '#82cfff', '#f48fb1', '#66bb6a', '#ffa726'
];

// =================== PAGE NAVIGATION ===================
function switchPage(page, btn) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));

  document.getElementById('page-' + page)?.classList.add('active');
  btn?.classList.add('active');

  if (page === 'dashboard') loadDashboard();
  if (page === 'reports')   initReportDates(), loadReports();
  if (page === 'categories') loadCategories();
  if (page === 'add')       populateCategoryDropdown(), setDefaultDate();
}

// =================== TOAST NOTIFICATIONS ===================
let toastTimer = null;
function showToast(msg, type = 'success') {
  const toast = document.getElementById('toast');
  toast.textContent = msg;
  toast.className = 'toast ' + type + ' show';
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { toast.classList.remove('show'); }, 3000);
}

// =================== MODAL ===================
let modalCallback = null;

function showModal(title, message, onConfirm) {
  document.getElementById('modalTitle').textContent   = title;
  document.getElementById('modalMessage').textContent = message;
  document.getElementById('modalOverlay').style.display = 'flex';
  modalCallback = onConfirm;
  document.getElementById('modalConfirmBtn').onclick = () => {
    closeModal();
    if (modalCallback) modalCallback();
  };
}

function closeModal() {
  document.getElementById('modalOverlay').style.display = 'none';
  modalCallback = null;
}

// Close modal on overlay click
document.getElementById('modalOverlay').addEventListener('click', (e) => {
  if (e.target.id === 'modalOverlay') closeModal();
});

// =================== DASHBOARD ===================
function loadDashboard() {
  const expenses = getExpenses();
  const now      = new Date();
  const today    = toDateStr(now);
  const ym       = `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}`;
  const weekAgo  = new Date(now); weekAgo.setDate(weekAgo.getDate() - 6);
  const weekStr  = toDateStr(weekAgo);

  // Calculate totals
  const totalAll   = expenses.reduce((s, e) => s + +e.amount, 0);
  const totalMonth = expenses.filter(e => e.date.startsWith(ym)).reduce((s, e) => s + +e.amount, 0);
  const totalWeek  = expenses.filter(e => e.date >= weekStr).reduce((s, e) => s + +e.amount, 0);
  const totalToday = expenses.filter(e => e.date === today).reduce((s, e) => s + +e.amount, 0);

  document.getElementById('total-all-time').textContent = formatCurrency(totalAll);
  document.getElementById('total-month').textContent    = formatCurrency(totalMonth);
  document.getElementById('total-week').textContent     = formatCurrency(totalWeek);
  document.getElementById('total-today').textContent    = formatCurrency(totalToday);

  // Render dashboard pie chart
  renderDashPie(expenses, ym);

  // Populate filter dropdowns
  populateFilterCategory();

  // Apply table filter
  applyDashboardFilter();
}

function renderDashPie(expenses, ym) {
  const monthExp = expenses.filter(e => e.date.startsWith(ym));
  const grouped  = groupByCategory(monthExp);
  const labels   = Object.keys(grouped);
  const data     = Object.values(grouped);

  const ctx = document.getElementById('dashPieChart').getContext('2d');

  if (dashPieChartInst) dashPieChartInst.destroy();
  dashPieChartInst = new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels,
      datasets: [{
        data,
        backgroundColor: PALETTE.slice(0, labels.length),
        borderColor: '#0f0f13',
        borderWidth: 3,
        hoverOffset: 8
      }]
    },
    options: {
      cutout: '60%',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'bottom',
          labels: { color: '#8888aa', padding: 14, font: { size: 12 }, boxWidth: 12 }
        },
        tooltip: {
          callbacks: {
            label: ctx => ` ${ctx.label}: ${formatCurrency(ctx.raw)}`
          }
        }
      }
    }
  });
}

function populateFilterCategory() {
  const sel  = document.getElementById('filterCategory');
  const cats = getCategories();
  const cur  = sel.value;
  sel.innerHTML = '<option value="">All Categories</option>';
  cats.forEach(c => {
    const opt = document.createElement('option');
    opt.value = c; opt.textContent = c;
    sel.appendChild(opt);
  });
  sel.value = cur;
}

function applyDashboardFilter() {
  let expenses = getExpenses();
  const catFilter  = document.getElementById('filterCategory').value;
  const timeFilter = document.getElementById('filterTime').value;
  const now        = new Date();
  const today      = toDateStr(now);

  if (catFilter) {
    expenses = expenses.filter(e => e.category === catFilter);
  }

  if (timeFilter === 'today') {
    expenses = expenses.filter(e => e.date === today);
  } else if (timeFilter === 'week') {
    const weekAgo = new Date(now); weekAgo.setDate(weekAgo.getDate() - 6);
    expenses = expenses.filter(e => e.date >= toDateStr(weekAgo));
  } else if (timeFilter === 'month') {
    const ym = `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}`;
    expenses = expenses.filter(e => e.date.startsWith(ym));
  }

  renderExpensesTable(expenses);
}

function renderExpensesTable(expenses) {
  const tbody      = document.getElementById('expensesTableBody');
  const emptyState = document.getElementById('emptyState');

  tbody.innerHTML = '';

  if (!expenses.length) {
    emptyState.style.display = 'block';
    return;
  }
  emptyState.style.display = 'none';

  // Sort descending by date
  const sorted = [...expenses].sort((a, b) => b.date.localeCompare(a.date));

  sorted.forEach(exp => {
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td class="date-cell">${formatDateDisplay(exp.date)}</td>
      <td><span class="category-badge">${exp.category}</span></td>
      <td class="amount-cell">${formatCurrency(exp.amount)}</td>
      <td class="desc-cell" title="${exp.description}">${exp.description || '—'}</td>
      <td class="actions-cell">
        <button class="btn btn-icon" title="Edit" onclick="editExpense(${exp.id})">✏️</button>
        <button class="btn btn-icon" title="Delete" onclick="deleteExpensePrompt(${exp.id})">🗑️</button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

// =================== ADD / EDIT EXPENSE ===================
function setDefaultDate() {
  const input = document.getElementById('inputDate');
  if (input && !input.value) {
    input.value = toDateStr(new Date());
  }
}

function populateCategoryDropdown() {
  const sel  = document.getElementById('inputCategory');
  const cats = getCategories();
  const cur  = sel.value;
  sel.innerHTML = '';
  cats.forEach(c => {
    const opt = document.createElement('option');
    opt.value = c; opt.textContent = c;
    sel.appendChild(opt);
  });
  if (cur) sel.value = cur;
}

function saveExpense() {
  const id     = document.getElementById('editingId').value;
  const amount = parseFloat(document.getElementById('inputAmount').value);
  const cat    = document.getElementById('inputCategory').value;
  const date   = document.getElementById('inputDate').value;
  const desc   = document.getElementById('inputDescription').value.trim();

  if (isNaN(amount) || amount <= 0) {
    showToast('⚠️ Please enter a valid positive amount.', 'error');
    document.getElementById('inputAmount').focus();
    return;
  }
  if (!cat) {
    showToast('⚠️ Please select a category.', 'error');
    return;
  }
  if (!date) {
    showToast('⚠️ Please select a date.', 'error');
    return;
  }

  const expenses = getExpenses();

  if (id) {
    // UPDATE
    const idx = expenses.findIndex(e => e.id == id);
    if (idx !== -1) {
      expenses[idx] = { ...expenses[idx], amount, category: cat, date, description: desc };
      saveExpenses(expenses);
      showToast('✅ Expense updated successfully!');
    }
  } else {
    // INSERT
    expenses.push({ id: genId(), amount, category: cat, date, description: desc });
    saveExpenses(expenses);
    showToast('✅ Expense added successfully!');
  }

  clearForm();
  switchPage('dashboard', document.getElementById('nav-dashboard'));
}

function editExpense(id) {
  const expenses = getExpenses();
  const exp = expenses.find(e => e.id == id);
  if (!exp) return;

  populateCategoryDropdown();
  document.getElementById('editingId').value        = exp.id;
  document.getElementById('inputAmount').value      = exp.amount;
  document.getElementById('inputCategory').value    = exp.category;
  document.getElementById('inputDate').value        = exp.date;
  document.getElementById('inputDescription').value = exp.description || '';

  document.getElementById('addPageTitle').textContent = 'Edit Expense';
  document.getElementById('saveBtn').textContent      = '💾 Update Expense';

  switchPage('add', document.getElementById('nav-add'));
}

function deleteExpensePrompt(id) {
  const expenses = getExpenses();
  const exp = expenses.find(e => e.id == id);
  if (!exp) return;
  showModal(
    'Delete Expense?',
    `${exp.category} — ${formatCurrency(exp.amount)} on ${formatDateDisplay(exp.date)}`,
    () => {
      const updated = expenses.filter(e => e.id != id);
      saveExpenses(updated);
      showToast('🗑️ Expense deleted.', 'info');
      loadDashboard();
    }
  );
}

function clearForm() {
  document.getElementById('editingId').value        = '';
  document.getElementById('inputAmount').value      = '';
  document.getElementById('inputDate').value        = toDateStr(new Date());
  document.getElementById('inputDescription').value = '';
  document.getElementById('addPageTitle').textContent = 'Add Expense';
  document.getElementById('saveBtn').textContent      = '💾 Save Expense';
  populateCategoryDropdown();
}

// =================== REPORTS ===================
function initReportDates() {
  const from = document.getElementById('reportFrom');
  const to   = document.getElementById('reportTo');
  if (!from.value) {
    const d = new Date();
    d.setMonth(d.getMonth() - 5);
    d.setDate(1);
    from.value = toDateStr(d);
  }
  if (!to.value) {
    to.value = toDateStr(new Date());
  }
}

function loadReports() {
  const fromDate = document.getElementById('reportFrom').value;
  const toDate   = document.getElementById('reportTo').value;
  if (!fromDate || !toDate) return;

  const expenses = getExpenses().filter(e => e.date >= fromDate && e.date <= toDate);

  renderBarChart(expenses);
  renderRepPieChart(expenses);
  renderLineChart(expenses, fromDate, toDate);
}

function renderBarChart(expenses) {
  const monthly = {};
  expenses.forEach(e => {
    const key = e.date.substring(0, 7); // YYYY-MM
    monthly[key] = (monthly[key] || 0) + +e.amount;
  });
  const sortedKeys = Object.keys(monthly).sort();
  const labels = sortedKeys.map(k => {
    const [y, m] = k.split('-');
    return new Date(+y, +m - 1).toLocaleDateString('en-US', { month: 'short', year: '2-digit' });
  });
  const data = sortedKeys.map(k => monthly[k]);

  const ctx = document.getElementById('barChart').getContext('2d');
  if (barChartInst) barChartInst.destroy();
  barChartInst = new Chart(ctx, {
    type: 'bar',
    data: {
      labels,
      datasets: [{
        label: 'Monthly Spending',
        data,
        backgroundColor: PALETTE.slice(0, labels.length).map(c => c + 'cc'),
        borderColor: PALETTE.slice(0, labels.length),
        borderWidth: 2,
        borderRadius: 8,
        borderSkipped: false
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: ctx => ' ' + formatCurrency(ctx.raw) } }
      },
      scales: {
        x: { grid: { color: '#2a2a3d' }, ticks: { color: '#8888aa' } },
        y: { grid: { color: '#2a2a3d' }, ticks: { color: '#8888aa', callback: v => '$' + v } }
      }
    }
  });
}

function renderRepPieChart(expenses) {
  const grouped = groupByCategory(expenses);
  const labels  = Object.keys(grouped);
  const data    = Object.values(grouped);

  const ctx = document.getElementById('repPieChart').getContext('2d');
  if (repPieChartInst) repPieChartInst.destroy();
  repPieChartInst = new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels,
      datasets: [{
        data,
        backgroundColor: PALETTE.slice(0, labels.length),
        borderColor: '#1a1a24',
        borderWidth: 3,
        hoverOffset: 6
      }]
    },
    options: {
      cutout: '55%',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'right',
          labels: { color: '#8888aa', padding: 14, font: { size: 12 }, boxWidth: 12 }
        },
        tooltip: {
          callbacks: { label: ctx => ` ${ctx.label}: ${formatCurrency(ctx.raw)}` }
        }
      }
    }
  });
}

function renderLineChart(expenses, fromDate, toDate) {
  // Build daily map
  const daily = {};
  expenses.forEach(e => {
    daily[e.date] = (daily[e.date] || 0) + +e.amount;
  });

  // Build consecutive date list
  const dates = [];
  const values = [];
  let cur = new Date(fromDate);
  const end = new Date(toDate);
  // Cap at 90 days for readability
  const maxDays = 90;
  let dayCount = 0;

  while (cur <= end && dayCount < maxDays) {
    const key = toDateStr(cur);
    dates.push(cur.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }));
    values.push(daily[key] || 0);
    cur.setDate(cur.getDate() + 1);
    dayCount++;
  }

  const ctx = document.getElementById('lineChart').getContext('2d');
  if (lineChartInst) lineChartInst.destroy();
  lineChartInst = new Chart(ctx, {
    type: 'line',
    data: {
      labels: dates,
      datasets: [{
        label: 'Daily Spending',
        data: values,
        borderColor: '#28c76f',
        backgroundColor: (context) => {
          const chart = context.chart;
          const { ctx: c, chartArea } = chart;
          if (!chartArea) return 'transparent';
          const gradient = c.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
          gradient.addColorStop(0, 'rgba(40,199,111,0.25)');
          gradient.addColorStop(1, 'rgba(40,199,111,0.01)');
          return gradient;
        },
        borderWidth: 2.5,
        fill: true,
        tension: 0.4,
        pointBackgroundColor: '#28c76f',
        pointBorderColor: '#0f0f13',
        pointBorderWidth: 2,
        pointRadius: 4,
        pointHoverRadius: 7
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: ctx => ' ' + formatCurrency(ctx.raw) } }
      },
      scales: {
        x: {
          grid: { color: '#2a2a3d' },
          ticks: { color: '#8888aa', maxTicksLimit: 15, maxRotation: 45 }
        },
        y: {
          grid: { color: '#2a2a3d' },
          ticks: { color: '#8888aa', callback: v => '$' + v }
        }
      }
    }
  });
}

// =================== CATEGORIES ===================
function loadCategories() {
  const cats     = getCategories();
  const expenses = getExpenses();
  const ul       = document.getElementById('categoryList');
  ul.innerHTML   = '';

  cats.forEach(cat => {
    const li = document.createElement('li');
    li.innerHTML = `
      <span>${cat}</span>
      <button class="btn btn-icon" title="Delete" onclick="deleteCategoryPrompt('${cat.replace(/'/g, "\\'")}')">🗑️</button>
    `;
    ul.appendChild(li);
  });

  // Category stats
  const statsEl = document.getElementById('categoryStats');
  const grouped = groupByCategory(expenses);
  const total   = Object.values(grouped).reduce((s, v) => s + v, 0);
  const sorted  = Object.entries(grouped).sort((a, b) => b[1] - a[1]);

  statsEl.innerHTML = sorted.length ? sorted.map(([cat, amt]) => `
    <div class="cat-stat-item">
      <div class="cat-stat-header">
        <span>${cat}</span>
        <span class="cat-stat-amount">${formatCurrency(amt)}</span>
      </div>
      <div class="cat-stat-bar">
        <div class="cat-stat-fill" style="width:${total ? Math.round((amt/total)*100) : 0}%"></div>
      </div>
    </div>
  `).join('') : '<p style="color:var(--text-muted);font-size:13px;">No spending data yet.</p>';
}

function addCategory() {
  const input = document.getElementById('newCategoryInput');
  const name  = input.value.trim();
  if (!name) { showToast('⚠️ Category name cannot be empty.', 'error'); return; }

  const cats = getCategories();
  if (cats.map(c => c.toLowerCase()).includes(name.toLowerCase())) {
    showToast('⚠️ Category already exists.', 'error'); return;
  }
  cats.push(name);
  saveCategories(cats);
  input.value = '';
  showToast(`✅ Category "${name}" added!`);
  loadCategories();
}

function deleteCategoryPrompt(name) {
  showModal(
    'Delete Category?',
    `"${name}" will be removed. Expenses using this category will remain.`,
    () => {
      const cats = getCategories().filter(c => c !== name);
      saveCategories(cats);
      showToast(`🗑️ Category "${name}" deleted.`, 'info');
      loadCategories();
    }
  );
}

// Allow adding category on Enter
document.getElementById('newCategoryInput').addEventListener('keydown', e => {
  if (e.key === 'Enter') addCategory();
});

// =================== UTILITY FUNCTIONS ===================
function toDateStr(d) {
  return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
}

function formatCurrency(n) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(+n || 0);
}

function formatDateDisplay(dateStr) {
  if (!dateStr) return '';
  const [y, m, d] = dateStr.split('-');
  return new Date(+y, +m-1, +d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function groupByCategory(expenses) {
  const map = {};
  expenses.forEach(e => {
    map[e.category] = (map[e.category] || 0) + +e.amount;
  });
  return map;
}

// =================== INITIALIZATION ===================
(function init() {
  // Seed sample data if completely fresh
  if (!localStorage.getItem(DB_KEY_EXPENSES)) {
    const today = toDateStr(new Date());
    const d     = new Date();
    const sample = [
      { id: genId(), amount: 42.50, category: 'Food 🍔',     date: today,                       description: 'Grocery shopping' },
      { id: genId(), amount: 28.00, category: 'Travel 🚗',    date: toDateStr(new Date(d.getFullYear(), d.getMonth(), d.getDate()-2)), description: 'Uber ride' },
      { id: genId(), amount: 89.99, category: 'Shopping 🛍️',  date: toDateStr(new Date(d.getFullYear(), d.getMonth(), d.getDate()-5)), description: 'Amazon order' },
      { id: genId(), amount: 120.00,category: 'Bills 💡',     date: toDateStr(new Date(d.getFullYear(), d.getMonth(), 1)),            description: 'Electric bill' },
      { id: genId(), amount: 15.00, category: 'Food 🍔',      date: toDateStr(new Date(d.getFullYear(), d.getMonth()-1, 15)),         description: 'Lunch' },
      { id: genId(), amount: 200.00,category: 'Shopping 🛍️',  date: toDateStr(new Date(d.getFullYear(), d.getMonth()-1, 8)),          description: 'Clothing' },
      { id: genId(), amount: 55.00, category: 'Travel 🚗',    date: toDateStr(new Date(d.getFullYear(), d.getMonth()-2, 20)),         description: 'Gas' },
      { id: genId(), amount: 9.99,  category: 'Other 📦',     date: toDateStr(new Date(d.getFullYear(), d.getMonth(), d.getDate()-1)), description: 'Netflix' },
    ];
    saveExpenses(sample);
  }

  // Initialize categories if needed
  getCategories();

  // Start on dashboard
  loadDashboard();

  // Initialize report dates
  initReportDates();

  // Set form default date
  setDefaultDate();

  // Handle Enter key on add form
  ['inputAmount','inputCategory','inputDate','inputDescription'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
      if (e.key === 'Enter') saveExpense();
    });
  });
})();
