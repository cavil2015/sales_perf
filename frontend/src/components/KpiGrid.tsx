import { KpiDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency, formatPercent } from "../utils/formatters";

interface KpiGridProps {
  kpis: KpiDto | null;
  loading: boolean;
}

export function KpiGrid({ kpis, loading }: KpiGridProps) {
  if (loading && kpis === null) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4 animate-pulse">
        {[...Array(5)].map((_, i) => (
          <div
            key={i}
            className="h-28 bg-white rounded-xl shadow-sm border border-slate-100"
          ></div>
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
      <KpiCard
        title="Revenue"
        value={kpis ? formatCurrency(kpis.revenue) : "N/A"}
        trend={kpis?.revenueDiff}
      />
      <KpiCard
        title="Gross Profit"
        value={kpis ? formatCurrency(kpis.grossProfit) : "N/A"}
        trend={kpis?.grossProfitDiff}
      />
      <KpiCard
        title="Avg Check"
        value={kpis ? formatCurrency(kpis.averageCheck) : "N/A"}
        trend={kpis?.averageCheckDiff}
      />
      <KpiCard 
        title="Sales Count" 
        value={kpis ? kpis.salesCount : "N/A"} 
        trend={kpis?.salesCountDiff}
      />
      <KpiCard
        title="Top Manager"
        value={kpis?.topManager || "N/A"}
        isHighlight
      />
    </div>
  );
}

function KpiCard({
  title,
  value,
  trend,
  isHighlight = false,
}: {
  title: string;
  value: React.ReactNode;
  trend?: number | string;
  isHighlight?: boolean;
}) {
  const numTrend = trend ? Number(trend) : undefined;
  
  return (
    <div
      className={`p-5 rounded-xl shadow-sm border transition-all hover:shadow-md ${isHighlight ? "bg-gradient-to-br from-blue-600 to-indigo-700 text-white border-transparent" : "bg-white border-slate-100"}`}
    >
      <div className="flex items-center justify-between mb-1">
        <h3
          className={`text-sm font-medium ${isHighlight ? "text-blue-100" : "text-slate-500"}`}
        >
          {title}
        </h3>
        {numTrend !== undefined && !isHighlight && (
          <div className={`flex items-center text-xs font-medium ${numTrend > 0 ? "text-emerald-600" : numTrend < 0 ? "text-red-500" : "text-slate-400"}`}>
            {numTrend > 0 ? "↑ " : numTrend < 0 ? "↓ " : "– "}
            {numTrend !== 0 ? formatPercent(Math.abs(numTrend)) : ""}
          </div>
        )}
      </div>
      <p
        className={`text-2xl font-bold tracking-tight ${isHighlight ? "text-white" : "text-slate-900"}`}
      >
        {value}
      </p>
    </div>
  );
}
