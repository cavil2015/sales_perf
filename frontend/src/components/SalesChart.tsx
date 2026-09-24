import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { ChartDataDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency, safeFormatDate } from "../utils/formatters";

interface SalesChartProps {
  chartData: ChartDataDto[] | null;
  loading: boolean;
}

export function SalesChart({ chartData, loading }: SalesChartProps) {
  return (
    <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-slate-100 p-6 min-h-[400px] flex flex-col">
      <h2 className="text-lg font-semibold mb-4">Revenue Dynamics</h2>
      {loading && chartData === null ? (
        <div className="h-full w-full flex items-center justify-center text-gray-400">
          Loading chart...
        </div>
      ) : chartData === null ? (
        <div className="h-full w-full flex items-center justify-center text-red-400">
          Failed to load chart
        </div>
      ) : chartData.length === 0 ? (
        <div className="h-full w-full flex items-center justify-center text-slate-400">
          No sales in this period
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
                tickFormatter={(v) => `$${Number(v)}`}
              />
              <Tooltip
                cursor={{ fill: "#f8fafc" }}
                formatter={(val: any) => [formatCurrency(val), "Revenue"]}
                labelFormatter={(l) =>
                  safeFormatDate(l as string, "MMM d, yyyy")
                }
              />
              <Bar dataKey="revenue" fill="#3b82f6" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
