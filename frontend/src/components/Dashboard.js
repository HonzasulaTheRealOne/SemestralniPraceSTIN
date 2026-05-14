import React, { useState, useEffect } from 'react';
import { ratesService, settingsService, authService } from '../services/api';
import { Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend);

function Dashboard({ onLogout }) {
    const [data, setData] = useState(null);
    const [settings, setSettings] = useState(null);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        loadDashboardData();
    }, []);

    const loadDashboardData = async () => {
        try {
            setLoading(true);
            const userSettings = await settingsService.getSettings();
            setSettings(userSettings);

            const analysisData = await ratesService.analyze();
            setData(analysisData);
            setError('');
        } catch (err) {
            setError('Nepodařilo se načíst data z API. Zkuste to prosím později.');
        } finally {
            setLoading(false);
        }
    };

    const handleLogout = () => {
        authService.logout();
        onLogout();
    };

    const chartData = {
        labels: data ? Object.keys(data.rates) : [],
        datasets: [
            {
                label: `Kurz vůči ${settings ? settings.baseCurrency : 'EUR'}`,
                data: data ? Object.values(data.rates) : [],
                backgroundColor: 'rgba(54, 162, 235, 0.6)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1,
            },
        ],
    };

    const chartOptions = {
        responsive: true,
        plugins: {
            legend: { position: 'top' },
            title: { display: true, text: 'Aktuální měnové kurzy' },
        },
    };

    if (loading) return <div style={{ textAlign: 'center', marginTop: '50px' }}>Načítání dat...</div>;

    return (
        <div style={{ maxWidth: '800px', margin: '20px auto', padding: '20px', fontFamily: 'Arial, sans-serif' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid #eee', paddingBottom: '10px', marginBottom: '20px' }}>
                <h2>Currency Analyzer Dashboard</h2>
                <button onClick={handleLogout} style={{ padding: '8px 15px', backgroundColor: '#dc3545', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                    Logout
                </button>
            </div>

            {error && (
                <div style={{ padding: '15px', backgroundColor: '#f8d7da', color: '#721c24', marginBottom: '20px', borderRadius: '4px' }}>
                    <strong>Error:</strong> {error}
                    <br />
                    <button onClick={loadDashboardData} style={{ marginTop: '10px', padding: '5px 10px' }}>Zkusit znovu</button>
                </div>
            )}

            {data && settings && (
                <div style={{ display: 'flex', gap: '20px', marginBottom: '30px' }}>
                    <div style={{ flex: 1, padding: '15px', backgroundColor: '#f8f9fa', borderRadius: '5px', border: '1px solid #ddd' }}>
                        <h3>Nastavení</h3>
                        <p><strong>Základní měna:</strong> {settings.baseCurrency}</p>
                        <p><strong>Sledované měny:</strong> {settings.selectedCurrencies}</p>
                    </div>
                    
                    <div style={{ flex: 1, padding: '15px', backgroundColor: '#e9ecef', borderRadius: '5px', border: '1px solid #ddd' }}>
                        <h3>Výsledky analýzy</h3>
                        <p><strong>Nejsilnější měna:</strong> {data.strongestCurrency}</p>
                        <p><strong>Nejslabší měna:</strong> {data.weakestCurrency}</p>
                        <p><strong>Průměrný kurz:</strong> {data.averageRate.toFixed(4)}</p>
                    </div>
                </div>
            )}

            {data && (
                <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '5px' }}>
                    <Bar data={chartData} options={chartOptions} />
                </div>
            )}
        </div>
    );
}

export default Dashboard;