import { useState, Component } from 'react'
import type { ReactNode } from 'react'
import SceneCanvas from './SceneCanvas'
import CinematicEnvironment from '../CinematicEnvironment'

/**
 * WebGL Capability Detection Helper
 */
function isWebGLAvailable(): boolean {
  if (typeof window === 'undefined') return false
  try {
    const canvas = document.createElement('canvas')
    return Boolean(
      window.WebGLRenderingContext &&
        (canvas.getContext('webgl2') || canvas.getContext('webgl') || canvas.getContext('experimental-webgl'))
    )
  } catch {
    return false
  }
}

type SceneErrorBoundaryProps = {
  fallback: ReactNode
  children: ReactNode
}

type SceneErrorBoundaryState = {
  hasError: boolean
}

class SceneErrorBoundary extends Component<SceneErrorBoundaryProps, SceneErrorBoundaryState> {
  constructor(props: SceneErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(): SceneErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error) {
    console.warn('[Login3DScene] WebGL 3D scene error, falling back to SVG environment:', error.message)
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback
    }
    return this.props.children
  }
}

type Login3DSceneProps = {
  forceSvgFallback?: boolean
}

/**
 * Master 3D Scene Controller for the DVLD Login Environment.
 * Detects WebGL capability, respects prefers-reduced-motion,
 * handles runtime rendering errors gracefully, and falls back to the SVG environment
 * seamlessly if WebGL is unavailable.
 */
export default function Login3DScene({ forceSvgFallback = false }: Login3DSceneProps) {
  // Synchronous initialization prevents cascading re-renders in effects
  const [webglSupported] = useState<boolean>(() => isWebGLAvailable())

  // If WebGL is unavailable or SVG fallback is requested, render the verified SVG environment
  if (forceSvgFallback || !webglSupported) {
    return <CinematicEnvironment />
  }

  return (
    <SceneErrorBoundary fallback={<CinematicEnvironment />}>
      <div className="login-3d-scene-wrapper absolute inset-0 w-full h-full pointer-events-none select-none z-0 overflow-hidden">
        {/* Render 3D Scene Canvas */}
        <SceneCanvas enableShadows={true} />
      </div>
    </SceneErrorBoundary>
  )
}
