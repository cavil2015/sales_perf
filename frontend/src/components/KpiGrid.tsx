import { KpiDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency } from "../utils/formatters";

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
      />
      <KpiCard
        title="Gross Profit"
        value={kpis ? formatCurrency(kpis.grossProfit) : "N/A"}
      />
      <KpiCard
        title="Avg Check"
        value={kpis ? formatCurrency(kpis.averageCheck) : "N/A"}
      />
      <KpiCard title="Sales Count" value={kpis ? kpis.salesCount : "N/A"} />
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
  isHighlight = false,
}: {
  title: string;
  value: React.ReactNode;
  isHighlight?: boolean;
}) {
  return (
    <div
      className={`p-5 rounded-xl shadow-sm border transition-all hover:shadow-md ${isHighlight ? "bg-gradient-to-br from-blue-600 to-indigo-700 text-white border-transparent" : "bg-white border-slate-100"}`}
    >
      <h3
        className={`text-sm font-medium ${isHighlight ? "text-blue-100" : "text-slate-500"} mb-1`}
      >
        {title}
      </h3>
      <p
        className={`text-2xl font-bold tracking-tight ${isHighlight ? "text-white" : "text-slate-900"}`}
      >
        {value}
      </p>
    </div>
  );
}
