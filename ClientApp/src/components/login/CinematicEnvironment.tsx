import { useMemo } from 'react'
import CitadelRuins from './CitadelRuins'
import HeadquartersArchitecture from './HeadquartersArchitecture'
import VehicleAndRoad from './VehicleAndRoad'

export default function CinematicEnvironment() {
  // Realistic astronomical star field: heavily weighted toward faint stars (mag 4-6) with few bright pinpricks
  const stars = useMemo(() => {
    return Array.from({ length: 130 }).map((_, i) => {
      // Clustered pseudorandom distribution avoiding uniform grid
      const x = (i * 23.71 + (i % 7) * 11.3) % 100
      const y = ((i * 19.33 + (i % 5) * 8.7) % 52) // Confined to upper sky (0% - 52%)
      const isProminent = i % 19 === 0
      const isMedium = !isProminent && i % 4 === 0
      const size = isProminent ? 2 : isMedium ? 1.4 : 0.9
      const opacity = isProminent ? 0.9 : isMedium ? 0.6 : 0.2 + ((i * 9) % 40) / 100
      const twinkleSpeed = 3 + ((i * 13) % 7) * 0.7

      return {
        id: i,
        x,
        y,
        size,
        opacity,
        twinkleSpeed,
        isProminent,
      }
    })
  }, [])

  // Sprawling nighttime panorama of Amman hillside lights (Jabal Al-Qal'a, Jabal Amman, Jabal Al-Weibdeh)
  const cityHillsideLights = useMemo(() => {
    return Array.from({ length: 90 }).map((_, i) => {
      const x = 50 + ((i * 11.23 + (i % 9) * 5.7) % 46) // Mountain horizon from center to right
      const y = 34 + ((i * 7.41 + (i % 6) * 4.3) % 20)  // Slopes of the distant Amman hills
      const colorType = i % 5
      const color =
        colorType === 0
          ? '#fde047' // Sodium warm gold
          : colorType === 1
            ? '#fbbf24' // Amber streetlight
            : colorType === 2
              ? '#f59e0b' // Deep warm incandescent
              : colorType === 3
                ? '#e0f2fe' // Cool LED window
                : '#fef08a' // Halogen gold
      const size = i % 11 === 0 ? 2 : i % 3 === 0 ? 1.5 : 1
      const opacity = 0.2 + ((i * 13) % 65) / 100

      return {
        id: i,
        x,
        y,
        color,
        size,
        opacity,
      }
    })
  }, [])

  return (
    <div
      className="cinematic-environment absolute inset-0 overflow-hidden pointer-events-none select-none z-0"
      aria-hidden="true"
    >
      {/* =====================================================
          1. SKY, STARS, NEBULA & DISTANT AMMAN CITY LIGHTS
      ===================================================== */}
      <div className="absolute inset-0 bg-[#01040a]">
        {/* Layer 1: Deep cosmic midnight-blue backdrop */}
        <div
          className="absolute inset-0"
          style={{
            background:
              'radial-gradient(1300px 750px at 45% 25%, #08152e 0%, #030a17 50%, #01040a 100%)',
          }}
        />

        {/* Layer 2: Subtle Atmospheric Blue Nebula Formations */}
        <div className="absolute -top-12 left-[12%] w-[900px] h-[550px] rounded-full bg-blue-600/12 blur-[150px]" />
        <div className="absolute top-8 left-[42%] w-[750px] h-[480px] rounded-full bg-cyan-500/10 blur-[160px]" />
        <div className="absolute top-0 right-[8%] w-[850px] h-[600px] rounded-full bg-indigo-950/30 blur-[170px]" />
        
        {/* Layer 3: Warm Ambient City Light Pollution Haze over Amman Horizon */}
        <div className="absolute top-[26%] left-[52%] w-[550px] h-[260px] rounded-full bg-amber-500/8 blur-[110px]" />
        <div className="absolute top-[32%] left-[65%] w-[420px] h-[200px] rounded-full bg-orange-500/6 blur-[90px]" />

        {/* Dynamic night sky starfield with natural astronomical variations */}
        {stars.map((star) => (
          <span
            key={star.id}
            className="absolute rounded-full bg-white transition-opacity"
            style={{
              left: `${star.x}%`,
              top: `${star.y}%`,
              width: `${star.size}px`,
              height: `${star.size}px`,
              opacity: star.opacity,
              boxShadow: star.isProminent ? '0 0 5px #e0f2fe, 0 0 10px rgba(56, 189, 248, 0.4)' : 'none',
              animation: `star-twinkle ${star.twinkleSpeed}s ease-in-out infinite alternate`,
            }}
          />
        ))}

        {/* Distant Amman hillside city night lights (twinkling mountain panorama) */}
        {cityHillsideLights.map((light) => (
          <span
            key={`amman-city-${light.id}`}
            className="absolute rounded-full"
            style={{
              left: `${light.x}%`,
              top: `${light.y}%`,
              width: `${light.size}px`,
              height: `${light.size}px`,
              backgroundColor: light.color,
              opacity: light.opacity,
              boxShadow: `0 0 3.5px ${light.color}`,
            }}
          />
        ))}
      </div>

      {/* =====================================================
          2. MASTER PANORAMIC 1920x1080 SVG SCENE
      ===================================================== */}
      <svg
        viewBox="0 0 1920 1080"
        preserveAspectRatio="xMidYMax slice"
        className="absolute inset-0 w-full h-full pointer-events-none select-none"
        xmlns="http://www.w3.org/2000/svg"
      >
        {/* Layer 1: Amman Citadel & Temple of Hercules Colonnade (Background Hillside) */}
        <CitadelRuins />

        {/* Layer 2: DVLD Modern Headquarters Complex (Midground Architecture) */}
        <HeadquartersArchitecture />

        {/* Layer 3: Wet Asphalt Plaza, Vanishing Road, Neon Curbs & Luxury Sedan (Foreground) */}
        <VehicleAndRoad />
      </svg>

      {/* =====================================================
          3. SUBTLE ATMOSPHERIC SCANLINE & GRAIN OVERLAY
      ===================================================== */}
      <div
        className="absolute inset-0 pointer-events-none opacity-[0.018]"
        style={{
          backgroundImage: 'radial-gradient(rgba(255, 255, 255, 0.8) 1px, transparent 0)',
          backgroundSize: '24px 24px',
        }}
      />
    </div>
  )
}
