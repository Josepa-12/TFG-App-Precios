# Descarga el Firebase Unity SDK y EDM4U (paquetes .tgz de Google) en unity/PriceMap/Packages/Firebase
# y comprueba su SHA-256. Solo descarga y compara: no abre ni ejecuta nada de dentro.
# Uso, desde cualquier carpeta:
#   powershell -NoProfile -ExecutionPolicy Bypass -File firebase/descargar-sdk.ps1
# Los archivos que ya están y coinciden no se vuelven a bajar.

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # sin barra: en PowerShell 5.1 multiplica el tiempo de descarga
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$origen = 'https://dl.google.com/games/registry/unity'
$destino = Join-Path (Split-Path -Parent $PSScriptRoot) 'unity\PriceMap\Packages\Firebase'

# Los mismos nombres y versiones que unity/PriceMap/Packages/manifest.json
$paquetes = @(
    @{ Nombre = 'com.google.external-dependency-manager'; Version = '1.2.190'; Sha256 = '9a326599dab4e6bcaf13cd267689cec9e302abe213929335b3d0f93083b998de' }
    @{ Nombre = 'com.google.firebase.app'; Version = '14.1.0'; Sha256 = '934c52d39cac8465d13f530cab4ebe2e1e336ed4b35286691a08c6854574cb6f' }
    @{ Nombre = 'com.google.firebase.auth'; Version = '14.1.0'; Sha256 = '1776ee6181ab7803ec2e2dfb2969d2b2299d3ff0fcd8b2bafc12333e73b9650a' }
    @{ Nombre = 'com.google.firebase.firestore'; Version = '14.1.0'; Sha256 = 'adae94248ba896c77fa32ef48b6a89a3edacb70bc794238e5a71e7e896cdd6d2' }
)

function Test-Huella($ruta, $esperada) {
    (Get-FileHash -LiteralPath $ruta -Algorithm SHA256).Hash -eq $esperada # -eq no distingue mayúsculas
}

New-Item -ItemType Directory -Force -Path $destino | Out-Null

foreach ($p in $paquetes) {
    $archivo = "$($p.Nombre)-$($p.Version).tgz"
    $final = Join-Path $destino $archivo
    if ((Test-Path -LiteralPath $final) -and (Test-Huella $final $p.Sha256)) {
        Write-Host "Ya estaba: $archivo"
        continue
    }

    # Cada descarga, en una carpeta nueva y vacía; solo pasa a Packages si su huella es la esperada
    $carpeta = Join-Path ([IO.Path]::GetTempPath()) ('pricemap-sdk-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $carpeta | Out-Null
    try {
        $bajado = Join-Path $carpeta $archivo
        Write-Host "Descargando: $archivo"
        Invoke-WebRequest -UseBasicParsing -TimeoutSec 600 -Uri "$origen/$($p.Nombre)/$archivo" -OutFile $bajado
        if (-not (Test-Huella $bajado $p.Sha256)) {
            throw "La huella SHA-256 de $archivo no es la esperada; no se instala."
        }
        Move-Item -LiteralPath $bajado -Destination $final -Force
    }
    finally {
        Remove-Item -LiteralPath $carpeta -Recurse -Force
    }
}

Write-Host "Firebase Unity SDK listo en $destino"
