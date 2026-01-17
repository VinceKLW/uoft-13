import express from 'express'
import cors from 'cors'
import { config } from './config.js'
import { productRoutes } from './routes/products.js'
import { shopRoutes } from './routes/shop.js'

const app = express()

// Middleware
app.use(cors())
app.use(express.json())

// Health check
app.get('/health', (_req, res) => {
  res.json({ status: 'ok', store: config.shopifyStoreDomain })
})

// API Routes
app.use('/api/products', productRoutes)
app.use('/api/shop', shopRoutes)

// 404 handler
app.use((_req, res) => {
  res.status(404).json({
    data: null,
    loading: false,
    error: { message: 'Endpoint not found' }
  })
})

// Error handler
app.use((err: Error, _req: express.Request, res: express.Response, _next: express.NextFunction) => {
  console.error('Server error:', err)
  res.status(500).json({
    data: null,
    loading: false,
    error: { message: err.message || 'Internal server error' }
  })
})

app.listen(config.port, () => {
  console.log(`\n🚀 Storefront API Server running at http://localhost:${config.port}`)
  console.log(`📦 Connected to store: ${config.shopifyStoreDomain}`)
  console.log(`\nEndpoints:`)
  console.log(`  GET  /api/products/search?query=shirt&first=10`)
  console.log(`  GET  /api/products/:idOrHandle`)
  console.log(`  GET  /api/products/:idOrHandle/media`)
  console.log(`  GET  /api/shop`)
  console.log(`  GET  /health`)
  console.log(`\nExpose via ngrok: ngrok http ${config.port}\n`)
})
