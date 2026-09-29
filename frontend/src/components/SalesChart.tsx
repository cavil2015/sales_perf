import { useState } from "react";
import { ChartDataDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency, safeFormatDate } from "../utils/formatters";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

interface SalesChartProps {
  chartData: ChartDataDto[] | null;
  loading: boolean;
}

type MetricType = 'revenue' | 'grossProfit' | 'salesCount';

export function SalesChart({ chartData, loading }: SalesChartProps) {
  const [metric, setMetric] = useState<MetricType>('revenue');

  const getMetricConfig = () => {
    switch (metric) {
      case 'grossProfit': return { key: 'grossProfit', color: '#10b981', label: 'Profit' };
      case 'salesCount': return { key: 'salesCount', color: '#f59e0b', label: 'Sales' };
      default: return { key: 'revenue', color: '#3b82f6', label: 'Revenue' };
    }
  };

  const config = getMetricConfig();

  const formatValue = (val: any) => {
    if (metric === 'salesCount') return [val, config.label];
    return [formatCurrency(val), config.label];
  };

  const formatAxis = (v: any) => {
    if (metric === 'salesCount') return v.toString();
    return `$${Number(v)}`;
  };

  return (
    <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-slate-100 p-6 min-h-[400px] flex flex-col">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-6 gap-4">
        <h2 className="text-lg font-semibold">Revenue Dynamics</h2>
        <div className="flex bg-slate-100 p-1 rounded-lg">
          <button
            onClick={() => setMetric('revenue')}
            className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${metric === 'revenue' ? 'bg-white shadow-sm text-blue-600' : 'text-slate-600 hover:text-slate-900'}`}
          >
            Revenue
          </button>
          <button
            onClick={() => setMetric('grossProfit')}
            className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${metric === 'grossProfit' ? 'bg-white shadow-sm text-emerald-600' : 'text-slate-600 hover:text-slate-900'}`}
          >
            Profit
          </button>
          <button
            onClick={() => setMetric('salesCount')}
            className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${metric === 'salesCount' ? 'bg-white shadow-sm text-amber-600' : 'text-slate-600 hover:text-slate-900'}`}
          >
            Sales
          </button>
        </div>
      </div>

      {loading && chartData === null ? (
        <div className="flex-1 flex items-center justify-center text-gray-400">
          Loading chart...
        </div>
      ) : chartData === null ? (
        <div className="flex-1 flex items-center justify-center text-red-400">
          Failed to load chart
        </div>
      ) : chartData.length === 0 ? (
        <div className="flex-1 flex items-center justify-center text-slate-400">
          No data available
        </div>
      ) : (
        <div className="flex-1 min-h-[300px]">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData}>
              <CartesianGrid
                strokeDasharray="3 3"
                vertical={false}
                stroke="#e2e8f0"
              />
              <XAxis
                dataKey="date"
                tick={{ fontSize: 12, fill: "#64748b" }}
                tickFormatter={(v) => safeFormatDate(v, "MMM d")}
              />
              <YAxis
                tick={{ fontSize: 12, fill: "#64748b" }}
                tickFormatter={formatAxis}
              />
              <Tooltip
                cursor={{ fill: "#f1f5f9" }}
                contentStyle={{ borderRadius: "8px", border: "none", boxShadow: "0 4px 6px -1px rgb(0 0 0 / 0.1)" }}
                labelFormatter={(l) => safeFormatDate(l as string, "MMM d, yyyy")}
                formatter={formatValue}
              />
              <Bar dataKey={config.key} fill={config.color} radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}

