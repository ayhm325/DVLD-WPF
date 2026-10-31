import { PBRMaterials } from '../materials/PBRMaterials'

/**
 * Technical 3D Visual Prototype demonstrating:
 * 1. PBR architectural glass & structural dark metal curtain-wall
 * 2. Weathered limestone Roman fluted column (Amman Citadel)
 * 3. Wet asphalt highway ground receiving dynamic shadows and reflections
 * 4. Luxury sedan silhouette with automotive metallic paint & continuous red LED taillight bar
 * Carefully composed in 3D world space to guarantee complete visual clearance
 * from the left branding (x < -2.6) and right authentication card (x > 1.8).
 */
export default function Prototype3DScene() {
  return (
    <group name="prototype-3d-scene">
      {/* ============================================================
          1. WET ASPHALT HIGHWAY GROUND PLANE
      ============================================================ */}
      <mesh
        rotation={[-Math.PI / 2, 0, 0]}
        position={[0, 0, 0]}
        receiveShadow
      >
        <planeGeometry args={[50, 40]} />
        <primitive object={PBRMaterials.wetAsphaltRoad} attach="material" />
      </mesh>

      {/* Paved Entrance Forecourt (Subtle Step) */}
      <mesh
        position={[-0.1, 0.02, -1.8]}
        rotation={[-Math.PI / 2, 0, 0]}
        receiveShadow
      >
        <planeGeometry args={[12, 5]} />
        <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
      </mesh>

      {/* ============================================================
          2. MODERN ARCHITECTURAL PAVILION (DVLD HQ PROTOTYPE)
             Positioned in central focal zone at x: -0.1, y: 0, z: -3.0
             Left edge stays strictly at x > -2.6 to prevent any left branding collision
      ============================================================ */}
      <group position={[-0.1, 0, -3.0]}>
        {/* Main Building Limestone Mass */}
        <mesh position={[0, 1.8, -0.6]} castShadow receiveShadow>
          <boxGeometry args={[4.8, 3.6, 2.0]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>

        {/* 2-Story Curtain Wall Glass Facade */}
        <mesh position={[0, 1.8, 0.42]}>
          <boxGeometry args={[4.2, 3.2, 0.06]} />
          <primitive object={PBRMaterials.architecturalGlass} attach="material" />
        </mesh>

        {/* Structural Steel Mullions (Vertical Grid) */}
        {[-1.6, -0.8, 0, 0.8, 1.6].map((mx) => (
          <mesh key={`mullion-v-${mx}`} position={[mx, 1.8, 0.46]} castShadow>
            <boxGeometry args={[0.07, 3.3, 0.1]} />
            <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
          </mesh>
        ))}

        {/* Structural Transoms (Horizontal Mezzanine Slab) */}
        <mesh position={[0, 1.6, 0.46]} castShadow>
          <boxGeometry args={[4.2, 0.1, 0.1]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>

        {/* Interior Mezzanine Floor Slab with Warm Interior Illumination */}
        <mesh position={[0, 1.55, -0.2]}>
          <boxGeometry args={[4.0, 0.08, 1.3]} />
          <primitive object={PBRMaterials.glowingWarmAmber} attach="material" />
        </mesh>

        {/* Cantilevered Steel Entrance Canopy */}
        <mesh position={[0, 1.35, 0.85]} castShadow>
          <boxGeometry args={[2.6, 0.08, 0.8]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>
        {/* Canopy Neon Front Edge */}
        <mesh position={[0, 1.35, 1.26]}>
          <boxGeometry args={[2.6, 0.03, 0.02]} />
          <primitive object={PBRMaterials.glowingCyanNeon} attach="material" />
        </mesh>

        {/* Integrated 3D "DVLD" Parapet Sign Block */}
        <mesh position={[0, 3.7, 0.45]} castShadow>
          <boxGeometry args={[1.8, 0.65, 0.2]} />
          <primitive object={PBRMaterials.darkStructuralMetal} attach="material" />
        </mesh>
        {/* Illuminated Sign Face */}
        <mesh position={[0, 3.7, 0.56]}>
          <boxGeometry args={[1.65, 0.5, 0.04]} />
          <primitive object={PBRMaterials.glowingCyanNeon} attach="material" />
        </mesh>

        {/* Architectural Neon Roofline Trim */}
        <mesh position={[0, 3.65, 0.42]}>
          <boxGeometry args={[4.8, 0.05, 0.05]} />
          <primitive object={PBRMaterials.glowingCyanNeon} attach="material" />
        </mesh>
      </group>

      {/* ============================================================
          3. AMMAN CITADEL — CLASSICAL ROMAN COLONNADE ELEMENT
             Positioned on Background Mountain at x: 2.3, y: 0 to 3.5, z: -3.8
      ============================================================ */}
      <group position={[2.3, 0, -3.8]}>
        {/* Stepped Bedrock Podium (Ashlar Limestone Foundation) */}
        <mesh position={[0, 0.25, 0]} castShadow receiveShadow>
          <boxGeometry args={[3.2, 0.5, 2.0]} />
          <primitive object={PBRMaterials.bedrockStone} attach="material" />
        </mesh>
        <mesh position={[0, 0.6, 0]} castShadow receiveShadow>
          <boxGeometry args={[2.8, 0.2, 1.6]} />
          <primitive object={PBRMaterials.weatheredLimestone} attach="material" />
        </mesh>

        {/* Roman Fluted Columns (Temple of Hercules Prototype) */}
        {[-0.9, 0, 0.9].map((cx, i) => (
          <group key={`roman-col-${cx}`} position={[cx, 0.7, 0]}>
            {/* Column Attic Base */}
            <mesh position={[0, 0.08, 0]} castShadow>
              <cylinderGeometry args={[0.24, 0.28, 0.16, 16]} />
              <primitive object={PBRMaterials.weatheredLimestone} attach="material" />
            </mesh>

            {/* Cylindrical Fluted Column Shaft */}
            <mesh position={[0, 1.15, 0]} castShadow>
              <cylinderGeometry args={[0.18, 0.21, i === 2 ? 1.4 : 2.0, 16]} />
              <primitive object={PBRMaterials.weatheredLimestone} attach="material" />
            </mesh>

            {/* Corinthian Capital (Intact on Col 0 & 1, broken on Col 2) */}
            {i !== 2 && (
              <mesh position={[0, 2.25, 0]} castShadow>
                <boxGeometry args={[0.45, 0.22, 0.45]} />
                <primitive object={PBRMaterials.weatheredLimestone} attach="material" />
              </mesh>
            )}
          </group>
        ))}

        {/* Intact Architrave Stone Entablature Beam across Col 0 and 1 */}
        <mesh position={[-0.45, 3.1, 0]} castShadow>
          <boxGeometry args={[1.8, 0.26, 0.45]} />
          <primitive object={PBRMaterials.weatheredLimestone} attach="material" />
        </mesh>

        {/* Fallen Ancient Ashlar Block on Bedrock */}
        <mesh position={[1.2, 0.2, 0.5]} rotation={[0, 0.4, 0.1]} castShadow>
          <boxGeometry args={[0.45, 0.26, 0.35]} />
          <primitive object={PBRMaterials.weatheredLimestone} attach="material" />
        </mesh>
      </group>

      {/* ============================================================
          4. LUXURY SPORTS PERFORMANCE SEDAN SILHOUETTE
             Positioned in Center Lane at x: -0.2, y: 0 to 0.95, z: 1.4
      ============================================================ */}
      <group position={[-0.2, 0, 1.4]}>
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

        {/* Jordanian License Plate (White Reflective Surface) */}
        <mesh position={[0, 0.32, 0.92]}>
          <boxGeometry args={[0.44, 0.15, 0.02]} />
          <meshStandardMaterial color="#f8fafc" roughness={0.3} metalness={0.1} />
        </mesh>
      </group>
    </group>
  )
}
