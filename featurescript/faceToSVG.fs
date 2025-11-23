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
/**
 * Get the endpoints of the passed edge query
 */
function getEndpointsForEdge(context, edgeQuery is Query)
{
    var qEndpoints = evaluateQuery(context, qAdjacent(edgeQuery, AdjacencyType.VERTEX, EntityType.VERTEX));
    var endpoints = [];
    for (var qE in qEndpoints)
    {
        var point = evVertexPoint(context, { "vertex" : qE });
        endpoints = append(endpoints, point);
    }
    return endpoints;
}

function assignLoopToEdgeIndex(loops is box, edgeData is box, loopIndex is number, edgeIndex is number)
{
    var loopId = "loop" ~ loopIndex;
    if (edgeData[][edgeIndex] == undefined)
        edgeData[][edgeIndex] = {};
    if (edgeData[][edgeIndex].loop == undefined)
    {
        edgeData[][edgeIndex].loop = loopId;
    }
    else
    {
        println("ERROR: This edge already belongs to a loop.");
    }
    if (loops[][loopId] == undefined)
        loops[][loopId] = [];
    loops[][loopId] = append(loops[][loopId], edgeIndex);
    //println("[assignLoopToEdgeIndex] loops[][\"" ~ loopId ~ "\"]: " ~ loops[][loopId]);
    //println("[assignLoopToEdgeIndex] edgeData[][\"" ~ edgeIndex ~ "\"]: " ~ edgeData[][edgeIndex]);
}

// This function helps us walk along an edge loop, either interior or exterior, returning the id of the next connected edge.
// The "forward" direction is the opposite direction of `lastEndpoint`.
//
//  Params:
//      edges  array[edgeQuery]
//          an evaluated query of edges that are the boundaries of the selected face
//      endpointsToEdges  map[Vertex(array) -> array[number]]
//          a map of evaluated vertexes (the endpoints of the edges in `edges`) to
//          an array of which edges that endpoint belongs to.
//      thisEdgeId  number
//          the index of the current edge in `edges`
//      lastEndpoint  Vector(array)
//          the next edge is the current edge's neighbor in the opposite direction of lastEndpoint
//  Returns:
//      array of [nextEdgeId, connectingEndpoint]
function getNextConnectedEdge(context is Context, edges is array, endpointsToEdges is map, thisEdgeId is number, lastEndpoint is array)
{
    var thisEdgeEndpoints = getEndpointsForEdge(context, edges[thisEdgeId]);
    var forwardEndpoint = (lastEndpoint == thisEdgeEndpoints[1]) ? thisEdgeEndpoints[0] : thisEdgeEndpoints[1];
    var forwardEndpointEdgeIds = endpointsToEdges[forwardEndpoint];
    if (size(forwardEndpointEdgeIds) != 2)
    {
        // We expect all the edges around the selected face to form one or more loops.
        // That means each edge (be it a line, a curve, or whatever) should have exactly
        // two endpoints. If that's not true, then this script has bad assumptions.
        println("ERROR: Unexpected number of edges for the first endpoint of edge " ~ thisEdgeId ~ ".");
        debug(context, forwardEndpoint);
        debug(context, edges[thisEdgeId]);
    }
    return [
            (forwardEndpointEdgeIds[1] == thisEdgeId) ? forwardEndpointEdgeIds[0] : forwardEndpointEdgeIds[1],
            forwardEndpoint,
        ];
}

/**
 * Extract edge loops from a face
 * Returns array of loops, where each loop is an array of edges
 */
function extractEdgeLoops(context is Context, face is Query) returns array
{
    // Get all edges of the face
    var edgeQueries = qOwnedByBody(qAdjacent(face, AdjacencyType.EDGE, EntityType.EDGE), qOwnerBody(face));
    var edges = evaluateQuery(context, edgeQueries);
    println("There are " ~ size(edges) ~ " edges.");

    //println("Working with this edge: " ~ edge); debug(context, edge);


    // collect map of vertices to edges for efficient edge matching
    var endpointsToEdges = {};
    for (var i = 0; i < size(edges); i += 1)
    {
        var edge = edges[i];
        //println("looping on edge " ~ i);
        var endpoints = getEndpointsForEdge(context, edge);

        for (var j = 0; j < size(endpoints); j += 1)
        {
            var endpoint = endpoints[j];
            //println("looping on endpoint " ~ i ~ "." ~ j);

            //areQueriesEquivalent(context, 1, 2)
            //debug(context, endpoint);
            //debug(context, endpointsToEdges[endpoint]);
            if (endpointsToEdges[endpoint] == undefined)
            {
                endpointsToEdges[endpoint] = [];
            }

            // Add the index of this edge to the list of edges for this endpoint
            endpointsToEdges[endpoint] = append(endpointsToEdges[endpoint], i);
        }
    }

    // make a map of endpoints to edges
    // walk the edges -
    //   for each each: pick an endpoint and find the next edge
    //   assigned each edge to a loop

    var edgeData = new box({});
    var loops = new box({});

    //var colors = [
    //    DebugColor.GREEN,
    //    DebugColor.YELLOW,
    //    DebugColor.BLUE,
    //    DebugColor.RED,
    //    DebugColor.CYAN,
    //    DebugColor.MAGENTA,
    //    DebugColor.BLACK,
    //];

    // Walk the edges of this face and assign each edge to a loop.
    // We walk the edges of this face by connection, and secondarily by index.
    // We should visit each edge exactly once.
    //
    // Walking edges by connection (inner loop):
    //      Find the next edge by looking at the current edge's endpoints, and finding the edge connected to
    //      the "forward" endpoint that's not this edge.
    //
    // Walking edges by index (outer loop):
    //      Increment the edge index, skipping any edges already assigned to a loop.
    var currentLoopIndex = 0;
    for (var i = 0; i < size(edges); i += 1)
    {
        // Skip any edges we've already visited and assigned to a loop
        if (edgeData[][i]?.loop != undefined)
        {
            //println("Skipping already visited edge: " ~ i);
            continue;
        }
        
        // Set initial vars for this edge, and walk connected edges until we complete this loop...
        var thisEdgeId = i;
        var endpoints = getEndpointsForEdge(context, edges[thisEdgeId]);
        if (size(endpoints) == 0) {
            // Special case: This edge has no endpoints, implying it is a circle or ellipse and is already a complete loop.
            assignLoopToEdgeIndex(loops, edgeData, currentLoopIndex, thisEdgeId);
            println("loop" ~ currentLoopIndex ~ " is complete. It has these edges: " ~ loops[]["loop" ~ currentLoopIndex]);
            //debug(context, edges[thisEdgeId], colors[currentLoopIndex]);
            currentLoopIndex += 1;
            continue;
        }
        var forwardEndpoint = endpoints[0]; // Just pick one to set direction
        while (true)
        {
            //debug(context, edges[thisEdgeId], colors[currentLoopIndex]);

            if (edgeData[][thisEdgeId]?.loop == undefined)
            {
                // This edge is unassigned, assign it to this loop.
                assignLoopToEdgeIndex(loops, edgeData, currentLoopIndex, thisEdgeId);
            }
            else if (edgeData[][thisEdgeId].loop != ("loop" ~ currentLoopIndex))
            {
                // While walking connected edges, we found an edge already assigned to a different loop.
                // We expect all the edges around the selected face to form one or more loops.
                // Each edge of the face should only belong to one loop.
                println("ERROR: Trying to join edges to different loops!");
                break;
            }
            else if (edgeData[][thisEdgeId].loop == ("loop" ~ currentLoopIndex))
            {
                // While walking connected edges, we found an edge already assigned to the current loop.
                println("loop" ~ currentLoopIndex ~ " is complete. It has these edges: " ~ loops[]["loop" ~ currentLoopIndex]);
                currentLoopIndex += 1;
                break;
            }

            // Before we loop, get the next edge (the other edge of the forwardEndpoint that's not this edge)
            var nextEdgeResult = getNextConnectedEdge(context, edges, endpointsToEdges, thisEdgeId, forwardEndpoint);
            thisEdgeId = nextEdgeResult[0];
            forwardEndpoint = nextEdgeResult[1];
        }
    }
    
    //println("loops[]: " ~ loops[]);
    //println("edgeData[]: " ~ edgeData[]);
    
    return [edges, loops];
}

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
        var yAxis = normalize(cross(plane.normal, plane.x));

        // Extract edge loops (outer boundary and holes)
        var extractResult = extractEdgeLoops(context, definition.face);
        var edges = extractResult[0];
        var loops = extractResult[1];
        //debug(context, loops);

        debug(context, 'done');
        // Convert edges to SVG paths
        var svgPaths = [];
        var bounds = { "minX" : undefined, "minY" : undefined, "maxX" : undefined, "maxY" : undefined };

        for (var loop in loops[])
        {
            var pathData = edgeLoopToSVGPath(context, edges, loop.value, origin, xAxis, yAxis,
            definition.units, definition.precision,
            definition.flipY, bounds);
            svgPaths = append(svgPaths, pathData);
        }
        debug(context, svgPaths);

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
 * Convert an edge loop to SVG path data
 */
function edgeLoopToSVGPath(
    context is Context,
    edges is array,
    edgeIndices is array,
    origin is Vector, xAxis is Vector, yAxis is Vector,
    units is string, precision is number, flipY is boolean,
    bounds is map) returns string
{
    var pathCommands = [];
    var firstPoint = undefined;

    for (var edgeIndex in edgeIndices)
    {
        var edge = edges[edgeIndex];
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
function project3DTo2D(
    point3D is Vector,
    origin is Vector,
    xAxis is Vector,
    yAxis is Vector,
    units is string,
    flipY is boolean) returns map
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

/* evEdgeTangentLines
   var tanlines = evEdgeTangentLines(context, {
   "edge" : edgeQuery,
   "parameters" : [0, 1],
   });

   println('1');
   //debug(context, tanlines[0]);

   println('2');
   //debug(context, tanlines[1]);
 */
