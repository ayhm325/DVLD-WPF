import { useRef } from 'react'
import type * as THREE from 'three'

type SceneLightingProps = {
  enableShadows?: boolean
}

/**
 * Professional Multi-Temperature Nighttime Architectural Lighting Rig.
 * Accurately contrasts cool midnight blue and electric cyan against warm amber 3000K.
 */
export default function SceneLighting({ enableShadows = true }: SceneLightingProps) {
  const dirLightRef = useRef<THREE.DirectionalLight | null>(null)

  return (
    <group name="scene-lighting-rig">
      {/* 1. Deep Midnight Blue Ambient Fill */}
      <ambientLight color="#071328" intensity={0.52} />

      {/* 2. Directional Distant Moonlight (Soft Cool Key) */}
      <directionalLight
        ref={dirLightRef}
        position={[-5, 12, 7]}
        intensity={0.65}
        color="#93c5fd"
        castShadow={enableShadows}
        shadow-mapSize-width={1024}
        shadow-mapSize-height={1024}
        shadow-camera-near={1}
        shadow-camera-far={32}
        shadow-camera-left={-10}
        shadow-camera-right={10}
        shadow-camera-top={10}
        shadow-camera-bottom={-5}
        shadow-bias={-0.0005}
      />

      {/* 3. Warm Interior Atrium Glow (3000K Interior Floodlight) */}
      <pointLight
        position={[-0.4, 2.0, -3.2]}
        intensity={3.2}
        distance={8.0}
        decay={2}
        color="#fef08a"
      />

      {/* 4. Ground-Mounted Warm Amber Ruins Floodlight (Temple of Hercules) */}
      <spotLight
        position={[2.4, 0.4, -3.0]}
        target-position={[2.4, 3.2, -5.2]}
        intensity={3.8}
        distance={10}
        angle={Math.PI / 3.8}
        penumbra={0.65}
        decay={1.8}
        color="#f59e0b"
        castShadow={enableShadows}
      />

      {/* 5. Architectural Cyan Edge Uplight */}
      <pointLight
        position={[-2.8, 0.4, -1.8]}
        intensity={1.2}
        distance={4.5}
        decay={2}
        color="#00e5ff"
      />
    </group>
  )
}
