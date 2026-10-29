/**
 * Decorative automotive scene for the login showcase panel.
 *
 * Pure SVG + CSS: no external assets, no animation libraries.
 * Every animated property is `transform` or `opacity`, so the scene
 * never triggers layout. Motion is disabled via `prefers-reduced-motion`
 * in Login.css, leaving a calm static composition.
 */

type CarProps = {
  /** Unique prefix so gradient ids never collide between instances. */
  id: string
  variant: 'primary' | 'secondary'
}

function Car({ id, variant }: CarProps) {
  const body = `${id}-body`
  const glass = `${id}-glass`
  const beam = `${id}-beam`
  const rim = `${id}-rim`

  const bodyStops =
    variant === 'primary'
      ? ['#60a5fa', '#2563eb', '#1e3a8a']
      : ['#e2e8f0', '#94a3b8', '#475569']

  return (
    <svg
      className="car-svg"
      viewBox="0 0 340 92"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
      focusable="false"
    >
      <defs>
        <linearGradient id={body} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={bodyStops[0]} />
          <stop offset="45%" stopColor={bodyStops[1]} />
          <stop offset="100%" stopColor={bodyStops[2]} />
        </linearGradient>
        <linearGradient id={glass} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#334155" />
          <stop offset="55%" stopColor="#0f172a" />
          <stop offset="100%" stopColor="#1e293b" />
        </linearGradient>
        <linearGradient id={beam} x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#dbeafe" stopOpacity="0.55" />
          <stop offset="100%" stopColor="#dbeafe" stopOpacity="0" />
        </linearGradient>
        <radialGradient id={rim} cx="0.5" cy="0.5" r="0.5">
          <stop offset="0%" stopColor="#e2e8f0" />
          <stop offset="70%" stopColor="#94a3b8" />
          <stop offset="100%" stopColor="#475569" />
        </radialGradient>
      </defs>

      {/* Headlight beam */}
      <polygon
        className="car-beam"
        points="230,50 340,32 340,74"
        fill={`url(#${beam})`}
      />

      {/* Ground shadow */}
      <ellipse cx="122" cy="86" rx="112" ry="5" fill="#020617" opacity="0.55" />

      {/* Body */}
      <path
        d="M14 62 C14 53 20 48 32 46 L72 40 C86 28 102 21 124 20 L150 20 C170 20 186 28 201 40 L219 44 C228 46 232 52 232 60 L232 66 C232 69 230 72 227 72 L206 72 A22 22 0 0 0 162 72 L84 72 A22 22 0 0 0 40 72 L20 72 C16 72 14 69 14 66 Z"
        fill={`url(#${body})`}
      />

      {/* Windows */}
      <path
        d="M80 42 C92 31 106 25 124 25 L148 25 C164 25 177 31 190 42 Z"
        fill={`url(#${glass})`}
      />
      <path d="M134 25 L132 42" stroke={bodyStops[2]} strokeWidth="3" />
      <path
        d="M98 30 L112 27 L104 41 L92 41 Z"
        fill="#ffffff"
        opacity="0.12"
      />

      {/* Shoulder highlight and door lines */}
      <path d="M30 51 C80 47 160 46 216 48" stroke="#ffffff" strokeOpacity="0.35" strokeWidth="1.5" fill="none" />
      <path d="M132 44 L130 70" stroke="#0f172a" strokeOpacity="0.35" strokeWidth="1" />
      <rect x="110" y="53" width="12" height="2.5" rx="1.25" fill="#0f172a" opacity="0.45" />
      <rect x="150" y="53" width="12" height="2.5" rx="1.25" fill="#0f172a" opacity="0.45" />

      {/* Lights */}
      <rect className="car-headlight" x="221" y="48" width="10" height="5" rx="2.5" fill="#f0f9ff" />
      <rect x="14" y="52" width="6" height="6" rx="2" fill="#ef4444" />
      <rect x="14" y="52" width="6" height="6" rx="2" fill="#f87171" opacity="0.6" />

      {/* Wheels */}
      {[62, 184].map((cx) => (
        <g key={cx}>
          <circle cx={cx} cy="72" r="18" fill="#020617" />
          <g className="car-wheel">
            <circle cx={cx} cy="72" r="11.5" fill={`url(#${rim})`} />
            {[0, 72, 144, 216, 288].map((angle) => {
              const rad = (angle * Math.PI) / 180
              return (
                <line
                  key={angle}
                  x1={cx}
                  y1="72"
                  x2={cx + Math.cos(rad) * 10}
                  y2={72 + Math.sin(rad) * 10}
                  stroke="#334155"
                  strokeWidth="2.2"
                  strokeLinecap="round"
                />
              )
            })}
            <circle cx={cx} cy="72" r="3" fill="#1e293b" />
          </g>
        </g>
      ))}
    </svg>
  )
}

/* Deterministic skyline: [x, width, height] in a 800 × 120 viewBox. */
const BUILDINGS: Array<[number, number, number]> = [
  [0, 46, 58], [50, 34, 84], [88, 58, 46], [150, 30, 96], [184, 52, 64],
  [240, 40, 78], [284, 64, 52], [352, 28, 104], [384, 48, 70], [436, 56, 50],
  [496, 36, 90], [536, 60, 60], [600, 32, 82], [636, 54, 56], [694, 40, 98],
  [738, 62, 62],
]

function Skyline() {
  return (
    <svg
      className="scene-skyline-svg"
      viewBox="0 0 800 120"
      preserveAspectRatio="none"
      aria-hidden="true"
      focusable="false"
    >
      {BUILDINGS.map(([x, w, h], index) => (
        <g key={x}>
          <rect x={x} y={120 - h} width={w} height={h} fill="#16213a" />
          {Array.from({ length: Math.floor(h / 18) }).map((_, row) =>
            (index + row) % 3 === 0 ? (
              <rect
                key={row}
                x={x + 8}
                y={120 - h + 10 + row * 16}
                width={Math.max(6, w - 16)}
                height="3"
                fill="#60a5fa"
                opacity="0.22"
              />
            ) : null,
          )}
        </g>
      ))}
    </svg>
  )
}

function StreetLights() {
  return (
    <svg
      className="scene-lights-svg"
      viewBox="0 0 800 140"
      preserveAspectRatio="none"
      aria-hidden="true"
      focusable="false"
    >
      {[100, 300, 500, 700].map((x) => (
        <g key={x}>
          <rect x={x} y="22" width="3" height="118" fill="#1e293b" />
          <path d={`M${x + 1.5} 24 Q ${x + 6} 14 ${x + 24} 16`} stroke="#1e293b" strokeWidth="3" fill="none" />
          <circle cx={x + 24} cy="18" r="16" fill="#93c5fd" opacity="0.08" />
          <circle cx={x + 24} cy="18" r="3.2" fill="#dbeafe" opacity="0.9" />
        </g>
      ))}
    </svg>
  )
}

export default function LoginScene() {
  return (
    <div className="login-scene" aria-hidden="true">
      <div className="scene-sky" />
      <div className="scene-moon" />

      <div className="scene-strip scene-skyline">
        <Skyline />
        <Skyline />
      </div>

      <div className="scene-strip scene-lights">
        <StreetLights />
        <StreetLights />
      </div>

      <div className="scene-road">
        <div className="scene-road-edge" />
        <div className="scene-lane" />
        <div className="scene-road-edge scene-road-edge--bottom" />
      </div>

      <div className="scene-car scene-car--pass">
        <Car id="dvld-car-pass" variant="secondary" />
      </div>

      <div className="scene-car scene-car--hero">
        <div className="scene-car-bob">
          <Car id="dvld-car-hero" variant="primary" />
        </div>
      </div>

      <div className="scene-vignette" />
    </div>
  )
}
