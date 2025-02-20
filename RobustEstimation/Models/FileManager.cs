using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public static class FileManager
    {
        public static async Task<Dataset> LoadFromFileAsync(string path)
        {
            var dataset = new Dataset();
            try
            {
                var content = await File.ReadAllTextAsync(path); // Читаем весь файл как строку
                var numbers = content.Split(new[] { ' ', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => double.TryParse(s, out var num) ? num : (double?)null)
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

        public static async Task SaveToFileAsync(Dataset dataset, string path)
        {
            if (dataset == null || dataset.Values.Count == 0)
                throw new Exception("Dataset is empty. Nothing to save.");

            try
            {
                var lines = dataset.Values.Select(v => v.ToString());
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
