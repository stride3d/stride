"""
One-shot exporter: produce lpips_alex.onnx from the lpips Python lib.
Run from this dir's venv: ./venv/Scripts/python.exe export.py
"""
import torch
import lpips

# 'alex' = AlexNet backbone, much faster than VGG, sufficient quality for screenshot regression.
model = lpips.LPIPS(net='alex', spatial=False, verbose=False)
model.eval()

# Wrap so the ONNX inputs are the two raw [-1, 1]-normalized RGB tensors and the output is a scalar distance.
class _Wrapper(torch.nn.Module):
    def __init__(self, m): super().__init__(); self.m = m
    def forward(self, a, b):
        # m(a, b) returns a (1,1,1,1) tensor; squeeze to scalar.
        return self.m(a, b).reshape(-1)

w = _Wrapper(model).eval()

# Dummy inputs at 256x256; the model is fully convolutional so any HxW works at runtime.
a = torch.zeros(1, 3, 256, 256)
b = torch.zeros(1, 3, 256, 256)

torch.onnx.export(
    w,
    (a, b),
    'lpips_alex_external.onnx',
    input_names=['a', 'b'],
    output_names=['distance'],
    dynamic_axes={
        'a': {0: 'batch', 2: 'height', 3: 'width'},
        'b': {0: 'batch', 2: 'height', 3: 'width'},
        'distance': {0: 'batch'},
    },
    opset_version=17,
)
# torch.onnx.export emits external data by default; re-save with everything inlined so the .NET
# comparator only needs the single .onnx file.
import onnx
m = onnx.load('lpips_alex_external.onnx', load_external_data=True)
onnx.save(m, 'lpips_alex.onnx', save_as_external_data=False)
import os
os.remove('lpips_alex_external.onnx')
if os.path.exists('lpips_alex_external.onnx.data'):
    os.remove('lpips_alex_external.onnx.data')
print('exported lpips_alex.onnx')

# Sanity: round-trip ORT vs torch on a non-trivial input.
import numpy as np
import onnxruntime as ort
torch.manual_seed(0)
a = torch.rand(1, 3, 256, 256) * 2 - 1
b = torch.rand(1, 3, 256, 256) * 2 - 1
torch_d = float(w(a, b)[0])
sess = ort.InferenceSession('lpips_alex.onnx', providers=['CPUExecutionProvider'])
ort_d = sess.run(None, {'a': a.numpy(), 'b': b.numpy()})[0][0]
print(f'  torch={torch_d:.6f}  ort={ort_d:.6f}  delta={abs(torch_d-ort_d):.2e}')
