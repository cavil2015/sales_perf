import { useState, useEffect } from "react";
import {
  fetchKpis,
  fetchManagersRating,
  fetchChartData,
  fetchRecentSales,
  fetchCategoryAnalytics
} from "../lib/api";

export interface KpiDto {
  revenue: number | string;
  grossProfit: number | string;
  salesCount: number;
  averageCheck: number | string;
  topManager: string | null;
  revenueDiff?: number | string;
  grossProfitDiff?: number | string;
  salesCountDiff?: number | string;
  averageCheckDiff?: number | string;
}

export interface ManagerRatingDto {
  managerId: number;
  managerName: string;
  avatarUrl: string | null;
  revenue: number | string;
  grossProfit: number | string;
  salesCount: number;
  averageCheck: number | string;
  margin: number | string;
  grossProfitDiff?: number | string;
  averageCheckDiff?: number | string;
}

export interface ChartDataDto {
  date: string; // "YYYY-MM-DD"
  revenue: number | string;
  grossProfit: number | string;
  salesCount: number;
}

export interface RecentSaleDto {
  id: number;
  date: string; // ISO 8601
  managerName: string;
  customerName: string;
  status: string;
  revenue: number | string;
  grossProfit: number | string;
  products?: string;
}

export interface CategoryAnalyticsDto {
  categoryName: string;
  revenue: number | string;
  grossProfit: number | string;
  salesCount: number;
}

export interface TopProductDto {
  productId: number;
  productName: string;
  categoryName: string;
  revenue: number | string;
  salesCount: number;
}

export function useAnalyticsDashboard(from?: string, to?: string) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [kpis, setKpis] = useState<KpiDto | null>(null);
  const [managers, setManagers] = useState<ManagerRatingDto[] | null>(null);
  const [chartData, setChartData] = useState<ChartDataDto[] | null>(null);
  const [recentSales, setRecentSales] = useState<RecentSaleDto[] | null>(null);
  const [categoryData, setCategoryData] = useState<{ categories: CategoryAnalyticsDto[], topProducts: TopProductDto[] } | null>(null);

  useEffect(() => {
    let isMounted = true;
    const abortController = new AbortController();

    async function loadData() {
      try {
        setLoading(true);
        setError(null);

        const results = await Promise.allSettled([
          fetchKpis(from, to, abortController.signal),
          fetchManagersRating(from, to, abortController.signal),
          fetchChartData(from, to, abortController.signal),
          fetchRecentSales(abortController.signal),
          fetchCategoryAnalytics(from, to, abortController.signal)
        ]);

        if (!isMounted) return;

        if (results[0].status === "fulfilled") setKpis(results[0].value);
        else setKpis(null);

        if (results[1].status === "fulfilled") setManagers(results[1].value);
        else setManagers(null);

        if (results[2].status === "fulfilled") setChartData(results[2].value);
        else setChartData(null);

        if (results[3].status === "fulfilled") setRecentSales(results[3].value);
        else setRecentSales(null);

        if (results[4].status === "fulfilled") setCategoryData(results[4].value);
        else setCategoryData(null);

        if (results.every((r) => r.status === "rejected")) {
          throw new Error("Dashboard API is offline.");
        }
      } catch (err: any) {
        if (err.name === "AbortError") return;
        if (isMounted) setError(err.message || "Error loading dashboard data");
      } finally {
        if (isMounted) setLoading(false);
      }
    }
    loadData();

    return () => {
      isMounted = false;
      abortController.abort();
    };
  }, [from, to]);

  return { loading, error, kpis, managers, chartData, recentSales, categoryData };
}

