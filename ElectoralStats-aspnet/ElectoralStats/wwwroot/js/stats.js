const charts = {};
function mkChart(id, type, labels, data, label) {
  const ctx = document.getElementById(id);
  if (charts[id]) { charts[id].data.labels = labels; charts[id].data.datasets[0].data = data; charts[id].update(); return; }
  charts[id] = new Chart(ctx, {
    type,
    data: { labels, datasets: [{ label, data, backgroundColor: ['#0d6efd','#198754','#dc3545','#ffc107','#6f42c1','#20c997','#fd7e14','#0dcaf0'] }] },
    options: { responsive:true, maintainAspectRatio:false, plugins:{ legend:{ display: type!=='bar' } } }
  });
}
function apply(s) {
  document.getElementById('kpiTotal').textContent = s.total;
  document.getElementById('kpiIns').textContent = s.inscriptions;
  document.getElementById('kpiRad').textContent = s.radiations;
  document.getElementById('kpiMod').textContent = s.modifications;
  mkChart('chartDay','line', Object.keys(s.perDay), Object.values(s.perDay),'Per Day');
  mkChart('chartType','doughnut', Object.keys(s.perType), Object.values(s.perType),'Type');
  mkChart('chartCommune','bar', Object.keys(s.perCommune), Object.values(s.perCommune),'Commune');
  mkChart('chartCircum','bar', Object.keys(s.perCircumscription), Object.values(s.perCircumscription),'Circumscription');
  mkChart('chartGender','pie', Object.keys(s.perGender), Object.values(s.perGender),'Gender');
  mkChart('chartAge','bar', Object.keys(s.perAgeBucket), Object.values(s.perAgeBucket),'Age');
}
async function refresh() { const r = await fetch('/Stats/Data'); apply(await r.json()); }
refresh();

const conn = new signalR.HubConnectionBuilder().withUrl('/hubs/stats').withAutomaticReconnect().build();
conn.on('StatsUpdated', apply);
conn.start();

document.getElementById('resetBtn').addEventListener('click', async () => {
  if (!confirm('Reset all data?')) return;
  await fetch('/Stats/Reset', { method:'POST' });
  refresh();
});
