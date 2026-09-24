import { useState } from 'react';
import { useAnalyticsDashboard, DateFilter } from './hooks/useAnalyticsDashboard';
import { KpiGrid } from './components/KpiGrid';
import { SalesChart } from './components/SalesChart';
import { ManagerLeaderboard } from './components/ManagerLeaderboard';
import { RecentSalesTable } from './components/RecentSalesTable';

function App() {
  const [dateRange, setDateRange] = useState<DateFilter>('30days');
  
  // Custom Hook fully encapsulates fetching, loading states, and error handling
  const { loading, error, kpis, managers, chartData, recentSales } = useAnalyticsDashboard(dateRange);

  if (error) {
    return (
      <div className="flex h-screen items-center justify-center bg-gray-50">
        <div className="rounded-lg bg-red-50 p-6 shadow text-center text-red-600">
          <h2 className="text-xl font-semibold mb-2">Failed to load dashboard</h2>
          <p>{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 text-slate-800 font-sans p-6">
      <header className="max-w-7xl mx-auto mb-8 flex justify-between items-center">
        <div className="flex items-center gap-4">
            <h1 className="text-3xl font-bold text-slate-900 tracking-tight">Sales Performance</h1>
            {loading && kpis && (
                <span className="flex h-3 w-3 relative mt-1" aria-hidden="true">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-3 w-3 bg-blue-500"></span>
                </span>
            )}
        </div>
        <select 
          aria-label="Select Date Range"
          value={dateRange}
          onChange={(e) => setDateRange(e.target.value as DateFilter)}
          className="border-gray-300 rounded-md shadow-sm text-sm focus:ring-blue-500 focus:border-blue-500 py-2 px-3 border bg-white cursor-pointer"
        >
          <option value="30days">Last 30 Days</option>
          <option value="month">This Month</option>
          <option value="12months">Last 12 Months</option>
        </select>
      </header>

      <main className="max-w-7xl mx-auto space-y-6">
        <KpiGrid kpis={kpis} loading={loading} />

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <SalesChart chartData={chartData} loading={loading} />
          <ManagerLeaderboard managers={managers} loading={loading} />
        </div>

        <RecentSalesTable recentSales={recentSales} loading={loading} />
      </main>
    </div>
  );
}

export default App;
