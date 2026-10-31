import { Canvas } from '@react-three/fiber'
import * as THREE from 'three'
import SceneCamera from './SceneCamera'
import SceneLighting from './SceneLighting'
import SceneEnvironment from './SceneEnvironment'
import Login3DContent from './models/Login3DContent'

type SceneCanvasProps = {
  className?: string
  enableShadows?: boolean
}

/**
 * Full-Screen Responsive 3D Three.js Canvas.
 * Clamps DPR to maximum 2.0 to protect GPU performance,
 * enables ACESFilmicToneMapping for cinematic dynamic range,
 * and passes pointer events through to underlying and overlying UI elements.
 */
export default function SceneCanvas({
  className = '',
  enableShadows = true,
}: SceneCanvasProps) {
  return (
    <div
      className={`scene-canvas-container absolute inset-0 w-full h-full pointer-events-none select-none z-0 ${className}`}
      aria-hidden="true"
    >
      <Canvas
        shadows={enableShadows}
        dpr={[1, Math.min(typeof window !== 'undefined' ? window.devicePixelRatio : 1, 2)]}
        gl={{
          antialias: true,
          alpha: true,
          powerPreference: 'high-performance',
          toneMapping: THREE.ACESFilmicToneMapping,
          toneMappingExposure: 1.15,
        }}
        camera={{ position: [0.25, 1.8, 7.5], fov: 38 }}
      >
        <SceneCamera fov={38} />
        <SceneLighting enableShadows={enableShadows} />
        <SceneEnvironment />
        <Login3DContent />
      </Canvas>
    </div>
  )
}
