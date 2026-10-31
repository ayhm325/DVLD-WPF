import { useRef, useCallback } from 'react'
import * as THREE from 'three'
import { GLTFModel } from '../loaders/ModelLoader'
import { getLimestonePBRTextures } from '../materials/PBRMaterials'

type CitadelModelProps = {
  position?: [number, number, number]
  rotation?: [number, number, number]
  scale?: number | [number, number, number]
}

/**
 * Procedural Fallback Mesh for Amman Citadel
 * Rendered instantly while GLB loads or if network fails
 */
function CitadelFallback() {
  return (
    <group>
      {/* Mountain Ridge Base */}
      <mesh position={[0, 0.9, -0.6]} castShadow receiveShadow>
        <boxGeometry args={[8.5, 1.8, 5.0]} />
        <meshStandardMaterial color="#1e293b" roughness={0.95} metalness={0.04} />
      </mesh>
      {/* Stylobate Podium */}
      <mesh position={[0, 2.6, 0]} castShadow receiveShadow>
        <boxGeometry args={[4.8, 0.18, 2.4]} />
        <meshStandardMaterial color="#b8874e" roughness={0.86} metalness={0.05} />
      </mesh>
      {/* Fallback Columns */}
      {[-1.75, -1.05, -0.35, 0.35, 1.05, 1.75].map((x, i) => (
        <mesh key={i} position={[x, 4.0, 0]} castShadow>
          <cylinderGeometry args={[0.21, 0.23, i >= 4 ? 1.4 : 2.6, 16]} />
          <meshStandardMaterial color="#b8874e" roughness={0.86} metalness={0.05} />
        </mesh>
      ))}
    </group>
  )
}

/**
 * Amman Citadel & Temple of Hercules 3D Model Component
 * Integrates the high-fidelity GLTF asset with dramatic warm amber archaeological floodlights
 */
export function CitadelModel({
  position = [2.4, 0.1, -5.2],
  rotation = [0, -0.22, 0],
  scale = 0.65,
}: CitadelModelProps) {
  const groupRef = useRef<THREE.Group>(null)

  // Attach PBR Textures to Citadel Limestone & Archaeological Remains
  const handleSceneReady = useCallback((group: THREE.Group) => {
    const limestoneTex = getLimestonePBRTextures()

    group.traverse((child) => {
      if ((child as THREE.Mesh).isMesh) {
        const mesh = child as THREE.Mesh
        const mat = mesh.material as THREE.MeshStandardMaterial
        if (mat && limestoneTex) {
          if (mat.name === 'AncientLimestone' || mat.name === 'AncientLimestoneDark') {
            mat.map = limestoneTex.map
            mat.normalMap = limestoneTex.normalMap
            mat.roughnessMap = limestoneTex.roughnessMap
            mat.needsUpdate = true
          }
        }
      }
    })
  }, [])

  return (
    <group ref={groupRef} position={position} rotation={rotation} scale={scale}>
      {/* 3D GLTF Citadel Asset */}
      <GLTFModel
        url="/models/citadel/citadel.glb"
        fallback={<CitadelFallback />}
        onSceneReady={handleSceneReady}
        castShadow
        receiveShadow
      />

      {/* --- Ancient Colonnade Archaeological Floodlighting (3200K Warm Amber) --- */}
      {/* Left Colonnade & Architrave Beam Upward Floodlight */}
      <pointLight
        position={[-1.0, 3.8, 1.4]}
        color="#fbbf24"
        intensity={4.0}
        distance={9.0}
        decay={2}
        castShadow
      />

      {/* Right Fractured Ruin Columns Floodlight */}
      <pointLight
        position={[1.2, 3.2, 1.4]}
        color="#f59e0b"
        intensity={3.5}
        distance={8.5}
        decay={2}
        castShadow
      />

      {/* Stepped Stylobate Podium & Mountain Bedrock Wash */}
      <pointLight
        position={[0, 1.8, 2.0]}
        color="#d97706"
        intensity={2.4}
        distance={7.0}
        decay={2}
      />
    </group>
  )
}

export default CitadelModel
