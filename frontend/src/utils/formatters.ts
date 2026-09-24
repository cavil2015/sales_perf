import { format } from 'date-fns';

export const safeFormatDate = (dateStr: string, formatStr: string) => {
    if (!dateStr) return '';
    if (dateStr.length === 10) {
        const [y, m, d] = dateStr.split('-');
        return format(new Date(Number(y), Number(m)-1, Number(d)), formatStr);
    }
    return format(new Date(dateStr), formatStr);
};

export const formatCurrency = (val: number | string | undefined | null) => {
    if (val === null || val === undefined) return 'N/A';
    return new Intl.NumberFormat('en-US', { 
        style: 'currency', 
        currency: 'USD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(Number(val));
};
