FeatureScript 2796;
import(path : "onshape/std/common.fs", version : "2796.0");

/**
 * Face to SVG Exporter
 *
 * Generates an SVG representation of a selected face for CNC machining.
 * Projects the face edges onto the face's plane and converts to SVG paths.
 *
 * Usage:
 * 1. Add this custom feature to your Part Studio
 * 2. Select a planar face
 * 3. The SVG will be stored as a feature attribute
 * 4. Copy the SVG from the feature properties or extract via API
 */

annotation { "Feature Type Name" : "faceToSvg", "Feature Type Description" : "Convert a flat face to an SVG representation" }
export const faceToSVG = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Face to export", "Filter" : EntityType.FACE, "MaxNumberOfPicks" : 1 }
        definition.face is Query;

        annotation { "Name" : "SVG Units", "Default" : "mm",
                    "UIHint" : UIHint.DISPLAY_SHORT }
        definition.units is string;

        annotation { "Name" : "Decimal precision", "Default" : 3 }
        isInteger(definition.precision, POSITIVE_COUNT_BOUNDS);

        annotation { "Name" : "Include dimensions" }
        definition.includeDimensions is boolean;

        annotation { "Name" : "Flip Y axis (for CNC)" }
        definition.flipY is boolean;
    }
    {
        // Verify face is planar
        var faceGeometry = evSurfaceDefinition(context, {
                "face" : definition.face
            });

        if (faceGeometry.surfaceType != SurfaceType.PLANE)
        {
            throw regenError("Selected face must be planar (flat). Selected face type: " ~ faceGeometry.surfaceType);
        }

        // Get face plane for coordinate system
        var plane = evFaceTangentPlane(context, {
                "face" : definition.face,
                "parameter" : vector(0.5, 0.5) // Center of face
            });

        // Create 2D coordinate system on the face
        var origin = plane.origin;
        var xAxis = plane.x;
        var yAxis = plane.y;

        // Get all edges of the face
        var edges = qOwnedByBody(qAdjacent(definition.face, AdjacencyType.EDGE, EntityType.EDGE), qOwnerBody(definition.face));
        var edgeArray = evaluateQuery(context, edges);

        if (size(edgeArray) == 0)
        {
            throw regenError("No edges found on selected face");
        }

        // Extract edge loops (outer boundary and holes)
        var loops = extractEdgeLoops(context, definition.face, edgeArray);

        // Convert edges to SVG paths
        var svgPaths = [];
        var bounds = { "minX" : undefined, "minY" : undefined, "maxX" : undefined, "maxY" : undefined };

        for (var loop in loops)
        {
            var pathData = edgeLoopToSVGPath(context, loop, origin, xAxis, yAxis,
            definition.units, definition.precision,
            definition.flipY, bounds);
            svgPaths = append(svgPaths, pathData);
        }

        // Calculate dimensions
        var width = bounds.maxX - bounds.minX;
        var height = bounds.maxY - bounds.minY;

        // Adjust viewBox to include padding
        var padding = 5; // 5 units padding
        var viewBoxMinX = bounds.minX - padding;
        var viewBoxMinY = bounds.minY - padding;
        var viewBoxWidth = width + (2 * padding);
        var viewBoxHeight = height + (2 * padding);

        // Generate SVG document
        var svg = '<?xml version="1.0" encoding="UTF-8"?>\n';
        svg ~= '<svg xmlns="http://www.w3.org/2000/svg" ';
        svg ~= 'width="' ~ formatNumber(width, definition.precision) ~ definition.units ~ '" ';
        svg ~= 'height="' ~ formatNumber(height, definition.precision) ~ definition.units ~ '" ';
        svg ~= 'viewBox="' ~ formatNumber(viewBoxMinX, definition.precision) ~ ' ';
        svg ~= formatNumber(viewBoxMinY, definition.precision) ~ ' ';
        svg ~= formatNumber(viewBoxWidth, definition.precision) ~ ' ';
        svg ~= formatNumber(viewBoxHeight, definition.precision) ~ '">\n';

        // Add description
        svg ~= '  <desc>Generated from Onshape face - Width: ';
        svg ~= formatNumber(width, definition.precision) ~ definition.units;
        svg ~= ' Height: ' ~ formatNumber(height, definition.precision) ~ definition.units;
        svg ~= '</desc>\n';

        // Add paths
        svg ~= '  <g id="face-outline" fill="none" stroke="black" stroke-width="0.5">\n';
        for (var pathData in svgPaths)
        {
            svg ~= '    <path d="' ~ pathData ~ '" />\n';
        }
        svg ~= '  </g>\n';

        // Optionally add dimension annotations
        if (definition.includeDimensions)
        {
            svg ~= '  <g id="dimensions" fill="red" font-family="Arial" font-size="12">\n';
            svg ~= '    <text x="' ~ formatNumber(bounds.minX, 1) ~ '" y="' ~ formatNumber(bounds.minY - 2, 1) ~ '">';
            svg ~= 'W: ' ~ formatNumber(width, definition.precision) ~ definition.units ~ '</text>\n';
            svg ~= '    <text x="' ~ formatNumber(bounds.minX, 1) ~ '" y="' ~ formatNumber(bounds.minY - 15, 1) ~ '">';
            svg ~= 'H: ' ~ formatNumber(height, definition.precision) ~ definition.units ~ '</text>\n';
            svg ~= '  </g>\n';
        }

        svg ~= '</svg>';

        // Store SVG as feature attribute
        setAttribute(context, {
                    "entities" : qCreatedBy(id, EntityType.BODY),
                    "name" : "svgData",
                    "value" : svg
                });

        // Also store as a more accessible attribute
        setFeatureComputedParameter(context, id, {
                    "name" : "svgOutput",
                    "value" : svg
                });

        // Report to user
        reportFeatureInfo(context, id, "SVG generated: " ~ formatNumber(width, 2) ~ " x " ~ formatNumber(height, 2) ~ " " ~ definition.units);
    });

/**
 * Extract edge loops from a face
 * Returns array of loops, where each loop is an array of edges
 */
function extractEdgeLoops(context is Context, face is Query, edges is array) returns array
{
    // For now, return all edges as a single loop
    // TODO: Implement proper loop detection for faces with holes
    return [edges];
}

/**
 * Convert an edge loop to SVG path data
 */
function edgeLoopToSVGPath(context is Context, edgeLoop is array,
    origin is Vector, xAxis is Vector, yAxis is Vector,
    units is string, precision is number, flipY is boolean,
    bounds is map) returns string
{
    var pathCommands = [];
    var firstPoint = undefined;

    for (var edge in edgeLoop)
    {
        var edgeGeom = evCurveDefinition(context, {
                "edge" : edge
            });

        // Get edge endpoints
        var startPoint3D = evEdgeTangentLine(context, {
                    "edge" : edge,
                    "parameter" : 0
                }).origin;

        var endPoint3D = evEdgeTangentLine(context, {
                    "edge" : edge,
                    "parameter" : 1
                }).origin;

        // Project to 2D
        var start2D = project3DTo2D(startPoint3D, origin, xAxis, yAxis, units, flipY);
        var end2D = project3DTo2D(endPoint3D, origin, xAxis, yAxis, units, flipY);

        // Update bounds
        updateBounds(bounds, start2D.x, start2D.y);
        updateBounds(bounds, end2D.x, end2D.y);

        // Start path with Move command if first edge
        if (firstPoint == undefined)
        {
            pathCommands = append(pathCommands, "M " ~ formatNumber(start2D.x, precision) ~ " " ~ formatNumber(start2D.y, precision));
            firstPoint = start2D;
        }

        // Add curve segment
        if (edgeGeom.curveType == CurveType.LINE)
        {
            // Straight line
            pathCommands = append(pathCommands, "L " ~ formatNumber(end2D.x, precision) ~ " " ~ formatNumber(end2D.y, precision));
        }
        else if (edgeGeom.curveType == CurveType.CIRCLE)
        {
            // Arc - need to determine if it's a full circle or arc
            // For now, approximate with line (TODO: implement proper arc conversion)
            pathCommands = append(pathCommands, "L " ~ formatNumber(end2D.x, precision) ~ " " ~ formatNumber(end2D.y, precision));

            // Proper arc command would be:
            // A rx ry x-axis-rotation large-arc-flag sweep-flag x y
        }
        else
        {
            // Other curve types - approximate with line for now
            pathCommands = append(pathCommands, "L " ~ formatNumber(end2D.x, precision) ~ " " ~ formatNumber(end2D.y, precision));
        }
    }

    // Close the path
    pathCommands = append(pathCommands, "Z");

    return joinStrings(pathCommands, " ");
}

/**
 * Project a 3D point onto the 2D face coordinate system
 */
function project3DTo2D(point3D is Vector, origin is Vector,
    xAxis is Vector, yAxis is Vector,
    units is string, flipY is boolean) returns map
{
    var relativePos = point3D - origin;

    // Convert string unit to actual unit value
    var unitValue = getUnitValue(units);

    var x = dot(relativePos, xAxis) / unitValue;
    var y = dot(relativePos, yAxis) / unitValue;

    if (flipY)
    {
        y = -y;
    }

    return { "x" : x, "y" : y };
}

/**
 * Convert unit string to FeatureScript unit value
 */
function getUnitValue(unitStr is string) returns ValueWithUnits
{
    if (unitStr == "mm")
        return 1 * millimeter;
    else if (unitStr == "cm")
        return 1 * centimeter;
    else if (unitStr == "in")
        return 1 * inch;
    else if (unitStr == "m")
        return 1 * meter;
    else
        return 1 * millimeter; // Default to mm
}

/**
 * Update bounding box
 */
function updateBounds(bounds is map, x is number, y is number)
{
    if (bounds.minX == undefined || x < bounds.minX)
        bounds.minX = x;
    if (bounds.maxX == undefined || x > bounds.maxX)
        bounds.maxX = x;
    if (bounds.minY == undefined || y < bounds.minY)
        bounds.minY = y;
    if (bounds.maxY == undefined || y > bounds.maxY)
        bounds.maxY = y;
}

/**
 * Format number with specified precision
 */
function formatNumber(value is number, precision is number) returns string
{
    var multiplier = 10 ^ precision;
    var rounded = round(value * multiplier) / multiplier;
    return rounded ~ "";
}

/**
 * Join array of strings
 */
function joinStrings(strings is array, separator is string) returns string
{
    if (size(strings) == 0)
        return "";

    var result = strings[0];
    for (var i = 1; i < size(strings); i += 1)
    {
        result ~= separator ~ strings[i];
    }
    return result;
}
