export default function CitadelRuins() {
  return (
    <g id="citadel-ruins" className="citadel-ruins" aria-label="Amman Citadel and Temple of Hercules">
      <defs>
        {/* Soft Ambient Amber Floodlight Cone with Natural Falloff */}
        <linearGradient id="citadel-floodlight-primary" x1="1190" y1="520" x2="1190" y2="170" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#f59e0b" stopOpacity="0.88" />
          <stop offset="25%" stopColor="#fbbf24" stopOpacity="0.55" />
          <stop offset="60%" stopColor="#fde68a" stopOpacity="0.2" />
          <stop offset="85%" stopColor="#fef3c7" stopOpacity="0.06" />
          <stop offset="100%" stopColor="#fef3c7" stopOpacity="0" />
        </linearGradient>

        <linearGradient id="citadel-floodlight-secondary" x1="1310" y1="530" x2="1310" y2="230" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#d97706" stopOpacity="0.75" />
          <stop offset="35%" stopColor="#f59e0b" stopOpacity="0.4" />
          <stop offset="75%" stopColor="#fef3c7" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#fef3c7" stopOpacity="0" />
        </linearGradient>

        {/* Ambient Warm Atmosphere Glow */}
        <filter id="citadel-soft-amber-glow" x="-30%" y="-30%" width="160%" height="160%">
          <feGaussianBlur stdDeviation="15" result="blur" />
          <feComposite in="SourceGraphic" in2="blur" operator="over" />
        </filter>

        {/* Mountain Geological Strata Gradients */}
        <linearGradient id="jabal-distant-ridge" x1="1200" y1="200" x2="1200" y2="760" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#182234" />
          <stop offset="30%" stopColor="#101724" />
          <stop offset="65%" stopColor="#0a0e16" />
          <stop offset="100%" stopColor="#040609" />
        </linearGradient>

        <linearGradient id="jabal-midground-cliff" x1="1200" y1="310" x2="1200" y2="760" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#2c374c" />
          <stop offset="25%" stopColor="#1f2736" />
          <stop offset="65%" stopColor="#141a24" />
          <stop offset="100%" stopColor="#080b10" />
        </linearGradient>

        {/* Natural Weathered Limestone Rock Face (Warm floodlit wash) */}
        <linearGradient id="jabal-bedrock-wash" x1="1200" y1="530" x2="1200" y2="350" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#78350f" stopOpacity="0.9" />
          <stop offset="35%" stopColor="#b45309" stopOpacity="0.7" />
          <stop offset="70%" stopColor="#d97706" stopOpacity="0.45" />
          <stop offset="90%" stopColor="#f59e0b" stopOpacity="0.2" />
          <stop offset="100%" stopColor="#fbbf24" stopOpacity="0" />
        </linearGradient>

        {/* Classical Column Cylindrical 3D Limestone Gradient (Full Depth) */}
        <linearGradient id="col-cylindrical-limestone" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#451a03" />
          <stop offset="12%" stopColor="#78350f" />
          <stop offset="28%" stopColor="#b45309" />
          <stop offset="45%" stopColor="#f59e0b" />
          <stop offset="60%" stopColor="#fef08a" />
          <stop offset="78%" stopColor="#fbbf24" />
          <stop offset="90%" stopColor="#92400e" />
          <stop offset="100%" stopColor="#451a03" />
        </linearGradient>

        {/* Architrave Stone Beam Gradient */}
        <linearGradient id="architrave-entablature-grad" x1="1070" y1="240" x2="1225" y2="240" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#fef08a" />
          <stop offset="30%" stopColor="#fbbf24" />
          <stop offset="70%" stopColor="#d97706" />
          <stop offset="95%" stopColor="#92400e" />
          <stop offset="100%" stopColor="#5a2707" />
        </linearGradient>

        {/* Capital Corinthian Carved Gold/Limestone */}
        <linearGradient id="corinthian-capital-grad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#fef08a" />
          <stop offset="45%" stopColor="#f59e0b" />
          <stop offset="85%" stopColor="#b45309" />
          <stop offset="100%" stopColor="#78350f" />
        </linearGradient>

        {/* Classical Stylobate Step Shadow */}
        <linearGradient id="stylobate-drop-shadow" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#000000" stopOpacity="0.8" />
          <stop offset="100%" stopColor="#000000" stopOpacity="0" />
        </linearGradient>
      </defs>

      {/* ============================================================
          1. UPWARD AMBER ARCHITECTURAL FLOODLIGHTING
      ============================================================ */}
      {/* Primary Colonnade Upward Floodlight Cones */}
      <polygon
        points="1050,560 1370,560 1330,170 1080,170"
        fill="url(#citadel-floodlight-primary)"
        filter="url(#citadel-soft-amber-glow)"
        opacity="0.92"
      />
      <polygon
        points="1240,570 1500,570 1460,250 1270,250"
        fill="url(#citadel-floodlight-secondary)"
        filter="url(#citadel-soft-amber-glow)"
        opacity="0.7"
      />
      {/* Broad Ambient Warm Radiance behind Summit */}
      <circle cx="1200" cy="330" r="175" fill="#f59e0b" opacity="0.14" filter="url(#citadel-soft-amber-glow)" />

      {/* ============================================================
          2. JABAL AL-QAL'A MOUNTAIN RIDGE & AMMAN HILLSIDE TIERS
      ============================================================ */}
      {/* Background High Mountain Ridge (Rising majestically into the night sky) */}
      <path
        d="M 880 680 
           Q 960 490 1060 395 
           T 1190 305 
           T 1340 275 
           T 1530 325 
           T 1730 475 
           L 1730 760 
           L 880 760 Z"
        fill="url(#jabal-distant-ridge)"
      />

      {/* Amman Hillside Stacked Residential Silhouettes (Authentic white/limestone cubic houses of Amman) */}
      <g fill="#131c2a" stroke="#1e293b" strokeWidth="0.8" opacity="0.75">
        {/* Distant Ridge Houses & Retaining Walls */}
        <rect x="1350" y="278" width="14" height="9" rx="1" />
        <rect x="1366" y="275" width="18" height="12" rx="1" />
        <rect x="1386" y="280" width="16" height="10" rx="1" />
        <rect x="1404" y="284" width="22" height="14" rx="1" />
        <rect x="1428" y="290" width="18" height="12" rx="1" />
        <rect x="1448" y="296" width="24" height="16" rx="1" />
        <rect x="1474" y="306" width="20" height="14" rx="1" />
        <rect x="1496" y="314" width="26" height="16" rx="1" />
        <rect x="1524" y="324" width="22" height="15" rx="1" />

        {/* Lower Ridge Houses on Jabal Amman / Jabal Al-Weibdeh */}
        <rect x="1370" y="292" width="20" height="13" rx="1" fill="#182334" />
        <rect x="1392" y="298" width="25" height="15" rx="1" fill="#182334" />
        <rect x="1420" y="306" width="22" height="14" rx="1" fill="#182334" />
        <rect x="1445" y="316" width="26" height="16" rx="1" fill="#182334" />
        <rect x="1473" y="326" width="24" height="15" rx="1" fill="#182334" />
      </g>

      {/* Lit Windows & Streetlight Points in Distant Amman Hillside */}
      <g opacity="0.85">
        <circle cx="1357" cy="282" r="1.2" fill="#fde047" />
        <circle cx="1375" cy="281" r="1.5" fill="#fef08a" />
        <circle cx="1394" cy="285" r="1.2" fill="#fde047" />
        <circle cx="1415" cy="291" r="1.4" fill="#f59e0b" />
        <circle cx="1437" cy="296" r="1.2" fill="#e0f2fe" />
        <circle cx="1460" cy="304" r="1.5" fill="#fde047" />
        <circle cx="1484" cy="313" r="1.3" fill="#fef08a" />
        <circle cx="1509" cy="322" r="1.5" fill="#f59e0b" />
        <circle cx="1535" cy="332" r="1.3" fill="#e0f2fe" />

        <circle cx="1380" cy="298" r="1.4" fill="#fde047" />
        <circle cx="1404" cy="305" r="1.6" fill="#fef08a" />
        <circle cx="1431" cy="313" r="1.3" fill="#f59e0b" />
        <circle cx="1458" cy="324" r="1.5" fill="#fde047" />
        <circle cx="1485" cy="333" r="1.4" fill="#fef08a" />
      </g>

      {/* Midground Terraced Rocky Cliff Formation (Jabal Al-Qal'a Archaeological Plateau) */}
      <path
        d="M 910 720 
           Q 1010 530 1100 425 
           T 1230 355 
           T 1380 335 
           T 1540 395 
           T 1680 525 
           L 1680 760 
           L 910 760 Z"
        fill="url(#jabal-midground-cliff)"
      />

      {/* Natural Limestone Cliff Strata & Eroded Ledges */}
      <path
        d="M 1040 540 Q 1140 445 1260 455 T 1460 475 L 1480 515 Q 1320 495 1180 485 T 1030 545 Z"
        fill="#212a3a"
      />
      <path
        d="M 1090 465 Q 1190 405 1330 415 T 1530 435 L 1550 465 Q 1380 445 1230 435 T 1080 485 Z"
        fill="#2b3648"
      />
      <path
        d="M 1160 415 Q 1270 375 1410 385 T 1600 415 L 1610 435 Q 1440 405 1290 395 T 1150 435 Z"
        fill="#1e2736"
      />

      {/* Warm Amber Floodlight Wash on Bedrock Strata Directly beneath Ruins */}
      <path
        d="M 1060 485 Q 1180 395 1320 405 T 1520 425 L 1460 545 Q 1280 515 1140 515 Z"
        fill="url(#jabal-bedrock-wash)"
        opacity="0.55"
      />

      {/* Geological Crags & Crevices Texture Lines (Weathered Limestone Faults) */}
      <g stroke="#64748b" strokeWidth="1.2" opacity="0.38" fill="none">
        <path d="M 1020 530 L 1080 488 L 1150 498 L 1220 468 L 1310 478 L 1420 448 L 1540 468" />
        <path d="M 1070 478 L 1130 438 L 1210 448 L 1290 408 L 1390 418 L 1490 388 L 1600 418" />
        <path d="M 1120 438 L 1180 388 L 1270 398 L 1360 368 L 1460 378 L 1580 348" />
        <path d="M 1010 610 Q 1140 528 1290 538 T 1560 548" strokeWidth="1.5" />
      </g>

      {/* Ancient Ashlar Fortification Wall Courses (Ammonite, Roman & Umayyad Citadel Walls) */}
      <g fill="#273244" stroke="#3b485d" strokeWidth="0.8" opacity="0.85">
        {/* Upper Course */}
        <rect x="1055" y="442" width="28" height="11" rx="1" />
        <rect x="1086" y="442" width="34" height="11" rx="1" />
        <rect x="1123" y="442" width="30" height="11" rx="1" />
        <rect x="1285" y="422" width="32" height="11" rx="1" />
        <rect x="1320" y="422" width="28" height="11" rx="1" />
        <rect x="1351" y="422" width="34" height="11" rx="1" />

        {/* Lower Course */}
        <rect x="1070" y="455" width="32" height="11" rx="1" />
        <rect x="1105" y="455" width="30" height="11" rx="1" />
        <rect x="1138" y="455" width="26" height="11" rx="1" />
        <rect x="1300" y="435" width="34" height="11" rx="1" />
        <rect x="1337" y="435" width="30" height="11" rx="1" />
      </g>

      {/* ============================================================
          3. TEMPLE OF HERCULES — CLASSICAL ROMAN COLONNADE ON THE SUMMIT
      ============================================================ */}
      {/* Ancient Stepped Crepidoma / Stylobate Podium (Integrated Bedrock Platform) */}
      {/* Lower Platform Tier (Earthy umber stone foundation) */}
      <polygon
        points="1055,406 1325,406 1317,420 1063,420"
        fill="#3b1d08"
        stroke="#5a2707"
        strokeWidth="1"
      />
      {/* Middle Podium Step */}
      <polygon
        points="1064,396 1316,396 1310,406 1070,406"
        fill="#78350f"
        stroke="#92400e"
        strokeWidth="1"
      />
      {/* Upper Stylobate Step (Lit by warm floodlights) */}
      <polygon
        points="1072,386 1308,386 1304,396 1076,396"
        fill="#b45309"
        stroke="#d97706"
        strokeWidth="0.8"
      />

      {/* Classical Architrave & Entablature Beam across Columns 1, 2, 3 */}
      <g id="temple-architrave">
        {/* Main Lower Architrave Beam */}
        <polygon
          points="1078,248 1206,248 1206,262 1078,262"
          fill="url(#architrave-entablature-grad)"
          stroke="#92400e"
          strokeWidth="1"
        />
        {/* Classical Molded Frieze Band */}
        <polygon
          points="1075,240 1209,240 1206,248 1078,248"
          fill="#fbbf24"
          stroke="#b45309"
          strokeWidth="0.8"
        />
        {/* Dentils / Classical Modillion Teeth */}
        <g fill="#78350f" opacity="0.9">
          <rect x="1081" y="244" width="5" height="3.5" />
          <rect x="1091" y="244" width="5" height="3.5" />
          <rect x="1101" y="244" width="5" height="3.5" />
          <rect x="1111" y="244" width="5" height="3.5" />
          <rect x="1121" y="244" width="5" height="3.5" />
          <rect x="1131" y="244" width="5" height="3.5" />
          <rect x="1141" y="244" width="5" height="3.5" />
          <rect x="1151" y="244" width="5" height="3.5" />
          <rect x="1161" y="244" width="5" height="3.5" />
          <rect x="1171" y="244" width="5" height="3.5" />
          <rect x="1181" y="244" width="5" height="3.5" />
          <rect x="1191" y="244" width="5" height="3.5" />
          <rect x="1201" y="244" width="5" height="3.5" />
        </g>
      </g>

      {/* --- COLUMN 1 (Full Height, Supporting Architrave) --- */}
      <g id="column-hercules-1">
        {/* Attic Column Base (Plinth, Torus & Scotia) */}
        <rect x="1084" y="378" width="22" height="8" rx="1.5" fill="#78350f" stroke="#92400e" strokeWidth="0.8" />
        <rect x="1087" y="374" width="16" height="4" rx="1" fill="#b45309" />
        {/* Cylindrical Fluted Shaft */}
        <rect x="1089" y="262" width="12" height="112" rx="1" fill="url(#col-cylindrical-limestone)" />
        {/* Fluting Channels (3D Longitudinal Crevices) */}
        <line x1="1091" y1="262" x2="1091" y2="374" stroke="#451a03" strokeWidth="0.8" opacity="0.8" />
        <line x1="1094" y1="262" x2="1094" y2="374" stroke="#fef08a" strokeWidth="1" opacity="0.9" />
        <line x1="1097" y1="262" x2="1097" y2="374" stroke="#78350f" strokeWidth="0.8" opacity="0.65" />
        {/* Horizontal Drum Seams (Ancient Stacked Stone Cylinders) */}
        <line x1="1089" y1="290" x2="1101" y2="290" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1089" y1="318" x2="1101" y2="318" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1089" y1="346" x2="1101" y2="346" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        {/* Corinthian Capital (Carved Acanthus Leaves & Volutes) */}
        <path
          d="M 1083 262 L 1107 262 L 1104 250 L 1086 250 Z"
          fill="url(#corinthian-capital-grad)"
          stroke="#b45309"
          strokeWidth="0.8"
        />
        <circle cx="1085" cy="252" r="2.2" fill="#fef08a" />
        <circle cx="1105" cy="252" r="2.2" fill="#fef08a" />
      </g>

      {/* --- COLUMN 2 (Full Height, Supporting Architrave) --- */}
      <g id="column-hercules-2">
        <rect x="1120" y="378" width="22" height="8" rx="1.5" fill="#78350f" stroke="#92400e" strokeWidth="0.8" />
        <rect x="1123" y="374" width="16" height="4" rx="1" fill="#b45309" />
        <rect x="1125" y="262" width="12" height="112" rx="1" fill="url(#col-cylindrical-limestone)" />
        <line x1="1127" y1="262" x2="1127" y2="374" stroke="#451a03" strokeWidth="0.8" opacity="0.8" />
        <line x1="1130" y1="262" x2="1130" y2="374" stroke="#fef08a" strokeWidth="1" opacity="0.9" />
        <line x1="1133" y1="262" x2="1133" y2="374" stroke="#78350f" strokeWidth="0.8" opacity="0.65" />
        <line x1="1125" y1="290" x2="1137" y2="290" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1125" y1="318" x2="1137" y2="318" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1125" y1="346" x2="1137" y2="346" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <path
          d="M 1119 262 L 1143 262 L 1140 250 L 1122 250 Z"
          fill="url(#corinthian-capital-grad)"
          stroke="#b45309"
          strokeWidth="0.8"
        />
        <circle cx="1121" cy="252" r="2.2" fill="#fef08a" />
        <circle cx="1141" cy="252" r="2.2" fill="#fef08a" />
      </g>

      {/* --- COLUMN 3 (Full Height, Supporting Architrave) --- */}
      <g id="column-hercules-3">
        <rect x="1156" y="378" width="22" height="8" rx="1.5" fill="#78350f" stroke="#92400e" strokeWidth="0.8" />
        <rect x="1159" y="374" width="16" height="4" rx="1" fill="#b45309" />
        <rect x="1161" y="262" width="12" height="112" rx="1" fill="url(#col-cylindrical-limestone)" />
        <line x1="1163" y1="262" x2="1163" y2="374" stroke="#451a03" strokeWidth="0.8" opacity="0.8" />
        <line x1="1166" y1="262" x2="1166" y2="374" stroke="#fef08a" strokeWidth="1" opacity="0.9" />
        <line x1="1169" y1="262" x2="1169" y2="374" stroke="#78350f" strokeWidth="0.8" opacity="0.65" />
        <line x1="1161" y1="290" x2="1173" y2="290" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1161" y1="318" x2="1173" y2="318" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1161" y1="346" x2="1173" y2="346" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <path
          d="M 1155 262 L 1179 262 L 1176 250 L 1158 250 Z"
          fill="url(#corinthian-capital-grad)"
          stroke="#b45309"
          strokeWidth="0.8"
        />
        <circle cx="1157" cy="252" r="2.2" fill="#fef08a" />
        <circle cx="1177" cy="252" r="2.2" fill="#fef08a" />
      </g>

      {/* --- COLUMN 4 (Full Height Free-Standing with Capital) --- */}
      <g id="column-hercules-4">
        <rect x="1192" y="378" width="22" height="8" rx="1.5" fill="#78350f" stroke="#92400e" strokeWidth="0.8" />
        <rect x="1195" y="374" width="16" height="4" rx="1" fill="#b45309" />
        <rect x="1197" y="268" width="12" height="106" rx="1" fill="url(#col-cylindrical-limestone)" />
        <line x1="1199" y1="268" x2="1199" y2="374" stroke="#451a03" strokeWidth="0.8" opacity="0.8" />
        <line x1="1202" y1="268" x2="1202" y2="374" stroke="#fef08a" strokeWidth="1" opacity="0.9" />
        <line x1="1205" y1="268" x2="1205" y2="374" stroke="#78350f" strokeWidth="0.8" opacity="0.65" />
        <line x1="1197" y1="298" x2="1209" y2="298" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <line x1="1197" y1="334" x2="1209" y2="334" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
        <path
          d="M 1191 268 L 1215 268 L 1212 256 L 1194 256 Z"
          fill="url(#corinthian-capital-grad)"
          stroke="#b45309"
          strokeWidth="0.8"
        />
        <circle cx="1193" cy="258" r="2.2" fill="#fef08a" />
        <circle cx="1213" cy="258" r="2.2" fill="#fef08a" />
      </g>

      {/* --- COLUMN 5 (Broken Ancient Ruin Column with Jagged Eroded Stone Fracture) --- */}
      <g id="column-hercules-broken-1">
        <rect x="1228" y="378" width="22" height="8" rx="1.5" fill="#78350f" stroke="#92400e" strokeWidth="0.8" />
        <rect x="1231" y="374" width="16" height="4" rx="1" fill="#b45309" />
        {/* Fractured Column Shaft */}
        <polygon
          points="1233,374 1233,306 1236,298 1240,302 1243,294 1245,304 1245,374"
          fill="url(#col-cylindrical-limestone)"
        />
        <line x1="1235" y1="302" x2="1235" y2="374" stroke="#451a03" strokeWidth="0.8" opacity="0.8" />
        <line x1="1238" y1="298" x2="1238" y2="374" stroke="#fef08a" strokeWidth="1" opacity="0.9" />
        <line x1="1241" y1="300" x2="1241" y2="374" stroke="#78350f" strokeWidth="0.8" opacity="0.65" />
        <line x1="1233" y1="336" x2="1245" y2="336" stroke="#451a03" strokeWidth="0.8" opacity="0.65" />
      </g>

      {/* --- COLUMN 6 (Short Ancient Column Base Drum) --- */}
      <g id="column-hercules-broken-2">
        <rect x="1264" y="378" width="22" height="8" rx="1.5" fill="#78350f" stroke="#92400e" strokeWidth="0.8" />
        <rect x="1267" y="374" width="16" height="4" rx="1" fill="#b45309" />
        <polygon
          points="1269,374 1269,342 1273,336 1277,340 1281,344 1281,374"
          fill="url(#col-cylindrical-limestone)"
        />
        <line x1="1272" y1="338" x2="1272" y2="374" stroke="#451a03" strokeWidth="0.8" opacity="0.8" />
        <line x1="1275" y1="338" x2="1275" y2="374" stroke="#fef08a" strokeWidth="1" opacity="0.9" />
      </g>

      {/* Scattered Fallen Ashlar Blocks & Ancient Cornice Stones */}
      <g fill="#78350f" stroke="#92400e" strokeWidth="0.8" opacity="0.9">
        <polygon points="1286,382 1306,382 1303,394 1283,394" />
        <polygon points="1246,384 1260,384 1258,392 1244,392" />
        <polygon points="1068,388 1082,388 1080,398 1066,398" />
      </g>
    </g>
  )
}
