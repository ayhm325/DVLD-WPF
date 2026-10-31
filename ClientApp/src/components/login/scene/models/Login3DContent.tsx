import { PBRMaterials } from '../materials/PBRMaterials'
import HeadquartersModel from './HeadquartersModel'
import CitadelModel from './CitadelModel'

/**
 * Login3DContent Component
 * Assembles the upgraded photorealistic 3D environment for Phase 02.2:
 * 1. High-fidelity DVLD Headquarters Model with PBR Textures & Architectural Lighting
 * 2. High-fidelity Amman Citadel Model with PBR Stone Textures & Archaeological Floodlighting
 * 3. Wet asphalt highway ground plane with realistic specular sheen & reflections
 * 4. Luxury sports sedan silhouette placeholder (strictly reserved for Phase 03)
 */
export default function Login3DContent() {
  return (
    <group name="login-3d-content">
      {/* ============================================================
          1. WET ASPHALT HIGHWAY GROUND PLANE & FORECOURT
      ============================================================ */}
      <mesh
        rotation={[-Math.PI / 2, 0, 0]}
        position={[0, 0, 0]}
        receiveShadow
      >
        <planeGeometry args={[50, 40]} />
        <primitive object={PBRMaterials.wetAsphaltRoad} attach="material" />
      </mesh>

      {/* Paved Entrance Forecourt Platform */}
      <mesh
        position={[-0.45, 0.02, -2.4]}
        rotation={[-Math.PI / 2, 0, 0]}
        receiveShadow
      >
        <planeGeometry args={[14, 6]} />
        <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
      </mesh>

      {/* Wet Road Roadside Curb with Restrained Cyan Neon Accent */}
      <mesh position={[1.4, 0.05, 0]} rotation={[0, -0.24, 0]}>
        <boxGeometry args={[0.08, 0.05, 16]} />
        <meshStandardMaterial color="#00e5ff" emissive="#00e5ff" emissiveIntensity={0.6} />
      </mesh>

      {/* ============================================================
          2. DVLD HEADQUARTERS ARCHITECTURAL MODEL
             Positioned in central-left focal zone: x: -0.45, y: 0, z: -4.0
             Angled at 8 degrees (0.14 rad) for balanced 3-quarter view
      ============================================================ */}
      <HeadquartersModel
        position={[-0.45, 0, -4.0]}
        rotation={[0, 0.14, 0]}
        scale={0.92}
      />

      {/* ============================================================
          3. AMMAN CITADEL & TEMPLE OF HERCULES MODEL
             Positioned on elevated background ridge: x: 2.3, y: 0.1, z: -5.2
             Behind the highway and building, framed in warm amber floodlights
      ============================================================ */}
      <CitadelModel
        position={[2.3, 0.1, -5.2]}
        rotation={[0, -0.2, 0]}
        scale={0.68}
      />

      {/* ============================================================
          4. LUXURY SPORTS PERFORMANCE SEDAN SILHOUETTE
             (Reserved placeholder for Phase 03 - maintained in foreground)
      ============================================================ */}
      <group position={[-0.75, 0, 1.0]} rotation={[0, 0.12, 0]} scale={0.72}>
        {/* Wheels (Left & Right Rear Tires) */}
        <mesh position={[-0.78, 0.26, -0.35]} rotation={[0, 0, Math.PI / 2]} castShadow>
          <cylinderGeometry args={[0.26, 0.26, 0.22, 16]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>
        <mesh position={[0.78, 0.26, -0.35]} rotation={[0, 0, Math.PI / 2]} castShadow>
          <cylinderGeometry args={[0.26, 0.26, 0.22, 16]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>

        {/* Lower Bumper & Aerodynamic Diffuser */}
        <mesh position={[0, 0.2, 0.18]} castShadow>
          <boxGeometry args={[1.65, 0.22, 0.75]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>

        {/* Main Body Shell (Metallic Charcoal PBR Paint) */}
        <mesh position={[0, 0.48, -0.2]} castShadow>
          <boxGeometry args={[1.68, 0.38, 2.2]} />
          <primitive object={PBRMaterials.automotiveMetallicPaint} attach="material" />
        </mesh>

        {/* Sloping Fastback Cabin & Rear Glass */}
        <mesh position={[0, 0.78, -0.52]} rotation={[-0.32, 0, 0]} castShadow>
          <boxGeometry args={[1.3, 0.35, 1.3]} />
          <primitive object={PBRMaterials.automotiveGlass} attach="material" />
        </mesh>

        {/* Continuous Horizontal Red LED Taillight Bar */}
        <mesh position={[0, 0.54, 0.91]}>
          <boxGeometry args={[1.58, 0.06, 0.04]} />
          <primitive object={PBRMaterials.glowingRedTaillight} attach="material" />
        </mesh>

        {/* Red Taillight Road Reflection Glow */}
        <pointLight
          position={[0, 0.2, 1.1]}
          color="#ef4444"
          intensity={1.8}
          distance={2.8}
          decay={2}
        />

        {/* Jordanian License Plate */}
        <mesh position={[0, 0.32, 0.92]}>
          <boxGeometry args={[0.44, 0.15, 0.02]} />
          <meshStandardMaterial color="#f8fafc" roughness={0.3} metalness={0.1} />
        </mesh>
      </group>
    </group>
  )
}
