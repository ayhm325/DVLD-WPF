export default function SecurityEmblem({ className = '' }: { className?: string }) {
  return (
    <div
      className={`relative flex items-center justify-center ${className}`}
      aria-hidden="true"
    >
      {/* Outer ambient glow */}
      <div className="absolute inset-0 rounded-full bg-cyan-500/20 blur-xl animate-pulse" />

      <svg
        className="relative size-20 drop-shadow-[0_0_16px_rgba(6,182,212,0.65)]"
        viewBox="0 0 100 100"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <defs>
          <linearGradient id="emblem-cyan-grad" x1="0" y1="0" x2="100" y2="100" gradientUnits="userSpaceOnUse">
            <stop offset="0%" stopColor="#38bdf8" />
            <stop offset="50%" stopColor="#22d3ee" />
            <stop offset="100%" stopColor="#0284c7" />
          </linearGradient>
          <radialGradient id="emblem-core-glow" cx="50" cy="50" r="45" gradientUnits="userSpaceOnUse">
            <stop offset="0%" stopColor="#0284c7" stopOpacity="0.45" />
            <stop offset="60%" stopColor="#0369a1" stopOpacity="0.15" />
            <stop offset="100%" stopColor="#082f49" stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Central glowing disk */}
        <circle cx="50" cy="50" r="46" fill="url(#emblem-core-glow)" />

        {/* Outer dashed radar ring */}
        <circle
          cx="50"
          cy="50"
          r="46"
          stroke="url(#emblem-cyan-grad)"
          strokeWidth="1"
          strokeDasharray="2 4"
          strokeOpacity="0.5"
        />

        {/* Outer solid orbit ring */}
        <circle
          cx="50"
          cy="50"
          r="42"
          stroke="#38bdf8"
          strokeWidth="1.2"
          strokeOpacity="0.75"
        />

        {/* 12 Cardinal and sub-cardinal tick marks */}
        {Array.from({ length: 12 }).map((_, i) => {
          const angle = (i * 30 * Math.PI) / 180
          const x1 = 50 + 38 * Math.cos(angle)
          const y1 = 50 + 38 * Math.sin(angle)
          const x2 = 50 + 42 * Math.cos(angle)
          const y2 = 50 + 42 * Math.sin(angle)
          return (
            <line
              key={i}
              x1={x1}
              y1={y1}
              x2={x2}
              y2={y2}
              stroke="#22d3ee"
              strokeWidth={i % 3 === 0 ? '1.8' : '1'}
              strokeOpacity="0.8"
            />
          )
        })}

        {/* Inner concentric technical ring */}
        <circle
          cx="50"
          cy="50"
          r="34"
          stroke="#0284c7"
          strokeWidth="1"
          strokeDasharray="6 3"
          strokeOpacity="0.6"
        />

        {/* Orbital data nodes */}
        <circle cx="50" cy="8" r="1.75" fill="#38bdf8" />
        <circle cx="92" cy="50" r="1.75" fill="#38bdf8" />
        <circle cx="50" cy="92" r="1.75" fill="#38bdf8" />
        <circle cx="8" cy="50" r="1.75" fill="#38bdf8" />

        {/* Heraldic Royal Crown at the top of the crest */}
        <path
          d="M40 31L43 36L50 29L57 36L60 31L61 38H39L40 31Z"
          fill="url(#emblem-cyan-grad)"
          stroke="#67e8f9"
          strokeWidth="0.8"
        />
        <circle cx="40" cy="30" r="1" fill="#e0f2fe" />
        <circle cx="50" cy="28" r="1.2" fill="#e0f2fe" />
        <circle cx="60" cy="30" r="1" fill="#e0f2fe" />

        {/* Royal Crest / Heraldic Wings Framing the Shield */}
        <path
          d="M31 43C31 38 36 36 39 37C36 40 35 44 36 48C34 45 32 46 31 43Z"
          fill="#38bdf8"
          fillOpacity="0.8"
        />
        <path
          d="M27 49C27 44 32 42 35 43C32 46 32 50 33 54C30 51 28 52 27 49Z"
          fill="#38bdf8"
          fillOpacity="0.6"
        />
        <path
          d="M69 43C69 38 64 36 61 37C64 40 65 44 64 48C66 45 68 46 69 43Z"
          fill="#38bdf8"
          fillOpacity="0.8"
        />
        <path
          d="M73 49C73 44 68 42 65 43C68 46 68 50 67 54C70 51 72 52 73 49Z"
          fill="#38bdf8"
          fillOpacity="0.6"
        />

        {/* Central Shield Outline */}
        <path
          d="M37 40H63V52C63 61 50 69 50 69C50 69 37 61 37 52V40Z"
          fill="#071830"
          stroke="url(#emblem-cyan-grad)"
          strokeWidth="1.6"
        />

        {/* Inner Shield Contour */}
        <path
          d="M40 43H60V51C60 58 50 65 50 65C50 65 40 58 40 51V43Z"
          fill="none"
          stroke="#0284c7"
          strokeWidth="0.8"
          strokeDasharray="2 1.5"
        />

        {/* Shield Heraldic Eagle / Falcon / Star Device */}
        <path
          d="M50 45L53 50H58L54 53L56 58L50 55L44 58L46 53L42 50H47L50 45Z"
          fill="#38bdf8"
          fillOpacity="0.9"
        />

        {/* Lower Security Ribbon */}
        <path
          d="M36 69L50 66L64 69L58 72L50 70L42 72L36 69Z"
          fill="#0369a1"
          stroke="#38bdf8"
          strokeWidth="0.75"
        />
      </svg>
    </div>
  )
}
