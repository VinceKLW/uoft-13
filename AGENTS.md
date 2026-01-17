# Agent Instructions

This document helps AI agents understand and work on this project effectively.

> **IMPORTANT:** The root `README.md` is the **source of truth** for project setup, architecture, and service status. Always keep it updated when making changes. This file (AGENTS.md) provides agent-specific guidance.

## Source of Truth

- **`README.md`** - Setup instructions, architecture, service status, endpoints
- **`AGENTS.md`** - How agents should work on this project (this file)
- **`CLAUDE.md`** - Claude-specific quick reference

When you make changes:
1. Update `README.md` with any new services, endpoints, or setup steps
2. Update this file if agent workflows change
3. Update `CLAUDE.md` if quick reference info changes

## Project Overview

**Name:** UofT-13 Hackathon Project  
**Purpose:** VR shopping experience powered by Shopify product data  
**Architecture:** REST API server that wraps Shopify's Storefront GraphQL API

## Services

### 1. Storefront API Server (`/storefront-api-server`)

A Node.js/Express server that provides REST endpoints for Shopify product and shop data.

**Tech Stack:**
- Express.js (REST API)
- TypeScript
- Shopify Storefront GraphQL API

**Data Source:**  
Currently connected to Shopify's Hydrogen demo store (`hydrogen-preview.myshopify.com`). Configurable via environment variables.

**Endpoints:**

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products/search?query=&first=` | Search products |
| GET | `/api/products/:idOrHandle` | Get product by ID or handle |
| GET | `/api/products/:idOrHandle/media` | Get product media (images, 3D models) |
| GET | `/api/shop` | Get shop information |
| GET | `/health` | Health check |

**Key Files:**
- `src/index.ts` - Express server entry point
- `src/config.ts` - Environment configuration
- `src/shopify/client.ts` - GraphQL client for Storefront API
- `src/shopify/queries.ts` - GraphQL queries
- `src/routes/products.ts` - Product endpoints
- `src/routes/shop.ts` - Shop endpoint
- `src/types.ts` - TypeScript interfaces

**Running the Server:**
```bash
cd storefront-api-server
npm install
npm run dev  # Runs on http://localhost:3000
```

**Environment Variables:**
```
SHOPIFY_STORE_DOMAIN=hydrogen-preview.myshopify.com
SHOPIFY_STOREFRONT_ACCESS_TOKEN=3b580e70970c4528da70c98e097c2fa0
PORT=3000
```

### 2. VR Application (Planned)

Consumer of the Storefront API Server. Will display products in a VR environment.

**Status:** Not yet implemented

**Planned Features:**
- Load product data from API
- Display product images and 3D models (GLB/USDZ)
- Show product descriptions and pricing
- Interactive product browsing in VR

## Project Structure

```
uoft-13/
├── AGENTS.md                    # This file - agent instructions
├── CLAUDE.md                    # Claude-specific instructions
├── storefront-api-server/       # REST API wrapping Shopify Storefront API
│   ├── src/
│   │   ├── index.ts            # Server entry
│   │   ├── config.ts           # Configuration
│   │   ├── types.ts            # TypeScript types
│   │   ├── shopify/
│   │   │   ├── client.ts       # GraphQL client
│   │   │   └── queries.ts      # GraphQL queries
│   │   └── routes/
│   │       ├── products.ts     # Product endpoints
│   │       └── shop.ts         # Shop endpoint
│   ├── package.json
│   └── tsconfig.json
└── [vr-app]/                    # VR application (to be created)
```

## Working on This Project

### Adding New API Endpoints

1. Add GraphQL query to `storefront-api-server/src/shopify/queries.ts`
2. Create route handler in `storefront-api-server/src/routes/`
3. Register route in `storefront-api-server/src/index.ts`
4. Add types to `storefront-api-server/src/types.ts` if needed

### Changing the Shopify Store

1. Create `.env` file in `storefront-api-server/`
2. Set `SHOPIFY_STORE_DOMAIN` and `SHOPIFY_STOREFRONT_ACCESS_TOKEN`
3. Restart the server

### Testing Endpoints

```bash
# Health check
curl http://localhost:3000/health

# Search products
curl "http://localhost:3000/api/products/search?query=*&first=10"

# Get product
curl http://localhost:3000/api/products/v2-snowboard

# Get product media (includes 3D models)
curl http://localhost:3000/api/products/v2-snowboard/media

# Get shop info
curl http://localhost:3000/api/shop
```

## Important Notes

- The Storefront API returns 3D models (GLB/USDZ) for some products - useful for VR
- Response format includes `loading: false, error: null` to match Shop Minis SDK hook patterns
- No authentication required for the REST API endpoints
- Use ngrok to expose locally for external access: `ngrok http 3000`

## Future Plans

- [ ] Build VR application to consume the API
- [ ] Add cart/checkout functionality
- [ ] Support multiple Shopify stores
- [ ] Add caching layer for performance

## Maintenance Rules

1. **Always update README.md** when:
   - Adding new services
   - Adding new endpoints
   - Changing setup steps
   - Changing ports or configuration

2. **Keep documentation in sync:**
   - README.md = source of truth
   - AGENTS.md = agent workflows
   - CLAUDE.md = quick reference

3. **Service status tracking:**
   - ✅ = Running/Complete
   - ⏳ = In Progress/Planned
   - ❌ = Deprecated/Removed
