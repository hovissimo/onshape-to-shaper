/**
 * Type definitions for Onshape API
 */

/**
 * Onshape API credentials
 */
export interface OnshapeCredentials {
  accessKey: string;
  secretKey: string;
  baseUrl?: string; // defaults to https://cad.onshape.com
}

/**
 * Document/Part/Assembly identifier
 */
export interface OnshapeDocumentId {
  documentId: string;
  workspaceId?: string;
  versionId?: string;
  elementId: string;
}

/**
 * 3D Point in space
 */
export interface Point3D {
  x: number;
  y: number;
  z: number;
}

/**
 * 3D Vector
 */
export interface Vector3D {
  x: number;
  y: number;
  z: number;
}

/**
 * UV Parameter point on a surface
 */
export interface UVPoint {
  u: number;
  v: number;
}

/**
 * Surface type enumeration
 */
export enum SurfaceType {
  PLANE = 'PLANE',
  CYLINDER = 'CYLINDER',
  CONE = 'CONE',
  SPHERE = 'SPHERE',
  TORUS = 'TORUS',
  SPLINE = 'SPLINE',
  EXTRUDED = 'EXTRUDED',
  REVOLVED = 'REVOLVED',
  OTHER = 'OTHER'
}

/**
 * Curve type enumeration
 */
export enum CurveType {
  LINE = 'LINE',
  CIRCLE = 'CIRCLE',
  ELLIPSE = 'ELLIPSE',
  SPLINE = 'SPLINE',
  OTHER = 'OTHER'
}

/**
 * Face/Surface data from Onshape
 */
export interface OnshapeFace {
  faceId: string;
  surfaceType: SurfaceType;
  surface: Surface;
  outerLoop: Loop;
  innerLoops?: Loop[];
  area?: number;
}

/**
 * Surface geometry definition
 */
export interface Surface {
  type: SurfaceType;
  // For planes
  origin?: Point3D;
  normal?: Vector3D;
  xAxis?: Vector3D;
  yAxis?: Vector3D;
  // For other surface types, additional properties would be added
}

/**
 * A loop of edges forming a boundary
 */
export interface Loop {
  edges: Edge[];
}

/**
 * Edge/Curve data
 */
export interface Edge {
  edgeId: string;
  curveType: CurveType;
  curve: Curve;
  startVertex: Vertex;
  endVertex: Vertex;
  trimParameters?: {
    start: number;
    end: number;
  };
}

/**
 * Curve geometry definition
 */
export interface Curve {
  type: CurveType;
  // For lines
  start?: Point3D;
  end?: Point3D;
  direction?: Vector3D;
  // For circles/arcs
  center?: Point3D;
  radius?: number;
  normal?: Vector3D;
  xAxis?: Vector3D;
  // For splines and other curve types, additional properties would be added
}

/**
 * Vertex/Point data
 */
export interface Vertex {
  vertexId: string;
  position: Point3D;
}

/**
 * Part metadata
 */
export interface OnshapePart {
  partId: string;
  name: string;
  elementId: string;
}

/**
 * Response from faces query
 */
export interface FacesResponse {
  faces: OnshapeFace[];
}

/**
 * Request options for evaluating face data
 */
export interface EvaluateFaceRequest {
  documentId: string;
  workspaceId?: string;
  versionId?: string;
  elementId: string;
  faceId: string;
  // Additional options for controlling what data to retrieve
  includeEdges?: boolean;
  includeTrimCurves?: boolean;
}

/**
 * Selection filter for faces
 */
export interface FaceSelectionFilter {
  surfaceType?: SurfaceType;
  minArea?: number;
  maxArea?: number;
  planar?: boolean; // Only get planar (flat) surfaces
}
