import { useRef, useMemo, useCallback } from 'react'
import * as THREE from 'three'
import { GLTFModel } from '../loaders/ModelLoader'
import { PBRMaterials, getLimestonePBRTextures } from '../materials/PBRMaterials'

type HeadquartersModelProps = {
  position?: [number, number, number]
  rotation?: [number, number, number]
  scale?: number | [number, number, number]
}

/**
 * Procedural Fallback Mesh for DVLD Headquarters
 * Rendered instantly while GLB loads or if network fails
 */
function HeadquartersFallback() {
  return (
    <group>
      {/* Structural Back Wall */}
      <mesh position={[0, 2.0, -1.8]} castShadow receiveShadow>
        <boxGeometry args={[5.4, 4.0, 0.25]} />
        <meshStandardMaterial color="#0f172a" roughness={0.28} metalness={0.88} />
      </mesh>
      {/* Interior Warm Illuminated Wall */}
      <mesh position={[0, 2.0, -1.65]}>
        <boxGeometry args={[5.0, 3.6, 0.06]} />
        <meshStandardMaterial color="#fff7ed" emissive="#f59e0b" emissiveIntensity={2.8} />
      </mesh>
      {/* Left Wing */}
      <mesh position={[-3.5, 1.8, -0.6]} castShadow receiveShadow>
        <boxGeometry args={[2.6, 3.6, 2.2]} />
        <meshStandardMaterial color="#334155" roughness={0.78} metalness={0.08} />
      </mesh>
      {/* Right Wing */}
      <mesh position={[3.6, 1.5, -0.6]} castShadow receiveShadow>
        <boxGeometry args={[2.8, 3.0, 2.0]} />
        <meshStandardMaterial color="#334155" roughness={0.78} metalness={0.08} />
      </mesh>
      {/* Glass Curtain Wall */}
      <mesh position={[0, 1.95, 0.6]}>
        <boxGeometry args={[4.8, 3.7, 0.05]} />
        <primitive object={PBRMaterials.architecturalGlass} attach="material" />
      </mesh>
    </group>
  )
}

/**
 * DVLD Headquarters 3D Architectural Model Component
 * Integrates the high-fidelity GLTF asset with signage textures, PBR maps, and real-time architectural lighting
 */
export function HeadquartersModel({
  position = [-0.45, 0, -4.0],
  rotation = [0, 0.14, 0],
  scale = 0.92,
}: HeadquartersModelProps) {
  const groupRef = useRef<THREE.Group>(null)

  // Attach PBR Textures to Loaded GLTF Scene
  const handleSceneReady = useCallback((group: THREE.Group) => {
    const limestoneTex = getLimestonePBRTextures()

    group.traverse((child) => {
      if ((child as THREE.Mesh).isMesh) {
        const mesh = child as THREE.Mesh
        const mat = mesh.material as THREE.MeshStandardMaterial
        if (mat) {
          if (mat.name === 'LimestonePanel' && limestoneTex) {
            mat.map = limestoneTex.map
            mat.normalMap = limestoneTex.normalMap
            mat.roughnessMap = limestoneTex.roughnessMap
            mat.needsUpdate = true
          }
        }
      }
    })
  }, [])

  // 1. High-Resolution "DVLD" Parapet Sign Texture
  const dvldSignTexture = useMemo(() => {
    if (typeof document === 'undefined') return null
    const canvas = document.createElement('canvas')
    canvas.width = 512
    canvas.height = 180
    const ctx = canvas.getContext('2d')
    if (!ctx) return null

    // Deep plate background
    ctx.fillStyle = '#060a14'
    ctx.fillRect(0, 0, 512, 180)

    // Cyan glowing outer rim
    ctx.strokeStyle = '#00e5ff'
    ctx.lineWidth = 5
    ctx.strokeRect(6, 6, 500, 168)

    // Bold typography
    ctx.fillStyle = '#ffffff'
    ctx.font = '900 96px system-ui, -apple-system, sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.shadowColor = '#00e5ff'
    ctx.shadowBlur = 24
    ctx.fillText('DVLD', 256, 90)

    const tex = new THREE.CanvasTexture(canvas)
    tex.needsUpdate = true
    return tex
  }, [])

  // 2. High-Resolution Arabic Department Sign Texture
  const arabicSignTexture = useMemo(() => {
    if (typeof document === 'undefined') return null
    const canvas = document.createElement('canvas')
    canvas.width = 1024
    canvas.height = 180
    const ctx = canvas.getContext('2d')
    if (!ctx) return null

    // Deep plate background
    ctx.fillStyle = '#060a14'
    ctx.fillRect(0, 0, 1024, 180)

    // Cyan border line
    ctx.strokeStyle = '#38bdf8'
    ctx.lineWidth = 4
    ctx.strokeRect(6, 6, 1012, 168)

    // Arabic Typography
    ctx.fillStyle = '#f8fafc'
    ctx.font = 'bold 52px "Segoe UI", Tahoma, Arial, sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.shadowColor = '#00e5ff'
    ctx.shadowBlur = 20
    ctx.fillText('دائرة ترخيص السواقين والمركبات', 512, 90)

    const tex = new THREE.CanvasTexture(canvas)
    tex.needsUpdate = true
    return tex
  }, [])

  // 3. Official Shield Emblem Texture for Left Wing Tower
  const shieldTexture = useMemo(() => {
    if (typeof document === 'undefined') return null
    const canvas = document.createElement('canvas')
    canvas.width = 256
    canvas.height = 256
    const ctx = canvas.getContext('2d')
    if (!ctx) return null

    ctx.fillStyle = '#060a14'
    ctx.fillRect(0, 0, 256, 256)

    ctx.save()
    ctx.strokeStyle = '#38bdf8'
    ctx.lineWidth = 6
    ctx.shadowColor = '#00e5ff'
    ctx.shadowBlur = 18
    ctx.beginPath()
    ctx.moveTo(128, 36)
    ctx.lineTo(204, 68)
    ctx.lineTo(194, 172)
    ctx.quadraticCurveTo(128, 230, 128, 230)
    ctx.quadraticCurveTo(62, 172, 52, 172)
    ctx.lineTo(52, 68)
    ctx.closePath()
    ctx.stroke()

    ctx.fillStyle = '#00e5ff'
    ctx.beginPath()
    ctx.arc(128, 130, 36, 0, Math.PI * 2)
    ctx.fill()
    ctx.restore()

    const tex = new THREE.CanvasTexture(canvas)
    tex.needsUpdate = true
    return tex
  }, [])

  return (
    <group ref={groupRef} position={position} rotation={rotation} scale={scale}>
      {/* 3D GLTF Headquarters Asset with Dynamic PBR Enhancements */}
      <GLTFModel
        url="/models/headquarters/headquarters.glb"
        fallback={<HeadquartersFallback />}
        onSceneReady={handleSceneReady}
        castShadow
        receiveShadow
      />

      {/* --- High-Definition Signage Planes --- */}
      {/* 1. Main Parapet "DVLD" Illuminated Sign */}
      {dvldSignTexture && (
        <mesh position={[0, 4.38, 0.81]} castShadow={false}>
          <planeGeometry args={[1.9, 0.54]} />
          <meshBasicMaterial map={dvldSignTexture} transparent />
        </mesh>
      )}

      {/* 2. Right Wing Arabic Department Sign */}
      {arabicSignTexture && (
        <mesh position={[3.6, 2.35, 0.56]} castShadow={false}>
          <planeGeometry args={[2.1, 0.38]} />
          <meshBasicMaterial map={arabicSignTexture} transparent />
        </mesh>
      )}

      {/* 3. Left Wing Tower Official Shield Crest */}
      {shieldTexture && (
        <mesh position={[-3.5, 2.5, 0.52]} castShadow={false}>
          <planeGeometry args={[0.82, 0.82]} />
          <meshBasicMaterial map={shieldTexture} transparent />
        </mesh>
      )}

      {/* --- Specialized Architectural Lighting Rig --- */}
      {/* Warm Interior Reception Atrium (3000K) illuminating through the glass */}
      <pointLight
        position={[0, 2.0, -0.3]}
        color="#ffaa44"
        intensity={3.6}
        distance={8.5}
        decay={2}
        castShadow={false}
      />

      {/* Cantilever Entrance Canopy Downlights */}
      <pointLight
        position={[-0.8, 1.3, 1.3]}
        color="#fff5eb"
        intensity={1.8}
        distance={3.8}
        decay={2}
      />
      <pointLight
        position={[0.8, 1.3, 1.3]}
        color="#fff5eb"
        intensity={1.8}
        distance={3.8}
        decay={2}
      />

      {/* DVLD Rooftop Sign Cyan Wash */}
      <pointLight
        position={[0, 4.5, 1.0]}
        color="#38bdf8"
        intensity={2.0}
        distance={3.6}
        decay={2}
      />

      {/* Right Wing Arabic Sign Cyan Backlight */}
      <pointLight
        position={[3.6, 2.4, 0.7]}
        color="#00e5ff"
        intensity={1.5}
        distance={3.0}
        decay={2}
      />
    </group>
  )
}

export default HeadquartersModel
