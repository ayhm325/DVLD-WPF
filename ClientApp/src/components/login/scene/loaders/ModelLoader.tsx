import { Suspense, Component, useMemo } from 'react'
import type { ReactNode } from 'react'
import { useGLTF } from '@react-three/drei'
import * as THREE from 'three'

type ErrorBoundaryProps = {
  fallback?: ReactNode
  children: ReactNode
}

type ErrorBoundaryState = {
  hasError: boolean
  error?: Error
}

export class ModelErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error) {
    console.error('[3D ModelLoader Catch]:', error.message, error.stack)
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback ?? null
    }
    return this.props.children
  }
}

type GLTFModelProps = {
  url: string
  position?: [number, number, number]
  rotation?: [number, number, number]
  scale?: number | [number, number, number]
  castShadow?: boolean
  receiveShadow?: boolean
  onSceneReady?: (scene: THREE.Group) => void
}

function InnerGLTFModel({
  url,
  position = [0, 0, 0],
  rotation = [0, 0, 0],
  scale = 1,
  castShadow = true,
  receiveShadow = true,
  onSceneReady,
}: GLTFModelProps) {
  const gltf = useGLTF(url)
  const scene = gltf.scene

  const clonedScene = useMemo(() => {
    try {
      const clone = scene.clone(true)
      clone.traverse((child) => {
        if ((child as THREE.Mesh).isMesh) {
          child.castShadow = castShadow
          child.receiveShadow = receiveShadow
        }
      })
      if (onSceneReady) {
        onSceneReady(clone)
      }
      return clone
    } catch (err: unknown) {
      console.error('[InnerGLTFModel clonedScene Error]:', (err as Error).message, (err as Error).stack)
      throw err
    }
  }, [scene, castShadow, receiveShadow, onSceneReady])

  return (
    <primitive
      object={clonedScene}
      position={position}
      rotation={rotation}
      scale={scale}
    />
  )
}

/**
 * Reusable robust GLTF/GLB loader with Suspense and ErrorBoundary protection
 */
export function GLTFModel(props: GLTFModelProps & { fallback?: ReactNode }) {
  return (
    <ModelErrorBoundary fallback={props.fallback}>
      <Suspense fallback={props.fallback ?? null}>
        <InnerGLTFModel {...props} />
      </Suspense>
    </ModelErrorBoundary>
  )
}
