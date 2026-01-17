# VR Shop - Unity Setup

A VR-ready shopping room environment that displays Shopify store branding.

## Quick Setup

1. Open Unity and let it import the new assets
2. Go to menu: **VRShop > Quick Setup (Create Everything)**
3. Open the scene: `Assets/VRShop/Scenes/VRShopScene.unity`
4. Make sure the API server is running: `cd storefront-api-server && npm run dev`
5. Press Play to test

## What Gets Created

The Quick Setup creates:

- **Materials**: Wall, floor, and logo materials (URP compatible)
- **ShopRoom Prefab**: A 10x4x10 meter enclosed room with:
  - 4 walls, floor, and ceiling
  - Brand logo display on the front wall (loaded from API)
  - Shopify logo display on the back wall (static texture)
- **VRShopScene**: Complete scene with camera and lighting

## Manual Setup (Alternative)

If you prefer to create things step by step:

1. **VRShop > Build Shop Room** - Opens the builder window
2. Click "Create Materials"
3. Click "Create Room Prefab"
4. Click "Create Scene"

## Components

### StationaryCameraController
- Stationary first-person camera with mouse look
- VR-ready: set `vrModeEnabled = true` to disable mouse input when VR headset takes over
- Configurable sensitivity and clamp angles

### ShopDataManager
- Fetches shop data from `http://localhost:3000/api/shop`
- Downloads and exposes brand logo as Texture2D
- Events: `OnShopDataLoaded`, `OnBrandLogoLoaded`, `OnError`

### LogoDisplay
- Applies textures to wall quads
- Supports static textures (Shopify logo) or dynamic loading from ShopDataManager

## VR Support (Future)

To add Meta Quest 2 support later:

1. Install packages via Package Manager:
   - XR Plugin Management
   - Oculus XR Plugin
   - XR Interaction Toolkit

2. Enable Oculus in XR Plugin Management settings

3. Replace the camera with an XR Origin

4. The `StationaryCameraController` will automatically disable mouse input when `vrModeEnabled` is set

## API Requirements

The storefront API server must be running at `http://localhost:3000`:

```bash
cd storefront-api-server
npm install
npm run dev
```

The `/api/shop` endpoint provides:
- Shop name and description
- Brand logo URL (if configured in Shopify)
- Payment settings

## Troubleshooting

**Logo not loading**: 
- Check console for API errors
- Ensure API server is running
- The demo store may not have a brand logo configured

**Pink/magenta materials**:
- Materials are created for URP
- Make sure your project uses Universal Render Pipeline

**Mouse look not working**:
- Click in the Game view to focus it
- Check that `lockCursor` is enabled on the camera
