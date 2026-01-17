# Claude Instructions

Project-specific instructions for Claude AI assistant.

> **IMPORTANT:** The root `README.md` is the **source of truth**. Always update it when making changes to services, endpoints, or setup.

## Quick Context

This is a hackathon project building a **VR shopping experience** powered by Shopify data.

**Current State:**
- ✅ REST API server wrapping Shopify Storefront API (complete)
- ⏳ VR application (not started)

## Key Commands

```bash
# Start the API server
cd storefront-api-server && npm run dev

# Test endpoints
curl "http://localhost:3000/api/products/search?query=*&first=5"
curl http://localhost:3000/api/products/v2-snowboard/media
curl http://localhost:3000/api/shop

# Expose via ngrok
ngrok http 3000
```

## Code Patterns

### API Response Format
All endpoints return this shape (matching Shop Minis SDK hooks):
```typescript
{
  data: T,           // or products/product/shop/media
  loading: false,
  error: null        // or { message: string }
}
```

### Adding New Endpoints
1. Query in `src/shopify/queries.ts`
2. Route in `src/routes/*.ts`
3. Register in `src/index.ts`

### GraphQL Client Usage
```typescript
import { storefrontFetch } from '../shopify/client.js'
import { SOME_QUERY } from '../shopify/queries.js'

const data = await storefrontFetch<ResponseType>(SOME_QUERY, { variables })
```

## Data Source

Connected to Shopify's **Hydrogen demo store** (snowboards, etc.)
- Store: `hydrogen-preview.myshopify.com`
- Has real product data including 3D models (GLB/USDZ)

## What NOT to Do

- Don't add user authentication (not needed for public product data)
- Don't modify the Storefront API token (it's a public demo token)
- Don't add a frontend framework to storefront-api-server (keep it API-only)

## What to Update

When making changes, **always update the root README.md first** (source of truth):
1. `README.md` - Setup, architecture, endpoints, service status
2. `AGENTS.md` - If agent workflows change  
3. `CLAUDE.md` - If quick reference info changes

**Before finishing any task, ask yourself:** "Did I update README.md?"

## VR App Guidance (When Building)

The VR app should:
- Fetch product data from `http://localhost:3000/api/products/search`
- Load 3D models from the `media` endpoint (GLB format for Three.js/Unity)
- Display product info: title, description, price, images
- Keep API server running separately

Recommended VR tech:
- WebXR: React Three Fiber + Remix (if web-based)
- Native: Unity or Unreal consuming REST API
