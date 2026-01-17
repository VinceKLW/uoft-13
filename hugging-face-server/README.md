# Hugging Face 3D Generation Server

Flask API wrapper for converting images to 3D models using Tencent's Hunyuan3D-2.1 model.

## Quick Start

```bash
# Activate virtual environment
.\venv\Scripts\activate  # Windows
source venv/bin/activate  # Linux/Mac

# Install dependencies
pip install -r requirements.txt

# Run server
python app.py
```

Server runs on `http://localhost:5000`

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | API documentation |
| GET | `/health` | Health check |
| POST | `/api/generate` | Generate 3D from uploaded image |
| POST | `/api/generate/url` | Generate 3D from image URL |
| GET | `/api/download/<job_id>/<format>` | Download model (glb/obj) |
| GET | `/api/models` | List generated models |

## Usage Examples

### Generate from uploaded image

```bash
curl -X POST http://localhost:5000/api/generate \
  -F "file=@product_image.jpg" \
  -F "steps=30" \
  -F "output_format=glb"
```

### Generate from URL

```bash
curl -X POST http://localhost:5000/api/generate/url \
  -H "Content-Type: application/json" \
  -d '{"image_url": "https://example.com/product.jpg"}'
```

### Download generated model

```bash
curl -O http://localhost:5000/api/download/<job_id>/glb
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | 5000 | Server port |
| `HF_TOKEN` | (built-in) | Hugging Face API token |
| `FLASK_DEBUG` | false | Enable debug mode |

## Integration with VR Shop

This server is designed to generate 3D product models for the VR shopping experience:

1. Product image → Upload to `/api/generate`
2. Get back GLB/OBJ 3D model
3. Import into Unity VR scene

## Notes

- Generation takes 30-60 seconds depending on server load
- The Hugging Face Space may be rate-limited during high traffic
- GLB format is recommended for Unity (smaller file size)

