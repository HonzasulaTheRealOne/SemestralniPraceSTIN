import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || 'https://stin-backend-jan-frantisek-sula.onrender.com/api';

const api = axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json'
    }
});

api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

export const authService = {
    login: async (username, password) => {
        const response = await api.post('/auth/login', { username, password });
        if (response.data.token) {
            localStorage.setItem('token', response.data.token);
        }
        return response.data;
    },
    logout: () => {
        localStorage.removeItem('token');
    },
    isAuthenticated: () => {
        return !!localStorage.getItem('token');
    },
    getToken: () => {
        return localStorage.getItem('token');
    }
};

export const ratesService = {
    analyze: async (startDate, endDate) => {
        const params = new URLSearchParams();
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        const response = await api.get(`/rates/analyze?${params.toString()}`);
        return response.data;
    },
    getAvailableCurrencies: async () => {
        const response = await api.get('/rates/currencies');
        return response.data;
    }
};

export const settingsService = {
    getSettings: async () => {
        const response = await api.get('/settings');
        return response.data;
    },
    updateSettings: async (settings) => {
        const response = await api.post('/settings', settings);
        return response.data;
    }
};

// NOVÁ SLUŽBA PRO LOGY
export const logsService = {
    getLogs: async () => {
        const response = await api.get('/logs');
        return response.data;
    }
};

export default api;