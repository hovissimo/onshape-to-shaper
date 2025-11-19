#!/usr/bin/env node
/**
 * CLI tool for Onshape face selection
 *
 * Usage:
 *   npm run selection-server
 *   or
 *   node dist/cli.js
 */

import { startSelectionServer } from './selection-server';

const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  dim: '\x1b[2m',
  cyan: '\x1b[36m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
};

function printBanner() {
  console.clear();
  console.log(`${colors.bright}${colors.cyan}`);
  console.log('╔════════════════════════════════════════════════════════════════════════╗');
  console.log('║                   Onshape Face Selection Tool                         ║');
  console.log('║                     onshape-to-shaper v1.0.0                          ║');
  console.log('╚════════════════════════════════════════════════════════════════════════╝');
  console.log(`${colors.reset}\n`);
}

function printInstructions() {
  console.log(`${colors.bright}Setup Instructions:${colors.reset}\n`);

  console.log(`${colors.green}1. Start the Selection Server${colors.reset}`);
  console.log(`   ${colors.dim}This server is now running and waiting for selections...${colors.reset}\n`);

  console.log(`${colors.green}2. Host the Client App${colors.reset}`);
  console.log(`   ${colors.dim}Serve the client/index.html file on a public URL (required for Onshape)${colors.reset}`);
  console.log(`   ${colors.dim}Example: npx http-server client -p 8080 --cors${colors.reset}\n`);

  console.log(`${colors.green}3. Create Onshape Extension${colors.reset}`);
  console.log(`   ${colors.dim}Go to https://dev-portal.onshape.com${colors.reset}`);
  console.log(`   ${colors.dim}Create a new OAuth Application / Extension${colors.reset}`);
  console.log(`   ${colors.dim}Set the iframe URL to your hosted client app${colors.reset}`);
  console.log(`   ${colors.dim}Grant permissions: Read documents${colors.reset}\n`);

  console.log(`${colors.green}4. Use in Onshape${colors.reset}`);
  console.log(`   ${colors.dim}Open a document in Onshape${colors.reset}`);
  console.log(`   ${colors.dim}Add your extension as a tab${colors.reset}`);
  console.log(`   ${colors.dim}Click "Request Face Selection" and select a face${colors.reset}`);
  console.log(`   ${colors.dim}Face information will appear in this terminal!${colors.reset}\n`);

  console.log(`${colors.yellow}Optional: Set API Credentials for Detailed Geometry${colors.reset}`);
  console.log(`   ${colors.dim}export ONSHAPE_ACCESS_KEY="your_key"${colors.reset}`);
  console.log(`   ${colors.dim}export ONSHAPE_SECRET_KEY="your_secret"${colors.reset}\n`);

  console.log('─'.repeat(80) + '\n');
}

function main() {
  printBanner();
  printInstructions();

  // Check for API credentials
  const hasCredentials = process.env.ONSHAPE_ACCESS_KEY && process.env.ONSHAPE_SECRET_KEY;
  if (hasCredentials) {
    console.log(`${colors.green}✓ Onshape API credentials detected${colors.reset}`);
    console.log(`${colors.dim}  Will fetch detailed geometry for selected faces${colors.reset}\n`);
  } else {
    console.log(`${colors.yellow}⚠ Onshape API credentials not set${colors.reset}`);
    console.log(`${colors.dim}  Will display basic selection info only${colors.reset}\n`);
  }

  // Get port from command line args or environment
  const args = process.argv.slice(2);
  const portArg = args.find(arg => arg.startsWith('--port='));
  const port = portArg
    ? parseInt(portArg.split('=')[1], 10)
    : parseInt(process.env.PORT || '3000', 10);

  // Start the server
  startSelectionServer(port);
}

// Run if executed directly
if (require.main === module) {
  main();
}
