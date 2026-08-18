const $ = (selector) => document.querySelector(selector);

const formatMoney = (value, currency) =>
  new Intl.NumberFormat('es-CR', { style: 'currency', currency }).format(value);

const statusLabels = { Draft: 'Borrador', Generated: 'Generado' };
const actionLabels = { CREATE: 'CREACIÓN', GENERATE_XML: 'XML GENERADO' };

function translateAuditDetail(detail) {
  return detail
    .replace(/^XML generated for (FATCA|CRS) report\.$/, 'XML generado para el reporte $1.')
    .replace(/^(FATCA|CRS) report created with (\d+) records\.$/, 'Reporte $1 creado con $2 registros.')
    .replace(/^Client (.+) registered\.$/, 'Cliente $1 registrado.');
}

async function loadSession() {
  const session = await fetch('/api/session/me').then(r => r.json());
  $('#login-form').hidden = session.authenticated;
  $('#session-info').hidden = !session.authenticated;
  $('#session-info').style.display = session.authenticated ? 'flex' : 'none';
  $('#session-user').textContent = session.authenticated ? `${session.username} · ${session.role}` : '';
  return session;
}

async function loadDashboard() {
  const [health, clients, reports, audit] = await Promise.all([
    fetch('/api/health').then(r => r.json()),
    fetch('/api/clients').then(r => r.json()),
    fetch('/api/reports').then(r => r.json()),
    fetch('/api/audit').then(r => r.json())
  ]);

  $('#api-status').textContent = health.status === 'healthy' ? '● API disponible' : 'API no disponible';
  $('#api-status').classList.toggle('online', health.status === 'healthy');
  $('#client-count').textContent = clients.length;
  $('#report-count').textContent = reports.length;
  $('#audit-count').textContent = audit.length;

  $('#clients').innerHTML = clients.map(client => `
    <tr>
      <td><strong>${client.legalName}</strong><br><small>${client.taxIdentificationNumber}</small></td>
      <td>${client.countryCode}</td>
      <td>${formatMoney(client.accountBalance, client.currency)}</td>
    </tr>`).join('');

  $('#audit').innerHTML = audit.slice(0, 5).map(event => `
    <li><strong>${actionLabels[event.action] ?? event.action}</strong>${translateAuditDetail(event.detail)}</li>`).join('') || '<li>Sin actividad todavía.</li>';

  $('#reports').innerHTML = reports.map(report => `
    <div class="report">
      <div><strong>${report.standard} · ${report.reportingYear}</strong><small>${report.clientIds.length} registros · ${statusLabels[report.status] ?? report.status}</small></div>
      <a class="download" href="/api/reports/${report.id}/xml" target="_blank">Ver XML</a>
    </div>`).join('') || '<p>Aún no se han creado reportes.</p>';
}

document.querySelectorAll('[data-standard]').forEach(button => {
  button.addEventListener('click', async () => {
    button.disabled = true;
    const response = await fetch('/api/reports', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ standard: button.dataset.standard })
    });
    button.disabled = false;
    if (response.status === 401) {
      alert('Inicia sesión como analista para crear reportes.');
      return;
    }
    await loadDashboard();
  });
});

$('#login-form').addEventListener('submit', async event => {
  event.preventDefault();
  const values = Object.fromEntries(new FormData(event.currentTarget));
  const response = await fetch('/api/session/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values)
  });
  if (!response.ok) {
    alert('Credenciales incorrectas.');
    return;
  }
  await loadSession();
});

$('#logout').addEventListener('click', async () => {
  await fetch('/api/session/logout', { method: 'POST' });
  await loadSession();
});

Promise.all([loadSession(), loadDashboard()]).catch(() => {
  $('#api-status').textContent = 'API no disponible';
});
