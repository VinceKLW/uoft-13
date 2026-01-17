import { config } from '../config.js'

interface GraphQLResponse<T> {
  data?: T
  errors?: { message: string; locations?: { line: number; column: number }[] }[]
}

export async function storefrontFetch<T>(
  query: string,
  variables?: Record<string, unknown>
): Promise<T> {
  const response = await fetch(config.storefrontApiUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Shopify-Storefront-Access-Token': config.shopifyStorefrontAccessToken,
    },
    body: JSON.stringify({ query, variables }),
  })

  if (!response.ok) {
    throw new Error(`Storefront API error: ${response.status} ${response.statusText}`)
  }

  const json: GraphQLResponse<T> = await response.json()

  if (json.errors && json.errors.length > 0) {
    throw new Error(`GraphQL error: ${json.errors.map(e => e.message).join(', ')}`)
  }

  if (!json.data) {
    throw new Error('No data returned from Storefront API')
  }

  return json.data
}
