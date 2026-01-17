import { Router } from 'express'
import { storefrontFetch } from '../shopify/client.js'
import {
  SEARCH_PRODUCTS,
  GET_PRODUCT_BY_HANDLE,
  GET_PRODUCT_BY_ID,
  GET_PRODUCT_MEDIA,
  GET_PRODUCT_MEDIA_BY_ID,
} from '../shopify/queries.js'
import type { Product, Media } from '../types.js'

const router = Router()

// Helper to check if ID is a Shopify GID
function isShopifyGid(id: string): boolean {
  return id.startsWith('gid://shopify/')
}

// Transform GraphQL edges to array
function transformEdges<T>(edges: { node: T }[]): T[] {
  return edges.map(edge => edge.node)
}

// Transform product from GraphQL response
function transformProduct(product: any): Product {
  return {
    id: product.id,
    title: product.title,
    handle: product.handle,
    description: product.description,
    descriptionHtml: product.descriptionHtml,
    vendor: product.vendor,
    productType: product.productType,
    tags: product.tags,
    availableForSale: product.availableForSale,
    createdAt: product.createdAt,
    updatedAt: product.updatedAt,
    priceRange: product.priceRange,
    featuredImage: product.featuredImage,
    options: product.options,
    images: product.images ? transformEdges(product.images.edges) : [],
    variants: product.variants ? transformEdges(product.variants.edges) : [],
  }
}

// GET /api/products/search?query=shirt&first=10
router.get('/search', async (req, res) => {
  try {
    const query = req.query.query as string
    const first = parseInt(req.query.first as string) || 10

    if (!query) {
      return res.status(400).json({
        data: null,
        loading: false,
        error: { message: 'Query parameter is required' },
      })
    }

    const data = await storefrontFetch<{ products: { edges: { node: any }[] } }>(
      SEARCH_PRODUCTS,
      { query, first }
    )

    const products = transformEdges(data.products.edges).map(transformProduct)

    res.json({
      products,
      loading: false,
      error: null,
    })
  } catch (error) {
    console.error('Search error:', error)
    res.status(500).json({
      products: null,
      loading: false,
      error: { message: error instanceof Error ? error.message : 'Search failed' },
    })
  }
})

// GET /api/products/:idOrHandle
router.get('/:idOrHandle', async (req, res) => {
  try {
    const { idOrHandle } = req.params

    let data: { product: any }

    if (isShopifyGid(idOrHandle)) {
      // Fetch by ID
      data = await storefrontFetch<{ product: any }>(GET_PRODUCT_BY_ID, {
        id: idOrHandle,
      })
    } else {
      // Fetch by handle
      data = await storefrontFetch<{ product: any }>(GET_PRODUCT_BY_HANDLE, {
        handle: idOrHandle,
      })
    }

    if (!data.product) {
      return res.status(404).json({
        product: null,
        loading: false,
        error: { message: 'Product not found' },
      })
    }

    const product = transformProduct(data.product)

    res.json({
      product,
      loading: false,
      error: null,
    })
  } catch (error) {
    console.error('Get product error:', error)
    res.status(500).json({
      product: null,
      loading: false,
      error: { message: error instanceof Error ? error.message : 'Failed to fetch product' },
    })
  }
})

// GET /api/products/:idOrHandle/media
router.get('/:idOrHandle/media', async (req, res) => {
  try {
    const { idOrHandle } = req.params

    let data: { product: { id: string; title: string; media: { edges: { node: any }[] } } }

    if (isShopifyGid(idOrHandle)) {
      data = await storefrontFetch(GET_PRODUCT_MEDIA_BY_ID, { id: idOrHandle })
    } else {
      data = await storefrontFetch(GET_PRODUCT_MEDIA, { handle: idOrHandle })
    }

    if (!data.product) {
      return res.status(404).json({
        media: null,
        loading: false,
        error: { message: 'Product not found' },
      })
    }

    const media: Media[] = transformEdges(data.product.media.edges)

    res.json({
      productId: data.product.id,
      productTitle: data.product.title,
      media,
      loading: false,
      error: null,
    })
  } catch (error) {
    console.error('Get media error:', error)
    res.status(500).json({
      media: null,
      loading: false,
      error: { message: error instanceof Error ? error.message : 'Failed to fetch media' },
    })
  }
})

export { router as productRoutes }
