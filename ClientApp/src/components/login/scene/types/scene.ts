export type SceneQuality = 'low' | 'medium' | 'high'

export type CameraPreset = {
  position: [number, number, number]
  target: [number, number, number]
  fov: number
}

export type LightRigConfig = {
  ambientIntensity: number
  moonlightIntensity: number
  architecturalCyanIntensity: number
  citadelAmberIntensity: number
  enableShadows: boolean
}
