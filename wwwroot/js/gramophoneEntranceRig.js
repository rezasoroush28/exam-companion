// Deterministic, DOM-free pose sampler for the articulated Gramophone entrance.
// The controller owns Web Animations, lifecycle, audio and cancellation; this module
// only describes a pose at an absolute point in a named stage.

export const entranceStages = Object.freeze([
  { name: "rig-walk-in", duration: 1800 },
  { name: "rig-stop-and-settle", duration: 250 },
  { name: "rig-sit-settle", duration: 500 },
  { name: "rig-notice-pocket", duration: 250 },
  { name: "rig-reach-pocket", duration: 450 },
  { name: "rig-open-pocket", duration: 250 },
  { name: "rig-grab-disc", duration: 350 },
  { name: "rig-pull-disc", duration: 550 },
  { name: "rig-hold-disc", duration: 500 },
]);

export const entranceStageNames = Object.freeze(entranceStages.map((stage) => stage.name));
export const entranceRigDefaults = Object.freeze({
  left: { hip: { x: 452, y: 939 }, knee: { x: 466, y: 1034 }, ankle: { x: 443, y: 1090 } },
  right: { hip: { x: 659, y: 966 }, knee: { x: 714, y: 1048 }, ankle: { x: 701, y: 1100 } },
  arm: {
    shoulder: { x: 786, y: 792 }, elbow: { x: 914, y: 830 }, wrist: { x: 943, y: 711 },
  },
  pocket: { flap: { x: 765, y: 804 }, disc: { x: 798, y: 855 } },
  discGripOffset: { x: 32, y: -15 },
});

const radians = (degrees) => (degrees * Math.PI) / 180;
const degrees = (radiansValue) => (radiansValue * 180) / Math.PI;
const point = (x, y) => ({ x, y });
const finite = (value, fallback) => (Number.isFinite(value) ? value : fallback);
const mix = (from, to, amount) => from + (to - from) * amount;
export const clamp01 = (value) => Math.max(0, Math.min(1, finite(value, 0)));
export const smoothstep = (value) => {
  const t = clamp01(value);
  return t * t * (3 - 2 * t);
};
const lerpPoint = (from, to, t) => point(mix(from.x, to.x, t), mix(from.y, to.y, t));
const distance = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
const angleOf = (from, to) => Math.atan2(to.y - from.y, to.x - from.x);

function dataNumber(source, name, fallback) {
  return finite(Number(source?.dataset?.[name]), fallback);
}

// This accepts already-resolved SVG group elements but intentionally does not query
// the DOM. It keeps the shared coordinate contract in the markup, not in the controller.
export function readRigGeometry(nodes = {}) {
  const left = nodes.leftLeg;
  const right = nodes.rightLeg;
  const arm = nodes.rightArm;
  const pocket = nodes.pocketRoot;
  return {
    left: {
      hip: point(dataNumber(left, "hipX", entranceRigDefaults.left.hip.x), dataNumber(left, "hipY", entranceRigDefaults.left.hip.y)),
      knee: point(dataNumber(left, "kneeX", entranceRigDefaults.left.knee.x), dataNumber(left, "kneeY", entranceRigDefaults.left.knee.y)),
      ankle: point(dataNumber(left, "ankleX", entranceRigDefaults.left.ankle.x), dataNumber(left, "ankleY", entranceRigDefaults.left.ankle.y)),
    },
    right: {
      hip: point(dataNumber(right, "hipX", entranceRigDefaults.right.hip.x), dataNumber(right, "hipY", entranceRigDefaults.right.hip.y)),
      knee: point(dataNumber(right, "kneeX", entranceRigDefaults.right.knee.x), dataNumber(right, "kneeY", entranceRigDefaults.right.knee.y)),
      ankle: point(dataNumber(right, "ankleX", entranceRigDefaults.right.ankle.x), dataNumber(right, "ankleY", entranceRigDefaults.right.ankle.y)),
    },
    arm: {
      shoulder: point(dataNumber(arm, "shoulderX", entranceRigDefaults.arm.shoulder.x), dataNumber(arm, "shoulderY", entranceRigDefaults.arm.shoulder.y)),
      elbow: point(dataNumber(arm, "elbowX", entranceRigDefaults.arm.elbow.x), dataNumber(arm, "elbowY", entranceRigDefaults.arm.elbow.y)),
      wrist: point(dataNumber(arm, "wristX", entranceRigDefaults.arm.wrist.x), dataNumber(arm, "wristY", entranceRigDefaults.arm.wrist.y)),
    },
    pocket: {
      flap: point(dataNumber(pocket, "flapX", entranceRigDefaults.pocket.flap.x), dataNumber(pocket, "flapY", entranceRigDefaults.pocket.flap.y)),
      disc: point(dataNumber(pocket, "discX", entranceRigDefaults.pocket.disc.x), dataNumber(pocket, "discY", entranceRigDefaults.pocket.disc.y)),
    },
    discGripOffset: { ...entranceRigDefaults.discGripOffset },
  };
}

// Returns relative rotations for nested upper/lower SVG links and reports clamping.
export function solveTwoLink({ root, joint, end, target, bend = 1 }) {
  const upper = distance(root, joint);
  const lower = distance(joint, end);
  const requested = distance(root, target);
  const minReach = Math.abs(upper - lower) + 0.001;
  const maxReach = upper + lower - 0.001;
  const solvedDistance = Math.max(minReach, Math.min(maxReach, requested));
  const clamped = Math.abs(solvedDistance - requested) > 0.01;
  const direction = angleOf(root, target);
  const elbowInternal = Math.acos(
    Math.max(-1, Math.min(1, (solvedDistance ** 2 - upper ** 2 - lower ** 2) / (2 * upper * lower))),
  );
  const rootOffset = Math.acos(
    Math.max(-1, Math.min(1, (solvedDistance ** 2 + upper ** 2 - lower ** 2) / (2 * solvedDistance * upper))),
  );
  const upperAngle = direction - bend * rootOffset;
  const lowerAngle = upperAngle + bend * elbowInternal;
  const solvedJoint = point(root.x + Math.cos(upperAngle) * upper, root.y + Math.sin(upperAngle) * upper);
  const solvedEnd = point(solvedJoint.x + Math.cos(lowerAngle) * lower, solvedJoint.y + Math.sin(lowerAngle) * lower);
  const defaultUpper = angleOf(root, joint);
  const defaultLower = angleOf(joint, end);
  return {
    upperDeg: degrees(upperAngle - defaultUpper),
    lowerDeg: degrees(lowerAngle - upperAngle - (defaultLower - defaultUpper)),
    joint: solvedJoint,
    end: solvedEnd,
    requested,
    solvedDistance,
    clamped,
  };
}

function standingTargets(geometry) {
  return { left: geometry.left.ankle, right: geometry.right.ankle };
}

function walkingTargets(geometry, progress) {
  // Four 260-unit strides match the 1040-unit linear locomotion. During each
  // stance half-cycle, local -130 travel exactly cancels root +130 travel.
  const foot = (side, shift) => {
    const phase = (clamp01(progress) * 4 + shift) % 1;
    const stance = phase < 0.5;
    const swing = (phase - 0.5) * 2;
    return {
      x: side.hip.x + (stance ? 65 - phase * 260 : -65 + 130 * smoothstep(swing)),
      y: side.ankle.y - (stance ? 0 : 28 * Math.sin(Math.PI * swing) ** 2),
      stance,
    };
  };
  return { left: foot(geometry.left, 0), right: foot(geometry.right, 0.5) };
}

function seatedTargets(geometry) {
  return {
    left: point(geometry.left.ankle.x - 55, geometry.left.ankle.y),
    right: point(geometry.right.ankle.x + 45, geometry.right.ankle.y),
  };
}

function solveLeg(side, target, bodyY, bend, bodyX = 0) {
  const root = point(side.hip.x + bodyX, side.hip.y + bodyY);
  const solved = solveTwoLink({ root, joint: point(side.knee.x + bodyX, side.knee.y + bodyY), end: point(side.ankle.x + bodyX, side.ankle.y + bodyY), target, bend });
  return {
    thigh: solved.upperDeg,
    shin: solved.lowerDeg,
    // The foot stays on its ankle; counter-rotation keeps the sole horizontal.
    foot: { x: 0, y: 0, angle: -(solved.upperDeg + solved.lowerDeg) },
    target,
    end: solved.end,
    clamped: solved.clamped,
  };
}

function solveArm(geometry, wristTarget) {
  const arm = geometry.arm;
  const solved = solveTwoLink({
    root: arm.shoulder,
    joint: arm.elbow,
    end: arm.wrist,
    target: wristTarget,
    bend: -1,
  });
  return {
    upper: solved.upperDeg,
    forearm: solved.lowerDeg,
    wrist: solved.end,
    clamped: solved.clamped,
  };
}

function basePose(geometry) {
  const feet = standingTargets(geometry);
  const left = solveLeg(geometry.left, feet.left, 0, 1);
  const right = solveLeg(geometry.right, feet.right, 0, 1);
  return {
    locomotionX: 0,
    bodyX: 0,
    bodyY: 0,
    bodyAngle: 0,
    horn: 0,
    leftArm: 0,
    rightArm: 0,
    rightForearm: 0,
    left,
    right,
    flap: 0,
    disc: { center: geometry.pocket.disc, angle: -10, scale: 0.36, opacity: 0, visible: false },
    diagnostics: { stage: "neutral", clamped: left.clamped || right.clamped, wrist: geometry.arm.wrist, discCenter: geometry.pocket.disc, feet },
  };
}

function seatPose(geometry, amount = 1) {
  const bodyY = 42 * amount;
  const targets = seatedTargets(geometry);
  const leftTarget = lerpPoint(geometry.left.ankle, targets.left, amount);
  const rightTarget = lerpPoint(geometry.right.ankle, targets.right, amount);
  const left = solveLeg(geometry.left, leftTarget, bodyY, 1);
  const right = solveLeg(geometry.right, rightTarget, bodyY, 1);
  return {
    ...basePose(geometry),
    bodyY,
    bodyAngle: 0,
    horn: -1.4 * amount,
    left,
    right,
    diagnostics: { stage: "seated", clamped: left.clamped || right.clamped, wrist: geometry.arm.wrist, discCenter: geometry.pocket.disc, feet: { left: leftTarget, right: rightTarget } },
  };
}

function withArmAndDisc(pose, geometry, wristTarget, flap = pose.flap, discState = pose.disc) {
  const arm = solveArm(geometry, wristTarget);
  const grip = point(arm.wrist.x + geometry.discGripOffset.x, arm.wrist.y + geometry.discGripOffset.y);
  return {
    ...pose,
    rightArm: arm.upper,
    rightForearm: arm.forearm,
    flap,
    disc: discState,
    diagnostics: {
      ...pose.diagnostics,
      clamped: pose.diagnostics.clamped || arm.clamped,
      wrist: arm.wrist,
      discCenter: discState.center ?? grip,
      gripOffset: geometry.discGripOffset,
      attachmentDistance: discState.visible ? distance(discState.center, arm.wrist) : 0,
      armClamped: arm.clamped,
    },
  };
}

function heldDisc(geometry, wristTarget, angle) {
  const arm = solveArm(geometry, wristTarget);
  const offset = geometry.discGripOffset, a = radians(angle);
  return {
    center: point(arm.wrist.x + offset.x * Math.cos(a) - offset.y * Math.sin(a),
      arm.wrist.y + offset.x * Math.sin(a) + offset.y * Math.cos(a)),
    angle, scale: 0.36, opacity: 1, visible: true,
  };
}

function stageProgress(progress) {
  return smoothstep(progress);
}

export function sampleEntrancePose(stageName, progress, geometry = entranceRigDefaults) {
  const t = stageProgress(progress);
  const stage = String(stageName).replace(/^rig-/, "");
  const standing = basePose(geometry);
  const seated = seatPose(geometry);
  const offset = geometry.discGripOffset, gripAngle = radians(-10);
  const gripTarget = point(geometry.pocket.disc.x - offset.x * Math.cos(gripAngle) + offset.y * Math.sin(gripAngle),
    geometry.pocket.disc.y - offset.x * Math.sin(gripAngle) - offset.y * Math.cos(gripAngle));
  let result = standing;
  if (stage === "walk-in") {
    const gaitPhase = clamp01(progress) * 4;
    const feet = walkingTargets(geometry, progress);
    const bodyY = 35 + Math.sin(gaitPhase * Math.PI * 2) ** 2 * 5;
    const bodyX = Math.sin(gaitPhase * Math.PI * 2) * 3;
    const left = solveLeg(geometry.left, feet.left, bodyY, 1, bodyX);
    const right = solveLeg(geometry.right, feet.right, bodyY, 1, bodyX);
    result = {
      ...standing,
      locomotionX: mix(-1040, 0, clamp01(progress)),
      bodyY,
      bodyX,
      bodyAngle: 0,
      horn: -Math.sin(gaitPhase * Math.PI * 2 - 0.6) * 2.4,
      leftArm: -Math.sin(gaitPhase * Math.PI * 2) * 8,
      rightArm: Math.sin(gaitPhase * Math.PI * 2) * 8,
      left,
      right,
      diagnostics: { stage: "rig-walk-in", clamped: left.clamped || right.clamped, wrist: geometry.arm.wrist, discCenter: geometry.pocket.disc, feet },
    };
  } else if (stage === "stop-and-settle") {
    const walk = sampleEntrancePose("rig-walk-in", 1, geometry);
    const feet = { left: lerpPoint(walk.left.target, geometry.left.ankle, t), right: lerpPoint(walk.right.target, geometry.right.ankle, t) };
    const bodyY = mix(walk.bodyY, 0, t);
    result = {
      ...standing,
      bodyY,
      left: solveLeg(geometry.left, feet.left, bodyY, 1),
      right: solveLeg(geometry.right, feet.right, bodyY, 1),
      horn: mix(walk.horn, 0, t),
      diagnostics: { ...standing.diagnostics, stage: "rig-stop-and-settle" },
    };
  } else if (stage === "sit-settle") {
    result = seatPose(geometry, t);
    result.diagnostics.stage = "rig-sit-settle";
  } else if (stage === "notice-pocket") {
    result = { ...seated, horn: -1.4 + Math.sin(t * Math.PI) * 7, leftArm: Math.sin(t * Math.PI) * -4 };
    result.diagnostics.stage = "rig-notice-pocket";
  } else if (stage === "reach-pocket") {
    const target = lerpPoint(geometry.arm.wrist, point(812, 814), t);
    result = withArmAndDisc(seated, geometry, target);
    result.diagnostics.stage = "rig-reach-pocket";
  } else if (stage === "open-pocket") {
    const target = lerpPoint(point(812, 814), gripTarget, t);
    const disc = { center: geometry.pocket.disc, angle: -10, scale: 0.36, opacity: t, visible: t > 0 };
    result = withArmAndDisc(seated, geometry, target, -32 * t, disc);
    result.diagnostics.stage = "rig-open-pocket";
  } else if (stage === "grab-disc") {
    const target = gripTarget;
    const disc = heldDisc(geometry, target, -10);
    result = withArmAndDisc(seated, geometry, target, -32, disc);
    result.diagnostics.stage = "rig-grab-disc";
  } else if (stage === "pull-disc") {
    const target = lerpPoint(gripTarget, point(895, 688), t);
    const disc = heldDisc(geometry, target, mix(-10, -8, t));
    result = withArmAndDisc(seated, geometry, target, -32 * (1 - smoothstep((t - 0.65) / 0.35)), disc);
    result.diagnostics.stage = "rig-pull-disc";
  } else if (stage === "hold-disc") {
    const target = point(895, 688);
    const disc = heldDisc(geometry, target, -8);
    result = withArmAndDisc(seated, geometry, target, 0, disc);
    result.diagnostics.stage = "rig-hold-disc";
  } else if (stage === "place-disc") {
    const start = heldDisc(geometry, point(895, 688), -8);
    const angle = radians(start.angle), size = start.scale;
    const from = [size * Math.cos(angle), size * Math.sin(angle), -size * Math.sin(angle), size * Math.cos(angle), start.center.x, start.center.y];
    const to = [0.99, 0.102, -0.108, 0.486, 607.8, 699];
    const disc = { ...start, matrix: from.map((value, index) => mix(value, to[index], t)) };
    result = withArmAndDisc(seatPose(geometry, 1 - t), geometry,
      lerpPoint(point(895, 688), point(790, 720), t), 0, disc);
  }
  result.diagnostics.stage = `rig-${stage}`;
  result.diagnostics.clamped = result.left.clamped || result.right.clamped || !!result.diagnostics.armClamped;
  result.diagnostics.feet = { left: result.left.target, right: result.right.target };
  return result;
}

export function buildEntranceTracks(stageName, geometry = entranceRigDefaults, samplesPerSecond = 60) {
  const stage = stageName === "place-disc" ? { name: stageName, duration: 600 }
    : entranceStages.find((candidate) => candidate.name === stageName);
  if (!stage) throw new Error(`Unknown entrance rig stage: ${stageName}`);
  const sampleCount = Math.max(2, Math.ceil((stage.duration / 1000) * Math.max(60, samplesPerSecond)) + 1);
  const samples = Array.from({ length: sampleCount }, (_, index) => sampleEntrancePose(stage.name, index / (sampleCount - 1), geometry));
  const frameSet = (makeFrame) => samples.map((sample, index) => ({
    ...makeFrame(sample),
    offset: index / Math.max(1, samples.length - 1),
  }));
  // Declarative, controller-agnostic SVG/CSS transform intent. The live controller
  // decides when and how these frames are animated.
  const tracks = {
    "gram-locomotion": frameSet((p) => ({ transform: `translateX(${p.locomotionX}px)` })),
    "machine-root": frameSet((p) => ({ transform: `translate(${p.bodyX}px, ${p.bodyY}px) rotate(${p.bodyAngle}deg)` })),
    "horn-follow": frameSet((p) => ({ transform: `rotate(${p.horn}deg)` })),
    "left-arm": frameSet((p) => ({ transform: `rotate(${p.leftArm}deg)` })),
    "right-arm": frameSet((p) => ({ transform: `rotate(${p.rightArm}deg)` })),
    "right-forearm": frameSet((p) => ({ transform: `rotate(${p.rightForearm}deg)` })),
    "left-thigh": frameSet((p) => ({ transform: `rotate(${p.left.thigh}deg)` })),
    "left-shin": frameSet((p) => ({ transform: `rotate(${p.left.shin}deg)` })),
    "right-thigh": frameSet((p) => ({ transform: `rotate(${p.right.thigh}deg)` })),
    "right-shin": frameSet((p) => ({ transform: `rotate(${p.right.shin}deg)` })),
    "left-foot": frameSet((p) => ({ transform: `translate(${p.left.foot.x}px, ${p.left.foot.y}px) rotate(${p.left.foot.angle}deg)` })),
    "right-foot": frameSet((p) => ({ transform: `translate(${p.right.foot.x}px, ${p.right.foot.y}px) rotate(${p.right.foot.angle}deg)` })),
    "pocket-flap": frameSet((p) => ({ transform: `rotate(${p.flap}deg)` })),
    "record-prop": frameSet((p) => ({
      transform: p.disc.matrix ? `matrix(${p.disc.matrix.join(",")})`
        : `translate(${p.disc.center.x}px, ${p.disc.center.y}px) rotate(${p.disc.angle}deg) scale(${p.disc.scale})`,
      opacity: p.disc.opacity,
      visibility: p.disc.visible ? "visible" : "hidden",
    })),
  };
  return {
    stage: stage.name,
    duration: stage.duration,
    sampleRate: Math.max(60, samplesPerSecond),
    sampleCount,
    samples,
    tracks,
    diagnostics: samples.map((sample) => sample.diagnostics),
  };
}
