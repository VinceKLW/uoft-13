// GraphQL queries for Shopify Storefront API

export const PRODUCT_FIELDS = `
  fragment ProductFields on Product {
    id
    title
    handle
    description
    descriptionHtml
    vendor
    productType
    tags
    availableForSale
    createdAt
    updatedAt
    priceRange {
      minVariantPrice {
        amount
        currencyCode
      }
      maxVariantPrice {
        amount
        currencyCode
      }
    }
    featuredImage {
      url
      altText
      width
      height
    }
    options {
      name
      values
    }
  }
`

export const SEARCH_PRODUCTS = `
  ${PRODUCT_FIELDS}
  query SearchProducts($query: String!, $first: Int!) {
    products(first: $first, query: $query) {
      edges {
        node {
          ...ProductFields
          images(first: 5) {
            edges {
              node {
                url
                altText
                width
                height
              }
            }
          }
          variants(first: 10) {
            edges {
              node {
                id
                title
                availableForSale
                price {
                  amount
                  currencyCode
                }
                selectedOptions {
                  name
                  value
                }
              }
            }
          }
        }
      }
    }
  }
`

export const GET_PRODUCT_BY_HANDLE = `
  ${PRODUCT_FIELDS}
  query GetProductByHandle($handle: String!) {
    product(handle: $handle) {
      ...ProductFields
      images(first: 20) {
        edges {
          node {
            url
            altText
            width
            height
          }
        }
      }
      variants(first: 100) {
        edges {
          node {
            id
            title
            availableForSale
            price {
              amount
              currencyCode
            }
            selectedOptions {
              name
              value
            }
            image {
              url
              altText
              width
              height
            }
          }
        }
      }
    }
  }
`

export const GET_PRODUCT_BY_ID = `
  ${PRODUCT_FIELDS}
  query GetProductById($id: ID!) {
    product(id: $id) {
      ...ProductFields
      images(first: 20) {
        edges {
          node {
            url
            altText
            width
            height
          }
        }
      }
      variants(first: 100) {
        edges {
          node {
            id
            title
            availableForSale
            price {
              amount
              currencyCode
            }
            selectedOptions {
              name
              value
            }
            image {
              url
              altText
              width
              height
            }
          }
        }
      }
    }
  }
`

export const GET_PRODUCT_MEDIA = `
  query GetProductMedia($handle: String!) {
    product(handle: $handle) {
      id
      title
      media(first: 20) {
        edges {
          node {
            mediaContentType
            ... on MediaImage {
              image {
                url
                altText
                width
                height
              }
            }
            ... on Video {
              sources {
                url
                mimeType
              }
            }
            ... on ExternalVideo {
              embedUrl
              host
            }
            ... on Model3d {
              sources {
                url
                mimeType
              }
            }
          }
        }
      }
    }
  }
`

export const GET_PRODUCT_MEDIA_BY_ID = `
  query GetProductMediaById($id: ID!) {
    product(id: $id) {
      id
      title
      media(first: 20) {
        edges {
          node {
            mediaContentType
            ... on MediaImage {
              image {
                url
                altText
                width
                height
              }
            }
            ... on Video {
              sources {
                url
                mimeType
              }
            }
            ... on ExternalVideo {
              embedUrl
              host
            }
            ... on Model3d {
              sources {
                url
                mimeType
              }
            }
          }
        }
      }
    }
  }
`

export const GET_SHOP = `
  query GetShop {
    shop {
      id
      name
      description
      primaryDomain {
        url
        host
      }
      brand {
        logo {
          image {
            url
            altText
          }
        }
        colors {
          primary {
            background
            foreground
          }
        }
      }
      paymentSettings {
        currencyCode
        acceptedCardBrands
      }
    }
  }
`
