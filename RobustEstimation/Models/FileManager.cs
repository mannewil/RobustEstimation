using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public static class FileManager
    {
        /// <summary>
        /// Loads a dataset from a text file. Supports numbers separated by spaces, commas, and semicolons.
        /// </summary>
        /// <param name="path">Path to the input file.</param>
        /// <returns>A dataset containing the parsed values.</returns>
        /// <exception cref="Exception">Throws an exception if the file format is invalid or a file error occurs.</exception>
        public static async Task<Dataset> LoadFromFileAsync(string path)
        {
            var dataset = new Dataset();
            try
            {
                var content = await File.ReadAllTextAsync(path);

                // Normalize delimiters: replace spaces and semicolons with commas
                content = Regex.Replace(content, @"[;\s]+", ",");

                // Parse numbers using invariant culture
                var numbers = content.Split(',')
                                     .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) ? num : (double?)null)
                                     .Where(n => n.HasValue)
                                     .Select(n => n.Value)
                                     .ToList();

                if (numbers.Count == 0)
                    throw new FormatException("The file contains no valid numbers.");

                dataset.SetValues(numbers);
            }
            catch (IOException ex)
            {
                throw new Exception($"File I/O error: {ex.Message}", ex);
            }
            catch (FormatException ex)
            {
                throw new Exception($"Invalid file format: {ex.Message}", ex);
            }

            return dataset;
        }

        /// <summary>
        /// Saves the dataset along with computation details.
        /// </summary>
        /// <param name="dataset">Dataset containing values.</param>
        /// <param name="path">Path to save the output file.</param>
        /// <param name="selectedMethod">Selected method name.</param>
        /// <param name="methodParameter">Method parameter, e.g., "Trim Fraction: 20%".</param>
        /// <param name="result">Computed result.</param>
        public static async Task SaveToFileAsync(Dataset dataset, string path, string selectedMethod, string methodParameter, string methodProcessedDataset, double result)
        {
            if (dataset == null || dataset.Values.Count == 0)
                throw new Exception("Dataset is empty. Nothing to save.");

            try
            {
                var lines = new List<string>();

                // Save original values
                if (dataset.Values.Count > 0)
                {
                    lines.Add("Original Values:");
                    lines.Add(string.Join(", ", dataset.Values.Select(v => v.ToString(CultureInfo.InvariantCulture))));
                }

                // Save selected method and parameter
                lines.Add($"\nMethod: {selectedMethod}");
                if (!string.IsNullOrEmpty(methodParameter))
                {
                    lines.Add($"Parameter: {methodParameter}");
                }

                // Save processed dataset
                if (!string.IsNullOrEmpty(methodProcessedDataset))
                {
                    lines.Add(methodProcessedDataset);
                }

                // Save computed result
                lines.Add("\nResult:");
                lines.Add(result.ToString(CultureInfo.InvariantCulture));

                await File.WriteAllLinesAsync(path, lines);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception($"Access denied to file: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new Exception($"File I/O error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while saving the file: {ex.Message}", ex);
            }
        }

    }
}
