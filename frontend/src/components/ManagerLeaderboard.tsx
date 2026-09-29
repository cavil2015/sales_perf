import { useState, useMemo } from "react";
import { ManagerRatingDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency, formatPercent } from "../utils/formatters";

interface ManagerLeaderboardProps {
  managers: ManagerRatingDto[] | null;
  loading: boolean;
}

type RankMode = 'grossProfit' | 'averageCheck';

export function ManagerLeaderboard({
  managers,
  loading,
}: ManagerLeaderboardProps) {
  const [rankMode, setRankMode] = useState<RankMode>('grossProfit');

  const sortedManagers = useMemo(() => {
    if (!managers) return [];
    return [...managers].sort((a, b) => {
      const valA = Number(a[rankMode]);
      const valB = Number(b[rankMode]);
      return valB - valA;
    });
  }, [managers, rankMode]);

  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-100 p-6 flex flex-col h-full">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Top Managers</h2>
        <select
          value={rankMode}
          onChange={(e) => setRankMode(e.target.value as RankMode)}
          className="text-sm border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 py-1 px-2 border bg-white cursor-pointer"
        >
          <option value="grossProfit">By Profit</option>
          <option value="averageCheck">By Avg Check</option>
        </select>
      </div>

      {loading && managers === null ? (
        <div className="space-y-4 animate-pulse mt-2">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-12 bg-gray-100 rounded"></div>
          ))}
        </div>
      ) : managers === null ? (
        <div className="text-red-400 text-sm mt-4">Failed to load managers</div>
      ) : managers.length === 0 ? (
        <div className="text-slate-400 text-sm mt-4">No managers active</div>
      ) : (
        <div className="space-y-2 overflow-y-auto pr-2 flex-1">
          {sortedManagers.slice(0, 7).map((m, idx) => {
            const diff = rankMode === 'grossProfit' ? Number(m.grossProfitDiff || 0) : Number(m.averageCheckDiff || 0);
            return (
              <div
                key={m.managerId}
                className="flex items-center justify-between p-3 hover:bg-slate-50 rounded-lg transition-colors border border-transparent hover:border-slate-100"
              >
                <div className="flex items-center gap-3">
                  <span className="text-sm font-bold text-slate-400 w-4">{idx + 1}</span>
                  <div className="w-10 h-10 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold text-sm overflow-hidden shrink-0">
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
                      className="font-medium text-sm text-slate-900 truncate flex items-center gap-2"
                      title={m.managerName}
                    >
                      {m.managerName}
                      {diff !== 0 && (
                        <span className={`text-[10px] font-bold ${diff > 0 ? "text-emerald-500" : "text-red-500"}`}>
                          {diff > 0 ? "↑" : "↓"} {formatPercent(Math.abs(diff))}
                        </span>
                      )}
                    </p>
                    <p className="text-xs text-slate-500 flex gap-2">
                      <span>{m.salesCount} deals</span>
                      <span>•</span>
                      <span className="text-emerald-600">{formatPercent(m.margin)} margin</span>
                    </p>
                  </div>
                </div>
                <div className="text-right shrink-0">
                  <p className="font-semibold text-sm text-slate-900">
                    {rankMode === 'grossProfit' ? formatCurrency(m.grossProfit) : formatCurrency(m.averageCheck)}
                  </p>
                  <p className="text-xs text-slate-500">
                    {rankMode === 'grossProfit' ? 'Profit' : 'Avg Check'}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
