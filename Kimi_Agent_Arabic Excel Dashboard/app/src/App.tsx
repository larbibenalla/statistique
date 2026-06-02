import { useState, useMemo, useRef, useEffect } from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js';
import { Line, Bar, Doughnut, Pie } from 'react-chartjs-2';
import { format, parseISO } from 'date-fns';
import { ar } from 'date-fns/locale';
import sampleData from './data/sample-data.json';
import {
  Upload, Search, Filter, Download, X, ChevronRight, ChevronLeft,
  BarChart3, Users, CheckCircle, XCircle, Clock, FileSpreadsheet,
  MapPin, GraduationCap, Heart, GitBranch, Calendar
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';

ChartJS.register(
  CategoryScale, LinearScale, PointElement, LineElement,
  BarElement, ArcElement, Title, Tooltip, Legend, Filler
);

ChartJS.defaults.font.family = "'Cairo', sans-serif";
ChartJS.defaults.color = '#374151';

const COLORS = {
  primary: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899', '#06B6D4', '#84CC16'],
  bg: ['rgba(59,130,246,0.7)', 'rgba(16,185,129,0.7)', 'rgba(245,158,11,0.7)', 
       'rgba(239,68,68,0.7)', 'rgba(139,92,246,0.7)', 'rgba(236,72,153,0.7)',
       'rgba(6,182,212,0.7)', 'rgba(132,204,22,0.7)']
};

interface Inscription {
  request_number: string;
  request_date: string;
  request_type: string;
  citizen_category: string;
  national_id: string;
  last_name: string;
  first_name: string;
  gender: string;
  birth_date: string;
  birth_country: string;
  birth_city: string;
  birth_province: string;
  birth_commune: string;
  residence_country: string;
  residence_city: string;
  residence_province: string;
  residence_commune: string;
  residence_address: string;
  marital_status: string;
  education_level: string;
  registration_province: string;
  registration_commune: string;
  registration_district: string;
  registration_office: string;
  registration_annex: string;
  status: string;
  status_reason: string;
  status_date: string;
}

function App() {
  const [data] = useState<Inscription[]>(sampleData as Inscription[]);
  const [filters, setFilters] = useState({
    dateFrom: '',
    dateTo: '',
    province: '',
    commune: '',
    gender: '',
    requestType: '',
    status: '',
    search: '',
  });
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [showFilters, setShowFilters] = useState(false);

  const filtered = useMemo(() => {
    let d = [...data];
    if (filters.dateFrom) d = d.filter(x => x.request_date >= filters.dateFrom);
    if (filters.dateTo) d = d.filter(x => x.request_date <= filters.dateTo);
    if (filters.province) d = d.filter(x => x.residence_province === filters.province);
    if (filters.commune) d = d.filter(x => x.residence_commune === filters.commune);
    if (filters.gender) d = d.filter(x => x.gender === filters.gender);
    if (filters.requestType) d = d.filter(x => x.request_type === filters.requestType);
    if (filters.status) d = d.filter(x => x.status === filters.status);
    if (filters.search) {
      const term = filters.search.toLowerCase();
      d = d.filter(x =>
        x.first_name?.toLowerCase().includes(term) ||
        x.last_name?.toLowerCase().includes(term) ||
        x.national_id?.includes(term) ||
        x.request_number?.toLowerCase().includes(term)
      );
    }
    return d;
  }, [data, filters]);

  const totalPages = Math.ceil(filtered.length / pageSize);
  const paged = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const stats = useMemo(() => {
    const approved = filtered.filter(x => x.status.includes('مقبول')).length;
    const rejected = filtered.filter(x => x.status.includes('مرفوض')).length;
    const pending = filtered.filter(x => x.status.includes('قيد') || x.status.includes('معلق')).length;
    return {
      total: filtered.length,
      approved,
      rejected,
      pending,
      rate: filtered.length > 0 ? Math.round((approved / filtered.length) * 100) : 0,
    };
  }, [filtered]);

  const dailyChart = useMemo(() => {
    const grouped: Record<string, number> = {};
    filtered.forEach(x => {
      if (x.request_date) {
        grouped[x.request_date] = (grouped[x.request_date] || 0) + 1;
      }
    });
    const sorted = Object.entries(grouped).sort((a, b) => a[0].localeCompare(b[0]));
    return {
      labels: sorted.map(([d]) => format(parseISO(d), 'dd/MM', { locale: ar })),
      data: sorted.map(([, v]) => v),
    };
  }, [filtered]);

  const groupBy = (field: keyof Inscription) => {
    const grouped: Record<string, number> = {};
    filtered.forEach(x => {
      const val = x[field] as string;
      if (val) grouped[val] = (grouped[val] || 0) + 1;
    });
    return Object.entries(grouped)
      .map(([label, value]) => ({ label, value }))
      .sort((a, b) => b.value - a.value);
  };

  const provinceChart = groupBy('residence_province').slice(0, 10);
  const genderChart = groupBy('gender');
  const typeChart = groupBy('request_type');
  const statusChart = groupBy('status');
  const maritalChart = groupBy('marital_status');
  const eduChart = groupBy('education_level');
  const rejectionChart = filtered.filter(x => x.status.includes('مرفوض') && x.status_reason)
    .reduce((acc, x) => { acc[x.status_reason] = (acc[x.status_reason] || 0) + 1; return acc; }, {} as Record<string, number>);

  const provinces = [...new Set(data.map(x => x.residence_province).filter(Boolean))].sort();
  const communes = [...new Set(data.map(x => x.residence_commune).filter(Boolean))].sort();
  const genders = [...new Set(data.map(x => x.gender).filter(Boolean))].sort();
  const requestTypes = [...new Set(data.map(x => x.request_type).filter(Boolean))].sort();
  const statuses = [...new Set(data.map(x => x.status).filter(Boolean))].sort();

  const handleExport = () => {
    const headers = ['رقم الطلب', 'تاريخ الطلب', 'نوع الطلب', 'الإسم الكامل', 'الجنس', 'العمالة/الإقليم', 'الجماعة', 'الوضعية'];
    const rows = filtered.map(x => [
      x.request_number, x.request_date, x.request_type,
      `${x.first_name} ${x.last_name}`, x.gender,
      x.residence_province, x.residence_commune, x.status
    ]);
    const csv = [headers, ...rows].map(r => r.join(',')).join('\n');
    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `inscriptions_${format(new Date(), 'yyyyMMdd_HHmmss')}.csv`;
    a.click();
  };

  const getStatusColor = (s: string) => {
    if (s.includes('مقبول')) return 'bg-emerald-100 text-emerald-800';
    if (s.includes('مرفوض')) return 'bg-red-100 text-red-800';
    if (s.includes('قيد') || s.includes('معلق')) return 'bg-amber-100 text-amber-800';
    return 'bg-gray-100 text-gray-800';
  };

  useEffect(() => { setCurrentPage(1); }, [filters]);

  return (
    <div className="min-h-screen bg-slate-50" dir="rtl">
      {/* Navbar */}
      <nav className="bg-blue-600 text-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <BarChart3 className="w-6 h-6" />
            <h1 className="text-xl font-bold">لوحة تحكم التسجيلات الانتخابية</h1>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-sm opacity-90">{format(new Date(), 'yyyy/MM/dd', { locale: ar })}</span>
          </div>
        </div>
      </nav>

      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Upload & Actions */}
        <div className="flex flex-wrap gap-3 mb-6">
          <input
            ref={fileInputRef}
            type="file"
            accept=".xlsx,.xls"
            className="hidden"
            onChange={(e) => console.log('File selected:', e.target.files?.[0]?.name)}
          />
          <button
            onClick={() => fileInputRef.current?.click()}
            className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
          >
            <Upload className="w-4 h-4" />
            رفع ملف Excel
          </button>
          <button
            onClick={handleExport}
            className="flex items-center gap-2 bg-emerald-600 text-white px-4 py-2 rounded-lg hover:bg-emerald-700 transition-colors"
          >
            <Download className="w-4 h-4" />
            تصدير CSV
          </button>
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="flex items-center gap-2 bg-slate-600 text-white px-4 py-2 rounded-lg hover:bg-slate-700 transition-colors"
          >
            <Filter className="w-4 h-4" />
            عوامل التصفية
          </button>
        </div>

        {/* Filters */}
        {showFilters && (
          <Card className="mb-6">
            <CardContent className="p-4">
              <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
                <div>
                  <label className="text-sm text-gray-500 block mb-1">من تاريخ</label>
                  <input type="date" className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.dateFrom} onChange={e => setFilters(f => ({ ...f, dateFrom: e.target.value }))} />
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">إلى تاريخ</label>
                  <input type="date" className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.dateTo} onChange={e => setFilters(f => ({ ...f, dateTo: e.target.value }))} />
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">العمالة/الإقليم</label>
                  <select className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.province} onChange={e => setFilters(f => ({ ...f, province: e.target.value }))}>
                    <option value="">الكل</option>
                    {provinces.map(p => <option key={p} value={p}>{p}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">الجماعة</label>
                  <select className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.commune} onChange={e => setFilters(f => ({ ...f, commune: e.target.value }))}>
                    <option value="">الكل</option>
                    {communes.map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">الجنس</label>
                  <select className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.gender} onChange={e => setFilters(f => ({ ...f, gender: e.target.value }))}>
                    <option value="">الكل</option>
                    {genders.map(g => <option key={g} value={g}>{g}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">نوع الطلب</label>
                  <select className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.requestType} onChange={e => setFilters(f => ({ ...f, requestType: e.target.value }))}>
                    <option value="">الكل</option>
                    {requestTypes.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">الوضعية</label>
                  <select className="w-full border rounded-lg px-3 py-2 text-sm"
                    value={filters.status} onChange={e => setFilters(f => ({ ...f, status: e.target.value }))}>
                    <option value="">الكل</option>
                    {statuses.map(s => <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-sm text-gray-500 block mb-1">بحث</label>
                  <div className="relative">
                    <Search className="w-4 h-4 absolute right-3 top-2.5 text-gray-400" />
                    <input type="text" placeholder="بحث..." className="w-full border rounded-lg px-3 py-2 pr-9 text-sm"
                      value={filters.search} onChange={e => setFilters(f => ({ ...f, search: e.target.value }))} />
                  </div>
                </div>
              </div>
              <div className="mt-3 flex gap-2">
                <button onClick={() => setFilters({ dateFrom: '', dateTo: '', province: '', commune: '', gender: '', requestType: '', status: '', search: '' })}
                  className="flex items-center gap-1 text-sm text-red-600 hover:text-red-700">
                  <X className="w-3 h-3" /> إلغاء التصفية
                </button>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Stats Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <Card className="stat-card bg-gradient-to-br from-blue-500 to-blue-600 text-white">
            <CardContent className="p-4">
              <div className="flex justify-between items-center">
                <div>
                  <p className="text-blue-100 text-sm">إجمالي التسجيلات</p>
                  <p className="text-3xl font-bold">{stats.total.toLocaleString()}</p>
                </div>
                <Users className="w-10 h-10 text-blue-200 opacity-50" />
              </div>
            </CardContent>
          </Card>
          <Card className="stat-card bg-gradient-to-br from-emerald-500 to-emerald-600 text-white">
            <CardContent className="p-4">
              <div className="flex justify-between items-center">
                <div>
                  <p className="text-emerald-100 text-sm">المقبولة</p>
                  <p className="text-3xl font-bold">{stats.approved.toLocaleString()}</p>
                  <p className="text-emerald-100 text-xs">{stats.rate}%</p>
                </div>
                <CheckCircle className="w-10 h-10 text-emerald-200 opacity-50" />
              </div>
            </CardContent>
          </Card>
          <Card className="stat-card bg-gradient-to-br from-red-500 to-red-600 text-white">
            <CardContent className="p-4">
              <div className="flex justify-between items-center">
                <div>
                  <p className="text-red-100 text-sm">المرفوضة</p>
                  <p className="text-3xl font-bold">{stats.rejected.toLocaleString()}</p>
                </div>
                <XCircle className="w-10 h-10 text-red-200 opacity-50" />
              </div>
            </CardContent>
          </Card>
          <Card className="stat-card bg-gradient-to-br from-amber-500 to-amber-600 text-white">
            <CardContent className="p-4">
              <div className="flex justify-between items-center">
                <div>
                  <p className="text-amber-100 text-sm">قيد المعالجة</p>
                  <p className="text-3xl font-bold">{stats.pending.toLocaleString()}</p>
                </div>
                <Clock className="w-10 h-10 text-amber-200 opacity-50" />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Charts Row 1 */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6">
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Calendar className="w-4 h-4" /> التسجيلات اليومية
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Line data={{
                labels: dailyChart.labels,
                datasets: [{
                  label: 'عدد التسجيلات',
                  data: dailyChart.data,
                  borderColor: '#3B82F6',
                  backgroundColor: 'rgba(59,130,246,0.1)',
                  fill: true,
                  tension: 0.4,
                }]
              }} options={{
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
              }} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Users className="w-4 h-4" /> التوزيع حسب الجنس
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Doughnut data={{
                labels: genderChart.map(d => d.label),
                datasets: [{
                  data: genderChart.map(d => d.value),
                  backgroundColor: ['#3B82F6', '#EC4899'],
                }]
              }} options={{
                responsive: true,
                plugins: { legend: { position: 'bottom' } }
              }} />
            </CardContent>
          </Card>
        </div>

        {/* Charts Row 2 */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <MapPin className="w-4 h-4" /> حسب العمالة/الإقليم
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Bar data={{
                labels: provinceChart.map(d => d.label),
                datasets: [{
                  label: 'التسجيلات',
                  data: provinceChart.map(d => d.value),
                  backgroundColor: COLORS.bg,
                }]
              }} options={{
                responsive: true,
                indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true, ticks: { precision: 0 } } }
              }} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <FileSpreadsheet className="w-4 h-4" /> الوضعية
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Bar data={{
                labels: statusChart.map(d => d.label),
                datasets: [{
                  label: 'العدد',
                  data: statusChart.map(d => d.value),
                  backgroundColor: statusChart.map(d =>
                    d.label.includes('مقبول') ? 'rgba(16,185,129,0.7)' :
                    d.label.includes('مرفوض') ? 'rgba(239,68,68,0.7)' :
                    d.label.includes('قيد') ? 'rgba(245,158,11,0.7)' : 'rgba(107,114,128,0.7)'
                  ),
                }]
              }} options={{
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
              }} />
            </CardContent>
          </Card>
        </div>

        {/* Charts Row 3 */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <GitBranch className="w-4 h-4" /> نوع الطلب
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Pie data={{
                labels: typeChart.map(d => d.label),
                datasets: [{ data: typeChart.map(d => d.value), backgroundColor: COLORS.bg }]
              }} options={{ responsive: true, plugins: { legend: { position: 'bottom', labels: { boxWidth: 12 } } } }} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Heart className="w-4 h-4" /> الحالة العائلية
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Pie data={{
                labels: maritalChart.map(d => d.label),
                datasets: [{ data: maritalChart.map(d => d.value), backgroundColor: COLORS.bg.slice(2) }]
              }} options={{ responsive: true, plugins: { legend: { position: 'bottom', labels: { boxWidth: 12 } } } }} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <GraduationCap className="w-4 h-4" /> المستوى الدراسي
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Doughnut data={{
                labels: eduChart.map(d => d.label),
                datasets: [{ data: eduChart.map(d => d.value), backgroundColor: COLORS.bg.slice(4) }]
              }} options={{ responsive: true, plugins: { legend: { position: 'bottom', labels: { boxWidth: 12 } } } }} />
            </CardContent>
          </Card>
        </div>

        {/* Rejection Reasons */}
        {Object.keys(rejectionChart).length > 0 && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <XCircle className="w-4 h-4" /> أسباب الرفض
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Bar data={{
                labels: Object.keys(rejectionChart),
                datasets: [{
                  label: 'العدد',
                  data: Object.values(rejectionChart),
                  backgroundColor: 'rgba(239,68,68,0.7)',
                }]
              }} options={{
                responsive: true,
                indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true, ticks: { precision: 0 } } }
              }} />
            </CardContent>
          </Card>
        )}

        {/* Data Table */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base flex items-center gap-2">
              <FileSpreadsheet className="w-4 h-4" /> بيانات التسجيلات
            </CardTitle>
            <span className="bg-slate-100 text-slate-700 px-3 py-1 rounded-full text-sm">
              {filtered.length.toLocaleString()} سجل
            </span>
          </CardHeader>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-slate-800 text-white">
                  <tr>
                    <th className="px-3 py-2 text-right">#</th>
                    <th className="px-3 py-2 text-right">رقم الطلب</th>
                    <th className="px-3 py-2 text-right">التاريخ</th>
                    <th className="px-3 py-2 text-right">نوع الطلب</th>
                    <th className="px-3 py-2 text-right">الإسم</th>
                    <th className="px-3 py-2 text-right">الجنس</th>
                    <th className="px-3 py-2 text-right">العمالة/الإقليم</th>
                    <th className="px-3 py-2 text-right">الجماعة</th>
                    <th className="px-3 py-2 text-right">الوضعية</th>
                  </tr>
                </thead>
                <tbody>
                  {paged.map((item, idx) => (
                    <tr key={item.request_number} className="border-b hover:bg-slate-50">
                      <td className="px-3 py-2">{(currentPage - 1) * pageSize + idx + 1}</td>
                      <td className="px-3 py-2 font-mono text-xs">{item.request_number}</td>
                      <td className="px-3 py-2">{format(parseISO(item.request_date), 'yyyy/MM/dd', { locale: ar })}</td>
                      <td className="px-3 py-2">{item.request_type}</td>
                      <td className="px-3 py-2">{item.first_name} {item.last_name}</td>
                      <td className="px-3 py-2">{item.gender}</td>
                      <td className="px-3 py-2">{item.residence_province}</td>
                      <td className="px-3 py-2">{item.residence_commune}</td>
                      <td className="px-3 py-2">
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${getStatusColor(item.status)}`}>
                          {item.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-center gap-2 p-4 border-t">
                <button
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className="p-1 rounded hover:bg-slate-100 disabled:opacity-30"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
                {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
                  <button
                    key={p}
                    onClick={() => setCurrentPage(p)}
                    className={`w-8 h-8 rounded-lg text-sm font-medium transition-colors ${
                      p === currentPage ? 'bg-blue-600 text-white' : 'hover:bg-slate-100'
                    }`}
                  >
                    {p}
                  </button>
                ))}
                <button
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className="p-1 rounded hover:bg-slate-100 disabled:opacity-30"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Footer */}
        <footer className="text-center text-gray-500 text-sm py-6 mt-6 border-t">
          <p>لوحة تحكم التسجيلات الانتخابية &copy; {new Date().getFullYear()}</p>
          <p className="mt-1 text-xs opacity-70">ASP.NET Core 8 Dashboard Demo with React</p>
        </footer>
      </div>
    </div>
  );
}

export default App;
