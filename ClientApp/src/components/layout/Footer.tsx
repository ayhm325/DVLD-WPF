import { Code } from "lucide-react";

export default function Footer() {
  return (
    <footer className="app-footer relative overflow-hidden flex items-center justify-between gap-4 px-6 sm:px-8 py-4 bg-linear-to-r from-[#070e1c] via-[#0a1832] to-[#0d1e3d] border-t border-blue-500/40 shadow-[0_-4px_20px_rgba(6,16,38,0.5),inset_0_1px_0_rgba(96,165,250,0.25)]">
      {/* Layered flowing blue and cyan decorative waves */}
      <svg
        className="pointer-events-none absolute inset-0 h-full w-full select-none opacity-75 z-0"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 1200 60"
        preserveAspectRatio="none"
        aria-hidden="true"
      >
        <defs>
          <linearGradient
            id="footer-wave-grad-1"
            x1="0%"
            y1="0%"
            x2="100%"
            y2="0%"
          >
            <stop offset="0%" stopColor="#3b82f6" stopOpacity="0" />
            <stop offset="25%" stopColor="#38bdf8" stopOpacity="0.6" />
            <stop offset="65%" stopColor="#60a5fa" stopOpacity="0.7" />
            <stop offset="100%" stopColor="#818cf8" stopOpacity="0" />
          </linearGradient>
          <linearGradient
            id="footer-wave-grad-2"
            x1="0%"
            y1="0%"
            x2="100%"
            y2="0%"
          >
            <stop offset="0%" stopColor="#06b6d4" stopOpacity="0" />
            <stop offset="45%" stopColor="#22d3ee" stopOpacity="0.5" />
            <stop offset="85%" stopColor="#3b82f6" stopOpacity="0.6" />
            <stop offset="100%" stopColor="#38bdf8" stopOpacity="0" />
          </linearGradient>
          <filter
            id="footer-wave-glow"
            x="-10%"
            y="-10%"
            width="120%"
            height="120%"
          >
            <feGaussianBlur stdDeviation="3.5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
        <path
          d="M 0 45 Q 300 15 600 35 T 1200 20"
          fill="none"
          stroke="url(#footer-wave-grad-1)"
          strokeWidth="2.5"
          filter="url(#footer-wave-glow)"
        />
        <path
          d="M 0 30 Q 350 50 700 25 T 1200 40"
          fill="none"
          stroke="url(#footer-wave-grad-2)"
          strokeWidth="1.8"
          opacity="0.8"
        />
        <circle
          cx="350"
          cy="25"
          r="2.5"
          fill="#38bdf8"
          filter="url(#footer-wave-glow)"
        />
        <circle
          cx="750"
          cy="35"
          r="2"
          fill="#22d3ee"
          filter="url(#footer-wave-glow)"
        />
        <circle
          cx="150"
          cy="40"
          r="1.5"
          fill="#60a5fa"
          filter="url(#footer-wave-glow)"
        />
        <circle
          cx="950"
          cy="28"
          r="1.5"
          fill="#38bdf8"
          filter="url(#footer-wave-glow)"
        />
      </svg>

      <div className="relative z-10 flex items-center gap-2">
        <span
          className="size-2 rounded-full bg-cyan-400 shadow-[0_0_8px_#22d3ee] mr-1"
          aria-hidden="true"
        />
        <span className="font-bold tracking-tight text-white text-xs sm:text-sm">
          <span className="bg-linear-to-r from-blue-400 via-sky-300 to-cyan-300 bg-clip-text text-transparent drop-shadow-[0_0_12px_rgba(56,189,248,0.4)]">
            DVLD
          </span>{" "}
          <span className="text-slate-300 font-medium">
            Management System
          </span>
        </span>
      </div>

      <div className="relative z-10 flex items-center gap-3">
        <span className="footer-badge inline-flex items-center gap-1.5 px-3 py-1 rounded-full border border-blue-400/50 bg-[#081834]/90 text-blue-200 text-[10px] font-bold tracking-wider uppercase shadow-[0_0_16px_rgba(59,130,246,0.35)]">
          <Code className="size-3.5 text-cyan-400" aria-hidden="true" />
          FRONTEND PREVIEW
        </span>
      </div>
    </footer>
  );
}
