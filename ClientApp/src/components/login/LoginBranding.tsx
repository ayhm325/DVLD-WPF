import { Layers } from 'lucide-react'

export default function LoginBranding({ className = '' }: { className?: string }) {
  return (
    <div className={`login-branding flex flex-col justify-between ${className}`}>
      {/* Top Header & Brand */}
      <div className="space-y-6">
        {/* Brand Lockup */}
        <div className="flex items-center gap-3.5">
          <div className="flex size-12 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-blue-500 via-blue-600 to-cyan-600 text-white border-[1.5px] border-cyan-300/60 shadow-[0_0_24px_rgba(6,182,212,0.65),inset_0_1px_2px_rgba(255,255,255,0.7)]">
            <Layers className="size-6 text-white stroke-[2.4]" />
          </div>

          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-black tracking-tight text-white leading-none">
                DVLD
              </h1>
            </div>
            <span className="text-xs font-medium text-slate-300/80 mt-1">
              Driving &amp; Vehicle Licensing Department
            </span>
          </div>
        </div>

        {/* Status Pill Badge */}
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#0a1832]/80 border border-cyan-500/35 backdrop-blur-md shadow-[0_0_15px_rgba(6,182,212,0.25)]">
          <span className="size-2 rounded-full bg-cyan-400 shadow-[0_0_8px_#22d3ee] animate-pulse" />
          <span className="text-[11.5px] font-semibold text-cyan-200 tracking-wide">
            Licensing operations platform
          </span>
        </div>

        {/* Main Headlines */}
        <div className="space-y-2 pt-2">
          <h2 className="text-4xl lg:text-5xl font-black tracking-tight leading-[1.12]">
            <span className="block text-white drop-shadow-[0_2px_12px_rgba(0,0,0,0.8)]">
              Driving forward.
            </span>
            <span className="block bg-gradient-to-r from-blue-400 via-cyan-400 to-sky-300 bg-clip-text text-transparent drop-shadow-[0_0_25px_rgba(34,211,238,0.35)]">
              Managing smarter.
            </span>
          </h2>
          <p className="text-sm text-slate-300/90 max-w-[420px] leading-relaxed pt-1">
            A secure and modern platform for people, drivers, licenses, and licensing applications.
          </p>
        </div>
      </div>

      {/* Bottom-left Slogan */}
      <div className="pt-10 text-xs font-semibold text-slate-300/90 leading-relaxed tracking-wider select-none">
        <p>Safer Roads.</p>
        <p>Smarter Services.</p>
        <p>A Better Tomorrow.</p>
        <div className="mt-3 w-10 h-[2px] bg-cyan-400 shadow-[0_0_8px_#22d3ee] rounded-full" />
      </div>
    </div>
  )
}
