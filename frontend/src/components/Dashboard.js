import React, { useState, useEffect } from 'react';
import { ratesService, settingsService, authService } from '../services/api';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend);

function Dashboard({ onLogout }) {
    const [data, setData] = useState(null);
    const [settings, setSettings] = useState(null);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(true);

    const [isEditing, setIsEditing] = useState(false);
    const [editBase, setEditBase] = useState('');
    const [editSelected, setEditSelected] = useState('');

    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');

    useEffect(() => {
        const end = new Date();
        const start = new Date();
        start.setDate(end.getDate() - 10);
        
        setEndDate(end.toISOString().split('T')[0]);
        setStartDate(start.toISOString().split('T')[0]);
    }, []);

    useEffect(() => {
        if (startDate && endDate) {
            loadDashboardData();
        }
    }, [startDate, endDate]);

    const loadDashboardData = async () => {
        try {
            setLoading(true);
            const userSettings = await settingsService.getSettings();
            setSettings(userSettings);
            if (!isEditing) {
                setEditBase(userSettings.baseCurrency);
                setEditSelected(userSettings.selectedCurrencies);
            }

            const analysisData = await ratesService.analyze(startDate, endDate);
            setData(analysisData);
            setError('');
        } catch (err) {
            setError('Nepodařilo se načíst data z API. Zkuste to prosím později.');
        } finally {
            setLoading(false);
        }
    };

    const handleSaveSettings = async () => {
        try {
            setLoading(true);
            await settingsService.updateSettings({
                id: 1,
                baseCurrency: editBase.trim().toUpperCase(),
                selectedCurrencies: editSelected.trim().toUpperCase()
            });
            setIsEditing(false);
            await loadDashboardData(); 
        } catch (err) {
            setError('Nepodařilo se uložit nastavení.');
            setLoading(false);
        }
    };

    const handleLogout = () => {
        authService.logout();
        onLogout();
    };

    const generateChartData = () => {
        if (!data || !settings || !data.timeSeriesRates) return { labels: [], datasets: [] };

        const dates = Object.keys(data.timeSeriesRates);
        const currencies = settings.selectedCurrencies.split(',').map(c => c.trim());
        const colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF'];

        const datasets = currencies.map((currency, index) => {
            return {
                label: currency,
                data: dates.map(date => data.timeSeriesRates[date][currency] || null),
                borderColor: colors[index % colors.length],
                backgroundColor: colors[index % colors.length],
                fill: false,
                tension: 0.1
            };
        });

        return { labels: dates, datasets };
    };

    const chartOptions = {
        responsive: true,
        plugins: {
            legend: { position: 'top' },
            title: { display: true, text: `Vývoj kurzů vůči ${settings?.baseCurrency || 'Základní měně'}` },
        },
    };

    if (loading && !data) return <div style={{ textAlign: 'center', marginTop: '50px' }}>Načítání dat...</div>;

    return (
        <div style={{ maxWidth: '900px', margin: '20px auto', padding: '20px', fontFamily: 'Arial, sans-serif' }}>
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
                <div style={{ display: 'flex', gap: '20px', marginBottom: '30px', flexWrap: 'wrap' }}>
                    
                    <div style={{ flex: '1 1 300px', padding: '15px', backgroundColor: '#f8f9fa', borderRadius: '5px', border: '1px solid #ddd' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                            <h3 style={{ margin: 0 }}>Nastavení</h3>
                            {!isEditing && (
                                <button onClick={() => setIsEditing(true)} style={{ padding: '5px 10px', cursor: 'pointer' }}>Upravit</button>
                            )}
                        </div>

                        {isEditing ? (
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                <div>
                                    <label style={{ display: 'block', fontSize: '14px' }}>Základní měna:</label>
                                    <input value={editBase} onChange={(e) => setEditBase(e.target.value)} style={{ width: '100%', padding: '5px' }} />
                                </div>
                                <div>
                                    <label style={{ display: 'block', fontSize: '14px' }}>Sledované měny:</label>
                                    <input value={editSelected} onChange={(e) => setEditSelected(e.target.value)} style={{ width: '100%', padding: '5px' }} />
                                </div>
                                <div style={{ display: 'flex', gap: '10px', marginTop: '5px' }}>
                                    <button onClick={handleSaveSettings} style={{ backgroundColor: '#28a745', color: 'white', border: 'none', padding: '5px 10px', cursor: 'pointer', borderRadius: '3px' }}>Uložit</button>
                                    <button onClick={() => setIsEditing(false)} style={{ backgroundColor: '#6c757d', color: 'white', border: 'none', padding: '5px 10px', cursor: 'pointer', borderRadius: '3px' }}>Zrušit</button>
                                </div>
                            </div>
                        ) : (
                            <>
                                <p><strong>Základní měna:</strong> {settings.baseCurrency}</p>
                                <p><strong>Sledované měny:</strong> {settings.selectedCurrencies}</p>
                            </>
                        )}
                        <hr style={{ margin: '15px 0' }} />
                        <h3 style={{ margin: '0 0 10px 0' }}>Časové období</h3>
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <div>
                                <label style={{ display: 'block', fontSize: '12px' }}>Od:</label>
                                <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} style={{ padding: '5px' }} />
                            </div>
                            <div>
                                <label style={{ display: 'block', fontSize: '12px' }}>Do:</label>
                                <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} style={{ padding: '5px' }} />
                            </div>
                        </div>
                    </div>
                    
                    <div style={{ flex: '1 1 300px', padding: '15px', backgroundColor: '#e9ecef', borderRadius: '5px', border: '1px solid #ddd' }}>
                        <h3 style={{ margin: '0 0 10px 0' }}>Výsledky analýzy (za období)</h3>
                        <p><strong>Nejsilnější měna:</strong> {data.strongestCurrency}</p>
                        <p><strong>Nejslabší měna:</strong> {data.weakestCurrency}</p>
                        <p><strong>Průměrný kurz:</strong> {data.averageRate.toFixed(4)}</p>
                    </div>
                </div>
            )}

            {data && (
                <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '5px', backgroundColor: 'white' }}>
                    <Line data={generateChartData()} options={chartOptions} />
                </div>
            )}
        </div>
    );
}

export default Dashboard;