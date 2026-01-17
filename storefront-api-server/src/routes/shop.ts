import { Router } from 'express'
import { storefrontFetch } from '../shopify/client.js'
import { GET_SHOP } from '../shopify/queries.js'
import type { Shop } from '../types.js'

const router = Router()

// GET /api/shop
router.get('/', async (_req, res) => {
  try {
    const data = await storefrontFetch<{ shop: Shop }>(GET_SHOP)

    res.json({
      shop: data.shop,
      loading: false,
      error: null,
    })
  } catch (error) {
    console.error('Get shop error:', error)
    res.status(500).json({
      shop: null,
      loading: false,
      error: { message: error instanceof Error ? error.message : 'Failed to fetch shop info' },
    })
  }
})

export { router as shopRoutes }
