import { RecentSaleDto } from '../hooks/useAnalyticsDashboard';
import { formatCurrency } from '../utils/formatters';
import { format } from 'date-fns';

interface RecentSalesTableProps {
  recentSales: RecentSaleDto[] | null;
  loading: boolean;
}

export function RecentSalesTable({ recentSales, loading }: RecentSalesTableProps) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-100 p-6">
      <h2 className="text-lg font-semibold mb-4">Recent Sales</h2>
      {loading && recentSales === null ? (
        <div className="h-40 flex items-center justify-center text-gray-400">Loading sales...</div>
      ) : recentSales === null ? (
        <div className="text-red-400 text-sm">Failed to load sales</div>
      ) : recentSales.length === 0 ? (
        <div className="text-slate-400 text-sm">No recent sales</div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="text-xs font-semibold text-slate-500 uppercase tracking-wider border-b border-slate-100">
                <th className="pb-3 pr-4">Date</th>
                <th className="pb-3 px-4">Manager</th>
                <th className="pb-3 px-4">Customer</th>
                <th className="pb-3 px-4">Status</th>
                <th className="pb-3 pl-4 text-right">Revenue</th>
              </tr>
            </thead>
            <tbody className="text-sm">
              {recentSales.map((sale) => (
                <tr key={sale.id} className="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                  <td className="py-3 pr-4 text-slate-600">{format(new Date(sale.date), 'MMM d, yyyy')}</td>
                  <td className="py-3 px-4 font-medium">{sale.managerName}</td>
                  <td className="py-3 px-4">{sale.customerName}</td>
                  <td className="py-3 px-4">
                    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                      sale.status === 'Paid' ? 'bg-green-100 text-green-800' :
                      sale.status === 'Refunded' ? 'bg-yellow-100 text-yellow-800' :
                      'bg-red-100 text-red-800'
                    }`}>
                      {sale.status}
                    </span>
                  </td>
                  <td className="py-3 pl-4 text-right font-semibold">{formatCurrency(sale.revenue)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
