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
    <div className="bg-white rounded-xl shadow-sm border border-slate-100 p-6 flex flex-col h-full lg:col-span-3">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Manager Leaderboard</h2>
        <select
          value={rankMode}
          onChange={(e) => setRankMode(e.target.value as RankMode)}
          className="text-sm border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 py-1 px-2 border bg-white cursor-pointer"
        >
          <option value="grossProfit">Rank by Profit</option>
          <option value="averageCheck">Rank by Avg Check</option>
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
        <div className="flex-1 flex items-center justify-center text-slate-400">No data available</div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[900px]">
            <thead>
              <tr className="text-xs font-semibold text-slate-500 uppercase tracking-wider border-b border-slate-100">
                <th className="pb-3 pr-4 w-10">#</th>
                <th className="pb-3 px-4">Manager</th>
                <th className="pb-3 px-4 text-right">Sales</th>
                <th className="pb-3 px-4 text-right">Revenue</th>
                <th className="pb-3 px-4 text-right">Profit</th>
                <th className="pb-3 px-4 text-right">Avg Check</th>
                <th className="pb-3 px-4 text-right">Margin</th>
                <th className="pb-3 pl-4 text-right">Diff (Trend)</th>
              </tr>
            </thead>
            <tbody className="text-sm">
              {sortedManagers.map((m, idx) => {
                const diff = rankMode === 'grossProfit' ? Number(m.grossProfitDiff || 0) : Number(m.averageCheckDiff || 0);
                return (
                  <tr
                    key={m.managerId}
                    className="border-b border-slate-50 hover:bg-slate-50 transition-colors"
                  >
                    <td className="py-3 pr-4 text-slate-400 font-bold">{idx + 1}</td>
                    <td className="py-3 px-4">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold text-xs overflow-hidden shrink-0">
                          {m.avatarUrl ? (
                            <img src={m.avatarUrl} alt={m.managerName} className="w-full h-full object-cover" />
                          ) : (
                            Array.from(m.managerName || "?")[0]
                          )}
                        </div>
                        <span className="font-medium text-slate-900">{m.managerName}</span>
                      </div>
                    </td>
                    <td className="py-3 px-4 text-right">{m.salesCount}</td>
                    <td className="py-3 px-4 text-right">{formatCurrency(m.revenue)}</td>
                    <td className={`py-3 px-4 text-right font-semibold ${rankMode === 'grossProfit' ? 'text-blue-600' : ''}`}>
                      {formatCurrency(m.grossProfit)}
                    </td>
                    <td className={`py-3 px-4 text-right font-semibold ${rankMode === 'averageCheck' ? 'text-blue-600' : ''}`}>
                      {formatCurrency(m.averageCheck)}
                    </td>
                    <td className="py-3 px-4 text-right text-emerald-600 font-medium">
                      {formatPercent(m.margin)}
                    </td>
                    <td className="py-3 pl-4 text-right">
                      {diff !== 0 ? (
                        <span className={`inline-flex items-center gap-1 font-bold ${diff > 0 ? "text-emerald-500" : "text-red-500"}`}>
                          {diff > 0 ? "â†‘" : "â†“"} {formatPercent(Math.abs(diff))}
                        </span>
                      ) : (
                        <span className="text-slate-400">â€“</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

