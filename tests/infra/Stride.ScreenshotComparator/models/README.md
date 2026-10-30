# LPIPS-AlexNet ONNX model

`lpips_alex.onnx` is a one-shot export of the [LPIPS](https://github.com/richzhang/PerceptualSimilarity)
perceptual-distance network (AlexNet backbone + LPIPS head). The Comparator references it via
ONNX Runtime to score screenshot pairs. Once exported, it never needs regenerating during normal
operation — the weights are static and content-independent.

Re-export only when you want to:
- swap the backbone (`alex` → `vgg`, ~4× slower but slightly better perceptual fidelity)
- adopt newer LPIPS head weights from the upstream Python package
- bump the ONNX opset

## How to regenerate

Requires Python 3.10+ on a path short enough that pip doesn't trip on Windows long-path limits
(`C:\tmp\lpips-export` works; deep paths under `AppData\Local\Packages\…` don't).

```bash
python -m venv venv
./venv/Scripts/python.exe -m pip install torch torchvision lpips onnx onnxruntime onnxscript
PYTHONIOENCODING=utf-8 ./venv/Scripts/python.exe export.py
```

The script also runs an inline parity check (PyTorch vs ONNX Runtime on a randomized input)
and prints the delta — should be `~1e-8`. If it isn't, something regressed in the export path.
