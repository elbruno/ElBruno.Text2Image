"""
Export Moondream 2 model to ONNX format.

Usage:
    pip install transformers torch onnx onnxruntime pillow einops
    python scripts/export_moondream_onnx.py [--revision 2025-04-14] [--output ./moondream2-onnx]

NOTE: Moondream uses a custom architecture (trust_remote_code=True) that is not
directly supported by optimum-cli. This script handles the custom export process.
"""

import argparse
import sys
from pathlib import Path


def export_moondream(revision: str, output_dir: str):
    """Export Moondream 2 to ONNX with custom handling."""
    try:
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer
    except ImportError:
        print("Please install: pip install transformers torch onnx einops")
        sys.exit(1)

    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    print(f"Loading Moondream 2 (revision: {revision})...")
    model = AutoModelForCausalLM.from_pretrained(
        "vikhyatk/moondream2",
        revision=revision,
        trust_remote_code=True,
        torch_dtype=torch.float32,
    )
    model.eval()

    tokenizer = AutoTokenizer.from_pretrained(
        "vikhyatk/moondream2",
        revision=revision,
        trust_remote_code=True,
    )

    # Export vision encoder
    print("Exporting vision encoder...")
    try:
        # Moondream's vision encoder takes pixel_values and returns image embeddings
        dummy_image = torch.randn(1, 3, 378, 378)

        # Access the vision encoder
        vision_encoder = model.vision_encoder if hasattr(model, 'vision_encoder') else model.model.vision_encoder

        torch.onnx.export(
            vision_encoder,
            dummy_image,
            str(output_path / "vision_encoder.onnx"),
            input_names=["pixel_values"],
            output_names=["image_embeddings"],
            dynamic_axes={
                "pixel_values": {0: "batch_size"},
                "image_embeddings": {0: "batch_size"},
            },
            opset_version=17,
        )
        print("  Vision encoder exported successfully.")
    except Exception as e:
        print(f"  Vision encoder export failed: {e}")
        print("  Moondream's custom architecture may require manual adaptation.")
        print("  Consider using the model directly via transformers for now.")

    # Save tokenizer
    tokenizer.save_pretrained(str(output_path))
    print(f"Tokenizer saved to {output_path}")

    print(f"\nExport complete. Files in {output_path}:")
    for f in sorted(output_path.iterdir()):
        if f.is_file():
            size_mb = f.stat().st_size / (1024 * 1024)
            print(f"  {f.name} ({size_mb:.1f} MB)")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Export Moondream 2 to ONNX")
    parser.add_argument("--revision", default="2025-04-14", help="Model revision/date")
    parser.add_argument("--output", default="./moondream2-onnx", help="Output directory")
    args = parser.parse_args()

    export_moondream(args.revision, args.output)
