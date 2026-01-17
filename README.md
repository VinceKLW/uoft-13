# UofT-13 Hackathon - VR Shopping Experience

A VR shopping experience powered by real Shopify product data.

## Quick Start

### Prerequisites

- Node.js v18+
- npm

### 1. Start the API Server

```bash
cd storefront-api-server
npm install
npm run dev
```

Server runs at **http://localhost:3000**

### 2. Test the API

```bash
# Health check
curl http://localhost:3000/health

# Search all products
curl "http://localhost:3000/api/products/search?query=*&first=10"

# Get a specific product
curl http://localhost:3000/api/products/v2-snowboard

# Get product media (images + 3D models)
curl http://localhost:3000/api/products/v2-snowboard/media

# Get shop info
curl http://localhost:3000/api/shop
```

### 3. Expose Externally (Optional)

```bash
ngrok http 3000
```

Use the ngrok URL to access from other devices/apps.

---

## Architecture

```
┌─────────────────────┐     ┌──────────────────────┐     ┌─────────────────────┐
│                     │     │                      │     │                     │
│  VR App (Planned)   │────▶│  Storefront API      │────▶│  Shopify Storefront │
│                     │     │  Server (Express)    │     │  GraphQL API        │
│                     │     │                      │     │                     │
└─────────────────────┘     └──────────────────────┘     └─────────────────────┘
                                 localhost:3000              hydrogen-preview
                                                            .myshopify.com
```

---

## Services

### Storefront API Server

**Location:** `/storefront-api-server`  
**Status:** ✅ Running  
**Port:** 3000

REST API that wraps Shopify's Storefront GraphQL API.

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/api/products/search` | GET | Search products (`?query=&first=`) |
| `/api/products/:id` | GET | Get product by ID or handle |
| `/api/products/:id/media` | GET | Get product images & 3D models |
| `/api/shop` | GET | Get shop information |

**Data Source:** Shopify Hydrogen demo store (snowboards, with 3D models)

### VR Application

**Location:** TBD  
**Status:** ⏳ Not started

Will consume the API to display products in VR.

---

## Project Structure

```
uoft-13/
├── README.md                    # This file - setup & overview
├── AGENTS.md                    # AI agent instructions
├── CLAUDE.md                    # Claude-specific instructions
└── storefront-api-server/       # REST API server
    ├── src/
    │   ├── index.ts             # Express server
    │   ├── config.ts            # Environment config
    │   ├── types.ts             # TypeScript types
    │   ├── shopify/
    │   │   ├── client.ts        # GraphQL client
    │   │   └── queries.ts       # GraphQL queries
    │   └── routes/
    │       ├── products.ts      # Product endpoints
    │       └── shop.ts          # Shop endpoint
    ├── package.json
    └── README.md                # API-specific docs
```

---

## Configuration

### Using a Different Shopify Store

Create `.env` in `storefront-api-server/`:

```bash
SHOPIFY_STORE_DOMAIN=your-store.myshopify.com
SHOPIFY_STOREFRONT_ACCESS_TOKEN=your-token
PORT=3000
```

Get a Storefront API token from Shopify Admin > Settings > Apps > Develop apps.

---

## Development

### Adding New API Endpoints

1. Add GraphQL query → `src/shopify/queries.ts`
2. Create route handler → `src/routes/`
3. Register route → `src/index.ts`
4. Add types if needed → `src/types.ts`
5. **Update this README**

### Running Tests

```bash
cd storefront-api-server
npm run typecheck  # Type checking
```

---

## API Response Format

All endpoints return consistent format:

```json
{
  "products": [...],
  "loading": false,
  "error": null
}
```

On error:
```json
{
  "products": null,
  "loading": false,
  "error": { "message": "Error description" }
}
```

---

## Product Data Available

The Hydrogen demo store includes:

- **Products:** Snowboards with variants (size, color, material)
- **Images:** High-res product photos
- **3D Models:** GLB and USDZ files (great for VR!)
- **Pricing:** USD prices with variants
- **Descriptions:** Full product descriptions

Example 3D model response:
```json
{
  "mediaContentType": "MODEL_3D",
  "sources": [
    { "url": "https://...snowboard.glb", "mimeType": "model/gltf-binary" },
    { "url": "https://...snowboard.usdz", "mimeType": "model/vnd.usdz+zip" }
  ]
}
```

---

## Troubleshooting

**Server won't start:**
```bash
cd storefront-api-server
rm -rf node_modules package-lock.json
npm install
npm run dev
```

**Port already in use:**
```bash
PORT=3001 npm run dev
```

**API returns empty products:**
- Check your search query
- Try `query=*` to get all products

---

## Contributing

1. Update this README when adding features
2. Keep AGENTS.md and CLAUDE.md in sync
3. Document new endpoints in the table above

---

## License

MIT
