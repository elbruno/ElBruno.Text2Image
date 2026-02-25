"""
Export Stable Diffusion models to ONNX format using Hugging Face Optimum.

Usage:
    python scripts/export_sd_onnx.py --model stabilityai/stable-diffusion-2-1-base --output sd21_onnx
    python scripts/export_sd_onnx.py --model stabilityai/sdxl-turbo --output sdxl_turbo_onnx --task stable-diffusion-xl
"""

import argparse
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description="Export Stable Diffusion to ONNX")
    parser.add_argument("--model", required=True, help="HuggingFace model ID")
    parser.add_argument("--output", required=True, help="Output directory")
    parser.add_argument(
        "--task",
        default="stable-diffusion",
        choices=["stable-diffusion", "stable-diffusion-xl"],
        help="Export task type",
    )
    parser.add_argument("--fp16", action="store_true", help="Export in FP16")
    args = parser.parse_args()

    cmd = [
        sys.executable,
        "-m",
        "optimum.exporters.onnx",
        "--model",
        args.model,
        "--task",
        args.task,
        args.output,
    ]

    if args.fp16:
        cmd.insert(-1, "--fp16")

    print(f"Exporting {args.model} to ONNX...")
    print(f"Command: {' '.join(cmd)}")
    subprocess.run(cmd, check=True)
    print(f"Export complete! Output: {args.output}")


if __name__ == "__main__":
    main()
