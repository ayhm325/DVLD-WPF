import { useRef, useEffect } from 'react'
import { PerspectiveCamera } from '@react-three/drei'
import { useThree } from '@react-three/fiber'
import type * as THREE from 'three'

type SceneCameraProps = {
  fov?: number
  position?: [number, number, number]
  target?: [number, number, number]
}

/**
 * Cinematic Perspective Camera with non-distorting 38-degree FOV.
 * Positioned to keep the architecture & car centered-left while leaving
 * the right quadrant open for the HTML glass login card.
 */
export default function SceneCamera({
  fov = 38,
  position = [0.25, 1.8, 7.5],
  target = [-0.25, 1.35, 0],
}: SceneCameraProps) {
  const { size } = useThree()
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null)

  useEffect(() => {
    if (cameraRef.current) {
      cameraRef.current.position.set(...position)
      cameraRef.current.lookAt(target[0], target[1], target[2])
      cameraRef.current.updateProjectionMatrix()
    }
  }, [position, target, fov])

  // Responsive FOV adjustment for laptop/tablet aspect ratios
  useEffect(() => {
    const aspect = size.width / size.height
    if (cameraRef.current) {
      cameraRef.current.fov = aspect < 1.6 ? fov + 4 : fov
      cameraRef.current.updateProjectionMatrix()
    }
  }, [size, fov])

  return (
    <PerspectiveCamera
      ref={cameraRef}
      makeDefault
      fov={fov}
      near={0.1}
      far={120}
      position={position}
    />
  )
}
