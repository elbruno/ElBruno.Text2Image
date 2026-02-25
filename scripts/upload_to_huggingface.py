"""
Upload ONNX model files to a Hugging Face repository.

Usage:
    pip install huggingface_hub
    python scripts/upload_to_huggingface.py --model-path ./git-base-coco-onnx --repo-id elbruno/git-base-coco-onnx

Prerequisites:
    - Run `huggingface-cli login` first to authenticate
    - Or set HF_TOKEN environment variable
"""

import argparse
import os
import sys
from pathlib import Path


def upload_to_huggingface(model_path: str, repo_id: str, commit_message: str):
    """Upload ONNX model directory to Hugging Face Hub."""
    try:
        from huggingface_hub import HfApi, create_repo
    except ImportError:
        print("Please install: pip install huggingface_hub")
        sys.exit(1)

    model_dir = Path(model_path)
    if not model_dir.exists():
        print(f"Error: Model directory not found: {model_dir}")
        sys.exit(1)

    api = HfApi()

    # Create repo if it doesn't exist
    print(f"Creating repository: {repo_id}")
    try:
        create_repo(repo_id, repo_type="model", exist_ok=True)
    except Exception as e:
        print(f"Warning: Could not create repo: {e}")

    # Upload all files
    print(f"Uploading files from {model_dir} to {repo_id}...")
    files = list(model_dir.rglob("*"))
    files = [f for f in files if f.is_file()]

    print(f"Found {len(files)} files to upload:")
    for f in files:
        size_mb = f.stat().st_size / (1024 * 1024)
        print(f"  {f.relative_to(model_dir)} ({size_mb:.1f} MB)")

    api.upload_folder(
        folder_path=str(model_dir),
        repo_id=repo_id,
        repo_type="model",
        commit_message=commit_message,
    )

    print(f"\nUpload complete!")
    print(f"Model available at: https://huggingface.co/{repo_id}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Upload ONNX model to Hugging Face")
    parser.add_argument("--model-path", required=True, help="Path to ONNX model directory")
    parser.add_argument("--repo-id", required=True, help="HuggingFace repo ID (e.g., elbruno/model-name)")
    parser.add_argument("--commit-message", default="Add ONNX model weights", help="Commit message")
    args = parser.parse_args()

    upload_to_huggingface(args.model_path, args.repo_id, args.commit_message)
