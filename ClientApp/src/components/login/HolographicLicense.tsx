export default function HolographicLicense({ className = '' }: { className?: string }) {
  return (
    <div
      className={`hologram-projector relative select-none pointer-events-none ${className}`}
      aria-hidden="true"
    >
      {/* Upper Ambient Sky Nebula Glow */}
      <div className="absolute -top-16 -left-12 size-80 rounded-full bg-cyan-500/15 blur-3xl" />
      <div className="absolute top-10 left-36 size-64 rounded-full bg-blue-600/20 blur-3xl" />

      {/* Floating Holographic Map of Jordan in the sky */}
      <div className="jordan-constellation-map absolute -top-8 left-48 opacity-75">
        <svg width="120" height="150" viewBox="0 0 100 130" fill="none">
          <defs>
            <linearGradient id="map-glow" x1="0" y1="0" x2="100" y2="130" gradientUnits="userSpaceOnUse">
              <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.8" />
              <stop offset="50%" stopColor="#22d3ee" stopOpacity="0.6" />
              <stop offset="100%" stopColor="#0284c7" stopOpacity="0.2" />
            </linearGradient>
            <filter id="glow-effect" x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="2.5" result="blur" />
              <feComposite in="SourceGraphic" in2="blur" operator="over" />
            </filter>
          </defs>

          {/* Polygon outline of Jordan */}
          <path
            d="M58 8L74 24L86 36L72 58L76 86L62 118L44 114L52 92L40 76L32 50L36 34L48 20L58 8Z"
            stroke="url(#map-glow)"
            strokeWidth="1.2"
            fill="#0284c7"
            fillOpacity="0.08"
            filter="url(#glow-effect)"
          />

          {/* Constellation grid lines between cities (Amman, Irbid, Aqaba, Zarqa, etc) */}
          <line x1="58" y1="28" x2="48" y2="38" stroke="#38bdf8" strokeWidth="0.8" strokeDasharray="2 2" strokeOpacity="0.7" />
          <line x1="48" y1="38" x2="52" y2="52" stroke="#38bdf8" strokeWidth="0.8" strokeDasharray="2 2" strokeOpacity="0.7" />
          <line x1="52" y1="52" x2="48" y2="78" stroke="#38bdf8" strokeWidth="0.8" strokeDasharray="2 2" strokeOpacity="0.7" />
          <line x1="48" y1="78" x2="46" y2="114" stroke="#38bdf8" strokeWidth="0.8" strokeDasharray="2 2" strokeOpacity="0.7" />

          {/* Glowing City Nodes */}
          <circle cx="58" cy="28" r="2.5" fill="#e0f2fe" filter="url(#glow-effect)" />
          <circle cx="48" cy="38" r="3" fill="#38bdf8" filter="url(#glow-effect)" />
          <circle cx="52" cy="52" r="2" fill="#22d3ee" />
          <circle cx="48" cy="78" r="2.2" fill="#38bdf8" />
          <circle cx="46" cy="114" r="2.8" fill="#e0f2fe" filter="url(#glow-effect)" />
        </svg>
      </div>

      {/* Floating 3D Holographic Driving License Card */}
      <div
        className="holographic-card relative w-[265px] h-[162px] rounded-xl p-3 backdrop-blur-md transition-transform duration-700"
        style={{
          background: 'linear-gradient(135deg, rgba(8, 24, 52, 0.72) 0%, rgba(4, 16, 36, 0.55) 100%)',
          border: '1.2px solid rgba(56, 189, 248, 0.65)',
          boxShadow: '0 0 24px rgba(6, 182, 212, 0.3), inset 0 0 15px rgba(56, 189, 248, 0.2), 0 16px 32px rgba(0,0,0,0.6)',
          transform: 'perspective(900px) rotateY(-14deg) rotateX(8deg)',
          animation: 'hologram-float 6s ease-in-out infinite',
        }}
      >
        {/* Hologram scanlines effect */}
        <div
          className="absolute inset-0 rounded-xl pointer-events-none opacity-15"
          style={{
            backgroundImage: 'repeating-linear-gradient(0deg, transparent, transparent 3px, rgba(56, 189, 248, 0.35) 4px, rgba(56, 189, 248, 0.35) 5px)',
          }}
        />

        {/* License Header */}
        <div className="flex items-center justify-between pb-2 mb-2 border-b border-cyan-500/30">
          <div className="flex items-center gap-1.5">
            <span className="size-2 rounded-full bg-cyan-400 animate-ping" />
            <span className="text-[11px] font-black tracking-widest text-cyan-300 uppercase">
              DRIVING LICENSE
            </span>
          </div>
          <div className="flex items-center gap-1 opacity-80">
            <svg className="size-4 text-cyan-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M12 2L4 7v6c0 5 8 9 8 9s8-4 8-9V7l-8-5z" />
            </svg>
            <span className="text-[9px] font-mono text-cyan-200">JORDAN</span>
          </div>
        </div>

        {/* Card Body */}
        <div className="flex gap-3">
          {/* Holographic Portrait Avatar */}
          <div className="relative size-[76px] rounded-xl bg-cyan-950/60 border border-cyan-400/50 p-1 flex items-center justify-center shrink-0 shadow-[0_0_15px_rgba(6,182,212,0.3)]">
            <svg className="size-12 text-cyan-300/80" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 12c2.76 0 5-2.24 5-5s-2.24-5-5-5-5 2.24-5 5 2.24 5 5 5zm0 2c-3.33 0-10 1.67-10 5v3h20v-3c0-3.33-6.67-5-10-5z" />
            </svg>
            <div className="absolute bottom-1 right-1 size-2 rounded-full bg-emerald-400 shadow-[0_0_6px_#34d399]" />
          </div>

          {/* License Data Fields */}
          <div className="flex-1 flex flex-col justify-between text-[9px] text-cyan-100 font-mono leading-tight">
            <div>
              <div className="text-[8px] text-cyan-400 uppercase tracking-wider">LICENSE NO.</div>
              <div className="font-bold text-white text-[11px] tracking-wide">JO-8942-019</div>
            </div>
            <div>
              <div className="text-[8px] text-cyan-400 uppercase tracking-wider">CLASS</div>
              <div className="text-cyan-200 font-semibold">CLASS 3 • PRIVATE MOTOR</div>
            </div>
            <div className="flex items-center justify-between text-[8px] text-cyan-300/80">
              <span>EXP: 2034/12</span>
              <span className="text-emerald-300 font-bold">VERIFIED ✓</span>
            </div>
          </div>
        </div>

        {/* Microchip & Holographic Data Bar */}
        <div className="mt-2.5 pt-1.5 border-t border-cyan-500/20 flex items-center justify-between">
          <div className="flex items-center gap-1.5">
            <div className="w-5 h-3.5 rounded bg-amber-400/30 border border-amber-300/50 flex items-center justify-center">
              <div className="w-2.5 h-1.5 border border-amber-200/60" />
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="h-0.5 w-12 bg-cyan-400/40 rounded" />
              <span className="h-0.5 w-8 bg-cyan-400/30 rounded" />
            </div>
          </div>
          <span className="text-[8px] font-mono text-cyan-400/80">DVLD DIGITAL ID</span>
        </div>
      </div>

      {/* Hologram Light Cone / Projection Vortex Rays radiating down to the building */}
      <div className="hologram-vortex relative -mt-3 left-6 w-[240px] flex flex-col items-center">
        {/* Layered glowing ellipses representing projection rings */}
        <div
          className="w-[220px] h-[22px] rounded-[100%] border border-cyan-400/70 shadow-[0_0_18px_rgba(6,182,212,0.6)]"
          style={{ transform: 'perspective(500px) rotateX(72deg)' }}
        />
        <div
          className="w-[170px] h-[18px] -mt-2 rounded-[100%] border border-cyan-400/50 shadow-[0_0_14px_rgba(6,182,212,0.4)]"
          style={{ transform: 'perspective(500px) rotateX(72deg)' }}
        />
        <div
          className="w-[120px] h-[14px] -mt-2 rounded-[100%] border border-blue-400/40 shadow-[0_0_10px_rgba(37,99,235,0.4)]"
          style={{ transform: 'perspective(500px) rotateX(72deg)' }}
        />
        <div
          className="w-[70px] h-[10px] -mt-1 rounded-[100%] border border-blue-400/30"
          style={{ transform: 'perspective(500px) rotateX(72deg)' }}
        />

        {/* Vertical Light Funnel Beam */}
        <div
          className="w-[110px] h-[80px] -mt-3 opacity-60"
          style={{
            background: 'linear-gradient(180deg, rgba(6, 182, 212, 0.45) 0%, rgba(37, 99, 235, 0.15) 60%, transparent 100%)',
            clipPath: 'polygon(15% 0%, 85% 0%, 55% 100%, 45% 100%)',
            filter: 'blur(1.5px)',
          }}
        />
      </div>
    </div>
  )
}
