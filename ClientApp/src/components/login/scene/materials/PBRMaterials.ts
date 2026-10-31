import * as THREE from 'three'

/**
 * Procedural PBR Texture Map Generators (Canvas2D Sobel Normal & Albedo Pipeline)
 * Generates seamless PBR texture sets with Base Color, Normal Maps, and Roughness Maps
 * with zero external HTTP requests, guaranteed offline reliability, and fast browser execution.
 */

// 1. Weathered Jordanian Ashlar Limestone PBR Maps
let cachedLimestoneTextures: { map: THREE.CanvasTexture; normalMap: THREE.CanvasTexture; roughnessMap: THREE.CanvasTexture } | null = null

export function getLimestonePBRTextures() {
  if (cachedLimestoneTextures) return cachedLimestoneTextures
  if (typeof document === 'undefined') return null

  const size = 512
  const canvasAlbedo = document.createElement('canvas')
  canvasAlbedo.width = size
  canvasAlbedo.height = size
  const ctxA = canvasAlbedo.getContext('2d')

  const canvasNormal = document.createElement('canvas')
  canvasNormal.width = size
  canvasNormal.height = size
  const ctxN = canvasNormal.getContext('2d')

  const canvasRoughness = document.createElement('canvas')
  canvasRoughness.width = size
  canvasRoughness.height = size
  const ctxR = canvasRoughness.getContext('2d')

  if (!ctxA || !ctxN || !ctxR) return null

  // Height map buffer for Sobel normal computation
  const heightMap = new Float32Array(size * size)

  // Generate Base Color & Height
  const imgDataA = ctxA.createImageData(size, size)
  const imgDataR = ctxR.createImageData(size, size)

  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const idx = (y * size + x) * 4
      const hIdx = y * size + x

      // Horizontal bedding strata wave
      const strata = Math.sin(y * 0.08) * 0.15 + Math.sin(y * 0.02) * 0.25
      // High-frequency stone grain noise
      const grain = (Math.sin(x * 0.35 + y * 0.2) + Math.cos(x * 0.2 - y * 0.35)) * 0.12
      // Chiseled block tool lines
      const chisel = (Math.sin(x * 0.05) * Math.sin(y * 0.05)) * 0.1

      const h = Math.max(0, Math.min(1, 0.5 + strata + grain + chisel))
      heightMap[hIdx] = h

      // Warm Jordanian limestone albedo (golden-beige to sandy-ochre)
      const r = Math.floor(180 + h * 45)
      const g = Math.floor(135 + h * 35)
      const b = Math.floor(85 + h * 25)

      imgDataA.data[idx] = r
      imgDataA.data[idx + 1] = g
      imgDataA.data[idx + 2] = b
      imgDataA.data[idx + 3] = 255

      // Roughness map (0.75 - 0.95: weathered porous limestone)
      const rough = Math.floor(190 + (1 - h) * 55)
      imgDataR.data[idx] = rough
      imgDataR.data[idx + 1] = rough
      imgDataR.data[idx + 2] = rough
      imgDataR.data[idx + 3] = 255
    }
  }
  ctxA.putImageData(imgDataA, 0, 0)
  ctxR.putImageData(imgDataR, 0, 0)

  // Compute Sobel Normal Map from Height Map
  const imgDataN = ctxN.createImageData(size, size)
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const idx = (y * size + x) * 4

      const x0 = x > 0 ? x - 1 : x
      const x1 = x < size - 1 ? x + 1 : x
      const y0 = y > 0 ? y - 1 : y
      const y1 = y < size - 1 ? y + 1 : y

      const dX = (heightMap[y * size + x1] - heightMap[y * size + x0]) * 3.5
      const dY = (heightMap[y1 * size + x] - heightMap[y0 * size + x]) * 3.5

      // Normal vector [ -dX, -dY, 1.0 ] normalized to [0..255]
      const len = Math.sqrt(dX * dX + dY * dY + 1.0)
      const nx = (-dX / len) * 0.5 + 0.5
      const ny = (-dY / len) * 0.5 + 0.5
      const nz = (1.0 / len) * 0.5 + 0.5

      imgDataN.data[idx] = Math.floor(nx * 255)
      imgDataN.data[idx + 1] = Math.floor(ny * 255)
      imgDataN.data[idx + 2] = Math.floor(nz * 255)
      imgDataN.data[idx + 3] = 255
    }
  }
  ctxN.putImageData(imgDataN, 0, 0)

  const map = new THREE.CanvasTexture(canvasAlbedo)
  map.wrapS = THREE.RepeatWrapping
  map.wrapT = THREE.RepeatWrapping
  map.repeat.set(3, 3)

  const normalMap = new THREE.CanvasTexture(canvasNormal)
  normalMap.wrapS = THREE.RepeatWrapping
  normalMap.wrapT = THREE.RepeatWrapping
  normalMap.repeat.set(3, 3)

  const roughnessMap = new THREE.CanvasTexture(canvasRoughness)
  roughnessMap.wrapS = THREE.RepeatWrapping
  roughnessMap.wrapT = THREE.RepeatWrapping
  roughnessMap.repeat.set(3, 3)

  cachedLimestoneTextures = { map, normalMap, roughnessMap }
  return cachedLimestoneTextures
}

// 2. Anodized Dark Metal Brushed PBR Maps
let cachedMetalTextures: { map: THREE.CanvasTexture; normalMap: THREE.CanvasTexture; roughnessMap: THREE.CanvasTexture } | null = null

export function getDarkMetalPBRTextures() {
  if (cachedMetalTextures) return cachedMetalTextures
  if (typeof document === 'undefined') return null

  const size = 256
  const canvasA = document.createElement('canvas')
  canvasA.width = size
  canvasA.height = size
  const ctxA = canvasA.getContext('2d')

  const canvasN = document.createElement('canvas')
  canvasN.width = size
  canvasN.height = size
  const ctxN = canvasN.getContext('2d')

  const canvasR = document.createElement('canvas')
  canvasR.width = size
  canvasR.height = size
  const ctxR = canvasR.getContext('2d')

  if (!ctxA || !ctxN || !ctxR) return null

  const imgDataA = ctxA.createImageData(size, size)
  const imgDataN = ctxN.createImageData(size, size)
  const imgDataR = ctxR.createImageData(size, size)

  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const idx = (y * size + x) * 4
      // Fine vertical brushed grain
      const grain = Math.sin(x * 1.5) * 0.08 + (Math.random() - 0.5) * 0.05

      const c = Math.floor(15 + grain * 20)
      imgDataA.data[idx] = c
      imgDataA.data[idx + 1] = c + 4
      imgDataA.data[idx + 2] = c + 10
      imgDataA.data[idx + 3] = 255

      // Normal map brushed relief
      imgDataN.data[idx] = Math.floor(128 + grain * 40)
      imgDataN.data[idx + 1] = 128
      imgDataN.data[idx + 2] = 255
      imgDataN.data[idx + 3] = 255

      // Roughness (0.25 - 0.35)
      imgDataR.data[idx] = Math.floor(70 + grain * 30)
      imgDataR.data[idx + 1] = Math.floor(70 + grain * 30)
      imgDataR.data[idx + 2] = Math.floor(70 + grain * 30)
      imgDataR.data[idx + 3] = 255
    }
  }
  ctxA.putImageData(imgDataA, 0, 0)
  ctxN.putImageData(imgDataN, 0, 0)
  ctxR.putImageData(imgDataR, 0, 0)

  const map = new THREE.CanvasTexture(canvasA)
  map.wrapS = THREE.RepeatWrapping
  map.wrapT = THREE.RepeatWrapping
  map.repeat.set(2, 4)

  const normalMap = new THREE.CanvasTexture(canvasN)
  normalMap.wrapS = THREE.RepeatWrapping
  normalMap.wrapT = THREE.RepeatWrapping
  normalMap.repeat.set(2, 4)

  const roughnessMap = new THREE.CanvasTexture(canvasR)
  roughnessMap.wrapS = THREE.RepeatWrapping
  roughnessMap.wrapT = THREE.RepeatWrapping
  roughnessMap.repeat.set(2, 4)

  cachedMetalTextures = { map, normalMap, roughnessMap }
  return cachedMetalTextures
}

// 3. Equirectangular Night Sky & City Radiosity Environment Texture
export function generateNightEnvironmentTexture(): THREE.CanvasTexture | null {
  if (typeof document === 'undefined') return null

  const width = 1024
  const height = 512
  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  const ctx = canvas.getContext('2d')
  if (!ctx) return null

  // Zenith to horizon celestial gradient
  const skyGrad = ctx.createLinearGradient(0, 0, 0, height)
  skyGrad.addColorStop(0, '#020612') // Deep cosmic midnight blue
  skyGrad.addColorStop(0.45, '#061329') // Atmospheric navy
  skyGrad.addColorStop(0.55, '#1e1c24') // Amman city horizon glow (warm golden/amber dust)
  skyGrad.addColorStop(0.7, '#080d1a') // Ground / roadway dark floor
  skyGrad.addColorStop(1.0, '#020408')
  ctx.fillStyle = skyGrad
  ctx.fillRect(0, 0, width, height)

  // Distant city light points at horizon
  for (let i = 0; i < 140; i++) {
    const x = Math.random() * width
    const y = height * 0.52 + (Math.random() - 0.5) * 20
    const rad = Math.random() * 2 + 0.8
    ctx.fillStyle = Math.random() > 0.3 ? '#ffaa44' : '#60a5fa'
    ctx.beginPath()
    ctx.arc(x, y, rad, 0, Math.PI * 2)
    ctx.fill()
  }

  // Directional moonlight reflection glint in upper hemisphere
  const moonGrad = ctx.createRadialGradient(width * 0.35, height * 0.2, 5, width * 0.35, height * 0.2, 140)
  moonGrad.addColorStop(0, 'rgba(186, 230, 253, 0.45)')
  moonGrad.addColorStop(0.5, 'rgba(56, 189, 248, 0.12)')
  moonGrad.addColorStop(1, 'rgba(2, 6, 18, 0)')
  ctx.fillStyle = moonGrad
  ctx.fillRect(0, 0, width, height * 0.5)

  const texture = new THREE.CanvasTexture(canvas)
  texture.mapping = THREE.EquirectangularReflectionMapping
  texture.needsUpdate = true
  return texture
}

/**
 * Reusable Physically Based Materials
 */
export const PBRMaterials = {
  /**
   * Architectural Curtain Wall Glass with Photorealistic Physical Properties
   */
  architecturalGlass: new THREE.MeshPhysicalMaterial({
    color: new THREE.Color('#1e293b'), // Realistic neutral dark architectural glass tint
    metalness: 0.1,
    roughness: 0.04,
    transmission: 0.88, // Photorealistic light transmission
    thickness: 0.5,
    ior: 1.52, // Index of refraction of soda-lime glass
    transparent: true,
    opacity: 0.85,
    reflectivity: 0.95,
    clearcoat: 1.0,
    clearcoatRoughness: 0.04,
  }),

  /**
   * Dark Structural Metal Frame
   */
  darkStructuralMetal: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#0f172a'),
    metalness: 0.88,
    roughness: 0.28,
  }),

  /**
   * Weathered Jordanian Limestone (Amman Citadel & Facade Cladding)
   */
  weatheredLimestone: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#b8874e'),
    metalness: 0.05,
    roughness: 0.86,
  }),

  /**
   * Bedrock & Mountain Ashlar Stone
   */
  bedrockStone: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#1e293b'),
    metalness: 0.04,
    roughness: 0.95,
  }),

  /**
   * Wet Nighttime Asphalt Highway Pavement (High Specular Sheen)
   */
  wetAsphaltRoad: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#070c16'),
    metalness: 0.35,
    roughness: 0.18,
  }),

  /**
   * Automotive Deep Metallic Charcoal Body Paint
   */
  automotiveMetallicPaint: new THREE.MeshPhysicalMaterial({
    color: new THREE.Color('#1e2530'),
    metalness: 0.92,
    roughness: 0.18,
    clearcoat: 1.0,
    clearcoatRoughness: 0.06,
  }),

  /**
   * Automotive Dark Tinted Glass
   */
  automotiveGlass: new THREE.MeshPhysicalMaterial({
    color: new THREE.Color('#090d16'),
    metalness: 0.1,
    roughness: 0.02,
    transmission: 0.65,
    thickness: 0.3,
    transparent: true,
    opacity: 0.92,
  }),

  /**
   * Continuous Red LED Taillight Bar
   */
  glowingRedTaillight: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#ef4444'),
    emissive: new THREE.Color('#ef4444'),
    emissiveIntensity: 3.5,
    roughness: 0.1,
  }),

  /**
   * Architectural Cyan Neon Accent
   */
  glowingCyanNeon: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#00e5ff'),
    emissive: new THREE.Color('#00e5ff'),
    emissiveIntensity: 2.2,
    roughness: 0.1,
  }),

  /**
   * Warm Amber Glow (3000K Interior & Archaeological Lights)
   */
  glowingWarmAmber: new THREE.MeshStandardMaterial({
    color: new THREE.Color('#f59e0b'),
    emissive: new THREE.Color('#f59e0b'),
    emissiveIntensity: 2.6,
    roughness: 0.2,
  }),
}
