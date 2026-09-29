"""生成程序图标 Assets/app.ico（蓝色圆角文件夹，PNG 压缩的 ICO）。"""
import math
import os
import struct
import zlib

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "src", "TaskbarDock", "Assets", "app.ico")


def sdf_round_rect(x, y, cx, cy, hw, hh, r):
    qx = abs(x - cx) - hw + r
    qy = abs(y - cy) - hh + r
    return math.hypot(max(qx, 0.0), max(qy, 0.0)) + min(max(qx, qy), 0.0) - r


def render(size):
    w = h = size
    px = bytearray(w * h * 4)
    top = (90, 169, 255)
    bottom = (20, 100, 200)
    tab_c = (120, 190, 255)
    for y in range(h):
        t = y / (h - 1)
        for x in range(w):
            fx, fy = x + 0.5, y + 0.5
            d_tab = sdf_round_rect(fx, fy, 0.40 * w, 0.335 * h, 0.215 * w, 0.055 * h, 0.035 * w)
            d_body = sdf_round_rect(fx, fy, 0.5 * w, 0.605 * h, 0.395 * w, 0.275 * h, 0.06 * w)
            d = min(d_tab, d_body)
            a = max(0.0, min(1.0, 0.5 - d))
            if a <= 0:
                continue
            if d == d_tab:
                base = tab_c
            else:
                base = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
            i = (y * w + x) * 4
            px[i] = base[0]
            px[i + 1] = base[1]
            px[i + 2] = base[2]
            px[i + 3] = int(255 * a)
    return bytes(px)


def png(w, h, data):
    raw = bytearray()
    stride = w * 4
    for y in range(h):
        raw.append(0)
        raw += data[y * stride:(y + 1) * stride]

    def chunk(tag, payload):
        return (struct.pack(">I", len(payload)) + tag + payload
                + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF))

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr)
            + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))


def main():
    sizes = [256, 64, 48, 32, 16]
    images = [(s, png(s, s, render(s))) for s in sizes]
    out = bytearray(struct.pack("<HHH", 0, 1, len(images)))
    offset = 6 + 16 * len(images)
    entries = bytearray()
    for s, data in images:
        entries += struct.pack("<BBBBHHII", s if s < 256 else 0, s if s < 256 else 0,
                               0, 0, 1, 32, len(data), offset)
        offset += len(data)
    out += entries
    for _, data in images:
        out += data
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "wb") as f:
        f.write(out)
    print("wrote", OUT, len(out), "bytes")


if __name__ == "__main__":
    main()
