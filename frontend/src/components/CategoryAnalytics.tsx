import { CategoryAnalyticsDto, TopProductDto } from "../hooks/useAnalyticsDashboard";
import { formatCurrency } from "../utils/formatters";
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from "recharts";

interface CategoryAnalyticsProps {
  categories: CategoryAnalyticsDto[] | null;
  topProducts: TopProductDto[] | null;
  loading: boolean;
}

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#06b6d4'];

export function CategoryAnalytics({ categories, topProducts, loading }: CategoryAnalyticsProps) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
      {/* Categories Pie Chart */}
      <div className="bg-white rounded-xl shadow-sm border border-slate-100 p-6 min-h-[300px] flex flex-col">
        <h2 className="text-lg font-semibold mb-4">Revenue by Category</h2>
        {loading && categories === null ? (
          <div className="flex-1 flex items-center justify-center text-gray-400">Loading...</div>
        ) : categories === null ? (
          <div className="flex-1 flex items-center justify-center text-red-400">Error loading categories</div>
        ) : categories.length === 0 ? (
          <div className="flex-1 flex items-center justify-center text-slate-400">No data</div>
        ) : (
          <div className="flex-1">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={categories}
                  dataKey="revenue"
                  nameKey="categoryName"
                  cx="50%"
                  cy="50%"
                  outerRadius={80}
                  fill="#8884d8"
                  label={(entry: any) => entry.categoryName}
                >
                  {categories.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(val: any) => formatCurrency(val)} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>

      {/* Top Products Table */}
      <div className="bg-white rounded-xl shadow-sm border border-slate-100 p-6 flex flex-col">
        <h2 className="text-lg font-semibold mb-4">Top 5 Products</h2>
        {loading && topProducts === null ? (
          <div className="space-y-4 animate-pulse">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-10 bg-gray-100 rounded"></div>
            ))}
          </div>
        ) : topProducts === null ? (
          <div className="text-red-400 text-sm mt-4">Failed to load top products</div>
        ) : topProducts.length === 0 ? (
          <div className="text-slate-400 text-sm mt-4">No products active</div>
        ) : (
          <div className="space-y-3">
            {topProducts.map((p, idx) => (
              <div key={p.productId} className="flex items-center justify-between p-2 hover:bg-slate-50 rounded-lg transition-colors">
                <div className="flex items-center gap-3">
                  <span className="text-sm font-bold text-slate-400 w-4">{idx + 1}</span>
                  <div className="min-w-0">
                    <p className="font-medium text-sm text-slate-900">{p.productName}</p>
                    <p className="text-xs text-slate-500">{p.categoryName} â€¢ {p.salesCount} sold</p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-sm">{formatCurrency(p.revenue)}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

