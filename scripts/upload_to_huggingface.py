"""
Upload ONNX model files to HuggingFace Hub.

Usage:
    python scripts/upload_to_huggingface.py --model-dir ./sd_v15_onnx --repo-id elbruno/sd-v1-5-onnx --token $HF_TOKEN
"""

import argparse
import os


def main():
    parser = argparse.ArgumentParser(description="Upload ONNX model to HuggingFace")
    parser.add_argument("--model-dir", required=True, help="Local model directory")
    parser.add_argument("--repo-id", required=True, help="HuggingFace repo ID (e.g., elbruno/model-name)")
    parser.add_argument("--token", required=True, help="HuggingFace API token")
    parser.add_argument("--private", action="store_true", help="Make repo private")
    args = parser.parse_args()

    try:
        from huggingface_hub import HfApi
    except ImportError:
        print("Please install huggingface_hub: pip install huggingface_hub")
        return

    api = HfApi(token=args.token)

    # Create repo if it doesn't exist
    api.create_repo(
        repo_id=args.repo_id,
        repo_type="model",
        private=args.private,
        exist_ok=True,
    )

    # Upload all files
    print(f"Uploading {args.model_dir} to {args.repo_id}...")
    api.upload_folder(
        folder_path=args.model_dir,
        repo_id=args.repo_id,
        repo_type="model",
    )
    print(f"Upload complete! https://huggingface.co/{args.repo_id}")


if __name__ == "__main__":
    main()
