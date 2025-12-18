using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{
    /// <summary>
    /// Custom CSV export implementation that preserves trailing/leading whitespace.
    /// Replaces the CsvExport.dll to ensure proper handling of whitespace in CSV files.
    /// </summary>
    public class CustomCsvExport
    {
        private readonly string _columnSeparator;
        private readonly bool _includeHeaderRow;
        private readonly List<string> _columns;
        private readonly List<Dictionary<string, string>> _rows;
        private Dictionary<string, string> _currentRow;

        /// <summary>
        /// Initializes a new instance of CustomCsvExport.
        /// </summary>
        /// <param name="columnSeparator">The separator to use between columns. Default is comma.</param>
        /// <param name="includeColumnSeparatorDefinitionPreamble">Whether to include a preamble (not used, kept for compatibility).</param>
        /// <param name="includeHeaderRow">Whether to include a header row in the export.</param>
        public CustomCsvExport(string columnSeparator = ",", bool includeColumnSeparatorDefinitionPreamble = false, bool includeHeaderRow = true)
        {
            _columnSeparator = columnSeparator ?? ",";
            _includeHeaderRow = includeHeaderRow;
            _columns = new List<string>();
            _rows = new List<Dictionary<string, string>>();
            _currentRow = null;
        }

        /// <summary>
        /// Adds a new row to the CSV export.
        /// </summary>
        public void AddRow()
        {
            if (_currentRow != null)
            {
                _rows.Add(_currentRow);
            }
            _currentRow = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets a value for the specified column in the current row.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>The value for the column, or null if not set.</returns>
        public string this[string columnName]
        {
            get
            {
                if (_currentRow == null)
                {
                    throw new InvalidOperationException("No row has been added. Call AddRow() first.");
                }
                return _currentRow.ContainsKey(columnName) ? _currentRow[columnName] : null;
            }
            set
            {
                if (_currentRow == null)
                {
                    throw new InvalidOperationException("No row has been added. Call AddRow() first.");
                }

                // Track column names for header row
                if (!_columns.Contains(columnName))
                {
                    _columns.Add(columnName);
                }

                _currentRow[columnName] = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Exports the CSV data to a string.
        /// </summary>
        /// <returns>The CSV content as a string.</returns>
        public string Export()
        {
            var csvBuilder = new StringBuilder();

            // Add the last row if it exists
            if (_currentRow != null)
            {
                _rows.Add(_currentRow);
                _currentRow = null;
            }

            // Add header row if requested
            if (_includeHeaderRow && _columns.Count > 0)
            {
                csvBuilder.AppendLine(string.Join(_columnSeparator, _columns.Select(EscapeCsvValue)));
            }

            // Add data rows
            foreach (var row in _rows)
            {
                var rowValues = _columns.Select(column => 
                    row.ContainsKey(column) ? EscapeCsvValue(row[column]) : string.Empty);
                csvBuilder.AppendLine(string.Join(_columnSeparator, rowValues));
            }

            return csvBuilder.ToString();
        }

        /// <summary>
        /// Escapes a CSV value by handling quotes, commas, newlines, and preserving leading/trailing spaces.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The escaped CSV value.</returns>
        private static string EscapeCsvValue(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            // Check if value needs quoting: contains special characters OR has leading/trailing spaces
            // Quoting preserves whitespace that might otherwise be trimmed by CSV parsers
            bool needsQuoting = value.Contains("\"") || 
                               value.Contains(",") || 
                               value.Contains("\n") ||
                               value.Contains("\r") ||
                               (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])));

            if (needsQuoting)
            {
                // Escape existing quotes by doubling them (CSV standard)
                string escapedValue = value.Replace("\"", "\"\"");
                return $"\"{escapedValue}\"";
            }
            
            return value;
        }
    }
}

