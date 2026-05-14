import axios from 'axios';

const API_URL = 'http://localhost:5134/api';

const api = axios.create({
    baseURL: API_URL,
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
    }
};

export const ratesService = {
    analyze: async () => {
        const response = await api.get('/rates/analyze');
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

export default api;