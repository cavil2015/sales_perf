import { useState, useEffect } from 'react';
import { fetchKpis, fetchManagersRating, fetchChartData, fetchRecentSales } from '../lib/api';

export type DateFilter = '30days' | 'month' | '12months';

export interface KpiDto {
  revenue: number | string;
  grossProfit: number | string;
  salesCount: number;
  averageCheck: number | string;
  topManager: string | null;
}

export interface ManagerRatingDto {
  managerId: number;
  managerName: string;
  avatarUrl: string | null;
  revenue: number | string;
  grossProfit: number | string;
  salesCount: number;
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
}

export function useAnalyticsDashboard(dateRange: DateFilter) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [kpis, setKpis] = useState<KpiDto | null>(null);
  const [managers, setManagers] = useState<ManagerRatingDto[] | null>(null);
  const [chartData, setChartData] = useState<ChartDataDto[] | null>(null);
  const [recentSales, setRecentSales] = useState<RecentSaleDto[] | null>(null);

  useEffect(() => {
    let isMounted = true;
    const abortController = new AbortController();
    
    async function loadData() {
      try {
        setLoading(true);
        setError(null);
        
        let from: string | undefined;
        const now = new Date();
        
        if (dateRange === '30days') {
           const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - 30));
           from = d.toISOString();
        } else if (dateRange === 'month') {
           const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1));
           from = d.toISOString();
        } else if (dateRange === '12months') {
           const d = new Date(Date.UTC(now.getUTCFullYear() - 1, now.getUTCMonth(), now.getUTCDate()));
           from = d.toISOString();
        }

        const results = await Promise.allSettled([
          fetchKpis(from, undefined, abortController.signal),
          fetchManagersRating(from, undefined, abortController.signal),
          fetchChartData(from, undefined, abortController.signal),
          fetchRecentSales(abortController.signal) 
        ]);
        
        if (!isMounted) return;

        if (results[0].status === 'fulfilled') setKpis(results[0].value);
        else setKpis(null); 

        if (results[1].status === 'fulfilled') setManagers(results[1].value);
        else setManagers(null);

        if (results[2].status === 'fulfilled') setChartData(results[2].value);
        else setChartData(null);

        if (results[3].status === 'fulfilled') setRecentSales(results[3].value);
        else setRecentSales(null);
        
        if (results.every(r => r.status === 'rejected')) {
           throw new Error("Dashboard API is offline.");
        }
      } catch (err: any) {
        if (err.name === 'AbortError') return;
        if (isMounted) setError(err.message || 'Error loading dashboard data');
      } finally {
        if (isMounted) setLoading(false);
      }
    }
    loadData();
    
    return () => { 
        isMounted = false;
        abortController.abort();
    };
  }, [dateRange]);

  return { loading, error, kpis, managers, chartData, recentSales };
}
