# Face Selection Guide

This guide explains how to use the Onshape client messaging system to select faces in the Onshape UI and display their information in the CLI.

## Overview

The face selection system consists of three components:

1. **Client App** (`client/index.html`) - Runs in an iframe within Onshape and uses `window.postMessage` to communicate with the Onshape UI
2. **Selection Server** (`src/selection-server.ts`) - Local Node.js server that receives selection events and displays face information
3. **CLI Tool** (`src/cli.ts`) - Command-line interface to start the server

## Architecture

```
┌─────────────────────────────────────────────────┐
│         Onshape Web UI (Parent Window)          │
│  ┌───────────────────────────────────────────┐  │
│  │   Your App (iframe)                       │  │
│  │   client/index.html                       │  │
│  │                                           │  │
│  │   1. User clicks "Request Selection"      │  │
│  │   2. postMessage to parent window         │  │
│  │   3. User selects face in 3D viewer       │  │
│  │   4. Onshape sends selectionEvent back    │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
                      │
                      │ HTTP POST
                      ▼
        ┌──────────────────────────┐
        │   Local Server (CLI)     │
        │   localhost:3000         │
        │                          │
        │   Displays face info     │
        │   in terminal            │
        └──────────────────────────┘
```

## Setup Instructions

### Step 1: Install Dependencies

```bash
npm install
```

### Step 2: Start the Selection Server

In a terminal window, run:

```bash
npm run selection-server
```

Or:

```bash
npm start
```

This starts a local server on `http://localhost:3000` that listens for face selection events.

### Step 3: Serve the Client App

The client app needs to be publicly accessible for Onshape to load it in an iframe. You have several options:

#### Option A: Use ngrok (Recommended for testing)

```bash
# In a new terminal
npm run serve-client

# In another terminal, expose it with ngrok
npx ngrok http 8080
```

Copy the ngrok HTTPS URL (e.g., `https://abc123.ngrok.io`)

#### Option B: Deploy to a public server

Deploy the `client/` directory to:
- GitHub Pages
- Netlify
- Vercel
- Any web host

### Step 4: Create an Onshape Extension

1. Go to https://dev-portal.onshape.com
2. Sign in with your Onshape account
3. Click "OAuth applications" → "Create new OAuth application"
4. Fill in the details:
   - **Name**: Face Selection Tool
   - **Format**: OAuth 2.0
   - **OAuth redirect URLs**: (not needed for this app)
   - **Extension locations**: Select "Element tab"
   - **Extension URL**: Your public client app URL (e.g., `https://abc123.ngrok.io/index.html`)
   - **Extension parameters**: Add these query parameters:
     ```
     documentId={$documentId}&workspaceId={$workspaceId}&elementId={$elementId}&server={$server}
     ```
   - **Permissions**: Check "Read documents"

5. Save the extension

### Step 5: Use in Onshape

1. Open any Onshape document
2. Click the "+" icon to add a tab
3. Find your "Face Selection Tool" extension
4. The extension will load in a new tab
5. Click "Request Face Selection"
6. Click on any face in the 3D viewer
7. Face information will appear in your terminal where the selection server is running!

## Output Example

When you select a face, you'll see output like this in the CLI:

```
================================================================================
FACE SELECTION RECEIVED
================================================================================

Document Info:
  Document ID: 5f7d8e9a0b1c2d3e4f5a6b7c
  Workspace ID: 1a2b3c4d5e6f7g8h9i0j1k2l
  Element ID: 9z8y7x6w5v4u3t2s1r0q9p8o

Selection Info:
  Face ID: JHD
  Timestamp: 2025-11-19T17:45:23.456Z

Raw Selection Data:
{
  "faceId": "JHD",
  "partId": "abc123",
  "entityType": "FACE",
  ...
}

================================================================================
```

## Client Messaging API

The client app uses Onshape's postMessage API to communicate:

### Requesting a Selection

```javascript
const message = {
  messagetype: 'select',
  documentId: documentId,
  workspaceId: workspaceId,
  elementId: elementId,
  selectionFilter: {
    type: 'face'  // Request face selection only
  }
};

window.parent.postMessage(message, 'https://cad.onshape.com');
```

### Receiving Selection Events

```javascript
window.addEventListener('message', function(event) {
  if (event.data.messagetype === 'selectionEvent') {
    const faceId = event.data.selection[0].faceId;
    // Process the selection...
  }
});
```

### Message Types

- `select` - Request user to make a selection
- `selectionEvent` - Onshape sends this when user selects something
- `keepAlive` - Prevent timeout of the app
- `applicationInit` - Notify Onshape the app is ready

## Selection Filter Options

You can filter what types of entities the user can select:

```javascript
selectionFilter: {
  type: 'face'      // Only faces
  // Other options: 'edge', 'vertex', 'body', 'part'
}
```

## Security Considerations

The client app validates messages to ensure they come from Onshape:

```javascript
if (!event.origin.includes('onshape.com')) {
  return; // Reject untrusted messages
}
```

## Advanced: Fetching Detailed Geometry

To fetch detailed face geometry (edges, curves, surface type, etc.), set your Onshape API credentials:

```bash
export ONSHAPE_ACCESS_KEY="your_access_key"
export ONSHAPE_SECRET_KEY="your_secret_key"
```

Then restart the selection server. When a face is selected, the server will automatically fetch and display:
- Surface type (plane, cylinder, sphere, etc.)
- Surface area
- Normal vector (for planar faces)
- Edge loops (outer boundary and holes)
- Curve geometry (lines, arcs, splines, etc.)

Get API keys from: https://dev-portal.onshape.com/keys

## Troubleshooting

### "No face selected" message

- Make sure you're clicking on a face in the 3D viewer, not an edge or vertex
- Try selecting a different face

### Client app doesn't load in Onshape

- Verify your client URL is publicly accessible
- Check that you added the required URL parameters in the extension settings
- Make sure the URL is HTTPS (required by Onshape)

### "Error sending to CLI" in the client app

- Verify the selection server is running on `localhost:3000`
- Check browser console for CORS errors
- Ensure the server port matches what's configured in the client

### Messages not appearing in terminal

- Check that the selection server is running
- Verify the client app can reach `http://localhost:3000/selection`
- Look for error messages in the browser console

## Next Steps

Once you have face selection working, you can extend it to:

1. **Export to SVG**: Convert the face boundary edges to SVG paths for CNC
2. **Generate Shaper Origin files**: Create `.shapertools` files from the geometry
3. **Batch process**: Select multiple faces and export them all at once
4. **Add filtering**: Only show flat faces above a certain size
5. **Generate toolpaths**: Create cutting paths with offsets and tabs

## References

- [Onshape Client Messaging Documentation](https://onshape-public.github.io/docs/app-dev/clientmessaging/)
- [Onshape Developer Portal](https://dev-portal.onshape.com)
- [Onshape API Explorer](https://cad.onshape.com/glassworks/explorer/)
- [Sample Onshape Apps](https://github.com/onshape-public)
