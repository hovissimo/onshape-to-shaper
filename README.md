# onshape-to-shaper

Extract flat surface data from Onshape CAD for Shaper Origin CNC processing.

## Overview

This module provides a TypeScript/JavaScript API client for accessing the Onshape API to retrieve geometric data for flat surfaces. It's designed to help extract surface information from Onshape documents for use with CNC tools like the Shaper Origin.

## Features

- ✅ **FeatureScript SVG Export**: Custom FeatureScript feature to convert faces to SVG files for CNC
- ✅ **User Face Selection**: Interactive face selection using Onshape client messaging
- ✅ **CLI Display**: Real-time face information display in terminal
- ✅ **SVG Extraction Tool**: Extract SVG data from FeatureScript features via REST API
- ✅ OAuth HMAC-SHA256 authentication with Onshape API
- ✅ Parse Onshape document URLs to extract identifiers
- ✅ Retrieve parts from documents
- ✅ Get face/surface data with detailed geometry
- ✅ Filter for flat (planar) surfaces
- ✅ Extract edge loops and curves for surface boundaries
- ✅ Comprehensive TypeScript type definitions

## Quick Start

### FeatureScript SVG Export (Recommended)

**Best for: Generating SVG files for CNC from Onshape faces**

1. Upload the FeatureScript to Onshape (see [FeatureScript Guide](./FEATURESCRIPT_GUIDE.md))
2. Add the custom feature to your Part Studio
3. Select a planar face
4. Extract the SVG:

```bash
npm install
export ONSHAPE_ACCESS_KEY="your_key"
export ONSHAPE_SECRET_KEY="your_secret"
npm run extract-svg "https://cad.onshape.com/documents/.../w/.../e/..." output.svg
```

See the [FeatureScript Guide](./FEATURESCRIPT_GUIDE.md) for complete instructions.

### Face Selection Tool (Interactive)

**Best for: Exploring face data and testing**

```bash
# Install dependencies
npm install

# Start the selection server
npm run selection-server

# In another terminal, serve the client app
npm run serve-client
```

Then follow the [Face Selection Guide](./FACE_SELECTION_GUIDE.md) to set up the Onshape extension.

### API Usage (Programmatic)

For programmatic access to the Onshape API:

```bash
npm install
```

## Setup

### 1. Get Onshape API Credentials

1. Go to https://dev-portal.onshape.com/keys
2. Create a new API key pair
3. Save your Access Key and Secret Key securely

### 2. Configure Credentials

Store your credentials securely (e.g., in environment variables):

```bash
export ONSHAPE_ACCESS_KEY="your_access_key"
export ONSHAPE_SECRET_KEY="your_secret_key"
```

## Usage

### Interactive Face Selection

See the [Face Selection Guide](./FACE_SELECTION_GUIDE.md) for a complete tutorial on using the interactive face selection tool.

### Programmatic API Examples

#### Basic Example

```typescript
import { createOnshapeClient, OnshapeApiClient } from './src';

// Create API client
const client = createOnshapeClient(
  process.env.ONSHAPE_ACCESS_KEY!,
  process.env.ONSHAPE_SECRET_KEY!
);

// Parse Onshape document URL
const url = 'https://cad.onshape.com/documents/abc123/w/def456/e/ghi789';
const docId = OnshapeApiClient.parseDocumentUrl(url);

// Get parts
const parts = await client.getParts(docId!);

// Get flat surfaces from a part
const flatSurfaces = await client.getFlatSurfaces(docId!, parts[0].partId, {
  planar: true,
  minArea: 100 // minimum area in square mm
});

// Get detailed geometry for a specific surface
const faceDetails = await client.getFaceById(docId!, flatSurfaces[0].faceId);
console.log('Edges:', faceDetails.outerLoop.edges);
```

### Get Flat Surface with Filtering

```typescript
import { FaceSelectionFilter } from './src/types/onshape';

const filter: FaceSelectionFilter = {
  planar: true,        // Only flat surfaces
  minArea: 100,        // Minimum area in square mm
  maxArea: 10000       // Maximum area in square mm
};

const surfaces = await client.getFlatSurfaces(docId, partId, filter);
```

### Extract Edge Geometry

```typescript
const face = await client.getFaceById(docId, faceId);

// Outer boundary
face.outerLoop.edges.forEach(edge => {
  console.log(`${edge.curveType}: from (${edge.startVertex.position.x}, ${edge.startVertex.position.y}) to (${edge.endVertex.position.x}, ${edge.endVertex.position.y})`);
});

// Inner loops (holes)
face.innerLoops?.forEach(loop => {
  console.log('Hole with', loop.edges.length, 'edges');
});
```

## API Reference

### OnshapeApiClient

#### `constructor(credentials: OnshapeCredentials)`
Create a new API client instance.

#### `getParts(docId: OnshapeDocumentId): Promise<OnshapePart[]>`
Get all parts in a document.

#### `getFaces(docId: OnshapeDocumentId, partId: string): Promise<OnshapeFace[]>`
Get all faces for a specific part.

#### `getFlatSurfaces(docId: OnshapeDocumentId, partId: string, filter?: FaceSelectionFilter): Promise<OnshapeFace[]>`
Get filtered flat (planar) surfaces from a part.

#### `getFaceById(docId: OnshapeDocumentId, faceId: string): Promise<OnshapeFace>`
Get detailed geometry for a specific face including edges and curves.

#### `static parseDocumentUrl(url: string): OnshapeDocumentId | null`
Parse an Onshape URL to extract document identifiers.

### Factory Function

#### `createOnshapeClient(accessKey: string, secretKey: string, baseUrl?: string): OnshapeApiClient`
Convenience function to create a client instance.

## Development

### Build

```bash
npm run build
```

### Watch Mode

```bash
npm run dev
```

### Project Structure

```
src/
├── index.ts              # Main entry point
├── onshape-api.ts        # API client implementation
├── selection-server.ts   # Server for face selection events
├── cli.ts                # CLI tool for face selection
├── types/
│   └── onshape.ts        # TypeScript type definitions
└── example.ts            # API usage examples

client/
└── index.html            # Client app for Onshape iframe
```

## Implementation Status

This is currently a **stub implementation**. The following items need to be completed:

- [ ] Implement HTTP client for API requests (currently uses placeholder)
- [ ] Complete `getParts()` implementation with actual API endpoint
- [ ] Complete `getFaces()` implementation
- [ ] Complete `evaluateFace()` implementation
- [ ] Add error handling and retry logic
- [ ] Add response validation
- [ ] Add unit tests
- [ ] Add integration tests with Onshape sandbox

## Onshape API Documentation

- [Onshape API Explorer](https://cad.onshape.com/glassworks/explorer/)
- [Authentication Guide](https://onshape-public.github.io/docs/auth/)
- [Part Studios API](https://cad.onshape.com/glassworks/explorer/#/PartStudio)

## Related Resources

- [Onshape Forum Discussion](https://forum.onshape.com/discussion/19338/featurescript-app-to-generate-svg-for-cnc)
- [Shaper Origin](https://www.shapertools.com/en-us/origin)

## License

Unlicense - see LICENSE file for details
