import * as THREE from 'three';
import { GLTFExporter } from 'three/examples/jsm/exporters/GLTFExporter.js';
import fs from 'fs';
import path from 'path';

// Node.js FileReader Polyfill for GLTFExporter binary GLB serialization
if (typeof globalThis.FileReader === 'undefined') {
  globalThis.FileReader = class FileReader {
    readAsArrayBuffer(blob) {
      blob.arrayBuffer().then((buf) => {
        this.result = buf;
        if (this.onload) this.onload({ target: this });
        if (this.onloadend) this.onloadend({ target: this });
      }).catch((err) => {
        if (this.onerror) this.onerror(err);
        if (this.onloadend) this.onloadend({ target: this });
      });
    }
  };
}

const PUBLIC_DIR = path.resolve('public');
const HQ_DIR = path.join(PUBLIC_DIR, 'models', 'headquarters');
const CITADEL_DIR = path.join(PUBLIC_DIR, 'models', 'citadel');

fs.mkdirSync(HQ_DIR, { recursive: true });
fs.mkdirSync(CITADEL_DIR, { recursive: true });

// ============================================================================
// PBR MATERIAL DEFINITIONS (ARCHITECTURAL SPECIFICATION)
// ============================================================================
const materials = {
  // Architectural Limestone Facade Panels
  limestonePanel: new THREE.MeshStandardMaterial({
    name: 'LimestonePanel',
    color: new THREE.Color('#334155'), // Modern deep architectural limestone
    roughness: 0.78,
    metalness: 0.08,
  }),
  limestoneWarmAccent: new THREE.MeshStandardMaterial({
    name: 'LimestoneWarmAccent',
    color: new THREE.Color('#475569'),
    roughness: 0.72,
    metalness: 0.1,
  }),
  // Dark Anodized Structural Aluminum / Steel
  darkStructuralMetal: new THREE.MeshStandardMaterial({
    name: 'DarkStructuralMetal',
    color: new THREE.Color('#1e293b'),
    roughness: 0.35,
    metalness: 0.82,
  }),
  darkSpandrelPanel: new THREE.MeshStandardMaterial({
    name: 'DarkSpandrelPanel',
    color: new THREE.Color('#090d16'),
    roughness: 0.35,
    metalness: 0.82,
  }),
  // Photorealistic Architectural Glass (Neutral Deep Blue/Slate Tint, High Specular, Realistic Transparency)
  curtainGlass: new THREE.MeshStandardMaterial({
    name: 'CurtainWallGlass',
    color: new THREE.Color('#0b172a'), // Deep architectural midnight glass
    roughness: 0.04,
    metalness: 0.55,
    transparent: true,
    opacity: 0.42,
  }),
  interiorGlassBalustrade: new THREE.MeshStandardMaterial({
    name: 'InteriorGlassBalustrade',
    color: new THREE.Color('#64748b'),
    roughness: 0.03,
    metalness: 0.1,
    transparent: true,
    opacity: 0.32,
  }),
  // Warm Atrium Interior Illumination (3000K)
  atriumWarmGlow: new THREE.MeshStandardMaterial({
    name: 'AtriumWarmGlow',
    color: new THREE.Color('#fffbeb'),
    emissive: new THREE.Color('#f59e0b'),
    emissiveIntensity: 2.8,
    roughness: 0.2,
  }),
  interiorWoodPanels: new THREE.MeshStandardMaterial({
    name: 'InteriorWoodPanels',
    color: new THREE.Color('#452a18'),
    roughness: 0.65,
    metalness: 0.05,
  }),
  // Architectural Neon Lighting Trim
  cyanNeon: new THREE.MeshStandardMaterial({
    name: 'CyanNeon',
    color: new THREE.Color('#00e5ff'),
    emissive: new THREE.Color('#00e5ff'),
    emissiveIntensity: 2.2,
    roughness: 0.1,
  }),
  coolWhiteSign: new THREE.MeshStandardMaterial({
    name: 'CoolWhiteSign',
    color: new THREE.Color('#ffffff'),
    emissive: new THREE.Color('#f8fafc'),
    emissiveIntensity: 2.4,
    roughness: 0.1,
  }),
  warmCanopySpot: new THREE.MeshStandardMaterial({
    name: 'WarmCanopySpot',
    color: new THREE.Color('#ffffff'),
    emissive: new THREE.Color('#fef08a'),
    emissiveIntensity: 3.2,
    roughness: 0.1,
  }),
  // Ancient Temple of Hercules Weathered Limestone
  ancientLimestone: new THREE.MeshStandardMaterial({
    name: 'AncientLimestone',
    color: new THREE.Color('#b8874e'),
    roughness: 0.86,
    metalness: 0.05,
  }),
  ancientLimestoneDark: new THREE.MeshStandardMaterial({
    name: 'AncientLimestoneDark',
    color: new THREE.Color('#8a5e30'),
    roughness: 0.92,
    metalness: 0.04,
  }),
  // Jabal Al-Qal'a Bedrock Cliffs
  bedrockStone: new THREE.MeshStandardMaterial({
    name: 'BedrockStone',
    color: new THREE.Color('#1e293b'),
    roughness: 0.95,
    metalness: 0.04,
  }),
  marbleFragment: new THREE.MeshStandardMaterial({
    name: 'MarbleFragment',
    color: new THREE.Color('#e2e8f0'),
    roughness: 0.42,
    metalness: 0.12,
  }),
  // Landscaping
  planterFoliage: new THREE.MeshStandardMaterial({
    name: 'PlanterFoliage',
    color: new THREE.Color('#143524'),
    roughness: 0.85,
    metalness: 0.02,
  }),
};

// ============================================================================
// PROCEDURAL ARCHITECTURAL GEOMETRY GENERATORS
// ============================================================================

/**
 * Procedural Roman Fluted Column Geometry
 * Accurately models classical 24-flute concave scallops and entasis taper
 */
function createFlutedColumnGeometry(radiusBase, radiusTop, height, numFlutes = 24, heightSegments = 32, radialSegments = 96) {
  const geom = new THREE.BufferGeometry();
  const positions = [];
  const normals = [];
  const uvs = [];
  const indices = [];

  const fluteDepthRatio = 0.038;

  for (let yIndex = 0; yIndex <= heightSegments; yIndex++) {
    const v = yIndex / heightSegments;
    const y = v * height;
    // Entasis subtle curve (gentle swell in lower third)
    const entasis = Math.sin(v * Math.PI * 0.85) * 0.012;
    const taper = radiusBase + (radiusTop - radiusBase) * v + entasis;

    for (let xIndex = 0; xIndex <= radialSegments; xIndex++) {
      const u = xIndex / radialSegments;
      const theta = u * Math.PI * 2;

      // Concave flute indentation wave
      const fluteWave = Math.pow(Math.cos(theta * (numFlutes / 2)), 2);
      const r = taper - (taper * fluteDepthRatio * fluteWave);

      const px = Math.cos(theta) * r;
      const pz = Math.sin(theta) * r;

      positions.push(px, y, pz);

      const nx = Math.cos(theta);
      const nz = Math.sin(theta);
      normals.push(nx, 0, nz);
      uvs.push(u * 2, v * 4);
    }
  }

  const rowSize = radialSegments + 1;
  for (let y = 0; y < heightSegments; y++) {
    for (let x = 0; x < radialSegments; x++) {
      const a = y * rowSize + x;
      const b = (y + 1) * rowSize + x;
      const c = (y + 1) * rowSize + (x + 1);
      const d = y * rowSize + (x + 1);

      indices.push(a, b, d);
      indices.push(b, c, d);
    }
  }

  geom.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  geom.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));
  geom.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
  geom.setIndex(indices);
  geom.computeVertexNormals();

  return geom;
}

/**
 * Broken Ruin Column Top (Jagged fractured stone cap)
 */
function createBrokenColumnGeometry(radiusBase, radiusTop, height, jaggedness = 0.28) {
  const geom = createFlutedColumnGeometry(radiusBase, radiusTop, height, 20, 24, 60);
  const pos = geom.attributes.position;

  for (let i = 0; i < pos.count; i++) {
    const y = pos.getY(i);
    if (y > height * 0.72) {
      const x = pos.getX(i);
      const z = pos.getZ(i);
      const angle = Math.atan2(z, x);
      const fracture = Math.sin(angle * 3.5) * Math.cos(angle * 2.1) * jaggedness;
      pos.setY(i, y + fracture);
    }
  }

  geom.computeVertexNormals();
  return geom;
}

/**
 * Detailed Corinthian Capital
 */
function createCorinthianCapital() {
  const capitalGroup = new THREE.Group();

  // 1. Kalathos (Inverted Bell Core)
  const kalathos = new THREE.Mesh(
    new THREE.CylinderGeometry(0.32, 0.22, 0.38, 24),
    materials.ancientLimestone
  );
  kalathos.position.set(0, 0.19, 0);
  capitalGroup.add(kalathos);

  // 2. Lower Tier Acanthus Leaves (8 radiating curved leaves)
  for (let i = 0; i < 8; i++) {
    const angle = (i / 8) * Math.PI * 2;
    const leaf = new THREE.Mesh(
      new THREE.BoxGeometry(0.08, 0.22, 0.04),
      materials.ancientLimestone
    );
    leaf.position.set(Math.cos(angle) * 0.21, 0.12, Math.sin(angle) * 0.21);
    leaf.rotation.y = -angle;
    leaf.rotation.x = 0.18;
    capitalGroup.add(leaf);
  }

  // 3. Upper Tier Acanthus Leaves (8 radiating leaves, offset 22.5 deg)
  for (let i = 0; i < 8; i++) {
    const angle = ((i + 0.5) / 8) * Math.PI * 2;
    const leaf = new THREE.Mesh(
      new THREE.BoxGeometry(0.09, 0.26, 0.04),
      materials.ancientLimestone
    );
    leaf.position.set(Math.cos(angle) * 0.25, 0.22, Math.sin(angle) * 0.25);
    leaf.rotation.y = -angle;
    leaf.rotation.x = 0.26;
    capitalGroup.add(leaf);
  }

  // 4. Four Classical Corner Volutes
  const voluteAngles = [Math.PI / 4, 3 * Math.PI / 4, 5 * Math.PI / 4, 7 * Math.PI / 4];
  voluteAngles.forEach((angle) => {
    const volute = new THREE.Mesh(
      new THREE.TorusGeometry(0.065, 0.024, 8, 16, Math.PI * 1.5),
      materials.ancientLimestone
    );
    volute.position.set(Math.cos(angle) * 0.28, 0.34, Math.sin(angle) * 0.28);
    volute.rotation.y = -angle + Math.PI / 2;
    capitalGroup.add(volute);
  });

  // 5. Molded Abacus Slab
  const abacusLower = new THREE.Mesh(
    new THREE.BoxGeometry(0.48, 0.06, 0.48),
    materials.ancientLimestone
  );
  abacusLower.position.set(0, 0.38, 0);
  capitalGroup.add(abacusLower);

  const abacusUpper = new THREE.Mesh(
    new THREE.BoxGeometry(0.52, 0.05, 0.52),
    materials.ancientLimestone
  );
  abacusUpper.position.set(0, 0.43, 0);
  capitalGroup.add(abacusUpper);

  // Central Rosette Fleuron on abacus front
  const rosette = new THREE.Mesh(
    new THREE.SphereGeometry(0.035, 8, 8),
    materials.ancientLimestoneDark
  );
  rosette.position.set(0, 0.41, 0.27);
  capitalGroup.add(rosette);

  return capitalGroup;
}

// ============================================================================
// 1. REBUILD DVLD HEADQUARTERS ARCHITECTURE
// ============================================================================
function buildHeadquarters() {
  const hq = new THREE.Group();
  hq.name = 'DVLD_Headquarters_Architecture';

  // --------------------------------------------------------------------------
  // A. CENTRAL 2-STORY RECEPTION ATRIUM (Hollow, Multi-Layered, Fully Detailed)
  // --------------------------------------------------------------------------
  const atriumGroup = new THREE.Group();
  atriumGroup.name = 'Central_Atrium_Pavilion';

  // Atrium Enclosing Shell: Structural Back Wall (Deep Charcoal Architectural Finish)
  const backWall = new THREE.Mesh(
    new THREE.BoxGeometry(5.4, 4.0, 0.25),
    materials.darkStructuralMetal
  );
  backWall.position.set(0, 2.0, -1.8);
  backWall.castShadow = true;
  backWall.receiveShadow = true;
  atriumGroup.add(backWall);

  // Interior Feature Backdrop: Dark Wood Slatted Paneling with Warm Accent Reveals
  for (let sx = -2.3; sx <= 2.3; sx += 0.25) {
    const slat = new THREE.Mesh(
      new THREE.BoxGeometry(0.12, 3.8, 0.04),
      materials.interiorWoodPanels
    );
    slat.position.set(sx, 2.0, -1.65);
    atriumGroup.add(slat);
  }

  // Horizontal Warm LED Accent Strip on Back Wall (Recessed Cove Lighting)
  const coveLight = new THREE.Mesh(
    new THREE.BoxGeometry(4.8, 0.08, 0.04),
    materials.atriumWarmGlow
  );
  coveLight.position.set(0, 2.1, -1.62);
  atriumGroup.add(coveLight);

  // Atrium Concrete Roof Slab
  const roofSlab = new THREE.Mesh(
    new THREE.BoxGeometry(5.6, 0.25, 2.6),
    materials.darkStructuralMetal
  );
  roofSlab.position.set(0, 4.05, -0.6);
  roofSlab.castShadow = true;
  atriumGroup.add(roofSlab);

  // Polished Terrazzo Ground Floor
  const groundFloor = new THREE.Mesh(
    new THREE.BoxGeometry(5.4, 0.1, 2.5),
    materials.darkStructuralMetal
  );
  groundFloor.position.set(0, 0.05, -0.6);
  groundFloor.receiveShadow = true;
  atriumGroup.add(groundFloor);

  // Heavy Structural Concrete Side Columns (Left & Right Flanking Piers)
  const leftPier = new THREE.Mesh(
    new THREE.BoxGeometry(0.4, 4.0, 2.6),
    materials.darkStructuralMetal
  );
  leftPier.position.set(-2.6, 2.0, -0.6);
  atriumGroup.add(leftPier);

  const rightPier = new THREE.Mesh(
    new THREE.BoxGeometry(0.4, 4.0, 2.6),
    materials.darkStructuralMetal
  );
  rightPier.position.set(2.6, 2.0, -0.6);
  atriumGroup.add(rightPier);

  // --------------------------------------------------------------------------
  // B. ATRIUM INTERIOR: MEZZANINE, RECEPTION & FURNITURE
  // --------------------------------------------------------------------------
  // Upper Mezzanine Floor Slab (Second Floor Administrative Balcony)
  const mezzanineSlab = new THREE.Mesh(
    new THREE.BoxGeometry(5.0, 0.16, 1.3),
    materials.darkStructuralMetal
  );
  mezzanineSlab.position.set(0, 1.95, -0.95);
  atriumGroup.add(mezzanineSlab);

  // Glowing Warm Mezzanine Fascia Light Strip
  const mezzanineFasciaLight = new THREE.Mesh(
    new THREE.BoxGeometry(5.0, 0.04, 0.02),
    materials.atriumWarmGlow
  );
  mezzanineFasciaLight.position.set(0, 1.95, -0.28);
  atriumGroup.add(mezzanineFasciaLight);

  // Mezzanine Glass Balustrade / Railing
  const balustrade = new THREE.Mesh(
    new THREE.BoxGeometry(4.9, 0.45, 0.03),
    materials.interiorGlassBalustrade
  );
  balustrade.position.set(0, 2.25, -0.29);
  atriumGroup.add(balustrade);

  // Balustrade Brushed Metal Handrail
  const handrail = new THREE.Mesh(
    new THREE.BoxGeometry(5.0, 0.04, 0.06),
    materials.darkStructuralMetal
  );
  handrail.position.set(0, 2.49, -0.29);
  atriumGroup.add(handrail);

  // 2 Interior Lobby Structural Columns
  const lobbyCol1 = new THREE.Mesh(
    new THREE.CylinderGeometry(0.09, 0.09, 3.8, 16),
    materials.darkStructuralMetal
  );
  lobbyCol1.position.set(-1.4, 1.95, -0.5);
  atriumGroup.add(lobbyCol1);

  const lobbyCol2 = new THREE.Mesh(
    new THREE.CylinderGeometry(0.09, 0.09, 3.8, 16),
    materials.darkStructuralMetal
  );
  lobbyCol2.position.set(1.4, 1.95, -0.5);
  atriumGroup.add(lobbyCol2);

  // Curved Reception Counter
  const receptionDesk = new THREE.Mesh(
    new THREE.BoxGeometry(1.9, 0.44, 0.6),
    materials.darkStructuralMetal
  );
  receptionDesk.position.set(0, 0.22, -0.2);
  atriumGroup.add(receptionDesk);

  // Illuminated Warm Front Panel on Reception Desk
  const receptionLightPanel = new THREE.Mesh(
    new THREE.BoxGeometry(1.75, 0.32, 0.02),
    materials.atriumWarmGlow
  );
  receptionLightPanel.position.set(0, 0.22, 0.11);
  atriumGroup.add(receptionLightPanel);

  // Waiting Lounge Seating Cubes
  [-1.5, 1.5].forEach((lx) => {
    const seat = new THREE.Mesh(
      new THREE.BoxGeometry(0.9, 0.26, 0.45),
      materials.limestonePanel
    );
    seat.position.set(lx, 0.13, -0.15);
    atriumGroup.add(seat);
  });

  // Suspended Ceiling Warm Downlight Panel Matrices
  for (let zx = -1.2; zx <= 0; zx += 0.6) {
    for (let xx = -1.8; xx <= 1.8; xx += 0.9) {
      const troffer = new THREE.Mesh(
        new THREE.BoxGeometry(0.35, 0.02, 0.35),
        materials.atriumWarmGlow
      );
      troffer.position.set(xx, 3.92, zx);
      atriumGroup.add(troffer);
    }
  }

  // --------------------------------------------------------------------------
  // C. 2-STORY STRUCTURAL GLASS CURTAIN WALL FACADE
  // --------------------------------------------------------------------------
  const glassFacade = new THREE.Mesh(
    new THREE.BoxGeometry(4.8, 3.7, 0.05),
    materials.curtainGlass
  );
  glassFacade.position.set(0, 1.95, 0.6);
  atriumGroup.add(glassFacade);

  // Vertical Architectural Mullions (7 structural posts)
  const mullionPositions = [-2.1, -1.4, -0.7, 0, 0.7, 1.4, 2.1];
  mullionPositions.forEach((mx) => {
    const mullion = new THREE.Mesh(
      new THREE.BoxGeometry(0.12, 3.8, 0.2),
      materials.darkStructuralMetal
    );
    mullion.position.set(mx, 1.95, 0.68);
    mullion.castShadow = true;
    atriumGroup.add(mullion);
  });

  // Horizontal Structural Transom Dividers
  [1.1, 1.95, 3.1].forEach((ty) => {
    const transom = new THREE.Mesh(
      new THREE.BoxGeometry(4.8, 0.12, 0.2),
      materials.darkStructuralMetal
    );
    transom.position.set(0, ty, 0.68);
    transom.castShadow = true;
    atriumGroup.add(transom);
  });

  // Horizontal Sun Louvers (Brise-Soleil projecting shading blades)
  [2.4, 2.75, 3.4].forEach((ly) => {
    const louver = new THREE.Mesh(
      new THREE.BoxGeometry(4.8, 0.04, 0.22),
      materials.darkStructuralMetal
    );
    louver.position.set(0, ly, 0.74);
    louver.rotation.x = -0.22;
    atriumGroup.add(louver);
  });

  // Recessed Sliding Entry Doors Frame
  const entryFrame = new THREE.Mesh(
    new THREE.BoxGeometry(1.8, 1.25, 0.08),
    materials.darkStructuralMetal
  );
  entryFrame.position.set(0, 0.65, 0.66);
  atriumGroup.add(entryFrame);

  hq.add(atriumGroup);

  // --------------------------------------------------------------------------
  // D. CANTILEVERED ENTRANCE CANOPY WITH INTEGRATED DOWNLIGHTS
  // --------------------------------------------------------------------------
  const canopy = new THREE.Mesh(
    new THREE.BoxGeometry(3.2, 0.14, 1.4),
    materials.darkStructuralMetal
  );
  canopy.position.set(0, 1.42, 1.25);
  canopy.castShadow = true;
  hq.add(canopy);

  // Canopy Fascia Cyan Neon Strip
  const canopyNeonEdge = new THREE.Mesh(
    new THREE.BoxGeometry(3.2, 0.035, 0.03),
    materials.cyanNeon
  );
  canopyNeonEdge.position.set(0, 1.42, 1.96);
  hq.add(canopyNeonEdge);

  // 4 Recessed High-CRI LED Downlight Fixtures Under Canopy
  [-1.0, -0.35, 0.35, 1.0].forEach((dx) => {
    const downlight = new THREE.Mesh(
      new THREE.CylinderGeometry(0.08, 0.08, 0.02, 16),
      materials.warmCanopySpot
    );
    downlight.position.set(dx, 1.34, 1.3);
    hq.add(downlight);
  });

  // --------------------------------------------------------------------------
  // E. LEFT ADMINISTRATIVE WING / CLADDED INSTITUTIONAL TOWER
  // --------------------------------------------------------------------------
  const leftWing = new THREE.Group();
  leftWing.position.set(-3.5, 0, -0.6);

  // Main Stone Clad Volume
  const leftTowerBody = new THREE.Mesh(
    new THREE.BoxGeometry(2.6, 3.6, 2.2),
    materials.limestonePanel
  );
  leftTowerBody.position.set(0, 1.8, 0);
  leftTowerBody.castShadow = true;
  leftTowerBody.receiveShadow = true;
  leftWing.add(leftTowerBody);

  // Vertical Architectural Shadow Reveals & Pilasters
  [-0.8, 0, 0.8].forEach((px) => {
    const fin = new THREE.Mesh(
      new THREE.BoxGeometry(0.12, 3.62, 0.12),
      materials.darkStructuralMetal
    );
    fin.position.set(px, 1.8, 1.15);
    leftWing.add(fin);
  });

  // Recessed Ribbon Window Slots
  [1.0, 2.2].forEach((wy) => {
    const win = new THREE.Mesh(
      new THREE.BoxGeometry(2.2, 0.5, 0.08),
      materials.darkSpandrelPanel
    );
    win.position.set(0, wy, 1.08);
    leftWing.add(win);

    const winGlass = new THREE.Mesh(
      new THREE.BoxGeometry(2.1, 0.44, 0.02),
      materials.curtainGlass
    );
    winGlass.position.set(0, wy, 1.12);
    leftWing.add(winGlass);
  });

  // Parapet Coping
  const leftCoping = new THREE.Mesh(
    new THREE.BoxGeometry(2.7, 0.12, 2.3),
    materials.darkStructuralMetal
  );
  leftCoping.position.set(0, 3.66, 0);
  leftWing.add(leftCoping);

  hq.add(leftWing);

  // --------------------------------------------------------------------------
  // F. RIGHT ADMINISTRATIVE WING (Department Registry & Arabic Sign)
  // --------------------------------------------------------------------------
  const rightWing = new THREE.Group();
  rightWing.position.set(3.6, 0, -0.6);

  // Stone Body
  const rightBody = new THREE.Mesh(
    new THREE.BoxGeometry(2.8, 3.0, 2.0),
    materials.limestonePanel
  );
  rightBody.position.set(0, 1.5, 0);
  rightBody.castShadow = true;
  rightBody.receiveShadow = true;
  rightWing.add(rightBody);

  // Continuous Horizontal Ribbon Windows
  [1.0, 2.0].forEach((wy) => {
    const winFrame = new THREE.Mesh(
      new THREE.BoxGeometry(2.6, 0.45, 0.1),
      materials.darkStructuralMetal
    );
    winFrame.position.set(0, wy, 1.02);
    rightWing.add(winFrame);

    const winGlass = new THREE.Mesh(
      new THREE.BoxGeometry(2.5, 0.38, 0.02),
      materials.curtainGlass
    );
    winGlass.position.set(0, wy, 1.07);
    rightWing.add(winGlass);
  });

  // Arabic Sign Fascia Backing Box
  const arabicBacking = new THREE.Mesh(
    new THREE.BoxGeometry(2.2, 0.44, 0.12),
    materials.darkStructuralMetal
  );
  arabicBacking.position.set(0, 2.35, 1.08);
  rightWing.add(arabicBacking);

  // Arabic Sign Luminous Plate Face
  const arabicPlate = new THREE.Mesh(
    new THREE.BoxGeometry(2.1, 0.36, 0.02),
    materials.cyanNeon
  );
  arabicPlate.position.set(0, 2.35, 1.15);
  rightWing.add(arabicPlate);

  // Parapet Coping
  const rightCoping = new THREE.Mesh(
    new THREE.BoxGeometry(2.9, 0.12, 2.1),
    materials.darkStructuralMetal
  );
  rightCoping.position.set(0, 3.06, 0);
  rightWing.add(rightCoping);

  hq.add(rightWing);

  // --------------------------------------------------------------------------
  // G. ROOFSCAPE & 3D "DVLD" PARAPET SIGNAGE
  // --------------------------------------------------------------------------
  // Rooftop Mechanical Penthouse (HVAC Screen Enclosure)
  const penthouse = new THREE.Mesh(
    new THREE.BoxGeometry(2.8, 0.9, 1.6),
    materials.darkStructuralMetal
  );
  penthouse.position.set(0, 4.6, -0.6);
  hq.add(penthouse);

  // Rooftop Telecom Mast
  const mast = new THREE.Mesh(
    new THREE.CylinderGeometry(0.04, 0.06, 1.6, 8),
    materials.darkStructuralMetal
  );
  mast.position.set(0, 5.8, -0.6);
  hq.add(mast);

  const beacon = new THREE.Mesh(
    new THREE.SphereGeometry(0.08, 8, 8),
    materials.cyanNeon
  );
  beacon.position.set(0, 6.6, -0.6);
  hq.add(beacon);

  // Continuous Parapet Cyan Neon Strip
  const rooflineNeon = new THREE.Mesh(
    new THREE.BoxGeometry(5.6, 0.04, 0.04),
    materials.cyanNeon
  );
  rooflineNeon.position.set(0, 4.19, 0.68);
  hq.add(rooflineNeon);

  // 3D Illuminated "DVLD" Parapet Sign Plinth
  const signPlinth = new THREE.Mesh(
    new THREE.BoxGeometry(2.1, 0.72, 0.22),
    materials.darkStructuralMetal
  );
  signPlinth.position.set(0, 4.38, 0.65);
  signPlinth.castShadow = true;
  hq.add(signPlinth);

  // 3D Glowing Sign Border Frame
  const signBorder = new THREE.Mesh(
    new THREE.BoxGeometry(2.0, 0.62, 0.04),
    materials.cyanNeon
  );
  signBorder.position.set(0, 4.38, 0.76);
  hq.add(signBorder);

  // High-Luminance "DVLD" Lettering Face
  const signFace = new THREE.Mesh(
    new THREE.BoxGeometry(1.88, 0.52, 0.02),
    materials.coolWhiteSign
  );
  signFace.position.set(0, 4.38, 0.79);
  hq.add(signFace);

  // --------------------------------------------------------------------------
  // H. ENTRANCE PLAZA FORECOURT & ARCHITECTURAL PLANTERS
  // --------------------------------------------------------------------------
  const plazaPodium = new THREE.Mesh(
    new THREE.BoxGeometry(8.2, 0.16, 2.2),
    materials.darkStructuralMetal
  );
  plazaPodium.position.set(0, 0.08, 1.3);
  plazaPodium.receiveShadow = true;
  hq.add(plazaPodium);

  const leftPlanter = new THREE.Mesh(
    new THREE.BoxGeometry(2.2, 0.26, 0.45),
    materials.darkStructuralMetal
  );
  leftPlanter.position.set(-2.5, 0.13, 1.8);
  hq.add(leftPlanter);

  for (let sx = -3.2; sx <= -1.8; sx += 0.35) {
    const shrub = new THREE.Mesh(
      new THREE.SphereGeometry(0.18, 8, 8),
      materials.planterFoliage
    );
    shrub.position.set(sx, 0.32, 1.8);
    hq.add(shrub);
  }

  const rightPlanter = new THREE.Mesh(
    new THREE.BoxGeometry(2.2, 0.26, 0.45),
    materials.darkStructuralMetal
  );
  rightPlanter.position.set(2.5, 0.13, 1.8);
  hq.add(rightPlanter);

  for (let sx = 1.8; sx <= 3.2; sx += 0.35) {
    const shrub = new THREE.Mesh(
      new THREE.SphereGeometry(0.18, 8, 8),
      materials.planterFoliage
    );
    shrub.position.set(sx, 0.32, 1.8);
    hq.add(shrub);
  }

  return hq;
}

// ============================================================================
// 2. REBUILD AMMAN CITADEL & TEMPLE OF HERCULES (HISTORICALLY ACCURATE)
// ============================================================================
function buildCitadel() {
  const citadel = new THREE.Group();
  citadel.name = 'Amman_Citadel_Temple_Of_Hercules';

  // --------------------------------------------------------------------------
  // A. JABAL AL-QAL'A BEDROCK CLIFFS & ARCHAEOLOGICAL HILL TERRAIN
  // --------------------------------------------------------------------------
  const hillGroup = new THREE.Group();

  // Natural Stratified Bedrock Ridge (Tiered rocky terraces)
  const baseTerrain = new THREE.Mesh(
    new THREE.BoxGeometry(8.5, 1.8, 5.0),
    materials.bedrockStone
  );
  baseTerrain.position.set(0, 0.9, -0.6);
  baseTerrain.castShadow = true;
  baseTerrain.receiveShadow = true;
  hillGroup.add(baseTerrain);

  // Upper Geological Terrace Step
  const upperTerrace = new THREE.Mesh(
    new THREE.BoxGeometry(7.2, 0.8, 4.0),
    materials.bedrockStone
  );
  upperTerrace.position.set(0.3, 1.8, -0.4);
  upperTerrace.castShadow = true;
  upperTerrace.receiveShadow = true;
  hillGroup.add(upperTerrace);

  // Ancient Ashlar Retaining Wall on Hillside
  const retainingWall = new THREE.Mesh(
    new THREE.BoxGeometry(6.6, 0.45, 0.4),
    materials.ancientLimestoneDark
  );
  retainingWall.position.set(0.2, 1.95, 1.5);
  retainingWall.castShadow = true;
  hillGroup.add(retainingWall);

  citadel.add(hillGroup);

  // --------------------------------------------------------------------------
  // B. 3-TIER STEPPED CREPIDOMA (STYLOBATE PODIUM)
  // --------------------------------------------------------------------------
  const podiumGroup = new THREE.Group();

  const step1 = new THREE.Mesh(
    new THREE.BoxGeometry(5.6, 0.22, 3.2),
    materials.ancientLimestoneDark
  );
  step1.position.set(0, 2.2, 0);
  step1.castShadow = true;
  step1.receiveShadow = true;
  podiumGroup.add(step1);

  const step2 = new THREE.Mesh(
    new THREE.BoxGeometry(5.2, 0.2, 2.8),
    materials.ancientLimestone
  );
  step2.position.set(0, 2.41, 0);
  step2.castShadow = true;
  step2.receiveShadow = true;
  podiumGroup.add(step2);

  const stylobate = new THREE.Mesh(
    new THREE.BoxGeometry(4.8, 0.18, 2.4),
    materials.ancientLimestone
  );
  stylobate.position.set(0, 2.6, 0);
  stylobate.castShadow = true;
  stylobate.receiveShadow = true;
  podiumGroup.add(stylobate);

  // Stylobate Chiseled Stone Paving Slabs
  for (let px = -2.1; px <= 2.1; px += 0.7) {
    const joint = new THREE.Mesh(
      new THREE.BoxGeometry(0.02, 0.02, 2.38),
      materials.ancientLimestoneDark
    );
    joint.position.set(px, 2.695, 0);
    podiumGroup.add(joint);
  }

  citadel.add(podiumGroup);

  // --------------------------------------------------------------------------
  // C. TEMPLE OF HERCULES COLONNADE (HEXASTYLE PORTICO)
  // --------------------------------------------------------------------------
  const colPositions = [-1.75, -1.05, -0.35, 0.35, 1.05, 1.75];
  const colBaseY = 2.69;

  colPositions.forEach((cx, idx) => {
    const colGroup = new THREE.Group();
    colGroup.position.set(cx, colBaseY, 0);

    // Classical Attic Base
    const plinth = new THREE.Mesh(
      new THREE.BoxGeometry(0.52, 0.1, 0.52),
      materials.ancientLimestone
    );
    plinth.position.set(0, 0.05, 0);
    plinth.castShadow = true;
    colGroup.add(plinth);

    const torusLower = new THREE.Mesh(
      new THREE.CylinderGeometry(0.24, 0.26, 0.08, 24),
      materials.ancientLimestone
    );
    torusLower.position.set(0, 0.14, 0);
    torusLower.castShadow = true;
    colGroup.add(torusLower);

    const torusUpper = new THREE.Mesh(
      new THREE.CylinderGeometry(0.22, 0.23, 0.06, 24),
      materials.ancientLimestone
    );
    torusUpper.position.set(0, 0.21, 0);
    colGroup.add(torusUpper);

    // Fluted Column Shafts
    let shaftHeight = 2.6;
    let isBroken = false;
    let hasCapital = true;

    if (idx === 4) {
      shaftHeight = 1.45;
      isBroken = true;
      hasCapital = false;
    } else if (idx === 5) {
      shaftHeight = 0.65;
      isBroken = true;
      hasCapital = false;
    }

    let shaftMesh;
    if (isBroken) {
      const brokenGeom = createBrokenColumnGeometry(0.21, 0.18, shaftHeight, 0.22);
      shaftMesh = new THREE.Mesh(brokenGeom, materials.ancientLimestone);
    } else {
      const flutedGeom = createFlutedColumnGeometry(0.21, 0.18, shaftHeight, 24, 28, 72);
      shaftMesh = new THREE.Mesh(flutedGeom, materials.ancientLimestone);
    }
    shaftMesh.position.set(0, 0.24, 0);
    shaftMesh.castShadow = true;
    colGroup.add(shaftMesh);

    // Drum Joint Seams
    const drumCount = Math.floor(shaftHeight / 0.65);
    for (let d = 1; d <= drumCount; d++) {
      const jointRing = new THREE.Mesh(
        new THREE.TorusGeometry(0.198, 0.008, 6, 24),
        materials.ancientLimestoneDark
      );
      jointRing.rotation.x = Math.PI / 2;
      jointRing.position.set(0, 0.24 + d * 0.65, 0);
      colGroup.add(jointRing);
    }

    // Corinthian Capital
    if (hasCapital) {
      const capital = createCorinthianCapital();
      capital.position.set(0, 0.24 + shaftHeight, 0);
      colGroup.add(capital);
    }

    citadel.add(colGroup);
  });

  // --------------------------------------------------------------------------
  // D. CLASSICAL ROMAN ENTABLATURE BEAM
  // --------------------------------------------------------------------------
  const entablatureY = colBaseY + 0.24 + 2.6 + 0.48;
  const entablatureX = -1.05;

  const architrave = new THREE.Mesh(
    new THREE.BoxGeometry(2.35, 0.32, 0.54),
    materials.ancientLimestone
  );
  architrave.position.set(entablatureX, entablatureY, 0);
  architrave.castShadow = true;
  citadel.add(architrave);

  const fasciaStep = new THREE.Mesh(
    new THREE.BoxGeometry(2.37, 0.08, 0.56),
    materials.ancientLimestone
  );
  fasciaStep.position.set(entablatureX, entablatureY + 0.12, 0);
  citadel.add(fasciaStep);

  const frieze = new THREE.Mesh(
    new THREE.BoxGeometry(2.32, 0.26, 0.52),
    materials.ancientLimestone
  );
  frieze.position.set(entablatureX, entablatureY + 0.29, 0);
  frieze.castShadow = true;
  citadel.add(frieze);

  for (let tx = -1.8; tx <= -0.3; tx += 0.35) {
    const triglyph = new THREE.Mesh(
      new THREE.BoxGeometry(0.12, 0.22, 0.04),
      materials.ancientLimestoneDark
    );
    triglyph.position.set(tx, entablatureY + 0.29, 0.28);
    citadel.add(triglyph);
  }

  const cornice = new THREE.Mesh(
    new THREE.BoxGeometry(2.45, 0.16, 0.64),
    materials.ancientLimestone
  );
  cornice.position.set(entablatureX, entablatureY + 0.5, 0);
  cornice.castShadow = true;
  citadel.add(cornice);

  // --------------------------------------------------------------------------
  // E. ARCHAEOLOGICAL RUIN FRAGMENTS & "HAND OF HERCULES"
  // --------------------------------------------------------------------------
  const fallenDrum1 = new THREE.Mesh(
    new THREE.CylinderGeometry(0.21, 0.21, 0.65, 20),
    materials.ancientLimestone
  );
  fallenDrum1.rotation.z = Math.PI / 2;
  fallenDrum1.rotation.y = 0.35;
  fallenDrum1.position.set(1.5, colBaseY + 0.22, 0.7);
  fallenDrum1.castShadow = true;
  citadel.add(fallenDrum1);

  const fallenDrum2 = new THREE.Mesh(
    new THREE.CylinderGeometry(0.2, 0.2, 0.5, 20),
    materials.ancientLimestone
  );
  fallenDrum2.rotation.x = Math.PI / 2;
  fallenDrum2.position.set(0.6, colBaseY + 0.21, 0.85);
  fallenDrum2.castShadow = true;
  citadel.add(fallenDrum2);

  const ashlarBlock1 = new THREE.Mesh(
    new THREE.BoxGeometry(0.65, 0.32, 0.42),
    materials.ancientLimestone
  );
  ashlarBlock1.position.set(-0.5, colBaseY + 0.16, 0.75);
  ashlarBlock1.rotation.y = 0.24;
  ashlarBlock1.castShadow = true;
  citadel.add(ashlarBlock1);

  const ashlarBlock2 = new THREE.Mesh(
    new THREE.BoxGeometry(0.55, 0.28, 0.38),
    materials.ancientLimestoneDark
  );
  ashlarBlock2.position.set(2.2, colBaseY + 0.14, 0.5);
  ashlarBlock2.rotation.y = -0.42;
  ashlarBlock2.castShadow = true;
  citadel.add(ashlarBlock2);

  const handBase = new THREE.Mesh(
    new THREE.BoxGeometry(0.42, 0.18, 0.32),
    materials.marbleFragment
  );
  handBase.position.set(0.1, colBaseY + 0.09, 0.82);
  citadel.add(handBase);

  [0.0, 0.08, 0.16].forEach((fx, i) => {
    const finger = new THREE.Mesh(
      new THREE.CylinderGeometry(0.045, 0.05, 0.18, 12),
      materials.marbleFragment
    );
    finger.rotation.x = 0.45;
    finger.position.set(-0.06 + fx, colBaseY + 0.22, 0.88 + i * 0.02);
    citadel.add(finger);
  });

  return citadel;
}

// ============================================================================
// MAIN EXPORT PIPELINE
// ============================================================================
function exportGLB(scene, outputPath) {
  return new Promise((resolve, reject) => {
    const exporter = new GLTFExporter();
    exporter.parse(
      scene,
      (glbArrayBuffer) => {
        const buffer = Buffer.from(glbArrayBuffer);
        fs.writeFileSync(outputPath, buffer);
        const stats = fs.statSync(outputPath);
        console.log(`[GLTFExporter] Successfully exported: ${outputPath} (${(stats.size / 1024).toFixed(1)} KB)`);
        resolve(outputPath);
      },
      (error) => {
        console.error(`[GLTFExporter] Error exporting ${outputPath}:`, error);
        reject(error);
      },
      { binary: true, embedImages: true }
    );
  });
}

async function main() {
  console.log('--- Phase 02.2: Rebuilding Photorealistic Architectural 3D Assets ---');

  const hqScene = buildHeadquarters();
  const hqPath = path.join(HQ_DIR, 'headquarters.glb');
  await exportGLB(hqScene, hqPath);

  const citadelScene = buildCitadel();
  const citadelPath = path.join(CITADEL_DIR, 'citadel.glb');
  await exportGLB(citadelScene, citadelPath);

  console.log('All architectural assets successfully generated and exported to binary GLB!');
}

main().catch((err) => {
  console.error('Fatal GLB build error:', err);
  process.exit(1);
});
