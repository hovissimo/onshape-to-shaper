/**
 * Local server to receive face selection events from Onshape client
 * and display face information in the CLI
 */

import http from 'http';
import { createOnshapeClient } from './onshape-api';
import { OnshapeDocumentId } from './types/onshape';

interface SelectionData {
  documentId: string;
  workspaceId: string;
  elementId: string;
  faceId: string;
  selectionData: any;
  timestamp: string;
}

// ANSI color codes for CLI output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  dim: '\x1b[2m',
  cyan: '\x1b[36m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m',
  red: '\x1b[31m',
};

// Format and print face information to CLI
function printFaceInfo(selection: SelectionData) {
  console.log('\n' + '='.repeat(80));
  console.log(`${colors.bright}${colors.cyan}FACE SELECTION RECEIVED${colors.reset}`);
  console.log('='.repeat(80));

  console.log(`\n${colors.bright}Document Info:${colors.reset}`);
  console.log(`  Document ID: ${colors.yellow}${selection.documentId}${colors.reset}`);
  console.log(`  Workspace ID: ${colors.yellow}${selection.workspaceId}${colors.reset}`);
  console.log(`  Element ID: ${colors.yellow}${selection.elementId}${colors.reset}`);

  console.log(`\n${colors.bright}Selection Info:${colors.reset}`);
  console.log(`  Face ID: ${colors.green}${selection.faceId}${colors.reset}`);
  console.log(`  Timestamp: ${colors.dim}${selection.timestamp}${colors.reset}`);

  console.log(`\n${colors.bright}Raw Selection Data:${colors.reset}`);
  console.log(JSON.stringify(selection.selectionData, null, 2));

  console.log('\n' + '='.repeat(80) + '\n');
}

// Fetch and print detailed face geometry from Onshape API
async function fetchAndPrintFaceGeometry(selection: SelectionData) {
  const accessKey = process.env.ONSHAPE_ACCESS_KEY;
  const secretKey = process.env.ONSHAPE_SECRET_KEY;

  if (!accessKey || !secretKey) {
    console.log(`${colors.yellow}⚠ Onshape API credentials not set${colors.reset}`);
    console.log(`${colors.dim}Set ONSHAPE_ACCESS_KEY and ONSHAPE_SECRET_KEY to fetch detailed geometry${colors.reset}\n`);
    return;
  }

  try {
    console.log(`${colors.dim}Fetching detailed face geometry from Onshape API...${colors.reset}`);

    const client = createOnshapeClient(accessKey, secretKey);

    const docId: OnshapeDocumentId = {
      documentId: selection.documentId,
      workspaceId: selection.workspaceId,
      elementId: selection.elementId
    };

    // This would fetch detailed geometry - currently stubbed
    // const faceDetails = await client.getFaceById(docId, selection.faceId);

    console.log(`${colors.dim}Note: Detailed geometry fetch is stubbed - implement makeRequest() in onshape-api.ts${colors.reset}\n`);

    // Example of what we would print:
    // console.log(`${colors.bright}Face Geometry:${colors.reset}`);
    // console.log(`  Surface Type: ${faceDetails.surfaceType}`);
    // console.log(`  Area: ${faceDetails.area} mm²`);
    // if (faceDetails.surface.normal) {
    //   console.log(`  Normal: (${faceDetails.surface.normal.x}, ${faceDetails.surface.normal.y}, ${faceDetails.surface.normal.z})`);
    // }

  } catch (error) {
    console.error(`${colors.red}Error fetching face geometry:${colors.reset}`, error);
  }
}

// Create HTTP server to receive selection events
export function startSelectionServer(port: number = 3000) {
  const server = http.createServer(async (req, res) => {
    // Enable CORS for local development
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    // Handle preflight requests
    if (req.method === 'OPTIONS') {
      res.writeHead(200);
      res.end();
      return;
    }

    // Handle selection events
    if (req.method === 'POST' && req.url === '/selection') {
      let body = '';

      req.on('data', chunk => {
        body += chunk.toString();
      });

      req.on('end', async () => {
        try {
          const selection: SelectionData = JSON.parse(body);

          // Print face information to CLI
          printFaceInfo(selection);

          // Optionally fetch detailed geometry from API
          await fetchAndPrintFaceGeometry(selection);

          // Send success response
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({
            success: true,
            message: 'Selection received',
            faceId: selection.faceId
          }));

        } catch (error) {
          console.error(`${colors.red}Error processing selection:${colors.reset}`, error);
          res.writeHead(400, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({
            success: false,
            error: 'Invalid request'
          }));
        }
      });
    } else {
      // Handle other requests
      res.writeHead(404);
      res.end('Not found');
    }
  });

  server.listen(port, () => {
    console.log(`${colors.bright}${colors.green}Selection Server Started${colors.reset}`);
    console.log(`${colors.dim}Listening on http://localhost:${port}${colors.reset}`);
    console.log(`${colors.dim}Waiting for face selections from Onshape...${colors.reset}\n`);
  });

  // Handle graceful shutdown
  process.on('SIGINT', () => {
    console.log(`\n${colors.dim}Shutting down server...${colors.reset}`);
    server.close(() => {
      console.log(`${colors.green}Server stopped${colors.reset}`);
      process.exit(0);
    });
  });

  return server;
}

// Run server if executed directly
if (require.main === module) {
  const port = parseInt(process.env.PORT || '3000', 10);
  startSelectionServer(port);
}
