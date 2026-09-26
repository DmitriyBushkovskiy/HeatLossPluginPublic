using System.Globalization;
using HeatLoss.Application.Models;
using HeatLoss.Domain.Enums;
using HeatLoss.Geometry;
using HeatLoss.Infrastructure.Common;
using HeatLoss.Infrastructure.Common.DTO;
using HeatLoss.Infrastructure.Common.Enums;
using HeatLoss.Infrastructure.Common.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;

namespace HeatLoss.Application;

public class BuildingModelFactory
{
    private readonly HeatLossGeometry _geometry;
    private readonly Mapper _mapper;
    private readonly Validator _validator;
    private readonly IParameterResolver _parameterResolver;
    
    public BuildingModelFactory(
        HeatLossGeometry geometry,
        IBimProvider bimProvider)
    {
        _geometry = geometry;
        _parameterResolver = bimProvider.ParameterResolver;
        _mapper = new Mapper(_parameterResolver);
        _validator = new Validator(bimProvider);
    }

    public BuildingIntermediateModel Build(BimExtractedData rawData)
    {
        var nanocadSpaces = rawData.Spaces;
        var nanocadWalls = rawData.Walls;
        var nanocadOpenings = rawData.Openings;
        var nanocadGrids = rawData.Grids;
        var nanocadSlabs = rawData.Slabs;
        var materialsThermalConductivity = rawData.MaterialsThermalConductivity;
        var cardinalDirections = rawData.CardinalDirections;
        var projectData = rawData.ProjectData;
        
        var spaces = CreateSpaces(nanocadSpaces, nanocadWalls, nanocadOpenings);
        
        MoveSpaceInsideEdges(spaces);

        CreateWalls(spaces, nanocadGrids, materialsThermalConductivity, cardinalDirections);
        
        CreateOpenings(spaces, cardinalDirections);
        
        CreateFloorAreas(spaces, nanocadGrids, projectData);
        
        CreateCeilings(spaces, nanocadGrids, nanocadSlabs, materialsThermalConductivity);
        
        return new BuildingIntermediateModel(projectData.OutsideTemperature, spaces);
    }

    private List<SpaceIntermediateModel> CreateSpaces(List<SpaceDto> nanocadSpaces, List<LinearWallDto> nanocadWalls, List<OpeningDto> nanocadOpenings)
    {
        var spaces = new List<SpaceIntermediateModel>();
        foreach (var nanocadSpace in nanocadSpaces)
        {
            // создаем помещение
            var space = _mapper.ToSpaceModel(nanocadSpace);
            var spaceCoordinates = nanocadSpace.Coordinates;
            for (int i = 0; i < spaceCoordinates.Count; i++)
            {
                //
                // Удалена бизнес-логика
                // 
            }
            spaces.Add(space);
        }
        _validator.ValidateSpaces(spaces);
        return spaces;
    }
    
    /// <summary>
    /// Сдвиг внутренних граней помещения до середины внутренней стены
    /// </summary>
    private void MoveSpaceInsideEdges(List<SpaceIntermediateModel> spaces)
    {
        foreach (var space in spaces)
        {
            var spaceEdges = space.Edges;
            for (int i = 0; i < spaceEdges.Count; i++)
            {
                //
                // Удалена бизнес-логика
                // 
            }
        }
    }
    
    /// <summary>
    /// Создание участка стены для помещения
    /// </summary>
    private void CreateWalls(List<SpaceIntermediateModel> spaces, List<CoordinateGridDto> nanocadGrids, Dictionary<string, double> materialsThermalConductivity, Dictionary<CardinalDirection, Vector2D> cardinalDirections)
    {
        var materialIdParameterName = _parameterResolver.GetParameterName(ParameterKey.MaterialId);
        foreach (var space in spaces)
        {
            for (var i = 0; i < space.Edges.Count; i++)
            {
                var edge = space.Edges[i];
                var modelWall = edge.ModelWall!;
                var axis = Enum.Parse<EntityAxis>(modelWall.Parameters.FirstOrDefault(x => x.Name == _parameterResolver.GetParameterName(ParameterKey.PartAxis)).Value ?? string.Empty);
                if (modelWall.Position == SurfacePosition.Outside)
                {
                    //
                    // Удалена бизнес-логика
                    // 
                }
                else if (modelWall.Position == SurfacePosition.Inside)
                {
                    //
                    // Удалена бизнес-логика
                    // 
                }
                else
                    throw new NotImplementedException(); // TODO: remove?
            }
        }
        _validator.ValidateWalls(nanocadGrids, spaces);
    }
    
    private double GetWallThickness(LinearWallDto wall)
    {
        switch (wall.Position)
        {
            case SurfacePosition.Inside: return 0;
            case SurfacePosition.Outside: return wall.Thickness;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    private void CreateOpenings(List<SpaceIntermediateModel> spaces, Dictionary<CardinalDirection, Vector2D> cardinalDirections)
    {
        foreach (var space in spaces)
        {
            foreach (var edge in space.Edges)
            {
                foreach (var wall in edge.Walls)
                {
                    //
                    // Удалена бизнес-логика
                    // 
                }
            }
        }

        _validator.ValidateOpenings(spaces.SelectMany(x => x.Edges).SelectMany(x => x.Walls).SelectMany(x => x.Openings).ToList());
    }
    
    private void CreateFloorAreas(List<SpaceIntermediateModel> spaces, List<CoordinateGridDto> nanocadGrids, ProjectDataDto projectData)
    {
        
        var fistFloor = nanocadGrids.Single().Levels.OrderBy(x => x.Position).First(); //TODO: что если несколько сеток осей?
        var firstFloorSpaces = spaces.Where(x => Math.Abs(x.BottomLevel - fistFloor.Position) < 1).ToList();
        var firstFloorGeometry = _geometry.GetCommonPerimeters(firstFloorSpaces.Select(x => x.GetPolygon()), 1000).ToList();
        var secondFloorGeometry = _geometry.CreatePolygonsWithOffset(firstFloorGeometry, -2000);
        var thirdFloorGeometry = _geometry.CreatePolygonsWithOffset(secondFloorGeometry, -2000);
        var fourthFloorGeometry = _geometry.CreatePolygonsWithOffset(thirdFloorGeometry, -2000);

        var fourthArea = UnaryUnionOp.Union(fourthFloorGeometry);
        var thirdArea = UnaryUnionOp.Union(thirdFloorGeometry);
        var secondArea = UnaryUnionOp.Union(secondFloorGeometry);
        var firstArea = UnaryUnionOp.Union(firstFloorGeometry);
        
        foreach (var space in firstFloorSpaces)
        {
            var floor = new FloorIntermediateModel();
            //
            // Удалена бизнес-логика
            // 
            space.Floor = floor;
        }
    }

    private void CreateCeilings(List<SpaceIntermediateModel> spaces, List<CoordinateGridDto> nanocadGrids, List<SlabDto> nanocadSlabs, Dictionary<string, double> materialsThermalConductivity)
    {
        var spacesByBottom = spaces.GroupBy(x => x.BottomLevel).ToDictionary(x => x.Key, x => x.ToList());
        var spacesByTop = spaces.GroupBy(x => x.BottomLevel + x.Height).ToDictionary(x => x.Key, x => x.ToList());
        var slabs = nanocadGrids.Single().Levels
            .OrderBy(x => x.Position)
            .ToDictionary(g => g.Position, g => nanocadSlabs.Where(x => Math.Abs(x.BasePoint.Z - g.Position) < 1).ToArray());

        foreach (var currentSpace in spaces)
        {
            spacesByTop.TryGetValue(currentSpace.BottomLevel, out var bottomSpaces);
            spacesByBottom.TryGetValue(currentSpace.BottomLevel + currentSpace.Height, out var topSpaces);

            var bottomSlabs = slabs[currentSpace.BottomLevel];
            var topSlabs = slabs[currentSpace.BottomLevel + currentSpace.Height];
            
            var levels = new[]
            {
                (bottomSpaces, bottomSlabs, false),
                (topSpaces, topSlabs, true)
            };
            
            foreach (var level in levels)
            {
                //
                // Удалена бизнес-логика
                // 
            }
        }
        _validator.ValidateCeilings(spaces);
    }
    
    /// <summary>
    /// Создание полигона для фактического участка стены помещения
    /// </summary>
    private Polygon CreateWallPolygon(LineString baseLine, double internalOffset, double externalOffset)
        => _geometry.CreatePolygonByLine(baseLine, internalOffset, externalOffset);
    
    /// <summary>
    /// Получение стороны света ограждающей конструкции
    /// </summary>
    private CardinalDirection GetCardinalDirection(Dictionary<CardinalDirection, Vector2D> cardinalDirections, LineString spaceEdge, Polygon surfacePolygon)
    {
        var vect = _geometry.GetInnerPerpendicular(surfacePolygon, spaceEdge);
        
        var minAngle = Math.PI;
        var cardinalDirection = CardinalDirection.N;
        foreach (var pair in cardinalDirections)
        {
            var angle = Math.Abs(vect.AngleTo(_mapper.ToVector2D(pair.Value)));
            if (angle < minAngle)
            {
                minAngle = angle;
                cardinalDirection = pair.Key;
            }
        }
        return cardinalDirection;
    }
}