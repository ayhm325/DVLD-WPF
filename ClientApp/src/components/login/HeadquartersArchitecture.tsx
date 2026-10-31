export default function HeadquartersArchitecture() {
  return (
    <g id="dvld-headquarters" className="dvld-headquarters" aria-label="DVLD Modern Headquarters Architecture">
      <defs>
        {/* Soft Ambient Neon Glow Filters */}
        <filter id="hq-neon-cyan" x="-20%" y="-20%" width="140%" height="140%">
          <feGaussianBlur stdDeviation="3" result="blur" />
          <feComposite in="SourceGraphic" in2="blur" operator="over" />
        </filter>

        <filter id="hq-neon-blue" x="-20%" y="-20%" width="140%" height="140%">
          <feGaussianBlur stdDeviation="4.5" result="blur" />
          <feComposite in="SourceGraphic" in2="blur" operator="over" />
        </filter>

        {/* DVLD Sign Neon Backlight Filter */}
        <filter id="hq-sign-glow" x="-30%" y="-30%" width="160%" height="160%">
          <feGaussianBlur stdDeviation="3" result="blur1" />
          <feGaussianBlur stdDeviation="7" result="blur2" />
          <feMerge>
            <feMergeNode in="blur2" />
            <feMergeNode in="blur1" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>

        {/* Architectural Limestone Cladding Panel Gradient (Warm Natural Jordanian Stone) */}
        <linearGradient id="hq-limestone-panel" x1="0" y1="440" x2="0" y2="750" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#2c3648" />
          <stop offset="25%" stopColor="#1e2634" />
          <stop offset="65%" stopColor="#151b26" />
          <stop offset="100%" stopColor="#0c1017" />
        </linearGradient>

        {/* Architectural Portal Frame (Dark Composite & Granite) */}
        <linearGradient id="hq-portal-frame" x1="0" y1="440" x2="0" y2="750" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#334155" />
          <stop offset="40%" stopColor="#1e293b" />
          <stop offset="100%" stopColor="#0f172a" />
        </linearGradient>

        {/* Cantilever Entrance Canopy 3D Gradient */}
        <linearGradient id="hq-canopy-metal" x1="770" y1="590" x2="1090" y2="615" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.4" />
          <stop offset="20%" stopColor="#1e293b" />
          <stop offset="80%" stopColor="#0f172a" />
          <stop offset="100%" stopColor="#0284c7" stopOpacity="0.3" />
        </linearGradient>

        {/* Multi-Floor Warm 3000K Interior Atrium Lighting */}
        {/* Ground Floor Main Reception & Security Gates (Bright 3000K Glow) */}
        <linearGradient id="hq-atrium-ground" x1="930" y1="615" x2="930" y2="745" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#fef08a" stopOpacity="0.95" />
          <stop offset="25%" stopColor="#fde047" stopOpacity="0.85" />
          <stop offset="55%" stopColor="#f59e0b" stopOpacity="0.65" />
          <stop offset="85%" stopColor="#b45309" stopOpacity="0.35" />
          <stop offset="100%" stopColor="#78350f" stopOpacity="0.15" />
        </linearGradient>

        {/* Mezzanine / Second Floor Office Interior */}
        <linearGradient id="hq-atrium-mezzanine" x1="930" y1="470" x2="930" y2="615" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#fef08a" stopOpacity="0.75" />
          <stop offset="35%" stopColor="#f59e0b" stopOpacity="0.55" />
          <stop offset="80%" stopColor="#b45309" stopOpacity="0.25" />
          <stop offset="100%" stopColor="#1e293b" stopOpacity="0.1" />
        </linearGradient>

        {/* High-Performance Architectural Glass Curtain Wall Tint */}
        <linearGradient id="hq-curtain-glass" x1="780" y1="470" x2="1080" y2="745" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.28" />
          <stop offset="25%" stopColor="#0284c7" stopOpacity="0.1" />
          <stop offset="70%" stopColor="#0369a1" stopOpacity="0.04" />
          <stop offset="100%" stopColor="#020617" stopOpacity="0.45" />
        </linearGradient>

        {/* Diagonal Specular Glass Reflection Sheen */}
        <linearGradient id="hq-glass-specular" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#ffffff" stopOpacity="0.25" />
          <stop offset="30%" stopColor="#38bdf8" stopOpacity="0.1" />
          <stop offset="65%" stopColor="#ffffff" stopOpacity="0.03" />
          <stop offset="100%" stopColor="#000000" stopOpacity="0" />
        </linearGradient>

        {/* Canopy Downlight Spotlight Cone */}
        <linearGradient id="canopy-downlight-cone" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#fef08a" stopOpacity="0.9" />
          <stop offset="40%" stopColor="#f59e0b" stopOpacity="0.45" />
          <stop offset="100%" stopColor="#f59e0b" stopOpacity="0" />
        </linearGradient>

        {/* Paved Entrance Forecourt Gradient */}
        <linearGradient id="hq-plaza-paving" x1="930" y1="735" x2="930" y2="785" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#1e2738" />
          <stop offset="40%" stopColor="#131b28" />
          <stop offset="100%" stopColor="#080c14" />
        </linearGradient>
      </defs>

      {/* ============================================================
          1. ROOFTOP ARCHITECTURAL LIGHTING & COMMUNICATIONS
      ============================================================ */}
      {/* Central High Telecom Mast on Atrium Roof */}
      <line x1="930" y1="435" x2="930" y2="350" stroke="#00e5ff" strokeWidth="2.5" filter="url(#hq-neon-cyan)" />
      <line x1="920" y1="385" x2="940" y2="385" stroke="#38bdf8" strokeWidth="1.5" />
      <line x1="923" y1="370" x2="937" y2="370" stroke="#38bdf8" strokeWidth="1.5" />
      <circle cx="930" cy="350" r="4" fill="#38bdf8" className="animate-ping" />
      <circle cx="930" cy="350" r="2.5" fill="#ffffff" />

      {/* Left Tower Aviation Obstruction Light */}
      <line x1="680" y1="495" x2="680" y2="440" stroke="#38bdf8" strokeWidth="2" />
      <circle cx="680" cy="440" r="3" fill="#ef4444" className="animate-pulse" />

      {/* Right Wing Aviation Obstruction Light */}
      <line x1="1200" y1="495" x2="1200" y2="445" stroke="#38bdf8" strokeWidth="2" />
      <circle cx="1200" cy="445" r="3" fill="#ef4444" className="animate-pulse" />

      {/* ============================================================
          2. LEFT ADMINISTRATIVE WING (CLEAR OF LEFT BRANDING)
             Positioned at x: 580 to 780 (Left branding is at x < 480)
      ============================================================ */}
      {/* Main Structural Mass of Left Wing */}
      <polygon
        points="580,530 780,480 780,750 580,750"
        fill="url(#hq-limestone-panel)"
        stroke="#1e293b"
        strokeWidth="1.5"
      />

      {/* Architectural Neon Accents on Roofline and Outer Edge */}
      <line x1="580" y1="530" x2="780" y2="480" stroke="#00e5ff" strokeWidth="3" filter="url(#hq-neon-cyan)" />
      <line x1="580" y1="530" x2="580" y2="750" stroke="#00e5ff" strokeWidth="2.5" filter="url(#hq-neon-cyan)" />
      <line x1="780" y1="480" x2="780" y2="750" stroke="#3b82f6" strokeWidth="3" filter="url(#hq-neon-blue)" />

      {/* Vertical Architectural Light Fins */}
      <line x1="630" y1="518" x2="630" y2="745" stroke="#0284c7" strokeWidth="1.5" opacity="0.75" />
      <line x1="680" y1="505" x2="680" y2="745" stroke="#0284c7" strokeWidth="1.5" opacity="0.75" />
      <line x1="730" y1="492" x2="730" y2="745" stroke="#0284c7" strokeWidth="1.5" opacity="0.75" />

      {/* Left Wing Ribbon Glass Windows with Interior Warm & Cool Lights */}
      <g opacity="0.9">
        {/* Floor 3 Windows */}
        <rect x="595" y="546" width="26" height="22" rx="2" fill="#38bdf8" fillOpacity="0.35" stroke="#0284c7" strokeWidth="0.8" />
        <rect x="645" y="536" width="28" height="22" rx="2" fill="#fde047" fillOpacity="0.4" stroke="#d97706" strokeWidth="0.8" />
        <rect x="695" y="526" width="28" height="22" rx="2" fill="#38bdf8" fillOpacity="0.3" stroke="#0284c7" strokeWidth="0.8" />
        <rect x="745" y="516" width="26" height="22" rx="2" fill="#60a5fa" fillOpacity="0.45" stroke="#2563eb" strokeWidth="0.8" />

        {/* Floor 2 Windows */}
        <rect x="595" y="596" width="26" height="22" rx="2" fill="#60a5fa" fillOpacity="0.4" stroke="#2563eb" strokeWidth="0.8" />
        <rect x="645" y="586" width="28" height="22" rx="2" fill="#38bdf8" fillOpacity="0.35" stroke="#0284c7" strokeWidth="0.8" />
        <rect x="695" y="576" width="28" height="22" rx="2" fill="#fde047" fillOpacity="0.45" stroke="#d97706" strokeWidth="0.8" />
        <rect x="745" y="566" width="26" height="22" rx="2" fill="#38bdf8" fillOpacity="0.35" stroke="#0284c7" strokeWidth="0.8" />

        {/* Floor 1 Windows */}
        <rect x="595" y="646" width="26" height="22" rx="2" fill="#fde047" fillOpacity="0.4" stroke="#d97706" strokeWidth="0.8" />
        <rect x="645" y="636" width="28" height="22" rx="2" fill="#60a5fa" fillOpacity="0.35" stroke="#2563eb" strokeWidth="0.8" />
        <rect x="695" y="626" width="28" height="22" rx="2" fill="#38bdf8" fillOpacity="0.4" stroke="#0284c7" strokeWidth="0.8" />
        <rect x="745" y="616" width="26" height="22" rx="2" fill="#fde047" fillOpacity="0.45" stroke="#d97706" strokeWidth="0.8" />
      </g>

      {/* Official Security Crest Medallion on Left Tower Facade */}
      <g id="hq-crest-medallion" transform="translate(600, 495)">
        <circle cx="16" cy="16" r="14" fill="#041226" stroke="#00e5ff" strokeWidth="1.5" filter="url(#hq-neon-cyan)" />
        <path d="M 16 6 L 24 10 L 24 17 C 24 22 16 26 16 26 C 16 26 8 22 8 17 L 8 10 Z" fill="#0284c7" opacity="0.6" />
        <polygon points="16,10 18,14 22,14 19,17 20,21 16,18 12,21 13,17 10,14 14,14" fill="#fef08a" />
      </g>

      {/* ============================================================
          3. MAIN CENTRAL ATRIUM & 2-STORY GLASS CURTAIN WALL
             Positioned at x: 770 to 1090
      ============================================================ */}
      {/* Massive Structural Portal Arch Framing the Curtain Wall */}
      <polygon
        points="770,470 1090,470 1090,750 770,750"
        fill="url(#hq-portal-frame)"
        stroke="#334155"
        strokeWidth="2"
      />

      {/* Neon Edge Highlight Along Portal Perimeter */}
      <polyline
        points="770,750 770,470 1090,470 1090,750"
        stroke="#00e5ff"
        strokeWidth="3.5"
        filter="url(#hq-neon-cyan)"
        fill="none"
      />

      {/* --- ATRIUM INTERIOR LIGHTING LAYERS --- */}
      {/* Mezzanine (Upper Tier) Interior Lighting */}
      <rect x="785" y="475" width="290" height="140" fill="url(#hq-atrium-mezzanine)" />
      {/* Ground Floor (Lower Tier) Reception Lobby Warm Lighting */}
      <rect x="785" y="615" width="290" height="130" fill="url(#hq-atrium-ground)" />

      {/* Interior Architectural Features (Lobby Depth) */}
      {/* Mezzanine Floor Slab Divider */}
      <rect x="785" y="610" width="290" height="8" fill="#1e293b" stroke="#334155" strokeWidth="1" />
      <line x1="785" y1="610" x2="1075" y2="610" stroke="#fef08a" strokeWidth="1.2" opacity="0.8" />

      {/* Ground Floor Reception Desk & Turnstiles Silhouette */}
      <rect x="880" y="700" width="100" height="26" rx="4" fill="#1e293b" stroke="#f59e0b" strokeWidth="1.5" />
      <rect x="895" y="694" width="70" height="7" rx="1.5" fill="#fef08a" opacity="0.9" />
      <circle cx="930" cy="675" r="7" fill="#1e293b" stroke="#f59e0b" strokeWidth="1" />
      {/* Ceiling Downlights in Grid */}
      <g fill="#ffffff" filter="url(#hq-neon-cyan)">
        <circle cx="820" cy="510" r="2.5" />
        <circle cx="870" cy="510" r="2.5" />
        <circle cx="930" cy="510" r="3" />
        <circle cx="990" cy="510" r="2.5" />
        <circle cx="1040" cy="510" r="2.5" />

        <circle cx="820" cy="560" r="2.5" />
        <circle cx="870" cy="560" r="2.5" />
        <circle cx="930" cy="560" r="3" />
        <circle cx="990" cy="560" r="2.5" />
        <circle cx="1040" cy="560" r="2.5" />

        <circle cx="820" cy="645" r="2.5" />
        <circle cx="870" cy="645" r="2.5" />
        <circle cx="930" cy="645" r="3.5" fill="#fef08a" />
        <circle cx="990" cy="645" r="2.5" />
        <circle cx="1040" cy="645" r="2.5" />
      </g>

      {/* --- STRUCTURAL GLASS CURTAIN WALL FACADE --- */}
      {/* Glass Surface Tint Overlay */}
      <rect x="785" y="475" width="290" height="270" fill="url(#hq-curtain-glass)" />

      {/* Primary Vertical Structural Mullions (Heavy Steel I-Beams) */}
      <g stroke="#1e293b" strokeWidth="3" opacity="0.9">
        <line x1="835" y1="475" x2="835" y2="745" />
        <line x1="885" y1="475" x2="885" y2="745" />
        <line x1="935" y1="475" x2="935" y2="745" stroke="#38bdf8" strokeWidth="2.5" opacity="0.8" />
        <line x1="985" y1="475" x2="985" y2="745" />
        <line x1="1035" y1="475" x2="1035" y2="745" />
      </g>

      {/* Horizontal Transoms */}
      <g stroke="#1e293b" strokeWidth="2" opacity="0.8">
        <line x1="785" y1="520" x2="1075" y2="520" />
        <line x1="785" y1="565" x2="1075" y2="565" />
        <line x1="785" y1="655" x2="1075" y2="655" />
        <line x1="785" y1="700" x2="1075" y2="700" />
      </g>

      {/* Specular Diagonal Glass Reflection Slashes */}
      <polygon
        points="785,475 920,475 830,745 785,745"
        fill="url(#hq-glass-specular)"
      />
      <polygon
        points="930,475 1060,475 980,745 900,745"
        fill="url(#hq-glass-specular)"
        opacity="0.75"
      />

      {/* --- ENTRANCE CANOPY & PORTAL --- */}
      {/* Cantilevered Steel & Glass Entrance Canopy */}
      <polygon
        points="805,600 1055,600 1065,616 795,616"
        fill="url(#hq-canopy-metal)"
        stroke="#38bdf8"
        strokeWidth="1.5"
      />
      <line x1="795" y1="616" x2="1065" y2="616" stroke="#00e5ff" strokeWidth="2" filter="url(#hq-neon-cyan)" />

      {/* Canopy Underside Recessed Spotlights Casting Light Cones on Pavement */}
      <g>
        <polygon points="830,616 850,616 880,745 800,745" fill="url(#canopy-downlight-cone)" opacity="0.6" />
        <polygon points="890,616 910,616 940,745 860,745" fill="url(#canopy-downlight-cone)" opacity="0.7" />
        <polygon points="950,616 970,616 1000,745 920,745" fill="url(#canopy-downlight-cone)" opacity="0.7" />
        <polygon points="1010,616 1030,616 1060,745 980,745" fill="url(#canopy-downlight-cone)" opacity="0.6" />
      </g>

      {/* Double Glass Revolving / Sliding Entry Doors */}
      <rect x="885" y="655" width="90" height="90" fill="#040b17" stroke="#38bdf8" strokeWidth="1.2" opacity="0.8" />
      <line x1="930" y1="655" x2="930" y2="745" stroke="#38bdf8" strokeWidth="1.5" />
      <rect x="910" y="695" width="4" height="20" rx="1" fill="#fef08a" />
      <rect x="946" y="695" width="4" height="20" rx="1" fill="#fef08a" />

      {/* ============================================================
          4. 3D INTEGRATED ILLUMINATED "DVLD" FACADE SIGNAGE
             Physically integrated onto the parapet above the entrance
      ============================================================ */}
      <g id="hq-dvld-sign" transform="translate(850, 435)">
        {/* Recessed Architectural Sign Box with Beveled Edges */}
        <rect
          x="0"
          y="0"
          width="160"
          height="50"
          rx="8"
          fill="#051024"
          stroke="#00e5ff"
          strokeWidth="2.5"
          filter="url(#hq-neon-cyan)"
        />
        <rect
          x="4"
          y="4"
          width="152"
          height="42"
          rx="6"
          fill="#030814"
          stroke="#0284c7"
          strokeWidth="1"
        />

        {/* 3D Drop Shadow for Letters */}
        <text
          x="81"
          y="37"
          fill="#002d5a"
          fontSize="33"
          fontWeight="900"
          fontFamily="system-ui, -apple-system, sans-serif"
          letterSpacing="5"
          textAnchor="middle"
        >
          DVLD
        </text>

        {/* Crisp Cool-White / Cyan Glowing Face Letters */}
        <text
          x="80"
          y="35"
          fill="#ffffff"
          fontSize="33"
          fontWeight="900"
          fontFamily="system-ui, -apple-system, sans-serif"
          letterSpacing="5"
          textAnchor="middle"
          filter="url(#hq-sign-glow)"
        >
          DVLD
        </text>
      </g>

      {/* ============================================================
          5. RIGHT WING ANNEX & ILLUMINATED ARABIC SIGNAGE
             Positioned at x: 1090 to 1300
      ============================================================ */}
      {/* Right Wing Structural Limestone Mass */}
      <polygon
        points="1090,520 1290,545 1290,750 1090,750"
        fill="url(#hq-limestone-panel)"
        stroke="#1e293b"
        strokeWidth="1.5"
      />

      {/* Right Wing Roofline & Perimeter Neon Edge */}
      <line x1="1090" y1="520" x2="1290" y2="545" stroke="#00e5ff" strokeWidth="3" filter="url(#hq-neon-cyan)" />
      <line x1="1290" y1="545" x2="1290" y2="750" stroke="#00e5ff" strokeWidth="2.5" filter="url(#hq-neon-cyan)" />

      {/* Illuminated Arabic Signage Plaque */}
      <g id="hq-arabic-sign" transform="translate(1105, 545)">
        <rect
          x="0"
          y="0"
          width="170"
          height="34"
          rx="6"
          fill="#041226"
          stroke="#00e5ff"
          strokeWidth="1.8"
          filter="url(#hq-neon-cyan)"
        />
        {/* Crisp Cyan Illuminated Arabic Department Title */}
        <text
          x="85"
          y="23"
          fill="#e0f2fe"
          fontSize="13.5"
          fontWeight="bold"
          fontFamily="'Segoe UI', Tahoma, Arial, sans-serif"
          textAnchor="middle"
          filter="url(#hq-neon-cyan)"
          direction="rtl"
        >
          دائرة ترخيص السواقين والمركبات
        </text>
      </g>

      {/* Ground Floor Horizontal Ribbon Glass Windows with Warm Interior Glow */}
      <g opacity="0.95">
        <rect x="1110" y="605" width="46" height="34" rx="2" fill="#fde047" fillOpacity="0.45" stroke="#d97706" strokeWidth="1" />
        <rect x="1165" y="605" width="48" height="34" rx="2" fill="#38bdf8" fillOpacity="0.35" stroke="#0284c7" strokeWidth="1" />
        <rect x="1222" y="605" width="48" height="34" rx="2" fill="#fde047" fillOpacity="0.5" stroke="#d97706" strokeWidth="1" />

        <rect x="1110" y="655" width="46" height="36" rx="2" fill="#38bdf8" fillOpacity="0.35" stroke="#0284c7" strokeWidth="1" />
        <rect x="1165" y="655" width="48" height="36" rx="2" fill="#fde047" fillOpacity="0.4" stroke="#d97706" strokeWidth="1" />
        <rect x="1222" y="655" width="48" height="36" rx="2" fill="#60a5fa" fillOpacity="0.45" stroke="#2563eb" strokeWidth="1" />
      </g>

      {/* ============================================================
          6. ENTRANCE PLAZA FORECOURT & LANDSCAPED SHRUBBERY
      ============================================================ */}
      {/* Stone Plaza Forecourt Blending into the Foreground Road */}
      <polygon
        points="550,785 580,735 1290,735 1320,785"
        fill="url(#hq-plaza-paving)"
      />

      {/* Architectural Low Planter Bed with Shrubbery Silhouettes */}
      <rect x="580" y="738" width="180" height="10" rx="3" fill="#0f172a" stroke="#1e293b" strokeWidth="1" />
      <g fill="#166534" opacity="0.85">
        <circle cx="595" cy="736" r="7" />
        <circle cx="615" cy="734" r="8" />
        <circle cx="635" cy="736" r="7" />
        <circle cx="655" cy="735" r="7.5" />
        <circle cx="675" cy="734" r="8" />
        <circle cx="695" cy="736" r="7" />
        <circle cx="715" cy="735" r="7.5" />
        <circle cx="735" cy="736" r="7" />
        <circle cx="755" cy="735" r="7.5" />
      </g>

      <rect x="1100" y="738" width="180" height="10" rx="3" fill="#0f172a" stroke="#1e293b" strokeWidth="1" />
      <g fill="#166534" opacity="0.85">
        <circle cx="1115" cy="736" r="7" />
        <circle cx="1135" cy="734" r="8" />
        <circle cx="1155" cy="736" r="7" />
        <circle cx="1175" cy="735" r="7.5" />
        <circle cx="1195" cy="734" r="8" />
        <circle cx="1215" cy="736" r="7" />
        <circle cx="1235" cy="735" r="7.5" />
        <circle cx="1255" cy="736" r="7" />
        <circle cx="1275" cy="735" r="7.5" />
      </g>

      {/* Ground Inground Uplights Illuminating Planters */}
      <g fill="#fef08a" opacity="0.75" filter="url(#hq-neon-cyan)">
        <circle cx="625" cy="742" r="2.5" />
        <circle cx="705" cy="742" r="2.5" />
        <circle cx="1145" cy="742" r="2.5" />
        <circle cx="1235" cy="742" r="2.5" />
      </g>
    </g>
  )
}
