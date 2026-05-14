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
    const [allCurrencies, setAllCurrencies] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

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
        
        ratesService.getAvailableCurrencies().then(setAllCurrencies).catch(console.error);
    }, []);

    useEffect(() => {
        if (startDate && endDate) loadDashboardData();
    }, [startDate, endDate]);

    const loadDashboardData = async () => {
        try {
            setLoading(true);
            const userSettings = await settingsService.getSettings();
            setSettings(userSettings);
            setEditBase(userSettings.baseCurrency);
            setEditSelected(userSettings.selectedCurrencies);

            const analysisData = await ratesService.analyze(startDate, endDate);
            setData(analysisData);
        } catch (err) {
            setError('Chyba při načítání dat.');
        } finally {
            setLoading(false);
        }
    };

    const handleSaveSettings = async () => {
        try {
            await settingsService.updateSettings({
                id: 1,
                baseCurrency: editBase,
                selectedCurrencies: editSelected
            });
            setIsEditing(false);
            loadDashboardData();
        } catch (err) {
            setError('Chyba při ukládání.');
        }
    };

    const toggleCurrency = (currency) => {
        let current = editSelected ? editSelected.split(',') : [];
        if (current.includes(currency)) {
            current = current.filter(c => c !== currency);
        } else {
            current.push(currency);
        }
        setEditSelected(current.join(','));
    };

    const generateChartData = () => {
        if (!data?.timeSeriesRates || !settings) return { labels: [], datasets: [] };
        const dates = Object.keys(data.timeSeriesRates);
        const selected = settings.selectedCurrencies.split(',');
        const colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'];

        return {
            labels: dates,
            datasets: selected.map((curr, i) => ({
                label: curr,
                data: dates.map(d => data.timeSeriesRates[d][curr]),
                borderColor: colors[i % colors.length],
                tension: 0.1
            }))
        };
    };

    if (loading && !data) return <div style={{textAlign:'center', marginTop:'50px'}}>Načítání...</div>;

    return (
        <div style={{ maxWidth: '900px', margin: '20px auto', fontFamily: 'Arial, sans-serif', padding: '0 20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #ccc', paddingBottom: '10px' }}>
                <h2>Currency Analyzer</h2>
                <button onClick={() => { authService.logout(); onLogout(); }} style={{backgroundColor:'#dc3545', color:'white', border:'none', padding:'8px 15px', borderRadius:'4px', cursor:'pointer'}}>Logout</button>
            </div>

            <div style={{ display: 'flex', gap: '20px', marginTop: '20px' }}>
                <div style={{ flex: 1, padding: '15px', backgroundColor: '#f8f9fa', border: '1px solid #ddd', borderRadius: '8px' }}>
                    <div style={{display:'flex', justifyContent:'space-between'}}>
                        <h3>Nastavení</h3>
                        {!isEditing && <button onClick={() => setIsEditing(true)}>Upravit</button>}
                    </div>

                    {isEditing ? (
                        <div>
                            <label>Základní měna:</label>
                            <select value={editBase} onChange={e => setEditBase(e.target.value)} style={{width:'100%', marginBottom:'10px'}}>
                                {allCurrencies.map(c => <option key={c} value={c}>{c}</option>)}
                            </select>
                            
                            <label>Sledované měny:</label>
                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '5px', maxHeight: '150px', overflowY: 'auto', border: '1px solid #ccc', padding: '5px', marginBottom: '10px' }}>
                                {allCurrencies.map(c => (
                                    <label key={c} style={{fontSize:'12px'}}>
                                        <input type="checkbox" checked={editSelected.split(',').includes(c)} onChange={() => toggleCurrency(c)} /> {c}
                                    </label>
                                ))}
                            </div>

                            <label>Jazyk:</label>
                            <select style={{width:'100%', marginBottom:'10px'}}><option>CZ</option><option>EN</option></select>

                            <button onClick={handleSaveSettings} style={{backgroundColor:'#28a745', color:'white', width:'100%', padding:'10px', border:'none', borderRadius:'4px', cursor:'pointer'}}>Uložit nastavení</button>
                        </div>
                    ) : (
                        <>
                            <p><strong>Základní:</strong> {settings?.baseCurrency}</p>
                            <p><strong>Sledované:</strong> {settings?.selectedCurrencies}</p>
                            <p><strong>Období:</strong> {startDate} až {endDate}</p>
                            <div style={{display:'flex', gap:'5px'}}>
                                <input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
                                <input type="date" value={endDate} onChange={e => setEndDate(e.target.value)} />
                            </div>
                        </>
                    )}
                </div>

                <div style={{ flex: 1, padding: '15px', backgroundColor: '#e9ecef', border: '1px solid #ddd', borderRadius: '8px' }}>
                    <h3>Výsledky analýzy</h3>
                    <p><strong>Nejsilnější měna:</strong> {data?.strongestCurrency}</p>
                    <p><strong>Nejslabší měna:</strong> {data?.weakestCurrency}</p>
                    <p><strong>Průměrný kurz:</strong> {data?.averageRate.toFixed(4)}</p>
                </div>
            </div>

            <div style={{ marginTop: '30px', padding: '20px', border: '1px solid #ddd', borderRadius: '8px', backgroundColor: 'white' }}>
                <Line data={generateChartData()} options={{ responsive: true, plugins: { title: { display: true, text: 'Vývoj kurzů v čase' } } }} />
            </div>
        </div>
    );
}

export default Dashboard;