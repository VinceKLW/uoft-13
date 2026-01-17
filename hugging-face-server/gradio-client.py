import os
import shutil
from gradio_client import Client, handle_file
import trimesh

HF_TOKEN = os.getenv("HF_TOKEN")  # Set via environment variable


def fetch_3d_from_image(image_path: str, hf_token: str = None) -> str:
    hf_token = hf_token or HF_TOKEN
    client = Client("tencent/Hunyuan3D-2.1", hf_token=hf_token)
    result = client.predict(
        image=handle_file(image_path),
        mv_image_front=None,
        mv_image_back=None,
        mv_image_left=None,
        mv_image_right=None,
        steps=30,
        guidance_scale=5,
        seed=1234,
        octree_resolution=256,
        check_box_rembg=True,
        num_chunks=8000,
        randomize_seed=True,
        api_name="/generation_all"
    )
    
    glb_path = result[0]
    print(f"GLB generated: {glb_path}")
    return glb_path


def convert_to_unity_asset(glb_path: str, output_dir: str) -> str:
    base_name = os.path.splitext(os.path.basename(glb_path))[0]
    file_output_dir = os.path.join(output_dir, base_name)
    os.makedirs(file_output_dir, exist_ok=True)
    
    print(f"Loading GLB: {glb_path}")
    scene = trimesh.load(glb_path, force='scene')
    
    obj_path = os.path.join(file_output_dir, base_name + ".obj")
    scene.export(obj_path)
    
    print(f"Exported OBJ to: {obj_path}")
    return obj_path


def image_to_unity_asset(image_path: str, output_dir: str) -> str:
    
    glb_path = fetch_3d_from_image(image_path)
    
    obj_path = convert_to_unity_asset(glb_path, output_dir)
    
    return obj_path


if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    image_path = os.path.join(script_dir, "EfUC06KWkAc1lma.jpg")
    output_dir = os.path.join(script_dir, "output")
    
    try:
        obj_path = image_to_unity_asset(image_path, output_dir)
        print(f"\nDone! Unity asset saved to: {obj_path}")
    except Exception as e:
        print(f"Error: {e}")
        print("\nThe Space may be overloaded. Try again later or check:")
        print("https://huggingface.co/spaces/tencent/Hunyuan3D-2.1")
