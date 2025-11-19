/**
 * Example usage of the Onshape API client
 *
 * This demonstrates how to:
 * 1. Create an API client with credentials
 * 2. Parse an Onshape document URL
 * 3. Retrieve flat surfaces from a part
 * 4. Get detailed geometry for a specific face
 */

import { createOnshapeClient, OnshapeApiClient } from './onshape-api';
import { OnshapeFace, FaceSelectionFilter } from './types/onshape';

async function example() {
  // Step 1: Create API client with your credentials
  // Get these from: https://dev-portal.onshape.com/keys
  const client = createOnshapeClient(
    'YOUR_ACCESS_KEY',
    'YOUR_SECRET_KEY'
  );

  // Step 2: Parse Onshape document URL
  // Example URL: https://cad.onshape.com/documents/abc123/w/def456/e/ghi789
  const documentUrl = 'https://cad.onshape.com/documents/YOUR_DOCUMENT_ID/w/YOUR_WORKSPACE_ID/e/YOUR_ELEMENT_ID';
  const docId = OnshapeApiClient.parseDocumentUrl(documentUrl);

  if (!docId) {
    console.error('Invalid Onshape URL');
    return;
  }

  console.log('Document ID:', docId);

  // Step 3: Get parts in the document
  try {
    const parts = await client.getParts(docId);
    console.log(`Found ${parts.length} parts`);

    if (parts.length === 0) {
      console.log('No parts found in document');
      return;
    }

    // Use the first part for this example
    const partId = parts[0].partId;
    console.log(`Using part: ${parts[0].name} (${partId})`);

    // Step 4: Get all flat surfaces from the part
    const filter: FaceSelectionFilter = {
      planar: true,
      minArea: 100 // Only surfaces with area >= 100 square mm
    };

    const flatSurfaces = await client.getFlatSurfaces(docId, partId, filter);
    console.log(`Found ${flatSurfaces.length} flat surfaces`);

    // Step 5: Get detailed geometry for the first flat surface
    if (flatSurfaces.length > 0) {
      const face = flatSurfaces[0];
      console.log('\nFlat surface details:');
      console.log(`- Face ID: ${face.faceId}`);
      console.log(`- Surface type: ${face.surfaceType}`);
      console.log(`- Area: ${face.area}`);

      if (face.surface.normal) {
        console.log(`- Normal vector: (${face.surface.normal.x}, ${face.surface.normal.y}, ${face.surface.normal.z})`);
      }

      // Get full geometry including edges
      const detailedFace = await client.getFaceById(docId, face.faceId);

      console.log('\nEdge information:');
      if (detailedFace.outerLoop) {
        console.log(`- Outer loop has ${detailedFace.outerLoop.edges.length} edges`);

        detailedFace.outerLoop.edges.forEach((edge, index) => {
          console.log(`  Edge ${index + 1}:`);
          console.log(`    Type: ${edge.curveType}`);
          console.log(`    Start: (${edge.startVertex.position.x}, ${edge.startVertex.position.y}, ${edge.startVertex.position.z})`);
          console.log(`    End: (${edge.endVertex.position.x}, ${edge.endVertex.position.y}, ${edge.endVertex.position.z})`);
        });
      }

      if (detailedFace.innerLoops && detailedFace.innerLoops.length > 0) {
        console.log(`- ${detailedFace.innerLoops.length} inner loop(s) (holes)`);
      }
    }
  } catch (error) {
    console.error('Error accessing Onshape API:', error);
  }
}

// Run the example
if (require.main === module) {
  example().catch(console.error);
}

export { example };
