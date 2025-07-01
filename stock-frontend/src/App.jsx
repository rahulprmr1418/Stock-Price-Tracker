import React, { useState } from 'react';
import './App.css'; // Import the CSS file for styling

function App() {
  const [ticker, setTicker] = useState('');
  const [date, setDate] = useState('');
  const [stockPrice, setStockPrice] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const fetchStockPrice = async () => {
    setLoading(true);
    setStockPrice(null);
    setError('');

    if (!ticker || !date) {
      setError('Please enter both stock ticker and date.');
      setLoading(false);
      return;
    }

    // UPDATED: Adjusted the backend URL to match the 'dotnet run' output
    const backendUrl = `http://localhost:5214/Stock/price?ticker=${ticker}&date=${date}`;

    try {
      const response = await fetch(backendUrl);

      if (!response.ok) {
        // If response is not OK (e.g., 404, 500), try to read the error message
        // Handle cases where the backend returns a non-JSON error or a JSON error
        const contentType = response.headers.get("content-type");
        if (contentType && contentType.indexOf("application/json") !== -1) {
          const errorData = await response.json();
          throw new Error(errorData.detail || JSON.stringify(errorData) || 'Failed to fetch stock price.');
        } else {
          const errorText = await response.text();
          throw new Error(`Failed to fetch stock price. Server response: ${errorText.substring(0, 100)}... (Status: ${response.status})`);
        }
      }

      const data = await response.json();
      setStockPrice(data.closingPrice);
    } catch (err) {
      console.error('Error fetching stock price:', err);
      setError(err.message || 'An error occurred while fetching data.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app-container">
      <h1>Stock Price Tracker</h1>

      <div className="input-group">
        <label htmlFor="ticker">Stock Ticker:</label>
        <input
          type="text"
          id="ticker"
          value={ticker}
          onChange={(e) => setTicker(e.target.value.toUpperCase())}
          placeholder="e.g., IBM"
        />
      </div>

      <div className="input-group">
        <label htmlFor="date">Date (YYYY-MM-DD):</label>
        <input
          type="date" // Use type="date" for a date picker
          id="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
        />
      </div>

      <button onClick={fetchStockPrice} disabled={loading}>
        {loading ? 'Fetching...' : 'Get Stock Price'}
      </button>

      {error && <p className="error-message">{error}</p>}

      {stockPrice !== null && (
        <div className="result-container">
          <h2>Result:</h2>
          <p>
            The closing price for <span className="highlight">{ticker}</span> on <span className="highlight">{date}</span> was:{' '}
            <span className="price">${stockPrice.toFixed(2)}</span>
          </p>
        </div>
      )}
    </div>
  );
}

export default App;
