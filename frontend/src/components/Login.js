import React, { useState } from 'react';
import { authService } from '../services/api';

function Login({ onLoginSuccess }) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            await authService.login(username, password);
            onLoginSuccess();
        } catch (err) {
            setError('Neplatné přihlašovací údaje. Zkuste to znovu.');
        }
    };

    return (
        <div style={{ maxWidth: '400px', margin: '100px auto', padding: '20px', border: '1px solid #ccc', borderRadius: '5px', fontFamily: 'Arial, sans-serif' }}>
            <h2 style={{ textAlign: 'center', color: '#333' }}>Currency Analyzer</h2>
            {error && <p style={{ color: 'red', textAlign: 'center' }}>{error}</p>}
            
            <form onSubmit={handleSubmit}>
                <div style={{ marginBottom: '15px' }}>
                    <label style={{ display: 'block', marginBottom: '5px' }}>Username:</label>
                    <input 
                        type="text" 
                        value={username} 
                        onChange={(e) => setUsername(e.target.value)} 
                        style={{ width: '100%', padding: '10px', boxSizing: 'border-box' }}
                        required
                    />
                </div>
                <div style={{ marginBottom: '20px' }}>
                    <label style={{ display: 'block', marginBottom: '5px' }}>Password:</label>
                    <input 
                        type="password" 
                        value={password} 
                        onChange={(e) => setPassword(e.target.value)} 
                        style={{ width: '100%', padding: '10px', boxSizing: 'border-box' }}
                        required
                    />
                </div>
                <button type="submit" style={{ width: '100%', padding: '10px', backgroundColor: '#0056b3', color: 'white', border: 'none', borderRadius: '3px', cursor: 'pointer', fontSize: '16px' }}>
                    Login
                </button>
            </form>
        </div>
    );
}

export default Login;