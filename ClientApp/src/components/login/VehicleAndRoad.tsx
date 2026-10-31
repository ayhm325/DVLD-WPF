export default function VehicleAndRoad() {
  return (
    <g id="vehicle-and-road" className="vehicle-and-road" aria-label="Foreground Highway, Wet Asphalt, and Luxury Sedan">
      <defs>
        {/* Taillight Intense Red Bloom Filter */}
        <filter id="taillight-red-bloom" x="-40%" y="-40%" width="180%" height="180%">
          <feGaussianBlur stdDeviation="3.5" result="blur1" />
          <feGaussianBlur stdDeviation="8" result="blur2" />
          <feMerge>
            <feMergeNode in="blur2" />
            <feMergeNode in="blur1" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>

        {/* Ambient Taillight Red Halation */}
        <radialGradient id="taillight-ambient-halo" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#ef4444" stopOpacity="0.75" />
          <stop offset="40%" stopColor="#dc2626" stopOpacity="0.35" />
          <stop offset="100%" stopColor="#991b1b" stopOpacity="0" />
        </radialGradient>

        {/* Neon Highway Rail Bloom Filter */}
        <filter id="neon-rail-bloom" x="-20%" y="-20%" width="140%" height="140%">
          <feGaussianBlur stdDeviation="5" result="blur" />
          <feComposite in="SourceGraphic" in2="blur" operator="over" />
        </filter>

        {/* Dark Wet Asphalt Ground Gradient */}
        <linearGradient id="wet-asphalt-grad" x1="960" y1="740" x2="960" y2="1080" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#080e18" />
          <stop offset="25%" stopColor="#050a12" />
          <stop offset="65%" stopColor="#03060c" />
          <stop offset="100%" stopColor="#010306" />
        </linearGradient>

        {/* Red Taillight Specular Reflection Gradient (Vertical Stream Down Wet Road) */}
        <linearGradient id="red-taillight-refl" x1="760" y1="885" x2="760" y2="1080" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#ef4444" stopOpacity="0.9" />
          <stop offset="15%" stopColor="#dc2626" stopOpacity="0.75" />
          <stop offset="45%" stopColor="#991b1b" stopOpacity="0.45" />
          <stop offset="75%" stopColor="#7f1d1d" stopOpacity="0.2" />
          <stop offset="100%" stopColor="#7f1d1d" stopOpacity="0" />
        </linearGradient>

        {/* Cyan Sign Specular Reflection Gradient */}
        <linearGradient id="cyan-sign-refl" x1="930" y1="755" x2="930" y2="1080" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#00e5ff" stopOpacity="0.55" />
          <stop offset="30%" stopColor="#0284c7" stopOpacity="0.28" />
          <stop offset="75%" stopColor="#0369a1" stopOpacity="0.08" />
          <stop offset="100%" stopColor="#0369a1" stopOpacity="0" />
        </linearGradient>

        {/* Warm Atrium Specular Reflection Gradient */}
        <linearGradient id="warm-atrium-refl" x1="930" y1="755" x2="930" y2="1050" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#fef08a" stopOpacity="0.55" />
          <stop offset="25%" stopColor="#f59e0b" stopOpacity="0.38" />
          <stop offset="65%" stopColor="#b45309" stopOpacity="0.15" />
          <stop offset="100%" stopColor="#78350f" stopOpacity="0" />
        </linearGradient>

        {/* Arabic Sign Specular Reflection Gradient */}
        <linearGradient id="arabic-sign-refl" x1="1190" y1="755" x2="1190" y2="1020" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.4" />
          <stop offset="45%" stopColor="#0284c7" stopOpacity="0.18" />
          <stop offset="100%" stopColor="#0284c7" stopOpacity="0" />
        </linearGradient>

        {/* Car Metallic Charcoal Body Paint Gradient */}
        <linearGradient id="car-body-metal" x1="760" y1="730" x2="760" y2="885" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#475569" />
          <stop offset="20%" stopColor="#334155" />
          <stop offset="55%" stopColor="#1e293b" />
          <stop offset="85%" stopColor="#0f172a" />
          <stop offset="100%" stopColor="#090d16" />
        </linearGradient>

        {/* Car Roof & Shoulder Specular Highlight */}
        <linearGradient id="car-shoulder-highlight" x1="640" y1="0" x2="880" y2="0" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#64748b" stopOpacity="0.8" />
          <stop offset="20%" stopColor="#94a3b8" stopOpacity="0.95" />
          <stop offset="50%" stopColor="#334155" stopOpacity="0.5" />
          <stop offset="80%" stopColor="#94a3b8" stopOpacity="0.95" />
          <stop offset="100%" stopColor="#64748b" stopOpacity="0.8" />
        </linearGradient>

        {/* Car Tinted Rear Windshield Gradient */}
        <linearGradient id="car-windshield-tint" x1="760" y1="735" x2="760" y2="795" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#0f172a" />
          <stop offset="35%" stopColor="#020617" />
          <stop offset="75%" stopColor="#1e293b" stopOpacity="0.8" />
          <stop offset="100%" stopColor="#38bdf8" stopOpacity="0.25" />
        </linearGradient>

        {/* Sweeping Neon Highway Rail Gradient */}
        <linearGradient id="neon-curb-cyan" x1="0" y1="840" x2="380" y2="1080" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#00e5ff" />
          <stop offset="50%" stopColor="#38bdf8" />
          <stop offset="100%" stopColor="#2563eb" />
        </linearGradient>
      </defs>

      {/* ============================================================
          1. WET ASPHALT ROAD SURFACE (SEAMLESS GROUND INTEGRATION)
      ============================================================ */}
      <polygon
        points="0,740 1920,740 1920,1080 0,1080"
        fill="url(#wet-asphalt-grad)"
      />

      {/* ============================================================
          2. SPECULAR WET PAVEMENT REFLECTIONS (BLURRED LIGHT TRAILS)
      ============================================================ */}
      {/* Cyan Reflection under DVLD Sign */}
      <polygon
        points="900,755 960,755 980,1080 880,1080"
        fill="url(#cyan-sign-refl)"
        opacity="0.75"
      />

      {/* Warm Golden Reflection under Entrance Atrium */}
      <polygon
        points="800,755 1060,755 1100,1050 760,1050"
        fill="url(#warm-atrium-refl)"
        opacity="0.65"
      />

      {/* Soft Cyan Reflection under Arabic Canopy Sign */}
      <polygon
        points="1120,755 1250,755 1290,1040 1080,1040"
        fill="url(#arabic-sign-refl)"
        opacity="0.5"
      />

      {/* Red Taillight Specular Reflection Stream Leading to Viewer */}
      <polygon
        points="700,885 820,885 860,1080 660,1080"
        fill="url(#red-taillight-refl)"
        opacity="0.85"
      />

      {/* Subtle Horizontal Wet Shimmer Waves Across Pavement */}
      <g stroke="#38bdf8" strokeWidth="1" opacity="0.15" fill="none">
        <line x1="500" y1="910" x2="980" y2="910" strokeDasharray="30 20 60 40" />
        <line x1="440" y1="955" x2="1100" y2="955" strokeDasharray="50 30 70 20" />
        <line x1="360" y1="1005" x2="1220" y2="1005" strokeDasharray="40 40 80 30" />
        <line x1="240" y1="1055" x2="1380" y2="1055" strokeDasharray="60 30 100 40" />
      </g>

      {/* ============================================================
          3. HIGHWAY PERSPECTIVE LANE MARKINGS & CURVED CYAN BARRIER
      ============================================================ */}
      {/* Natural Vanishing Lane Markings */}
      <g stroke="#38bdf8" strokeOpacity="0.45" strokeWidth="2.5" fill="none">
        {/* Left Lane Divider */}
        <line
          x1="450"
          y1="1080"
          x2="690"
          y2="760"
          strokeDasharray="35 30"
          strokeWidth="2.8"
        />
        {/* Center Lane Divider */}
        <line
          x1="860"
          y1="1080"
          x2="810"
          y2="760"
          strokeDasharray="30 35"
          strokeWidth="2.2"
          strokeOpacity="0.35"
        />
        {/* Right Lane Guide */}
        <line
          x1="1240"
          y1="1080"
          x2="950"
          y2="760"
          strokeDasharray="30 40"
          strokeWidth="2"
          strokeOpacity="0.25"
        />
      </g>

      {/* Sweeping Left Curved Roadway Guide Strip (Neon Barrier) */}
      <path
        d="M 0 850 Q 240 920 380 1080"
        stroke="url(#neon-curb-cyan)"
        strokeWidth="6"
        fill="none"
        filter="url(#neon-rail-bloom)"
      />
      <path
        d="M 0 850 Q 240 920 380 1080"
        stroke="#ffffff"
        strokeWidth="2"
        fill="none"
        opacity="0.9"
      />

      {/* ============================================================
          4. PHOTOREALISTIC LUXURY SPORTS SEDAN (REAR 3/4 PERSPECTIVE)
             Centered at x: 760, y: 725 to 895 (Width: 260px, Height: 170px)
      ============================================================ */}
      <g id="luxury-sedan">
        {/* Heavy Ambient Occlusion Ground Contact Shadow under Chassis & Tires */}
        <ellipse cx="760" cy="888" rx="135" ry="14" fill="#000000" opacity="0.95" />
        <ellipse cx="760" cy="886" rx="110" ry="8" fill="#000000" opacity="0.8" />

        {/* Rear Performance Tires */}
        {/* Left Rear Tire */}
        <rect x="642" y="855" width="28" height="32" rx="6" fill="#090d16" stroke="#1e293b" strokeWidth="1.5" />
        <line x1="645" y1="875" x2="667" y2="875" stroke="#334155" strokeWidth="2" />
        {/* Right Rear Tire */}
        <rect x="850" y="855" width="28" height="32" rx="6" fill="#090d16" stroke="#1e293b" strokeWidth="1.5" />
        <line x1="853" y1="875" x2="875" y2="875" stroke="#334155" strokeWidth="2" />

        {/* --- MAIN AERODYNAMIC BODYWORK --- */}
        {/* Aerodynamic Lower Diffuser & Bumper Underbody */}
        <polygon
          points="652,875 868,875 858,885 662,885"
          fill="#0a0e17"
          stroke="#1e293b"
          strokeWidth="1.2"
        />

        {/* 5 Carbon-Fiber Vertical Diffuser Fins */}
        <line x1="720" y1="870" x2="720" y2="885" stroke="#38bdf8" strokeWidth="1.8" opacity="0.8" />
        <line x1="740" y1="870" x2="740" y2="885" stroke="#475569" strokeWidth="1.8" />
        <line x1="760" y1="870" x2="760" y2="885" stroke="#38bdf8" strokeWidth="2" opacity="0.9" />
        <line x1="780" y1="870" x2="780" y2="885" stroke="#475569" strokeWidth="1.8" />
        <line x1="800" y1="870" x2="800" y2="885" stroke="#38bdf8" strokeWidth="1.8" opacity="0.8" />

        {/* Dual Chrome/Dark Exhaust Extraction Ports */}
        <rect x="674" y="870" width="30" height="9" rx="2.5" fill="#020617" stroke="#475569" strokeWidth="1.2" />
        <rect x="816" y="870" width="30" height="9" rx="2.5" fill="#020617" stroke="#475569" strokeWidth="1.2" />

        {/* Sculpted Rear Bumper Mass (Metallic Charcoal) */}
        <path
          d="M 640 825 
             Q 640 865 655 874 
             L 865 874 
             Q 880 865 880 825 
             Q 880 812 865 810 
             L 655 810 
             Q 640 812 640 825 Z"
          fill="url(#car-body-metal)"
          stroke="#1e293b"
          strokeWidth="1.5"
        />

        {/* Lateral Red Bumper Reflectors */}
        <rect x="652" y="842" width="22" height="4" rx="1.5" fill="#ef4444" opacity="0.85" />
        <rect x="846" y="842" width="22" height="4" rx="1.5" fill="#ef4444" opacity="0.85" />

        {/* --- RECESSED LICENSE PLATE HOUSING & JORDANIAN PLATE --- */}
        <g id="car-license-plate">
          {/* Recessed Bumper Cavity with Ambient Occlusion */}
          <rect x="726" y="830" width="68" height="25" rx="3" fill="#050811" stroke="#1e293b" strokeWidth="1.2" />
          {/* Dual White LED License Plate Lights */}
          <circle cx="744" cy="831" r="1.5" fill="#ffffff" filter="url(#neon-rail-bloom)" />
          <circle cx="776" cy="831" r="1.5" fill="#ffffff" filter="url(#neon-rail-bloom)" />

          {/* Jordanian Reflective White License Plate */}
          <rect x="730" y="834" width="60" height="18" rx="2" fill="#f8fafc" stroke="#090d16" strokeWidth="1.2" />
          {/* Left Country Code "JO" */}
          <rect x="731" y="835" width="12" height="16" rx="1" fill="#0f172a" />
          <text
            x="737"
            y="846"
            fill="#ffffff"
            fontSize="8"
            fontWeight="bold"
            fontFamily="system-ui, monospace"
            textAnchor="middle"
          >
            JO
          </text>
          {/* Registration Number "7-9942" */}
          <text
            x="766"
            y="847"
            fill="#090d16"
            fontSize="10"
            fontWeight="900"
            fontFamily="system-ui, monospace"
            letterSpacing="1"
            textAnchor="middle"
          >
            7-9942
          </text>
        </g>

        {/* --- MUSCULAR REAR HAUNCHES & TRUNK DECK --- */}
        <path
          d="M 648 812 
             Q 645 780 670 765 
             L 850 765 
             Q 875 780 872 812 
             Z"
          fill="url(#car-body-metal)"
        />

        {/* Fender Shoulder Line Specular Reflection */}
        <path
          d="M 648 812 Q 652 775 675 765 L 845 765 Q 868 775 872 812"
          stroke="url(#car-shoulder-highlight)"
          strokeWidth="2.5"
          fill="none"
        />

        {/* Integrated Ducktail Trunk Spoiler */}
        <path
          d="M 670 765 Q 760 760 850 765 L 852 770 Q 760 764 668 770 Z"
          fill="#334155"
          stroke="#475569"
          strokeWidth="0.8"
        />

        {/* --- CURVED REAR WINDSHIELD & CABIN --- */}
        {/* Sloping Fastback Rear Glass */}
        <polygon
          points="702,728 818,728 846,765 674,765"
          fill="url(#car-windshield-tint)"
          stroke="#1e293b"
          strokeWidth="1.5"
        />

        {/* Diagonal Skylight Specular Reflection across Rear Glass */}
        <polygon
          points="720,728 755,728 735,765 700,765"
          fill="#ffffff"
          opacity="0.12"
        />
        <polygon
          points="775,728 810,728 835,765 800,765"
          fill="#38bdf8"
          opacity="0.15"
        />

        {/* Center High-Mounted Stop Lamp (CHMSL Red LED Bar on Top of Rear Window) */}
        <rect x="742" y="729" width="36" height="3" rx="1.5" fill="#ef4444" filter="url(#taillight-red-bloom)" />

        {/* Cabin Roof Silhouette (Dark Metallic) */}
        <path
          d="M 700 728 Q 760 722 820 728 Z"
          stroke="#475569"
          strokeWidth="2"
          fill="#1e293b"
        />

        {/* --- CONTINUOUS HORIZONTAL RED LED TAILLIGHT BAR --- */}
        {/* Ambient Red Glow Halo Behind Taillight */}
        <ellipse cx="760" cy="806" rx="125" ry="14" fill="url(#taillight-ambient-halo)" opacity="0.75" />

        {/* Outer Dark Enclosure Housing for Light Bar */}
        <rect x="652" y="802" width="216" height="10" rx="4" fill="#04060c" stroke="#1e293b" strokeWidth="1" />

        {/* Continuous Radiant Red LED Light Bar */}
        <rect
          x="654"
          y="804"
          width="212"
          height="6"
          rx="3"
          fill="#ef4444"
          filter="url(#taillight-red-bloom)"
        />

        {/* Ultra-Bright Center Core Light Guide */}
        <rect
          x="660"
          y="805.5"
          width="200"
          height="3"
          rx="1.5"
          fill="#fee2e2"
          opacity="0.9"
        />

        {/* Taillight Corner End Caps / Accent Fins */}
        <path d="M 654 804 L 646 812" stroke="#ef4444" strokeWidth="2.5" filter="url(#taillight-red-bloom)" />
        <path d="M 866 804 L 874 812" stroke="#ef4444" strokeWidth="2.5" filter="url(#taillight-red-bloom)" />
      </g>
    </g>
  )
}
