using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RobustEstimation.Models;
using RobustEstimation.Models.Regression;

namespace RobustEstimation.Models;

public static class FileManager
{
    /// <summary>
    /// Loads a dataset from a text file.
    /// If every non-empty segment separated by ';' можно распарсить как "x,y",
    /// загружаем их в Dataset.Points, иначе — как одномерные Values.
    /// </summary>
    public static async Task<Dataset> LoadFromFileAsync(string path)
    {
        var dataset = new Dataset();
        string content;

        try
        {
            content = await File.ReadAllTextAsync(path);
        }
        catch (IOException ex)
        {
            throw new Exception($"File I/O error: {ex.Message}", ex);
        }

        // Разбиваем по точкам с запятой / переносу строки
        var segments = content
            .Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        // Проверяем, все ли сегменты имеют формат "x,y"
        bool allPairs = segments.All(seg =>
        {
            var parts = seg.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2
                   && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                   && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        });

        if (allPairs)
        {
            // Загружаем в Points
            var pts = segments
                .Select(seg =>
                {
                    var p = seg.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return (
                        X: double.Parse(p[0], CultureInfo.InvariantCulture),
                        Y: double.Parse(p[1], CultureInfo.InvariantCulture)
                    );
                })
                .ToList();
            if (pts.Count == 0)
                throw new FormatException("No valid points found in file.");
            dataset.SetPoints(pts);
        }
        else
        {
            // Иначе — разбиваем на числа любыми разделителями [;, \t\r\n]
            var tokens = Regex
                .Split(content, @"[;\s,]+")
                .Where(s => s.Length > 0)
                .ToList();

            var nums = tokens
                .Select(t => double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? (double?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            if (nums.Count == 0)
                throw new FormatException("No valid numbers found in file.");
            dataset.SetValues(nums);
        }

        return dataset;
    }

    /// <summary>
    /// Saves Dataset (Values или Points), результаты расчёта и, если есть — результаты регрессии.
    /// </summary>
    public static async Task SaveToFileAsync(
        Dataset dataset,
        string path,
        MethodType selectedMethod,
        string methodParameter,
        string methodProcessedData,
        string computedResult,
        RegressionResult regression = null)
    {
        if (dataset == null)
            throw new ArgumentNullException(nameof(dataset));

        var lines = new List<string>();

        // --- Исходные данные ---
        if (dataset.Values.Count > 0)
        {
            lines.Add("Původní hodnoty:");
            lines.Add(string.Join(", ", dataset.Values.Select(v =>
                v.ToString(CultureInfo.InvariantCulture))));
        }
        else if (dataset.Points.Count > 0)
        {
            lines.Add("Původní body:");
            lines.Add(string.Join("; ", dataset.Points.Select(p =>
                $"{p.X.ToString(CultureInfo.InvariantCulture)},{p.Y.ToString(CultureInfo.InvariantCulture)}")));
        }

        // --- Метод и его параметры ---
        lines.Add("");
        lines.Add($"Metoda: {selectedMethod}");
        if (!string.IsNullOrEmpty(methodParameter))
            lines.Add($"Parameter: {methodParameter}");

        // --- Обработанные данные ---
        if (!string.IsNullOrEmpty(methodProcessedData))
        {
            lines.Add("");
            lines.Add("Zpracovaná data:");
            lines.Add(methodProcessedData);
        }

        // --- Основной результат ---
        lines.Add("");
        lines.Add($"{computedResult.ToString(CultureInfo.InvariantCulture)}");

        // --- Доп.метрики регрессии, если есть ---
        if (regression != null)
        {
            lines.Add("");
            lines.Add("Regresní fit:");
            lines.Add($"Svala:    {regression.Slope:F4}");
            lines.Add($"Intercept:{regression.Intercept:F4}");
            lines.Add($"Medián čtvercových reziduí: {regression.MedianSquaredResidual:F4}");
        }

        try
        {
            await File.WriteAllLinesAsync(path, lines);
        }
        catch (IOException ex)
        {
            throw new Exception($"File I/O error: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new Exception($"Access denied to file: {ex.Message}", ex);
        }
    }
}
