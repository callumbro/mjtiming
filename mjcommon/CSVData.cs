using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RaceBeam
{
    /// <summary>
    /// Class to hold data loaded from csv file
    /// Requires that the csv file have a header line
    /// Read-only
    /// </summary>
    public class CSVData
    {
        // We use a dictionary containing other dictionaries
        // Key is first column in CSV file, it keys to dict of header names and values
        // Access via data["driver number"]["header name"]

        private readonly Dictionary<string, Dictionary<string, string>> _data = new Dictionary<string, Dictionary<string, string>>();
        private List<string> headers = new List<string>();

        // Return the array of headers
        public List<string> GetHeaders()
        {
            return headers;
        }
        // Return a list of keys
        public List<string> GetKeys()
        {
            var list = new List<string>(_data.Keys);
            return list;
        }

        /// <summary>
        /// Gets the field value at the specified record index for the supplied field name
        /// </summary>
        /// <param name="key">Car number</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Value string or empty string</returns>
        public string GetField(string key, string fieldName)
        {
            if (key == null)
            {
                return "";
            }
            if ((_data.ContainsKey(key)) && (_data[key].ContainsKey(fieldName)))
            {
                return _data[key][fieldName];
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Returns number of keyed elements (lines in CSV file)
        /// </summary>
        /// 
        public int Length()
        {
            return _data.Count;
        }

        /// <param name="filePath"></param>
        /// <summary>
        /// Reads in the driver or timing data from the specified csv file
        /// Dictionary key is first column heading name
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="separator">character separator</param>
        /// <param name="keyField">Name of key field (must be unique)</param>
        public string LoadData(string filePath, char separator, string keyField)
        {
            // Clear out the data for new data.
            _data.Clear();

            // Variables for generating a unique primary key when not specified.
            string myKeyHeaderDefault = "myKey";
            int myKey = 0;  // counter to keep the newly invented keys unique

            try
            {
                CsvConfiguration csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture);
                if (separator == ',')
                {
                    csvConfiguration.Delimiter = ",";
                }
                else
                {
                    csvConfiguration.Delimiter = "\t";
                }

                using (StreamReader reader = new StreamReader(filePath))
                using (CsvReader csvReader = new CsvReader(reader, csvConfiguration))
                {
                    // Read in the headers before the data.
                    csvReader.Read();
                    csvReader.ReadHeader();
                    headers = csvReader.HeaderRecord.ToList();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(headers[i]))
                        {
                            // Do not allow a column header to be blank.
                            headers[i] = "col" + i.ToString();
                        }
                    }
                    // Make sure there is a primary key column.
                    if (string.IsNullOrWhiteSpace(keyField))
                    {
                        headers.Add(myKeyHeaderDefault);

                        // Set the keyField to the default.
                        keyField = myKeyHeaderDefault;
                    }

                    // Read and parse the data rows.
                    while (csvReader.Read())
                    {
                        List<string> csvRowData = new List<string>();
                        try
                        {
                            // Read in all the data for the row.
                            for (int i = 0; i < csvReader.ColumnCount; i++)
                            {
                                csvRowData.Add(csvReader.GetField(i));
                            }
                        }
                        catch
                        {
                            // not a valid delimited line - log, terminate, or ignore
                            continue;
                        }

                        if ((csvRowData.Count < 3) || ((csvRowData.Count == 3) && string.IsNullOrEmpty(csvRowData[2])))
                        {
                            // didn't work -- skip this line
                            continue;
                        }
                        for (int i = 0; i < csvRowData.Count; i++)
                        {
                            csvRowData[i] = csvRowData[i].Replace(",", ";");
                            csvRowData[i] = csvRowData[i].Replace("\t", " ");
                        }

                        var record = new Dictionary<string, string>();

                        for (int i = 0; i < csvRowData.Count; i++)
                        {
                            if (i >= headers.Count)
                            {
                                // Somehow there was more data than headers.
                                // Skip as we do not know what the data represents.
                                break;
                            }
                            if (record.ContainsKey(headers[i]))
                            {
                                // Record already has data for this column.
                                continue;
                            }

                            // Add the data to the record.
                            record.Add(headers[i], csvRowData[i]);
                        }

                        if (keyField == myKeyHeaderDefault)
                        {
                            // Determine a unique primary key.
                            string nextKey = myKey.ToString();
                            myKey += 1;
                            if (record.ContainsKey(nextKey))
                            {
                                // There is alreay data for this key, skip to next csv data line.
                                continue;
                            }

                            // Add the primary key to the record.
                            record.Add("myKey", nextKey);
                        }

                        if (record.ContainsKey(keyField))
                        {
                            // The record as a key, so it can be saved to _data.

                            if (_data.ContainsKey(record[keyField]))
                            {
                                // This data has already been saved, don't overwrite it.
                                continue;
                            }

                            // Add the new record.
                            _data.Add(record[keyField], record);
                        }
                    }
                }

                return "";
            }
            catch (Exception e)
            {
                return (e.Message);
            }
        }
    }
}
