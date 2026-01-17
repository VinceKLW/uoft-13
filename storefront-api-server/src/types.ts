// Types matching Shop Minis SDK hook response shapes

export interface Money {
  amount: string
  currencyCode: string
}

export interface Image {
  url: string
  altText: string | null
  width?: number
  height?: number
}

export interface PriceRange {
  minVariantPrice: Money
  maxVariantPrice: Money
}

export interface ProductVariant {
  id: string
  title: string
  price: Money
  availableForSale: boolean
  selectedOptions: { name: string; value: string }[]
  image?: Image
}

export interface Product {
  id: string
  title: string
  handle: string
  description: string
  descriptionHtml?: string
  vendor: string
  productType: string
  tags: string[]
  priceRange: PriceRange
  featuredImage: Image | null
  images: Image[]
  variants: ProductVariant[]
  options: { name: string; values: string[] }[]
  availableForSale: boolean
  createdAt: string
  updatedAt: string
}

export interface MediaImage {
  mediaContentType: 'IMAGE'
  image: Image
}

export interface MediaVideo {
  mediaContentType: 'VIDEO'
  sources: { url: string; mimeType: string }[]
}

export interface MediaExternalVideo {
  mediaContentType: 'EXTERNAL_VIDEO'
  embedUrl: string
  host: string
}

export interface MediaModel3d {
  mediaContentType: 'MODEL_3D'
  sources: { url: string; mimeType: string }[]
}

export type Media = MediaImage | MediaVideo | MediaExternalVideo | MediaModel3d

export interface Shop {
  id: string
  name: string
  description: string | null
  primaryDomain: {
    url: string
    host: string
  }
  brand?: {
    logo?: {
      image: Image
    }
    colors?: {
      primary: { background: string; foreground: string }[]
    }
  }
  paymentSettings?: {
    currencyCode: string
    acceptedCardBrands: string[]
  }
}

// API Response wrappers (matching hook return shapes)
export interface ApiResponse<T> {
  data: T
  loading: false
  error: null
}

export interface ApiErrorResponse {
  data: null
  loading: false
  error: { message: string; code?: string }
}

export type ApiResult<T> = ApiResponse<T> | ApiErrorResponse
