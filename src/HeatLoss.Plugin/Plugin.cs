using System.Collections.Concurrent;
using System.Diagnostics;
using HeatLoss.Application;
using HeatLoss.Calculations;
using HeatLoss.Infrastructure.NanoCad;
using HeatLoss.Domain.Enums;
using HeatLoss.Domain.Models;
using HeatLoss.Domain.Models.Results;
using HeatLoss.Reports;
using Teigha.Runtime;

namespace HeatLoss.Plugin;

public class Plugin
{
    private Building? _building;
    private BuildingHeatLossResult? _buildingHeatLossResult;
    private ConcurrentDictionary<string, Building> _buildings = new();

    [CommandMethod("HL_CALCULATE")]
    public void CalculateHeatLoss()
    {
        var sw = new Stopwatch();
        sw.Start();
        var bimProvider = new NanoCadProvider();
        var buildingProvider = new BuildingService(bimProvider);
        _building = buildingProvider.CreateBuilding();

        var calculator = new HeatLossCalculator();
        _buildingHeatLossResult = calculator.Calculate(_building);

        buildingProvider.SaveHeatLossToModel(_buildingHeatLossResult);

        var options = new ReportGeneratorOptions
        {
            LengthMeasurementUnit = LengthMeasurementUnit.Meter,
            ReportFilePath = GetReportPath(bimProvider.DocumentPath)
        };

        var reportGenerator = new ReportGenerator(options);
        reportGenerator.GenerateReport(_buildingHeatLossResult);
        sw.Stop();
        bimProvider.WriteMessage($"Время выполнения: {Math.Round(sw.Elapsed.TotalSeconds, 2)} сек. Отчет сформирован: {options.ReportFilePath}");
    }

    private string GetReportPath(string documentPath)
    {
        var now = DateTime.Now;
        var directoryName = Path.GetDirectoryName(documentPath);
        var fileName = Path.GetFileNameWithoutExtension(documentPath);
        return Path.Combine(directoryName, $"{fileName} - теплопотери {now:yyyy.MM.dd HH_mm_ss}.xlsx");
    }
}