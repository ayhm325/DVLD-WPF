import { useMemo } from 'react'
import { generateNightEnvironmentTexture } from './materials/PBRMaterials'

/**
 * Deterministic pseudo-random number generator for pure idempotent renders
 */
function pseudoRandom(seed: number): number {
  const x = Math.sin(seed * 12.9898 + 78.233) * 43758.5453123
  return x - Math.floor(x)
}

/**
 * 3D Scene Environment providing atmospheric fog, declarative radiosity
 * environment map for physically based PBR reflections, and celestial stars.
 */
export default function SceneEnvironment() {
  // Pure declarative equirectangular environment texture
  const envTexture = useMemo(() => {
    return generateNightEnvironmentTexture()
  }, [])

  // Pure, deterministic 3D star points on celestial hemisphere
  const [starPositions, starColors] = useMemo(() => {
    const count = 180
    const positions = new Float32Array(count * 3)
    const colors = new Float32Array(count * 3)

    for (let i = 0; i < count; i++) {
      const theta = pseudoRandom(i * 3 + 1) * Math.PI * 2
      const phi = Math.acos(pseudoRandom(i * 3 + 2) * 0.8 + 0.2)
      const radius = 42 + pseudoRandom(i * 3 + 3) * 10

      positions[i * 3] = radius * Math.sin(phi) * Math.cos(theta)
      positions[i * 3 + 1] = radius * Math.cos(phi)
      positions[i * 3 + 2] = -Math.abs(radius * Math.sin(phi) * Math.sin(theta))

      const isWarm = (i % 5) === 0
      colors[i * 3] = isWarm ? 1.0 : 0.88
      colors[i * 3 + 1] = isWarm ? 0.9 : 0.95
      colors[i * 3 + 2] = isWarm ? 0.6 : 1.0
    }

    return [positions, colors]
  }, [])

  return (
    <group name="scene-environment">
      {/* 1. Declarative PBR Specular Environment Reflection Map */}
      {envTexture && <primitive object={envTexture} attach="environment" />}

      {/* 2. Cinematic Atmospheric Night Fog */}
      <fogExp2 attach="fog" args={['#020612', 0.026]} />

      {/* 3. Celestial 3D Starfield Points */}
      <points>
        <bufferGeometry>
          <bufferAttribute
            attach="attributes-position"
            args={[starPositions, 3]}
          />
          <bufferAttribute
            attach="attributes-color"
            args={[starColors, 3]}
          />
        </bufferGeometry>
        <pointsMaterial
          size={0.16}
          vertexColors
          transparent
          opacity={0.78}
          sizeAttenuation
        />
      </points>
    </group>
  )
}
