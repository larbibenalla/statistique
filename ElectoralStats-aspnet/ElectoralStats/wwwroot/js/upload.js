const form = document.getElementById('upForm');
const results = document.getElementById('results');
const progress = document.getElementById('progress');

form.addEventListener('submit', async (e) => {
  e.preventDefault();
  const fd = new FormData();
  const folderFiles = document.getElementById('files').files;
  const plainFiles = document.getElementById('filesPlain').files;
  const all = [...folderFiles, ...plainFiles].filter(f => /\.(xlsx|xls)$/i.test(f.name));
  if (!all.length) { alert('No Excel files selected'); return; }
  all.forEach(f => fd.append('files', f));
  progress.innerHTML = `<div class="alert alert-info">Uploading ${all.length} files...</div>`;
  const r = await fetch('/Upload/Upload', { method:'POST', body: fd });
  const data = await r.json();
  progress.innerHTML = '';
  results.innerHTML = '<h5>Results</h5><ul class="list-group">' +
    data.results.map(x => `<li class="list-group-item d-flex justify-content-between">
      <span>${x.fileName}</span>
      <span class="badge bg-${x.error?'danger':'success'}">${x.error ?? (x.imported+' imported')}</span>
    </li>`).join('') + '</ul>';
});
