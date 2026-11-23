import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC = r'C:/Users/guape/Documents/TFG-App-Precios/docs/diseno/inicio/inicio-pricemap.png'
OUT = r'C:/Users/guape/AppData/Local/Temp/claude/967482c3100680b3/5fb0986d-b7c3-4bd8-9010-d36de9ef35c4/scratchpad/inicio'
os.makedirs(OUT, exist_ok=True)
im = Image.open(SRC).convert('RGB')
print(im.size, im.getpixel((5, 5)), im.getpixel((420, 300)))
K = 3  # escala de salida


def capa_color(box):
    """Alfa = cuánto se aparta del blanco; el color se recupera (vale para el nombre, de colores oscuros)."""
    a = np.asarray(im.crop(box).resize(((box[2] - box[0]) * K, (box[3] - box[1]) * K), Image.LANCZOS)).astype(float) / 255
    return a


# Nombre: 'Price' azul marino, 'Map' azul
box = (130, 1118, 728, 1262)
a = capa_color(box)
dist = 1 - a.min(axis=2)
alfa = np.clip((dist - 0.12) / (0.62 - 0.12), 0, 1)
w = a.shape[1]
corte = int((462 - box[0]) * K)
navy = np.array(im.getpixel((160, 1180))) ; azul = np.array(im.getpixel((655, 1225)))
print('navy', navy, 'azul', azul)


def nombre(price, mapa, f):
    col = np.zeros(a.shape[:2] + (4,), np.uint8)
    col[:, :corte, :3] = price
    col[:, corte:, :3] = mapa
    col[..., 3] = (alfa * 255).astype(np.uint8)
    Image.fromarray(col).save(os.path.join(OUT, f))


nombre(navy, azul, 'nombre-claro.png')
nombre((255, 255, 255), (91, 140, 255), 'nombre-oscuro.png')

# Pin: la silueta se dibuja con su forma y dentro van los píxeles de la imagen
pb = (270, 655, 590, 1045)
px = im.crop(pb).resize(((pb[2] - pb[0]) * K, (pb[3] - pb[1]) * K), Image.LANCZOS)
p = np.asarray(px).astype(float)
# Fondo: blanco o azul claro de los anillos. Dentro del pin todo es azul fuerte, amarillo o marino.
r, g, b = p[..., 0], p[..., 1], p[..., 2]
azulf = (b > 170) & (r < 60) & (g < 110)
am = (r > 190) & (g > 150) & (b < 130)
ma = (b < 140) & (r < 60) & (g < 70)
m = Image.fromarray(((azulf | am | ma) * 255).astype(np.uint8))
m = m.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(9))
# El disco de arriba, entero: la imagen tiene ahí un brillo casi blanco que se perdía
ImageDraw.Draw(m).pieslice([(428.5 - pb[0] - 141) * K, (812 - pb[1] - 141) * K, (428.5 - pb[0] + 141) * K, (812 - pb[1] + 141) * K], 180, 360, fill=255)
# Solo lo de fuera es hueco
lleno = m.copy()
ImageDraw.floodfill(lleno, (0, 0), 128)
ImageDraw.floodfill(lleno, (lleno.size[0] - 1, 0), 128)
ImageDraw.floodfill(lleno, (0, lleno.size[1] - 1), 128)
ImageDraw.floodfill(lleno, (lleno.size[0] - 1, lleno.size[1] - 1), 128)
mas = Image.fromarray(((np.asarray(lleno) != 128) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(2))
# Borde: color azul sólido bajo el suavizado, para que no quede halo blanco
base = np.asarray(px).copy()
nucleo = np.asarray(mas) > 250
from PIL import ImageChops
yy, xx = np.mgrid[0:base.shape[0], 0:base.shape[1]]
lejos = ((xx / K + pb[0] - 428.5) / 94) ** 2 + ((yy / K + pb[1] - 811) / 90) ** 2 > 1
claro = lejos & ((base[..., 0] > 70) | (base[..., 1] > 130))
base[claro] = (31, 100, 253)
rgb = Image.fromarray(base)
azulp = tuple(int(v) for v in im.getpixel((300, 840)))
fondo = Image.new('RGB', rgb.size, azulp)
dentro = Image.fromarray((nucleo * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(7))
comp = Image.composite(rgb, fondo, dentro)
comp.putalpha(mas)
comp.save(os.path.join(OUT, 'pin.png'))
print('pin', comp.size, azulp, mas.getbbox())

# Anillos (elipses limpias)
S = 4
W, H = 360 * S, 120 * S
cx, cy = W / 2, H / 2
an = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(an)


def el(rx, ry, c):
    d.ellipse([cx - rx * S, cy - ry * S, cx + rx * S, cy + ry * S], fill=c)


el(173, 54, (58, 120, 250, 255))
el(143, 40, (0, 0, 0, 0))
an.resize((360 * K, 120 * K), Image.LANCZOS).save(os.path.join(OUT, 'anillo.png'))
an2 = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(an2)
el(104, 27, (178, 200, 250, 255))
an2.resize((360 * K, 120 * K), Image.LANCZOS).save(os.path.join(OUT, 'sombra.png'))

# Prueba de montaje en los dos fondos
for f, bg, nom in (('m-claro.png', (253, 253, 253), 'nombre-claro.png'), ('m-oscuro.png', (15, 20, 36), 'nombre-oscuro.png')):
    t = Image.new('RGBA', (852, 700), bg + (255,))
    oy = 640
    for n, (x, y, ww, hh) in (('anillo.png', (428 - 180, 1014 - 60, 360, 120)), ('sombra.png', (428 - 180, 1008 - 60, 360, 120)), ('pin.png', (270, 655, 320, 390)), (nom, (130, 1118, 598, 144))):
        l = Image.open(os.path.join(OUT, n)).resize((ww, hh), Image.LANCZOS)
        t.alpha_composite(l, (x, y - oy))
    t.convert('RGB').save(os.path.join(OUT, f))
