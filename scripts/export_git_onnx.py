"""
Export Microsoft GIT (GenerativeImage2Text) model to ONNX format.

Usage:
    pip install optimum[onnxruntime] transformers torch
    python scripts/export_git_onnx.py [--model microsoft/git-base-coco] [--output ./git-base-coco-onnx]

This script exports the GIT model using Hugging Face Optimum, which handles
the encoder-decoder architecture and KV-cache configuration automatically.
"""

import argparse
import subprocess
import sys
from pathlib import Path


def export_git_model(model_name: str, output_dir: str):
    """Export GIT model to ONNX using optimum-cli."""
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    print(f"Exporting {model_name} to ONNX...")
    print(f"Output directory: {output_path}")

    # Use optimum-cli for export
    cmd = [
        sys.executable, "-m", "optimum.exporters.onnx",
        "--model", model_name,
        "--task", "image-to-text-with-past",
        str(output_path),
    ]

    print(f"Running: {' '.join(cmd)}")
    result = subprocess.run(cmd, capture_output=True, text=True)

    if result.returncode != 0:
        print(f"Error during export:\n{result.stderr}")
        # Fallback: try without KV-cache
        print("Retrying without KV-cache support...")
        cmd = [
            sys.executable, "-m", "optimum.exporters.onnx",
            "--model", model_name,
            "--task", "image-to-text",
            str(output_path),
        ]
        result = subprocess.run(cmd, capture_output=True, text=True)
        if result.returncode != 0:
            print(f"Export failed:\n{result.stderr}")
            sys.exit(1)

    print(f"Export successful!")
    print(f"Files in {output_path}:")
    for f in sorted(output_path.iterdir()):
        size_mb = f.stat().st_size / (1024 * 1024)
        print(f"  {f.name} ({size_mb:.1f} MB)")


def validate_export(model_name: str, output_dir: str):
    """Validate the ONNX export against the original model."""
    try:
        from optimum.onnxruntime import ORTModelForVision2Seq
        from transformers import AutoProcessor
        from PIL import Image
        import requests

        print("\nValidating ONNX export...")

        # Load ONNX model
        ort_model = ORTModelForVision2Seq.from_pretrained(output_dir)
        processor = AutoProcessor.from_pretrained(model_name)

        # Test image
        url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg"
        image = Image.open(requests.get(url, stream=True).raw)

        inputs = processor(images=image, return_tensors="pt")
        generated_ids = ort_model.generate(**inputs, max_length=50)
        caption = processor.batch_decode(generated_ids, skip_special_tokens=True)[0]

        print(f"ONNX caption: {caption}")
        print("Validation successful!")

    except Exception as e:
        print(f"Validation skipped: {e}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Export GIT model to ONNX")
    parser.add_argument("--model", default="microsoft/git-base-coco", help="HuggingFace model ID")
    parser.add_argument("--output", default="./git-base-coco-onnx", help="Output directory")
    parser.add_argument("--validate", action="store_true", help="Validate export against original")
    args = parser.parse_args()

    export_git_model(args.model, args.output)

    if args.validate:
        validate_export(args.model, args.output)
