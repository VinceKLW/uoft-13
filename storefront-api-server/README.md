# Shop Minis Storefront API Server

A REST API server that wraps Shopify's Storefront GraphQL API, exposing endpoints that match the Shop Minis SDK hook interfaces. Use this to access real Shopify product and shop data from any application.

## Quick Start

```bash
# Install dependencies
npm install

# Start the server
npm run dev

# Server runs at http://localhost:3001
```

## Expose via ngrok

```bash
ngrok http 3001
# Get your public URL like https://abc123.ngrok.io
```

## API Endpoints

### Product Search

**Mirrors `useProductSearch` hook**

```bash
GET /api/products/search?query=shirt&first=10
```

Response:
```json
{
  "products": [
    {
      "id": "gid://shopify/Product/123",
      "title": "Classic T-Shirt",
      "handle": "classic-t-shirt",
      "description": "A comfortable cotton t-shirt",
      "vendor": "Demo Store",
      "priceRange": {
        "minVariantPrice": { "amount": "29.99", "currencyCode": "USD" }
      },
      "featuredImage": { "url": "https://...", "altText": "T-Shirt" },
      "images": [...],
      "variants": [...]
    }
  ],
  "loading": false,
  "error": null
}
```

### Get Product by ID or Handle

**Mirrors `useProducts` hook**

```bash
# By handle
GET /api/products/classic-t-shirt

# By Shopify GID
GET /api/products/gid://shopify/Product/123456789
```

Response:
```json
{
  "product": {
    "id": "gid://shopify/Product/123",
    "title": "Classic T-Shirt",
    "handle": "classic-t-shirt",
    "description": "...",
    "images": [...],
    "variants": [...],
    "options": [...]
  },
  "loading": false,
  "error": null
}
```

### Get Product Media

**Mirrors `useProductMedia` hook**

```bash
GET /api/products/classic-t-shirt/media
```

Response:
```json
{
  "productId": "gid://shopify/Product/123",
  "productTitle": "Classic T-Shirt",
  "media": [
    {
      "mediaContentType": "IMAGE",
      "image": { "url": "https://...", "altText": "Front view" }
    },
    {
      "mediaContentType": "VIDEO",
      "sources": [{ "url": "https://...", "mimeType": "video/mp4" }]
    }
  ],
  "loading": false,
  "error": null
}
```

### Get Shop Info

**Mirrors `useShop` hook**

```bash
GET /api/shop
```

Response:
```json
{
  "shop": {
    "id": "gid://shopify/Shop/123",
    "name": "Hydrogen Demo",
    "description": "A demo store",
    "primaryDomain": {
      "url": "https://hydrogen-preview.myshopify.com",
      "host": "hydrogen-preview.myshopify.com"
    }
  },
  "loading": false,
  "error": null
}
```

### Health Check

```bash
GET /health
```

## Configuration

By default, the server connects to Shopify's public Hydrogen demo store. To use your own store:

1. Create a `.env` file:
```bash
SHOPIFY_STORE_DOMAIN=your-store.myshopify.com
SHOPIFY_STOREFRONT_ACCESS_TOKEN=your-storefront-token
PORT=3001
```

2. Get your Storefront API token:
   - Go to Shopify Admin > Settings > Apps and sales channels
   - Click "Develop apps" > "Create an app"
   - Configure Storefront API scopes (products, shop)
   - Install the app and copy the Storefront API access token

## Using from External Apps

### JavaScript/TypeScript

```typescript
const API_URL = 'https://your-ngrok-url.ngrok.io'

// Search products
const searchProducts = async (query: string) => {
  const res = await fetch(`${API_URL}/api/products/search?query=${query}&first=10`)
  const data = await res.json()
  return data.products
}

// Get product details
const getProduct = async (handle: string) => {
  const res = await fetch(`${API_URL}/api/products/${handle}`)
  const data = await res.json()
  return data.product
}
```

### Python

```python
import requests

API_URL = 'https://your-ngrok-url.ngrok.io'

def search_products(query: str, limit: int = 10):
    response = requests.get(f'{API_URL}/api/products/search', params={'query': query, 'first': limit})
    return response.json()['products']

def get_product(handle: str):
    response = requests.get(f'{API_URL}/api/products/{handle}')
    return response.json()['product']
```

### cURL

```bash
# Search
curl "http://localhost:3001/api/products/search?query=shirt&first=5"

# Get product
curl "http://localhost:3001/api/products/classic-leather-jacket"

# Get media
curl "http://localhost:3001/api/products/classic-leather-jacket/media"

# Shop info
curl "http://localhost:3001/api/shop"
```

## Development

```bash
# Run with hot reload
npm run dev

# Type check
npm run typecheck

# Build for production
npm run build
npm start
```

## Project Structure

```
storefront-api-server/
├── src/
│   ├── index.ts           # Express server entry
│   ├── config.ts          # Environment configuration
│   ├── types.ts           # TypeScript interfaces
│   ├── shopify/
│   │   ├── client.ts      # GraphQL client
│   │   └── queries.ts     # GraphQL queries
│   └── routes/
│       ├── products.ts    # Product endpoints
│       └── shop.ts        # Shop endpoint
├── package.json
└── tsconfig.json
```

## License

MIT
