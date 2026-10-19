"""把评级图标拆成「字母版面」和「数字字形」两套素材，供 boss 动画使用。

输出到 Marks\_split\:
    <mark>_L.png     去掉数字后的图标（圆底 + 字母）
    <mark>_d0.png    第 1 个数字（白色，透明背景）
    <mark>_d1.png    第 2 个数字
    ...
"""
import os
import re
import sys

import numpy as np
from PIL import Image, ImageFilter

MARKS = r"D:\Steam\steamapps\common\A Dance of Fire and Ice\Mods\PhigrosLoading\Marks"
OUT = os.path.join(MARKS, "_split")

PATTERN = re.compile(r"^([PGU])(\d{1,2})(J?)$")


def load(name):
    return Image.open(os.path.join(MARKS, name + ".png")).convert("RGBA")


def bg_color(img):
    """取圆环内侧的颜色中位数，避开中间的字母/数字。"""
    arr = np.asarray(img).astype(np.int32)
    h, w = arr.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    r = np.sqrt((xx - w / 2.0) ** 2 + (yy - h / 2.0) ** 2)
    ring = (r > min(w, h) * 0.30) & (r < min(w, h) * 0.40)
    px = arr[ring][:, :3]
    if len(px) == 0:
        return (0, 0, 0)
    return tuple(int(v) for v in np.median(px, axis=0))


def split(img):
    w, h = img.size
    arr = np.asarray(img).astype(np.int32)
    bg = np.array(bg_color(img), dtype=np.int32)

    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = w / 2.0, h / 2.0
    radius = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)

    dist = np.sqrt(((arr[:, :, :3] - bg) ** 2).sum(axis=2))
    inside = radius < (min(w, h) * 0.40)
    digit_area = inside & (yy > h * 0.42) & (yy < h * 0.92) & (xx > w * 0.12) & (xx < w * 0.88)

    mask = digit_area & (dist > 70)
    if mask.sum() < 20:
        return None, None

    # 列投影切分字形
    cols = mask.sum(axis=0)
    groups = []
    start = None
    for x in range(w):
        if cols[x] > 0 and start is None:
            start = x
        elif cols[x] == 0 and start is not None:
            if x - start >= 3:
                groups.append((start, x))
            start = None
    if start is not None and w - start >= 3:
        groups.append((start, w))

    # 合并间隙很小的碎片（抗锯齿造成的断裂）
    merged = []
    for g in groups:
        if merged and g[0] - merged[-1][1] <= 2:
            merged[-1] = (merged[-1][0], g[1])
        else:
            merged.append(list(g))
    groups = [tuple(g) for g in merged]

    if not groups:
        return None, None

    glyphs = []
    for (x0, x1) in groups:
        sub = mask[:, x0:x1]
        rows = np.where(sub.any(axis=1))[0]
        if len(rows) == 0:
            continue
        y0, y1 = rows[0], rows[-1] + 1
        sub = sub[y0:y1, :]

        gh, gw = sub.shape
        rgba = np.zeros((gh, gw, 4), dtype=np.uint8)
        rgba[:, :, 0] = 255
        rgba[:, :, 1] = 255
        rgba[:, :, 2] = 255
        rgba[:, :, 3] = (sub * 255).astype(np.uint8)
        glyphs.append(Image.fromarray(rgba, "RGBA"))

    # 字母版：整条数字带直接涂成底色（背景是纯色，安全）
    letter = img.copy()
    lp = letter.load()
    ys, xs = np.where(inside & (yy > h * 0.40))
    for y, x in zip(ys, xs):
        lp[x, y] = (int(bg[0]), int(bg[1]), int(bg[2]), 255)

    return letter, glyphs


def main():
    os.makedirs(OUT, exist_ok=True)
    total = 0
    for name in sorted(os.listdir(MARKS)):
        if not name.lower().endswith(".png"):
            continue
        base = name[:-4]
        if not PATTERN.match(base):
            continue
        img = load(base)
        letter, glyphs = split(img)
        if letter is None:
            print("skip", base)
            continue
        letter.save(os.path.join(OUT, base + "_L.png"))
        for i, glyph in enumerate(glyphs):
            glyph.save(os.path.join(OUT, "%s_d%d.png" % (base, i)))
        total += 1
        print("ok", base, "digits:", len(glyphs))

    print("done", total)


if __name__ == "__main__":
    main()
