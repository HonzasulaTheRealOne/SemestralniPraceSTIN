import React, { useState, useEffect } from 'react';
import { logsService } from '../services/api';

function LogsViewer({ onBack, lang }) {
    const [logs, setLogs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    useEffect(() => {
        logsService.getLogs()
            .then(data => {
                setLogs(data);
                setLoading(false);
            })
            .catch(err => {
                console.error(err);
                setError(lang === 'CZ' ? 'Chyba při načítání logů z databáze.' : 'Error fetching logs from database.');
                setLoading(false);
            });
    }, [lang]);

    if (loading) return <div style={{textAlign:'center', marginTop:'50px'}}>Načítám logy...</div>;

    return (
        <div style={{ maxWidth: '900px', margin: '20px auto', fontFamily: 'Arial, sans-serif', padding: '0 20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #ccc', paddingBottom: '10px' }}>
                <h2>{lang === 'CZ' ? 'Systémové Logy' : 'System Logs'}</h2>
                <button onClick={onBack} style={{backgroundColor:'#6c757d', color:'white', border:'none', padding:'8px 15px', borderRadius:'4px', cursor:'pointer'}}>
                    {lang === 'CZ' ? 'Zpět na Dashboard' : 'Back to Dashboard'}
                </button>
            </div>

            {error && <p style={{ color: 'red' }}>{error}</p>}

            <div style={{ marginTop: '20px', overflowX: 'auto' }}>
                {logs.length === 0 ? (
                    <p style={{ textAlign: 'center', color: '#666' }}>
                        {lang === 'CZ' ? 'Zatím nebyly zaznamenány žádné chyby.' : 'No errors have been logged yet.'}
                    </p>
                ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse', backgroundColor: '#fff', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
                        <thead>
                            <tr style={{ backgroundColor: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                                <th style={{ padding: '12px', textAlign: 'left' }}>{lang === 'CZ' ? 'Čas (UTC)' : 'Time (UTC)'}</th>
                                <th style={{ padding: '12px', textAlign: 'left' }}>Level</th>
                                <th style={{ padding: '12px', textAlign: 'left' }}>{lang === 'CZ' ? 'Zpráva' : 'Message'}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {logs.map((log) => (
                                <tr key={log.id} style={{ borderBottom: '1px solid #dee2e6' }}>
                                    <td style={{ padding: '12px', whiteSpace: 'nowrap' }}>
                                        {new Date(log.timestamp).toLocaleString()}
                                    </td>
                                    <td style={{ padding: '12px' }}>
                                        <span style={{ backgroundColor: '#dc3545', color: 'white', padding: '3px 8px', borderRadius: '12px', fontSize: '12px' }}>
                                            {log.level}
                                        </span>
                                    </td>
                                    <td style={{ padding: '12px', wordBreak: 'break-word' }}>{log.message}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}

export default LogsViewer;