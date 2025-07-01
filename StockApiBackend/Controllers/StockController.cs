// Controllers/StockController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StockApiBackend.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq; // Added for .Any() method

namespace StockApiBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        // Initializing nullable fields directly in the constructor for clarity and to resolve CS8618 warnings.
        // Using null-coalescing to ensure they are never truly null, making them non-nullable string.
        // If config values are missing, they will default to string.Empty.
        private readonly string _alphaVantageApiKey;
        private readonly string _alphaVantageBaseUrl;

        public StockController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            // Resolve CS8601 and CS8618: Assigning configuration values, providing string.Empty as a default
            // if the configuration key is not found or its value is null.
            _alphaVantageApiKey = _configuration["AlphaVantage:ApiKey"] ?? string.Empty;
            _alphaVantageBaseUrl = _configuration["AlphaVantage:BaseUrl"] ?? string.Empty;

            // It's good practice to ensure these are not null before use or handle accordingly
            // For now, the API call logic will check for null/whitespace.
        }

        /// <summary>
        /// Gets the closing price of a stock for a specific date.
        /// </summary>
        /// <param name="ticker">The stock ticker symbol (e.g., "IBM").</param>
        /// <param name="date">The date in YYYY-MM-DD format (e.g., "2023-10-27").</param>
        /// <returns>The closing price as a decimal, or an error message.</returns>
        [HttpGet("price")]
        public async Task<IActionResult> GetStockPrice(string ticker, string date)
        {
            if (string.IsNullOrWhiteSpace(ticker) || string.IsNullOrWhiteSpace(date))
            {
                return BadRequest("Ticker and date must be provided.");
            }

            // Ensure API key and base URL are not null or empty before proceeding
            // This check is now robust because _alphaVantageApiKey and _alphaVantageBaseUrl
            // are guaranteed to be non-null strings (potentially empty) from the constructor.
            if (string.IsNullOrWhiteSpace(_alphaVantageApiKey) || string.IsNullOrWhiteSpace(_alphaVantageBaseUrl))
            {
                return StatusCode(500, "Alpha Vantage API key or base URL is not configured in appsettings.json.");
            }

            // Construct the Alpha Vantage API URL for daily time series
            string requestUrl = $"{_alphaVantageBaseUrl}?function=TIME_SERIES_DAILY&symbol={ticker}&apikey={_alphaVantageApiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP response status is an error code

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response
                // The return type TValue is nullable if it's a nullable reference type or Nullable<T>
                var alphaVantageData = JsonSerializer.Deserialize<AlphaVantageResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Handle case insensitivity during deserialization
                });

                // Check for API call errors (e.g., invalid symbol, API limit) or if deserialization results in null
                if (alphaVantageData?.TimeSeriesDaily == null || !alphaVantageData.TimeSeriesDaily.Any())
                {
                    // Alpha Vantage often returns an object with "Error Message" or "Note" if the call fails
                    if (jsonResponse.Contains("Error Message") || jsonResponse.Contains("Note"))
                    {
                        return NotFound($"Could not retrieve data for ticker '{ticker}'. Alpha Vantage message: {jsonResponse}");
                    }
                    return NotFound($"No daily data found for ticker '{ticker}' or date '{date}'.");
                }

                // Try to find the data for the specific date
                // Using ?. (null-conditional operator) for safer access
                if (alphaVantageData.TimeSeriesDaily.TryGetValue(date, out DailyStockData? dailyData) && dailyData != null)
                {
                    if (dailyData.Close != null && decimal.TryParse(dailyData.Close, out decimal closingPrice))
                    {
                        return Ok(new { ticker = ticker, date = date, closingPrice = closingPrice });
                    }
                    else
                    {
                        // Resolve CS8600 and CS8602: Explicitly get the raw value as a non-nullable string
                        string rawCloseValue = dailyData.Close ?? "null";
                        return StatusCode(500, $"Failed to parse closing price for {ticker} on {date}. Raw value: {rawCloseValue}");
                    }
                }
                else
                {
                    return NotFound($"No data available for {ticker} on {date}. It might be a weekend, holiday, or invalid date.");
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request errors (e.g., network issues, invalid URL)
                return StatusCode(500, $"Error communicating with Alpha Vantage API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // Handle JSON deserialization errors
                return StatusCode(500, $"Error parsing Alpha Vantage response: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
