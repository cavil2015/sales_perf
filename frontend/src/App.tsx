import { useState, useMemo } from "react";
import { useAnalyticsDashboard } from "./hooks/useAnalyticsDashboard";
import { KpiGrid } from "./components/KpiGrid";
import { SalesChart } from "./components/SalesChart";
import { ManagerLeaderboard } from "./components/ManagerLeaderboard";
import { RecentSalesTable } from "./components/RecentSalesTable";
import { CategoryAnalytics } from "./components/CategoryAnalytics";

type Preset = 'today' | '7days' | '30days' | 'this_month' | 'last_month' | 'custom';

function getPresetDates(preset: Preset): { from?: string, to?: string } {
  const now = new Date();
  
  if (preset === 'today') {
    const today = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
    return { from: today.toISOString() };
  }
  if (preset === '7days') {
    const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - 7));
    return { from: d.toISOString() };
  }
  if (preset === '30days') {
    const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - 30));
    return { from: d.toISOString() };
  }
  if (preset === 'this_month') {
    const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1));
    return { from: d.toISOString() };
  }
  if (preset === 'last_month') {
    const from = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1));
    const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 0, 23, 59, 59, 999));
    return { from: from.toISOString(), to: to.toISOString() };
  }
  return {};
}

export default function App() {
  const [preset, setPreset] = useState<Preset>('30days');
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');

  const dateArgs = useMemo(() => {
    if (preset === 'custom') {
      const fromIso = customFrom ? new Date(customFrom + 'T00:00:00Z').toISOString() : undefined;
      const toIso = customTo ? new Date(customTo + 'T23:59:59Z').toISOString() : undefined;
      return { from: fromIso, to: toIso };
    }
    return getPresetDates(preset);
  }, [preset, customFrom, customTo]);

  // Custom Hook fully encapsulates fetching, loading states, and error handling
  const { loading, error, kpis, managers, chartData, recentSales, categoryData } =
    useAnalyticsDashboard(dateArgs.from, dateArgs.to);

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
      <header className="max-w-7xl mx-auto mb-8 flex flex-col md:flex-row md:justify-between md:items-center gap-4">
        <div className="flex items-center gap-4">
          <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
            Sales Performance
          </h1>
          {loading && (
            <span className="flex h-3 w-3 relative mt-1" aria-hidden="true">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-3 w-3 bg-blue-500"></span>
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <select
            aria-label="Select Date Range"
            value={preset}
            onChange={(e) => setPreset(e.target.value as Preset)}
            className="border-gray-300 rounded-md shadow-sm text-sm focus:ring-blue-500 focus:border-blue-500 py-2 px-3 border bg-white cursor-pointer"
          >
            <option value="today">Today</option>
            <option value="7days">Last 7 Days</option>
            <option value="30days">Last 30 Days</option>
            <option value="this_month">This Month</option>
            <option value="last_month">Last Month</option>
            <option value="custom">Custom Range...</option>
          </select>
          {preset === 'custom' && (
            <div className="flex items-center gap-2">
              <input 
                type="date" 
                value={customFrom}
                onChange={(e) => setCustomFrom(e.target.value)}
                className="border-gray-300 rounded-md shadow-sm text-sm focus:ring-blue-500 focus:border-blue-500 py-2 px-3 border bg-white"
              />
              <span className="text-gray-500">to</span>
              <input 
                type="date" 
                value={customTo}
                onChange={(e) => setCustomTo(e.target.value)}
                className="border-gray-300 rounded-md shadow-sm text-sm focus:ring-blue-500 focus:border-blue-500 py-2 px-3 border bg-white"
              />
            </div>
          )}
        </div>
      </header>

      <main className="max-w-7xl mx-auto space-y-6">
        <KpiGrid kpis={kpis} loading={loading} />

        <SalesChart chartData={chartData} loading={loading} />
        
        <ManagerLeaderboard managers={managers} loading={loading} />

        <CategoryAnalytics 
          categories={categoryData?.categories || null} 
          topProducts={categoryData?.topProducts || null} 
          loading={loading} 
        />

        <RecentSalesTable recentSales={recentSales} loading={loading} />
      </main>
    </div>
  );
}
