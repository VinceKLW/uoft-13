"""
Flask wrapper for Hugging Face 3D Model Generation API
Converts images to 3D models using Hunyuan3D-2.1
"""

import os
import uuid
import tempfile
from flask import Flask, request, jsonify, send_file
from flask_cors import CORS
from werkzeug.utils import secure_filename
from gradio_client import Client, handle_file
import trimesh

app = Flask(__name__)
CORS(app)

# Configuration
HF_TOKEN = os.getenv("HF_TOKEN")  # Set via environment variable
UPLOAD_FOLDER = os.path.join(os.path.dirname(__file__), "uploads")
OUTPUT_FOLDER = os.path.join(os.path.dirname(__file__), "output")
ALLOWED_EXTENSIONS = {"png", "jpg", "jpeg", "webp"}

os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(OUTPUT_FOLDER, exist_ok=True)

app.config["UPLOAD_FOLDER"] = UPLOAD_FOLDER
app.config["MAX_CONTENT_LENGTH"] = 16 * 1024 * 1024  # 16MB max


def allowed_file(filename):
    return "." in filename and filename.rsplit(".", 1)[1].lower() in ALLOWED_EXTENSIONS


def fetch_3d_from_image(image_path: str, params: dict = None) -> str:
    """Generate 3D model from image using Hugging Face Hunyuan3D-2.1"""
    params = params or {}
    
    client = Client("tencent/Hunyuan3D-2.1", hf_token=HF_TOKEN)
    result = client.predict(
        image=handle_file(image_path),
        mv_image_front=None,
        mv_image_back=None,
        mv_image_left=None,
        mv_image_right=None,
        steps=params.get("steps", 30),
        guidance_scale=params.get("guidance_scale", 5),
        seed=params.get("seed", 1234),
        octree_resolution=params.get("octree_resolution", 256),
        check_box_rembg=params.get("remove_background", True),
        num_chunks=params.get("num_chunks", 8000),
        randomize_seed=params.get("randomize_seed", True),
        api_name="/generation_all"
    )
    
    return result[0]  # GLB path


def convert_glb_to_obj(glb_path: str, output_dir: str) -> str:
    """Convert GLB to OBJ format for Unity compatibility"""
    base_name = os.path.splitext(os.path.basename(glb_path))[0]
    file_output_dir = os.path.join(output_dir, base_name)
    os.makedirs(file_output_dir, exist_ok=True)
    
    scene = trimesh.load(glb_path, force="scene")
    obj_path = os.path.join(file_output_dir, base_name + ".obj")
    scene.export(obj_path)
    
    return obj_path


@app.route("/health", methods=["GET"])
def health_check():
    """Health check endpoint"""
    return jsonify({
        "status": "healthy",
        "service": "hugging-face-3d-server",
        "model": "tencent/Hunyuan3D-2.1"
    })


@app.route("/api/generate", methods=["POST"])
def generate_3d_model():
    """
    Generate a 3D model from an uploaded image
    
    Request:
        - file: Image file (PNG, JPG, JPEG, WEBP)
        - steps: (optional) Number of generation steps (default: 30)
        - guidance_scale: (optional) Guidance scale (default: 5)
        - seed: (optional) Random seed (default: 1234)
        - remove_background: (optional) Remove background (default: true)
        - output_format: (optional) "glb" or "obj" (default: "glb")
    
    Response:
        - job_id: Unique identifier for the generation job
        - status: "completed"
        - glb_url: URL to download the GLB file
        - obj_url: URL to download the OBJ file (if format is "obj")
    """
    
    if "file" not in request.files:
        return jsonify({"error": "No file provided"}), 400
    
    file = request.files["file"]
    
    if file.filename == "":
        return jsonify({"error": "No file selected"}), 400
    
    if not allowed_file(file.filename):
        return jsonify({"error": f"File type not allowed. Allowed: {ALLOWED_EXTENSIONS}"}), 400
    
    # Generate unique job ID
    job_id = str(uuid.uuid4())
    
    # Save uploaded file
    filename = secure_filename(file.filename)
    unique_filename = f"{job_id}_{filename}"
    image_path = os.path.join(app.config["UPLOAD_FOLDER"], unique_filename)
    file.save(image_path)
    
    try:
        # Parse parameters
        params = {
            "steps": int(request.form.get("steps", 30)),
            "guidance_scale": float(request.form.get("guidance_scale", 5)),
            "seed": int(request.form.get("seed", 1234)),
            "remove_background": request.form.get("remove_background", "true").lower() == "true",
            "randomize_seed": request.form.get("randomize_seed", "true").lower() == "true",
        }
        output_format = request.form.get("output_format", "glb").lower()
        
        # Generate 3D model
        glb_path = fetch_3d_from_image(image_path, params)
        
        # Copy GLB to output folder
        output_glb_path = os.path.join(OUTPUT_FOLDER, f"{job_id}.glb")
        import shutil
        shutil.copy(glb_path, output_glb_path)
        
        response = {
            "job_id": job_id,
            "status": "completed",
            "glb_url": f"/api/download/{job_id}/glb",
        }
        
        # Convert to OBJ if requested
        if output_format == "obj":
            obj_path = convert_glb_to_obj(output_glb_path, OUTPUT_FOLDER)
            response["obj_url"] = f"/api/download/{job_id}/obj"
        
        return jsonify(response)
        
    except Exception as e:
        return jsonify({
            "error": str(e),
            "message": "Generation failed. The Hugging Face Space may be overloaded.",
            "fallback_url": "https://huggingface.co/spaces/tencent/Hunyuan3D-2.1"
        }), 500
    
    finally:
        # Cleanup uploaded file
        if os.path.exists(image_path):
            os.remove(image_path)


@app.route("/api/generate/url", methods=["POST"])
def generate_from_url():
    """
    Generate a 3D model from an image URL
    
    Request JSON:
        - image_url: URL of the image
        - steps, guidance_scale, seed, remove_background, output_format: (optional)
    """
    data = request.get_json()
    
    if not data or "image_url" not in data:
        return jsonify({"error": "image_url is required"}), 400
    
    image_url = data["image_url"]
    job_id = str(uuid.uuid4())
    
    try:
        # Download image
        import requests
        response = requests.get(image_url, timeout=30)
        response.raise_for_status()
        
        # Save to temp file
        ext = image_url.split(".")[-1].split("?")[0][:4]
        if ext not in ALLOWED_EXTENSIONS:
            ext = "jpg"
        
        image_path = os.path.join(UPLOAD_FOLDER, f"{job_id}.{ext}")
        with open(image_path, "wb") as f:
            f.write(response.content)
        
        # Parse parameters
        params = {
            "steps": data.get("steps", 30),
            "guidance_scale": data.get("guidance_scale", 5),
            "seed": data.get("seed", 1234),
            "remove_background": data.get("remove_background", True),
            "randomize_seed": data.get("randomize_seed", True),
        }
        output_format = data.get("output_format", "glb").lower()
        
        # Generate 3D model
        glb_path = fetch_3d_from_image(image_path, params)
        
        # Copy GLB to output folder
        output_glb_path = os.path.join(OUTPUT_FOLDER, f"{job_id}.glb")
        import shutil
        shutil.copy(glb_path, output_glb_path)
        
        result = {
            "job_id": job_id,
            "status": "completed",
            "glb_url": f"/api/download/{job_id}/glb",
        }
        
        if output_format == "obj":
            obj_path = convert_glb_to_obj(output_glb_path, OUTPUT_FOLDER)
            result["obj_url"] = f"/api/download/{job_id}/obj"
        
        return jsonify(result)
        
    except Exception as e:
        return jsonify({
            "error": str(e),
            "message": "Generation failed"
        }), 500
    
    finally:
        if os.path.exists(image_path):
            os.remove(image_path)


@app.route("/api/download/<job_id>/<format>", methods=["GET"])
def download_model(job_id, format):
    """Download generated 3D model"""
    
    if format == "glb":
        file_path = os.path.join(OUTPUT_FOLDER, f"{job_id}.glb")
        if os.path.exists(file_path):
            return send_file(file_path, as_attachment=True, download_name=f"{job_id}.glb")
    
    elif format == "obj":
        # Look for OBJ in subdirectory
        obj_dir = os.path.join(OUTPUT_FOLDER, job_id)
        if os.path.exists(obj_dir):
            for f in os.listdir(obj_dir):
                if f.endswith(".obj"):
                    return send_file(
                        os.path.join(obj_dir, f),
                        as_attachment=True,
                        download_name=f"{job_id}.obj"
                    )
    
    return jsonify({"error": "File not found"}), 404


@app.route("/api/models", methods=["GET"])
def list_models():
    """List all generated models"""
    models = []
    
    for f in os.listdir(OUTPUT_FOLDER):
        if f.endswith(".glb"):
            job_id = f.replace(".glb", "")
            models.append({
                "job_id": job_id,
                "glb_url": f"/api/download/{job_id}/glb",
                "created_at": os.path.getctime(os.path.join(OUTPUT_FOLDER, f))
            })
    
    return jsonify({
        "models": sorted(models, key=lambda x: x["created_at"], reverse=True),
        "total": len(models)
    })


@app.route("/", methods=["GET"])
def index():
    """API documentation"""
    return jsonify({
        "name": "Hugging Face 3D Generation Server",
        "description": "Convert images to 3D models using Hunyuan3D-2.1",
        "version": "1.0.0",
        "endpoints": {
            "GET /health": "Health check",
            "POST /api/generate": "Generate 3D model from uploaded image",
            "POST /api/generate/url": "Generate 3D model from image URL",
            "GET /api/download/<job_id>/<format>": "Download generated model (glb/obj)",
            "GET /api/models": "List all generated models"
        },
        "model": {
            "name": "Hunyuan3D-2.1",
            "provider": "Tencent",
            "huggingface_space": "https://huggingface.co/spaces/tencent/Hunyuan3D-2.1"
        }
    })


if __name__ == "__main__":
    port = int(os.getenv("PORT", 5000))
    debug = os.getenv("FLASK_DEBUG", "false").lower() == "true"
    
    print(f"🚀 Starting Hugging Face 3D Server on port {port}")
    print(f"📖 API docs available at http://localhost:{port}/")
    print(f"💚 Health check at http://localhost:{port}/health")
    
    app.run(host="0.0.0.0", port=port, debug=debug)

