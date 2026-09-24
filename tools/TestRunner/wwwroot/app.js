// Сторінка запускача тестів ADAICA_AUTOTEST.
// Черга — це порядок, у якому позначено тести; вона й поля підключення (крім пароля) зберігаються в localStorage.
'use strict';

const STORAGE = {
  queue: 'adaica-runner.queue',
  connection: 'adaica-runner.connection',
  collapsed: 'adaica-runner.collapsed',
};

const STATUS_TEXT = {
  Pending: 'У черзі',
  Running: 'Виконується',
  Passed: 'Пройшов',
  Failed: 'Упав',
  Skipped: 'Пропущено',
  NotFound: 'Не знайдено',
};

const RUN_TITLE = {
  Building: 'Збирання тестів…',
  Running: 'Тести виконуються…',
  Reporting: 'Генерація звіту Allure…',
  Finished: 'Тести завершились',
  Stopped: 'Прогін зупинено',
  Failed: 'Прогін не вдався',
};

const $ = (id) => document.getElementById(id);

const state = {
  groups: [],
  tests: new Map(),
  queue: load(STORAGE.queue, []),
  collapsed: new Set(load(STORAGE.collapsed, [])),
  defaults: null,
  run: null,
  pollTimer: null,
};

function load(key, fallback) {
  try {
    const value = localStorage.getItem(key);
    return value ? JSON.parse(value) : fallback;
  } catch {
    return fallback;
  }
}

function save(key, value) {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Без localStorage сторінка працює, лише не пам'ятає вибір.
  }
}

async function api(path, options = {}) {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (response.status === 204) return null;
  const body = await response.json().catch(() => null);
  if (!response.ok) throw new Error(body?.error ?? `${response.status} ${response.statusText}`);
  return body;
}

// ---------- Тести ----------

async function loadTests(refresh = false) {
  const button = $('refresh');
  button.disabled = true;
  $('build-info').textContent = refresh ? 'Збирання тестів і оновлення списку…' : 'Завантаження списку тестів…';
  try {
    const data = refresh ? await api('/api/tests/refresh', { method: 'POST' }) : await api('/api/tests');
    state.groups = data.groups ?? [];
    state.tests = new Map(state.groups.flatMap((group) => group.tests.map((test) => [test.id, { ...test, group }])));
    // Тести, яких більше немає, прибираємо з черги.
    state.queue = state.queue.filter((id) => state.tests.has(id));
    save(STORAGE.queue, state.queue);

    const error = $('catalog-error');
    error.hidden = !data.error;
    error.textContent = data.error ?? '';

    const total = state.tests.size;
    const built = data.builtAt ? new Date(data.builtAt).toLocaleString('uk-UA') : null;
    $('build-info').textContent = `${total} тестів у ${state.groups.length} класах` + (built ? ` · збірка від ${built}` : '');
    renderGroups();
    renderQueue();
  } catch (error) {
    $('build-info').textContent = 'Не вдалося завантажити тести';
    showError('catalog-error', error.message);
  } finally {
    button.disabled = false;
  }
}

function renderGroups() {
  const container = $('groups');
  const template = $('group-template');
  const filter = $('search').value.trim().toLowerCase();
  container.replaceChildren();

  for (const group of state.groups) {
    const tests = group.tests.filter((test) => !filter
      || test.name.toLowerCase().includes(filter)
      || (test.summary ?? '').toLowerCase().includes(filter)
      || group.name.toLowerCase().includes(filter));
    if (tests.length === 0) continue;

    const node = template.content.firstElementChild.cloneNode(true);
    node.dataset.group = group.id;
    node.classList.toggle('collapsed', state.collapsed.has(group.id) && !filter);
    node.querySelector('.group-name').textContent = group.name;
    node.querySelector('.suite').textContent = group.suite ?? '';
    node.querySelector('.group-count').textContent = `${group.tests.length} ${plural(group.tests.length, 'тест', 'тести', 'тестів')}`;
    const summary = node.querySelector('.group-summary');
    summary.textContent = group.summary ?? '';
    summary.hidden = !group.summary;

    const toggle = node.querySelector('.group-toggle');
    toggle.addEventListener('click', () => {
      node.classList.toggle('collapsed');
      if (node.classList.contains('collapsed')) state.collapsed.add(group.id);
      else state.collapsed.delete(group.id);
      toggle.setAttribute('aria-expanded', String(!node.classList.contains('collapsed')));
      save(STORAGE.collapsed, [...state.collapsed]);
    });

    const groupCheck = node.querySelector('.group-check');
    groupCheck.addEventListener('change', () => {
      // Позначаємо в порядку тестів у класі; зняття прибирає з черги всі тести класу.
      if (groupCheck.checked) tests.forEach((test) => addToQueue(test.id));
      else tests.forEach((test) => removeFromQueue(test.id));
      syncSelection();
    });

    const list = node.querySelector('.test-list');
    for (const test of tests) list.append(renderTest(test));
    container.append(node);
  }

  if (!container.children.length) {
    const empty = document.createElement('p');
    empty.className = 'muted';
    empty.textContent = filter ? 'Нічого не знайдено.' : 'Тестів немає.';
    container.append(empty);
  }

  syncSelection();
}

function renderTest(test) {
  const item = document.createElement('li');
  item.className = 'test';
  item.dataset.id = test.id;

  const checkbox = document.createElement('input');
  checkbox.type = 'checkbox';
  checkbox.setAttribute('aria-label', test.name);
  checkbox.addEventListener('change', () => {
    if (checkbox.checked) addToQueue(test.id);
    else removeFromQueue(test.id);
    syncSelection();
  });

  const order = document.createElement('span');
  order.className = 'order';

  const body = document.createElement('div');
  const name = document.createElement('span');
  name.className = 'test-name';
  name.textContent = test.name;
  body.append(name);

  const chips = document.createElement('span');
  chips.className = 'chips';
  for (const category of test.categories) chips.append(badge(category, category.toLowerCase()));
  if (test.isExplicit) chips.append(badge('Explicit', 'explicit'));
  if (test.hasCaseSource) chips.append(badge('набір випадків'));
  else if (test.cases > 1) chips.append(badge(`${test.cases} випадків`));
  if (chips.children.length) body.append(chips);

  if (test.summary) {
    const summary = document.createElement('p');
    summary.className = 'test-summary';
    summary.textContent = test.summary;
    body.append(summary);
  }

  // Клік по рядку перемикає прапорець.
  item.addEventListener('click', (event) => {
    if (event.target === checkbox) return;
    checkbox.checked = !checkbox.checked;
    checkbox.dispatchEvent(new Event('change'));
  });

  item.append(checkbox, order, body);
  return item;
}

function badge(text, kind = '') {
  const element = document.createElement('span');
  element.className = `badge ${kind}`.trim();
  element.textContent = text;
  return element;
}

function plural(count, one, few, many) {
  const mod10 = count % 10;
  const mod100 = count % 100;
  if (mod10 === 1 && mod100 !== 11) return one;
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return few;
  return many;
}

// ---------- Черга ----------

function addToQueue(id) {
  if (!state.queue.includes(id)) state.queue.push(id);
  save(STORAGE.queue, state.queue);
}

function removeFromQueue(id) {
  state.queue = state.queue.filter((queued) => queued !== id);
  save(STORAGE.queue, state.queue);
}

function moveInQueue(id, delta) {
  const index = state.queue.indexOf(id);
  const target = index + delta;
  if (index < 0 || target < 0 || target >= state.queue.length) return;
  [state.queue[index], state.queue[target]] = [state.queue[target], state.queue[index]];
  save(STORAGE.queue, state.queue);
  syncSelection();
}

// Прапорці, номери в черзі й прапорці класів — за станом черги.
function syncSelection() {
  for (const item of document.querySelectorAll('.test')) {
    const position = state.queue.indexOf(item.dataset.id);
    item.classList.toggle('selected', position >= 0);
    item.querySelector('input').checked = position >= 0;
    item.querySelector('.order').textContent = position >= 0 ? position + 1 : '';
  }

  for (const node of document.querySelectorAll('.group')) {
    const ids = [...node.querySelectorAll('.test')].map((item) => item.dataset.id);
    const selected = ids.filter((id) => state.queue.includes(id)).length;
    const check = node.querySelector('.group-check');
    check.checked = selected > 0 && selected === ids.length;
    check.indeterminate = selected > 0 && selected < ids.length;
  }

  $('selected-counter').textContent = `${state.queue.length} вибрано`;
  renderQueue();
}

function renderQueue() {
  const list = $('queue');
  list.replaceChildren();
  for (const id of state.queue) {
    const test = state.tests.get(id);
    const item = document.createElement('li');
    const name = document.createElement('span');
    name.className = 'queue-name';
    name.textContent = test?.name ?? id;
    name.title = test ? `${test.group.name}.${test.name}` : id;

    const buttons = document.createElement('span');
    buttons.className = 'queue-buttons';
    buttons.append(
      queueButton('↑', 'Вище', () => moveInQueue(id, -1)),
      queueButton('↓', 'Нижче', () => moveInQueue(id, 1)),
      queueButton('✕', 'Прибрати з черги', () => { removeFromQueue(id); syncSelection(); }));
    item.append(name, buttons);
    list.append(item);
  }

  $('queue-empty').hidden = state.queue.length > 0;
  updateRunButtons();
}

function queueButton(text, title, onClick) {
  const button = document.createElement('button');
  button.type = 'button';
  button.textContent = text;
  button.title = title;
  button.setAttribute('aria-label', title);
  button.addEventListener('click', onClick);
  return button;
}

// ---------- Підключення ----------

const CONNECTION_FIELDS = ['user', 'database', 'schema'];

async function loadConnection() {
  try {
    state.defaults = await api('/api/defaults');
  } catch (error) {
    showError('run-error', `Не вдалося отримати значення за замовчуванням: ${error.message}`);
    state.defaults = {};
  }

  const saved = load(STORAGE.connection, {});
  for (const field of CONNECTION_FIELDS) $(field).value = saved[field] ?? state.defaults[field] ?? '';
  $('password').value = state.defaults.password ?? '';

  fillOptions('database-options', [state.defaults.database, saved.database]);
  fillOptions('schema-options', [state.defaults.schema, saved.schema]);
}

function fillOptions(id, values) {
  const list = $(id);
  list.replaceChildren();
  for (const value of new Set(values.filter(Boolean))) {
    const option = document.createElement('option');
    option.value = value;
    list.append(option);
  }
}

function saveConnection() {
  save(STORAGE.connection, Object.fromEntries(CONNECTION_FIELDS.map((field) => [field, $(field).value.trim()])));
}

// ---------- Прогін ----------

function isActive(run) {
  return run && ['Building', 'Running', 'Reporting'].includes(run.status);
}

function updateRunButtons() {
  const active = isActive(state.run);
  $('run').disabled = active || state.queue.length === 0;
  $('run').textContent = state.queue.length ? `▶ Запустити (${state.queue.length})` : '▶ Запустити';
  $('stop').hidden = !active;
  $('refresh').disabled = active;
}

async function startRun() {
  hideError('run-error');
  const form = $('connection');
  if (!form.reportValidity()) return;
  saveConnection();

  try {
    state.run = await api('/api/run', {
      method: 'POST',
      body: JSON.stringify({
        tests: state.queue,
        user: $('user').value.trim(),
        password: $('password').value,
        database: $('database').value.trim(),
        schema: $('schema').value.trim(),
      }),
    });
    renderRun();
    startPolling();
    $('results').scrollIntoView({ behavior: 'smooth', block: 'start' });
  } catch (error) {
    showError('run-error', error.message);
  }
}

async function stopRun() {
  $('stop').disabled = true;
  try {
    await api('/api/run/stop', { method: 'POST' });
  } catch (error) {
    showError('run-error', error.message);
  } finally {
    $('stop').disabled = false;
  }
}

function startPolling() {
  clearInterval(state.pollTimer);
  state.pollTimer = setInterval(pollRun, 1000);
}

async function pollRun() {
  try {
    state.run = await api('/api/run');
  } catch {
    return;
  }

  renderRun();
  if (!isActive(state.run)) clearInterval(state.pollTimer);
}

function renderRun() {
  const run = state.run;
  updateRunButtons();
  const section = $('results');
  if (!run) {
    section.hidden = true;
    return;
  }

  section.hidden = false;
  const done = !isActive(run);
  const failed = run.items.some((item) => ['Failed', 'NotFound'].includes(item.status)) || run.status === 'Failed';
  section.classList.toggle('done-ok', done && !failed && run.status === 'Finished');
  section.classList.toggle('done-failed', done && (failed || run.status !== 'Finished'));

  const finishedCount = run.items.filter((item) => !['Pending', 'Running'].includes(item.status)).length;
  const passed = run.items.filter((item) => item.status === 'Passed').length;
  $('results-title').textContent = RUN_TITLE[run.status] ?? run.status;
  const started = new Date(run.startedAt).toLocaleString('uk-UA');
  const connection = `${run.user} · ${run.database}:${run.schema}`;
  $('results-subtitle').textContent = done
    ? `${passed} з ${run.items.length} пройшли · початок ${started} · тривалість ${formatDuration(elapsed(run))} · ${connection}`
    : `${finishedCount} з ${run.items.length} виконано · початок ${started} · ${connection}`;

  $('progress').hidden = done;
  $('progress-bar').style.width = `${run.items.length ? (finishedCount / run.items.length) * 100 : 0}%`;

  const error = run.error ?? (done ? run.reportError : null);
  if (error) showError('results-error', error);
  else hideError('results-error');

  const report = $('report');
  report.hidden = !run.reportUrl;
  if (run.reportUrl) report.href = run.reportUrl;

  const body = $('results-body');
  body.replaceChildren(...run.items.map(renderResultRow));

  const total = run.items.reduce((sum, item) => sum + (item.durationSeconds ?? 0), 0);
  $('results-foot').innerHTML = done
    ? `<tr><td></td><td>Разом (час тестів за звітом NUnit)</td><td></td><td></td><td class="num">${formatDuration(total)}</td></tr>`
    : '';

  const log = $('log');
  const atBottom = log.scrollTop + log.clientHeight >= log.scrollHeight - 8;
  log.textContent = run.log.join('\n');
  if (atBottom) log.scrollTop = log.scrollHeight;
}

function renderResultRow(item, index) {
  const row = document.createElement('tr');
  const number = cell(String(index + 1));
  number.className = 'muted';

  const nameCell = document.createElement('td');
  const name = document.createElement('div');
  name.className = 'result-name';
  name.textContent = item.name;
  nameCell.append(name);
  if (item.message) {
    const message = document.createElement('div');
    message.className = 'result-message';
    message.textContent = item.message;
    nameCell.append(message);
  }

  const statusCell = document.createElement('td');
  const status = document.createElement('span');
  status.className = `status ${item.status}`;
  if (item.status === 'Running') {
    const spinner = document.createElement('span');
    spinner.className = 'spinner';
    status.append(spinner);
  }
  status.append(STATUS_TEXT[item.status] ?? item.status);
  statusCell.append(status);

  const cases = item.passed + item.failed + item.skipped;
  const casesCell = cell(cases > 1 ? `${item.passed}/${cases}` : '');
  casesCell.title = cases > 1 ? `Пройшли ${item.passed}, упали ${item.failed}, пропущено ${item.skipped}` : '';

  const duration = cell(item.durationSeconds != null ? formatDuration(item.durationSeconds) : '—');
  duration.className = 'num';

  row.append(number, nameCell, statusCell, casesCell, duration);
  return row;
}

function cell(text) {
  const element = document.createElement('td');
  element.textContent = text;
  return element;
}

function elapsed(run) {
  const end = run.finishedAt ? new Date(run.finishedAt) : new Date();
  return (end - new Date(run.startedAt)) / 1000;
}

function formatDuration(seconds) {
  if (seconds < 60) return `${seconds.toFixed(1).replace('.', ',')} с`;
  const minutes = Math.floor(seconds / 60);
  const rest = Math.round(seconds % 60);
  return minutes < 60 ? `${minutes} хв ${rest} с` : `${Math.floor(minutes / 60)} год ${minutes % 60} хв`;
}

function showError(id, message) {
  const element = $(id);
  element.textContent = message;
  element.hidden = false;
}

function hideError(id) {
  $(id).hidden = true;
}

// ---------- Події ----------

function selectSmoke() {
  for (const [id, test] of state.tests) {
    if (test.categories.includes('Smoke')) addToQueue(id);
  }
  syncSelection();
}

function setAllCollapsed(collapsed) {
  state.collapsed = new Set(collapsed ? state.groups.map((group) => group.id) : []);
  save(STORAGE.collapsed, [...state.collapsed]);
  renderGroups();
}

$('search').addEventListener('input', renderGroups);
$('refresh').addEventListener('click', () => loadTests(true));
$('expand-all').addEventListener('click', () => setAllCollapsed(false));
$('collapse-all').addEventListener('click', () => setAllCollapsed(true));
$('select-smoke').addEventListener('click', selectSmoke);
$('clear-selection').addEventListener('click', () => {
  state.queue = [];
  save(STORAGE.queue, state.queue);
  syncSelection();
});
$('run').addEventListener('click', startRun);
$('stop').addEventListener('click', stopRun);
$('connection').addEventListener('change', saveConnection);
$('reset-connection').addEventListener('click', () => {
  for (const field of CONNECTION_FIELDS) $(field).value = state.defaults?.[field] ?? '';
  $('password').value = state.defaults?.password ?? '';
  saveConnection();
});
$('toggle-password').addEventListener('click', () => {
  const input = $('password');
  const show = input.type === 'password';
  input.type = show ? 'text' : 'password';
  $('toggle-password').title = show ? 'Сховати пароль' : 'Показати пароль';
});

(async function init() {
  await Promise.all([loadTests(), loadConnection()]);
  // Показуємо останній прогін (наприклад, після перезавантаження сторінки) і стежимо, якщо він ще триває.
  await pollRun();
  if (isActive(state.run)) startPolling();
})();
