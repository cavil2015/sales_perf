import { ManagerRatingDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency } from "../utils/formatters";

interface ManagerLeaderboardProps {
  managers: ManagerRatingDto[] | null;
  loading: boolean;
}

export function ManagerLeaderboard({
  managers,
  loading,
}: ManagerLeaderboardProps) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-100 p-6">
      <h2 className="text-lg font-semibold mb-4">Top Managers</h2>
      {loading && managers === null ? (
        <div className="space-y-4 animate-pulse">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-10 bg-gray-100 rounded"></div>
          ))}
        </div>
      ) : managers === null ? (
        <div className="text-red-400 text-sm">Failed to load managers</div>
      ) : managers.length === 0 ? (
        <div className="text-slate-400 text-sm">No managers active</div>
      ) : (
        <div className="space-y-3">
          {managers.slice(0, 7).map((m) => (
            <div
              key={m.managerId}
              className="flex items-center justify-between p-2 hover:bg-slate-50 rounded-lg transition-colors"
            >
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold text-sm overflow-hidden shrink-0">
                  {m.avatarUrl ? (
                    <img
                      src={m.avatarUrl}
                      alt={m.managerName}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    Array.from(m.managerName || "?")[0]
                  )}
                </div>
                <div className="min-w-0">
                  <p
                    className="font-medium text-sm truncate"
                    title={m.managerName}
                  >
                    {m.managerName}
                  </p>
                  <p className="text-xs text-slate-500">{m.salesCount} deals</p>
                </div>
              </div>
              <div className="text-right">
                <p className="font-semibold text-sm">
                  {formatCurrency(m.grossProfit)}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
