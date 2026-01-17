import dotenv from 'dotenv'

dotenv.config()

export const config = {
  // Shopify Storefront API - defaults to Hydrogen demo store
  shopifyStoreDomain: process.env.SHOPIFY_STORE_DOMAIN || 'hydrogen-preview.myshopify.com',
  shopifyStorefrontAccessToken: process.env.SHOPIFY_STOREFRONT_ACCESS_TOKEN || '3b580e70970c4528da70c98e097c2fa0',
  
  // Server
  port: parseInt(process.env.PORT || '3000', 10),
  
  // Derived
  get storefrontApiUrl() {
    return `https://${this.shopifyStoreDomain}/api/2024-01/graphql.json`
  }
}
