import * as audio from "./gameAudio.js";
import {
  buildEntranceTracks,
  entranceStageNames,
  entranceStages,
  readRigGeometry,
  sampleEntrancePose,
} from "./gramophoneEntranceRig.js";

// Public entrance names and MotionLab's rig-prefixed controls share one sampler.
const rigPreviewAliases = Object.freeze({
  "walk-in": "rig-walk-in",
  "sit-settle": "rig-sit-settle",
  "reach-pocket": "rig-reach-pocket",
  "pull-disc": "rig-pull-disc",
  "stop-and-settle": "rig-stop-and-settle",
  "notice-pocket": "rig-notice-pocket",
  "open-pocket": "rig-open-pocket",
  "grab-disc": "rig-grab-disc",
  "hold-disc": "rig-hold-disc",
});
const rigStageFor = (name) => rigPreviewAliases[name] || (entranceStageNames.includes(name) ? name : null);

const controllers = new WeakMap();
const liveControllers = new Set();
const motionSpecs = {
  ...Object.fromEntries(entranceStages.map((stage) => [stage.name, [stage.duration, "RigPreview"]])),
  ...Object.fromEntries(entranceStages.filter((stage) => Object.values(rigPreviewAliases).includes(stage.name))
    .map((stage) => [Object.entries(rigPreviewAliases).find(([, rigName]) => rigName === stage.name)[0], [stage.duration, "RigPreview"]])),
  "prototype-entrance": [entranceStages.reduce((total, stage) => total + stage.duration, 0), "RigPreview"],
  "walk-in": [1800, "Entering"],
  "sit-settle": [500, "Settling"],
  "reach-pocket": [450, "AssemblingRecord"],
  "pull-disc": [550, "AssemblingRecord"],
  "place-disc": [600, "AssemblingRecord"],
  "enter-drop": [650, "Entering"],
  "land-impact": [240, "Landing"],
  "record-drop-in": [410, "AssemblingRecord"],
  "record-wobble-settle": [300, "AssemblingRecord"],
  wake: [500, "Waking"],
  "idle-settle": [3600, "Idle"],
  "listen-lean": [420, "Listening"],
  "correct-pulse": [460, "CorrectReact"],
  "wrong-recoil": [440, "WrongReact"],
  "challenge-resolve": [720, "ChallengeResolve"],
  "cycle-complete-hop": [900, "CycleCelebrate"],
  "topic-proud-hop": [1080, "TopicCelebrate"],
  "scratch-approach": [220, "ScratchApproach"],
  "scratch-hit": [260, "ScratchHit"],
  "repair-heal": [900, "Repairing"],
  "repair-relief-settle": [520, "RepairComplete"],
  "lesson-complete-bow": [1300, "LessonComplete"],
};
export const motionVocabulary = Object.keys(motionSpecs).filter((name) => !name.startsWith("rig-") && name !== "prototype-entrance");
const allMotionNames = Object.keys(motionSpecs);
const productionEntrance = [...entranceStageNames.map((name) => name.slice(4)),
  "place-disc", "record-wobble-settle", "wake", "listen-lean"];
export function createController(host) {
  return create(host);
}
export function playEntrance(element) {
  return controllers.get(element)?.playEntrance();
}
export function playMotion(element, motionName, options = {}) {
  return controllers.get(element)?.playMotion(motionName, options);
}
export function cancelMotion(element) {
  controllers.get(element)?.cancelMotion();
}
export function syncScratchHit(element, timing) {
  return controllers.get(element)?.syncScratchHit(timing);
}
export function setReducedMotion(enabled) {
  for (const controller of liveControllers)
    controller.setReducedMotion(enabled);
}
export function refreshParts(element) {
  return controllers.get(element)?.refreshParts();
}
export function dispose(element) {
  return controllers.get(element)?.dispose();
}

export function create(host) {
  const existing = controllers.get(host);
  if (existing) return existing;
  let disposed = false,
    frame = 0,
    playbackGeneration = 0,
    sequenceGeneration = 0,
    motionGeneration = 0;
  let currentOrder = -1,
    currentMotion = "",
    restAngle = 0,
    motionRate = 1,
    entrance = false,
    initializedSession = null,
    rigDiagnostics = null,
    rigPlayback = null,
    paused = false;
  let nextIdleAt = performance.now() + 11000,
    finalComposition = false;
  let reducePreference = false;
  try {
    reducePreference =
      localStorage.getItem("hamaahang-reduced-motion") === "on";
  } catch {}
  const media = matchMedia("(prefers-reduced-motion: reduce)");
  const reduced = () => reducePreference || media.matches;
  const animations = new Set(),
    animationChannels = new WeakMap(),
    channelGenerations = new Map(),
    activeChannels = new Map(),
    waits = new Map();
  const gram = () => host.querySelector("[data-gramophone]");
  const requiredParts = [
    "right-forearm",
    "gram-locomotion", "left-arm", "right-arm", "left-leg", "right-leg",
    "left-thigh", "left-shin", "right-thigh", "right-shin", "left-foot", "right-foot",
    "pocket-root", "pocket-front", "pocket-flap", "record-prop",
    "artifact-rest",
    "artifact-motion",
    "machine-root",
    "gram-shadow",
    "gram-body",
    "record-stage",
    "record-root",
    "record-disc",
    "horn-pivot",
    "horn-follow",
    "horn-root",
    "tonearm-pivot",
    "tonearm-follow",
    "tonearm-root",
    "needle",
    "playhead",
    "scratch-layer",
    "impact-fx",
    "sparkle-fx",
  ];
  let boundGram = null;
  let parts = new Map();
  function refreshLiveParts() {
    const current = gram();
    boundGram = current;
    parts = new Map();
    if (!current) return { count: 0, missing: requiredParts.slice() };
    for (const node of current.querySelectorAll("[data-part]")) {
      const name = node.dataset.part;
      if (name && !parts.has(name)) parts.set(name, node);
    }
    return {
      count: parts.size,
      missing: requiredParts.filter((name) => !parts.has(name)),
    };
  }
  const part = (name) => {
    const current = gram();
    if (current !== boundGram || !boundGram?.isConnected) refreshLiveParts();
    if (name === "active-groove-fx")
      return current?.querySelector(".groove.active .groove-reaction");
    let node = parts.get(name);
    if (!node?.isConnected || !current?.contains(node)) {
      refreshLiveParts();
      node = parts.get(name);
    }
    return node;
  };
  const emit = (name, detail = {}) =>
    host.dispatchEvent(new CustomEvent("gramophone-" + name, { detail }));
  const timingEvent = (name, detail = {}) =>
    host.dispatchEvent(new CustomEvent(name, { detail }));
  const scale = () =>
    1 / Math.max(0.1, gram()?.querySelector("svg")?.getScreenCTM()?.a || 1);
  const pose = (y = 0, angle = 0, sx = 1, sy = 1, x = 0) =>
    "translate(" +
    x * scale() +
    "px," +
    y * scale() +
    "px) rotate(" +
    angle +
    "deg) scale(" +
    sx +
    "," +
    sy +
    ")";
  function applyRestPose() {
    const rest = part("artifact-rest");
    if (rest) rest.style.transform = `rotate(${restAngle}deg)`;
  }
  function state(value) {
    if (gram()) gram().dataset.state = value;
  }
  function setEntrance(value) {
    entrance = value;
    host.dataset.entering = String(value);
    const panel = host.querySelector(".question-stage");
    if (panel) panel.inert = value;
    if (!value && part("record-root")) {
      for (const name of ["gram-locomotion", "machine-root", "artifact-motion", "horn-follow", "tonearm-follow", "record-root", "left-arm", "right-arm", "right-forearm",
        "left-leg", "right-leg", "left-thigh", "left-shin", "right-thigh", "right-shin",
        "left-foot", "right-foot", "pocket-flap", "record-prop"])
        if (part(name)) {
          part(name).style.transform = "";
          part(name).style.opacity = "";
          part(name).style.transformOrigin = "";
        }
      if (part("record-prop")) {
        part("record-prop").style.visibility = "hidden";
      }
      rigDiagnostics = null;
      rigPlayback = null;
      part("record-root").style.visibility = "visible";
      part("record-root").style.opacity = "1";
    }
    if (gram()) gram().dataset.ready = "true";
  }
  function clearWaits() {
    for (const [id, finish] of waits) {
      clearTimeout(id);
      finish(false);
    }
    waits.clear();
  }
  function delay(ms) {
    return new Promise((resolve) => {
      const id = setTimeout(() => {
        waits.delete(id);
        resolve(!disposed);
      }, ms / motionRate);
      waits.set(id, resolve);
    });
  }
  const channelFor = (target) =>
    ["gram-locomotion", "left-arm", "right-arm", "right-forearm", "left-leg", "right-leg",
      "left-thigh", "left-shin", "right-thigh", "right-shin", "left-foot", "right-foot",
      "pocket-flap", "record-prop"].includes(target)
      ? target
      :
    target === "artifact-motion" || target === "machine-root"
      ? "artifact"
      : target === "horn-follow"
        ? "horn"
        : target === "tonearm-follow"
          ? "tonearm"
          : target === "record-root"
            ? "record"
            : target === "gram-shadow"
              ? "shadow"
              : "fx";
  function cancelChannels(channels) {
    motionGeneration++;
    for (const channel of channels) {
      channelGenerations.set(channel, (channelGenerations.get(channel) || 0) + 1);
      activeChannels.delete(channel);
    }
    for (const animation of [...animations]) {
      if (!channels.has(animationChannels.get(animation))) continue;
      animation.cancel();
      animations.delete(animation);
    }
  }
  function resetMotion() {
    cancelChannels(new Set(["artifact", "horn", "tonearm", "record", "shadow", "fx",
      "gram-locomotion", "left-arm", "right-arm", "right-forearm", "left-leg", "right-leg",
      "left-thigh", "left-shin", "right-thigh", "right-shin", "left-foot", "right-foot",
      "pocket-flap", "record-prop"]));
    const g = gram();
    for (const name of allMotionNames) g?.classList.remove("motion-" + name);
    currentMotion = "";
    if (g) g.dataset.motion = "idle";
    const artifact = part("artifact-motion");
    if (artifact) artifact.style.transform = "";
    const machine = part("machine-root");
    if (machine) machine.style.transform = "";
    rigDiagnostics = null;
    rigPlayback = null;
    applyRestPose();
  }
  function cancelAllMotion() {
    sequenceGeneration++;
    paused = false;
    clearWaits();
    resetMotion();
    setEntrance(false);
    state(host.dataset.playing === "true" ? "Playing" : "Idle");
  }
  function track(
    target,
    frames,
    duration,
    delay = 0,
    easing = "cubic-bezier(.22,1,.36,1)",
    channel = channelFor(target),
  ) {
    return { target, frames, duration, delay, easing, channel };
  }
  function rigRecipe(name, duration) {
    const geometry = readRigGeometry({
      leftLeg: part("left-leg"),
      rightLeg: part("right-leg"),
      rightArm: part("right-arm"),
      pocketRoot: part("pocket-root"),
    });
    const rig = buildEntranceTracks(name, geometry);
    rigDiagnostics = {
      stage: rig.stage,
      sampleRate: rig.sampleRate,
      sampleCount: rig.sampleCount,
      clamped: rig.diagnostics.some((entry) => entry.clamped),
      wrist: rig.samples[0].diagnostics.wrist,
      discCenter: rig.samples[0].diagnostics.discCenter,
      attachmentDistance: rig.samples[0].diagnostics.attachmentDistance || 0,
      footTargets: rig.samples[0].diagnostics.feet,
    };
    rigPlayback = { stage: name, geometry, duration: duration ?? rig.duration, animation: null };
    return [
      ...Object.entries(rig.tracks).map(([target, frames]) => track(target, frames, duration ?? rig.duration, 0, "linear")),
    ];
  }
  function prepareRigPreview() {
    const record = part("record-root");
    const prop = part("record-prop");
    if (record) record.style.visibility = reduced() ? "visible" : "hidden";
    if (prop) prop.style.visibility = "hidden";
  }
  function recipe(name, options, ms) {
    const rigStage = rigStageFor(name);
    if (rigStage || name === "place-disc") return rigRecipe(rigStage || name, ms);
    // Two rigid paper links: inverse kinematics keeps the shoulder attached.
    const reach = (points) => {
      const l1 = Math.hypot(128,38), l2 = Math.hypot(29,-119);
      const base = Math.atan2(38,128), bend = Math.atan2(-119,29)-base;
      const poses = points.map(([x,y]) => {
        const dx=x-786, dy=y-792;
        const b=-Math.acos(Math.max(-1,Math.min(1,(dx*dx+dy*dy-l1*l1-l2*l2)/(2*l1*l2))));
        const a=Math.atan2(dy,dx)-Math.atan2(l2*Math.sin(b),l1+l2*Math.cos(b));
        return [(a-base)*180/Math.PI,(b-bend)*180/Math.PI];
      });
      return ["right-arm","right-forearm"].map((target,i) =>
        track(target, poses.map(p=>({transform:"rotate("+p[i]+"deg)"})), ms));
    };
    const neutral = pose(),
      turn = (deg) => ({ transform: "rotate(" + deg + "deg)" });
    const body = (frames) =>
      track(
        "artifact-motion",
        frames.map((transform) => ({ transform })),
        ms,
      );
    const horn = (degrees, delayMs = 70, duration = ms - delayMs) =>
      track(
        "horn-follow",
        degrees.map(turn),
        duration,
        delayMs,
        "cubic-bezier(.34,1.35,.64,1)",
      );
    switch (name) {
      case "enter-drop":
        return [
          track(
            "machine-root",
            [
              // Percentage translations on SVG groups are browser-dependent.
              // Use the shared viewBox pixels so the whole body visibly falls.
              {
                transform: "translate(0px, -420px) scale(.985,.985)",
                opacity: 0,
              },
              {
                transform: "translate(0px, -420px) scale(.985,.985)",
                opacity: 1,
                offset: 0.12,
              },
              { transform: neutral, opacity: 1, offset: 0.88 },
              { transform: neutral, opacity: 1 },
            ],
            ms,
          ),
          track(
            "gram-shadow",
            [
              { opacity: 0, transform: "scale(.6)" },
              { opacity: 1, transform: "scale(1)" },
            ],
            ms,
          ),
        ];
      case "land-impact":
        return [
          track("machine-root", [
            { transform: pose(0, 0, 1.018, 0.968) },
            { transform: pose(-7, 0.6), offset: 0.42 },
            { transform: neutral },
          ], ms),
          horn([-1.8, 0.7, 0], 70),
          track(
            "gram-shadow",
            [
              { transform: "scaleX(1.12)", opacity: 1 },
              { transform: "scale(1)", opacity: 0.9 },
            ],
            ms,
          ),
          track(
            "impact-fx",
            [{ opacity: 0 }, { opacity: 0.6, offset: 0.2 }, { opacity: 0 }],
            ms,
          ),
        ];
      case "record-drop-in":
        return [
          track(
            "record-root",
            [
              { transform: "translateY(-140px) rotate(6deg)" },
              { transform: "translateY(-140px) rotate(6deg)", offset: 0.08 },
              { transform: "translateY(0) rotate(6deg)", offset: 0.75 },
              { transform: "rotate(6deg)" },
            ],
            ms,
          ),
        ];
      case "record-wobble-settle":
        return [
          track("record-root", [turn(6), turn(-3), turn(1.2), turn(0)], ms),
        ];
      case "wake":
        return [
          ...(entrance ? reach([[790,720],[880,700],[943,711]]) : []),
          body([neutral, pose(-4, 0.3), neutral]),
          horn([0, -3, 0.6, 0], 45, ms - 45),
          track("tonearm-follow", [turn(0), turn(-1), turn(0.2), turn(0)], ms - 100, 100),
          track("record-root", [turn(0), turn(2), turn(-0.5), turn(0)], 220, 210),
        ];
      case "idle-settle":
        return [
          horn([0, -1.15, 0], 0, ms),
          track("tonearm-follow", [turn(0), turn(-0.4), turn(0)], ms - 250, 250),
        ];
      case "listen-lean":
        return [
          body([neutral, pose(0, 0.7, 1, 1, 3), pose(0, 0.25, 1, 1, 1), neutral]),
          horn([0, -2, 0.4, 0], 45),
          track("tonearm-follow", [turn(0), turn(-0.5), turn(0.1), turn(0)], ms - 70, 70),
        ];
      case "correct-pulse":
        return [
          track("artifact-motion", [
            { transform: neutral },
            { transform: pose(-10, 0.6, 1.008, 0.994), offset: 0.32 },
            { transform: pose(-3, -0.25), offset: 0.66 },
            { transform: neutral },
          ], ms),
          horn([0, -2.8, 0.7, 0], 55),
          track("tonearm-follow", [turn(0), turn(-0.8), turn(0.25), turn(0)], ms - 80, 80),
        ];
      case "wrong-recoil":
        return [
          track("artifact-motion", [
            { transform: neutral },
            { transform: pose(2, -1.25, 1, 1, -5), offset: 0.28 },
            { transform: pose(0, 0.55, 1, 1, 2), offset: 0.62 },
            { transform: neutral },
          ], ms),
          horn([0, 2.2, -0.5, 0], 50),
          track("tonearm-follow", [turn(0), turn(0.7), turn(-0.15), turn(0)], ms - 65, 65),
        ];
      case "challenge-resolve":
        return [
          track("artifact-motion", [
            { transform: neutral },
            { transform: pose(-15, 1.4), offset: 0.38 },
            { transform: pose(-5, -0.35), offset: 0.68 },
            { transform: neutral },
          ], ms),
          horn([0, -3.2, 0.8, 0], 75),
          track("tonearm-follow", [turn(0), turn(-1), turn(0.3), turn(0)], ms - 95, 95),
          track("record-root", [turn(0), turn(1.4), turn(-0.4), turn(0)], 300, 150),
        ];
      case "cycle-complete-hop": {
        return [
          track(
            "artifact-motion",
            [
              { transform: neutral },
              {
                transform: pose(3, 0, 1.012, 0.982),
                offset: 0.12,
              },
              { transform: pose(-24, 1.8, 0.995, 1.01), offset: 0.42 },
              { transform: pose(-20, 1.3), offset: 0.58 },
              {
                transform: pose(2, 0, 1.014, 0.98),
                offset: 0.8,
              },
              { transform: neutral },
            ],
            ms,
          ),
          horn([0, 2, -3, 0.8, 0], 80),
          track("tonearm-follow", [turn(0), turn(0.8), turn(-1), turn(0.25), turn(0)], ms - 105, 105),
          track(
            "gram-shadow",
            [
              { transform: "scale(1)", opacity: 0.9 },
              { transform: "scale(.78)", opacity: 0.48, offset: 0.46 },
              { transform: "scale(1)", opacity: 0.9 },
            ],
            ms,
          ),
        ];
      }
      case "topic-proud-hop":
        return [
          track("artifact-motion", [
            { transform: neutral },
            { transform: pose(4, 0, 1.015, 0.978), offset: 0.1 },
            { transform: pose(-38, 2.8), offset: 0.38 },
            { transform: pose(-34, 2), offset: 0.55 },
            { transform: pose(3, -0.45, 1.018, 0.975), offset: 0.78 },
            { transform: neutral },
          ], ms),
          horn([0, 2.5, -4, 1.1, 0], 90),
          track("tonearm-follow", [turn(0), turn(1), turn(-1.3), turn(0.35), turn(0)], ms - 120, 120),
          track("gram-shadow", [
            { transform: "scale(1)", opacity: 0.9 },
            { transform: "scale(.68)", opacity: 0.4, offset: 0.43 },
            { transform: "scale(1)", opacity: 0.9 },
          ], ms),
        ];
      case "scratch-approach":
        return [track("tonearm-follow", [turn(0), turn(-0.35), turn(0)], ms)];
      case "scratch-hit":
        return []; // The audio clock drives both impulses, never a separate timer.
      case "repair-heal":
        return [
          horn([0, -2, 0], 0, ms),
          track(
            "sparkle-fx",
            [{ opacity: 0 }, { opacity: 0, offset: 0.8 }, { opacity: 0.7 }],
            ms,
          ),
        ];
      case "repair-relief-settle":
        return [
          body([neutral, pose(3), pose(-3, 0.35), neutral]),
          horn([0, -1.8, 0.4, 0], 60),
          track("tonearm-follow", [turn(0), turn(-0.45), turn(0)], ms - 85, 85),
          track(
            "sparkle-fx",
            [{ opacity: 0.7 }, { opacity: 1, offset: 0.25 }, { opacity: 0 }],
            ms,
          ),
        ];
      case "lesson-complete-bow":
        return [
          track(
            "artifact-motion",
            [
              { transform: neutral },
              { transform: pose(-5, 0.4), offset: 0.18 },
              { transform: pose(2, 3), offset: 0.46 },
              { transform: pose(2, 3), offset: 0.62 },
              { transform: pose(-2, -0.45), offset: 0.88 },
              { transform: neutral },
            ],
            ms,
          ),
          horn([0, 0.5, 2.8, 2.8, -0.8, 0], 100),
          track("tonearm-follow", [turn(0), turn(0.5), turn(1.2), turn(-0.3), turn(0)], ms - 130, 130),
        ];
      default:
        return [];
    }
  }
  async function perform(name, options = {}) {
    const binding = refreshLiveParts();
    if (
      disposed ||
      !host.isConnected ||
      !gram() ||
      binding.missing.length ||
      !motionSpecs[name]
    )
      return false;
    const g = gram(),
      [ms, semantic] = motionSpecs[name],
      duration = Number.isFinite(options.duration) ? options.duration : ms;
    // Resolve and reveal the live SVG before each reaction.
    g.dataset.ready = "true";
    g.dataset.reducedMotion = String(reduced());
    if (name === "record-drop-in") {
      part("record-root").style.visibility = "visible";
      part("record-root").style.opacity = "1";
    }
    if (!reduced() && (name === "pull-disc" || name === "place-disc"))
      part("record-prop").style.visibility = "visible";
    if (rigStageFor(name) && options.rigPreview) prepareRigPreview();
    let tracks = reduced() ? [] : recipe(name, options, duration);
    const stageRig = (rigStageFor(name) || name === "place-disc") && !reduced() ? rigPlayback : null;
    if (reduced()) {
      tracks = [
        track(
          name === "record-drop-in" ? "record-root" : "record-label",
          [{ opacity: 0.7 }, { opacity: 1 }],
          Math.min(duration, 200),
        ),
      ];
    }
    if (
      [
        "correct-pulse",
        "challenge-resolve",
        "cycle-complete-hop",
        "topic-proud-hop",
      ].includes(name)
    ) {
      tracks.push(
        track(
          "active-groove-fx",
          reduced() || name === "correct-pulse"
            ? [{ opacity: 0 }, { opacity: 0.75 }, { opacity: 0 }]
            : [
                { opacity: 0, strokeDashoffset: 100 },
                { opacity: 0.8, offset: 0.15 },
                { opacity: 0, strokeDashoffset: 0 },
              ],
          reduced() ? 200 : duration,
        ),
      );
    }
    const channels = new Set(tracks.map((t) => t.channel));
    if (name === "scratch-hit" && !reduced()) {
      channels.add("artifact");
      channels.add("tonearm");
    }
    cancelChannels(channels);
    const channelTokens = new Map(
      [...channels].map((channel) => [channel, channelGenerations.get(channel) || 0]),
    );
    const ownSequence = sequenceGeneration;
    for (const channel of channels) activeChannels.set(channel, name);
    currentMotion = name;
    g.dataset.motion = name;
    g.classList.add("motion-" + name);
    state(options.state || semantic);
    emit("motion-started", {
      motion: name,
      state: options.state || semantic,
      reduced: reduced(),
      channels: [...channels],
    });
    const pending = [];
    for (const t of tracks) {
      const node = part(t.target);
      if (!node) continue;
      // Ease each physical phase. Easing the entire keyframe sequence compresses
      // anticipation/lift into its first frames and leaves a long still tail.
      const animation = node.animate(
        t.frames.map((frame) => ({ easing: t.easing, ...frame })),
        {
          duration: t.duration,
          delay: t.delay,
          easing: "linear",
          fill: "both",
        },
      );
      animation.playbackRate = motionRate;
      if (stageRig && t.target === "machine-root") stageRig.animation = animation;
      if (paused) animation.pause();
      animations.add(animation);
      animationChannels.set(animation, t.channel);
      pending.push(
        animation.finished
          .then(() => {
            if ((options.entrance || options.persist) && !reduced() &&
                ownSequence === sequenceGeneration && host.isConnected && gram() === g && node.isConnected &&
                (channelGenerations.get(t.channel) || 0) === channelTokens.get(t.channel)) {
              const last = t.frames.at(-1);
              for (const property of ["transform", "opacity", "visibility"])
                if (last[property] !== undefined) node.style[property] = String(last[property]);
            }
          })
          .catch(() => {})
          .finally(() => { animation.cancel(); animations.delete(animation); }),
      );
    }
    if (pending.length) await Promise.all(pending);
    else if (!(await delay(reduced() ? 120 : duration))) return false;
    const superseded = [...channelTokens].some(
        ([channel, token]) => (channelGenerations.get(channel) || 0) !== token,
      );
    if (disposed || superseded || ownSequence !== sequenceGeneration || !host.isConnected || gram() !== g) {
      // A replacement run may use the same name. Only the current generation
      // may clear its classes/channels or publish completion.
      if (ownSequence === sequenceGeneration && !superseded) {
        g.classList.remove("motion-" + name);
        for (const channel of channels) activeChannels.delete(channel);
      }
      return false;
    }
    if (name === "cycle-complete-hop" && !reduced())
      restAngle = options.cycleIndex % 2 === 0 ? -0.7 : 0.7;
    applyRestPose();
    if (name === "place-disc") {
      part("record-prop").style.visibility = "hidden";
      part("record-root").style.visibility = "visible";
    }
    if (stageRig) {
      stageRig.completed = true;
      if (!options.persist && !options.entrance) rigPlayback = null;
    }
    if (name === "scratch-hit") {
      const artifact = part("artifact-motion");
      if (artifact) artifact.style.transform = "";
    }
    for (const channel of channels)
      if (activeChannels.get(channel) === name) activeChannels.delete(channel);
    g.classList.remove("motion-" + name);
    currentMotion = [...activeChannels.values()].at(-1) || "";
    g.dataset.motion = currentMotion || "idle";
    if (!currentMotion)
      state(host.dataset.playing === "true" ? "Playing" : "Idle");
    nextIdleAt = performance.now() + 11000;
    emit("motion-completed", { motion: name });
    return true;
  }
  async function sequence(names, options = {}) {
    const own = ++sequenceGeneration;
    for (let index = 0; index < names.length; index++) {
      const name = names[index];
      if (
        disposed ||
        own !== sequenceGeneration ||
        !(await perform(name, options))
      )
        return;
      if (name === "challenge-resolve" && index < names.length - 1)
        if (!(await delay(150))) return;
    }
  }
  async function entranceSequence() {
    const binding = refreshLiveParts();
    if (!host.isConnected || binding.missing.length) return;
    stop();
    setEntrance(true);
    state("Hidden");
    const own = sequenceGeneration,
      g = gram();
    if (!g) return;
    part("record-root").style.visibility = "hidden";
    part("record-root").style.opacity = "1";
    g.dataset.entranceCount = String(Number(g.dataset.entranceCount || 0) + 1);
    const steps = reduced()
      ? ["place-disc", "wake", "listen-lean"]
      : productionEntrance;
    for (const name of steps) {
      if (disposed || own !== sequenceGeneration) return;
      if (!(await perform(name, { entrance: true }))) {
        if (own === sequenceGeneration) cancelAllMotion();
        return;
      }
      if (name === "wake") void audio.wake();
    }
    if (disposed || own !== sequenceGeneration) return;
    setEntrance(false);
    emit("entrance-complete");
    focusQuestion();
  }
  function resetPlayback() {
    const g = gram();
    if (g) {
      g.dataset.playing = "false";
      g.dataset.hit = "false";
      delete g.dataset.pulse;
      g.querySelector("[data-record]")?.setAttribute("transform", "rotate(0)");
      g.querySelectorAll(".groove.playing").forEach((e) =>
        e.classList.remove("playing"),
      );
      if (currentOrder >= 0) placeNeedle(g, currentOrder);
    }
    host.dataset.playing = "false";
    host.dataset.hit = "false";
    currentOrder = -1;
  }
  function stopPlayback() {
    playbackGeneration++;
    cancelAnimationFrame(frame);
    audio.stop();
    resetPlayback();
  }
  function stop() {
    stopPlayback();
    cancelAllMotion();
  }
  function placeNeedle(g, order, jump = 0) {
    const plane = g
      .querySelector("[data-turntable]")
      ?.transform.baseVal.consolidate()?.matrix;
    const arm = g.querySelector("[data-tone-arm]");
    if (!plane || !arm) return;
    const r = 175 - order * 25;
    const contact = new DOMPoint(
      r * Math.cos(0.55),
      r * Math.sin(0.55),
    ).matrixTransform(plane);
    const x = contact.x,
      y = contact.y - Number(arm.dataset.needleLift) - jump * scale();
    const px = Number(arm.dataset.pivotX),
      py = Number(arm.dataset.pivotY);
    const path = `M${px} ${py}C${px - 38} ${py + 40} ${x + 105} ${y - 18} ${x} ${y - 12}`;
    g.querySelector("[data-arm-path]")?.setAttribute("d", path);
    g.querySelector("[data-arm-highlight]")?.setAttribute("d", path);
    g.querySelector("[data-needle]")?.setAttribute(
      "transform",
      `translate(${x} ${y})`,
    );
  }
  function scratchHit(timing) {
    if (disposed) return;
    // Initial body language lasts 260ms; any longer requested noise keeps its waveform indication.
    void perform("scratch-hit");
    timingEvent("scratch-hit", timing);
    emit("scratch", timing);
  }
  async function play(topics, event = "preview", options = {}) {
    stop();
    const own = playbackGeneration;
    if (disposed || !topics.length) return;
    finalComposition = !!options.finalComposition;
    // Answer feedback must not wait for AudioContext.resume()/autoplay permission.
    if (options.motions?.length) void sequence(options.motions, options);
    const timeline = await audio.schedule(topics, event);
    if (disposed || own !== playbackGeneration) return;
    let hitOnce = false,
      approachOnce = false,
      reactionStarted = false;
    emit("started", { event, topics: topics.length });
    timingEvent("playback-started", { event, start: timeline.start });
    function tick() {
      if (disposed || own !== playbackGeneration) return;
      const now = timeline.clock(),
        g = gram();
      if (now >= timeline.end || !g || !host.isConnected) {
        const bow = finalComposition && !!g;
        stopPlayback();
        emit("completed");
        timingEvent("playback-completed", { event, at: now });
        if (event === "repair") timingEvent("repair-completed", { at: now });
        if (bow) void sequence(["lesson-complete-bow"]);
        else if (!currentMotion) state("Idle");
        return;
      }
      const t = timeline.entries.find(
        (t) => now >= t.start && now < t.start + t.duration,
      );
      if (t) {
        const p = (now - t.start) / t.duration,
          hitAt = t.start + t.duration * t.hit;
        const atHit =
          t.damaged && now >= hitAt && now < hitAt + t.noiseDuration;
        if (currentOrder !== t.order) {
          g.querySelectorAll(".groove.playing").forEach((e) =>
            e.classList.remove("playing"),
          );
          currentOrder = t.order;
          hitOnce = false;
          approachOnce = false;
          emit("track", { order: t.order });
        }
        g.querySelector('[data-groove="' + t.order + '"]')?.classList.add(
          "playing",
        );
        g.dataset.playing = "true";
        host.dataset.playing = "true";
        g.dataset.pulse = event;
        g.dataset.order = t.order;
        g.dataset.hit = String(atHit);
        host.dataset.hit = String(atHit);
        if (!reactionStarted) {
          reactionStarted = true;
          if (!currentMotion) state("Playing");
        }
        const occupiedMotionChannels = ["artifact", "horn", "tonearm", "record"]
          .filter((channel) => activeChannels.has(channel)).length;
        if (!reduced() && occupiedMotionChannels < 2) {
          g.querySelector("[data-record]")?.setAttribute(
            "transform",
            "rotate(" +
              (p + (t.scratched && t.damaged ? t.contact - t.hit : 0)) * 360 +
              ")",
          );
        } else if (reduced())
          g.querySelector("[data-record]")?.setAttribute(
            "transform",
            "rotate(0)",
          );
        const hitElapsed = now - hitAt;
        const jump =
          atHit && !reduced() && hitElapsed < 0.26
            ? 2.2 * Math.sin((Math.PI * Math.max(0, hitElapsed)) / 0.26) ** 2
            : 0;
        placeNeedle(g, t.order, jump);
        if (activeChannels.get("artifact") === "scratch-hit" && !reduced())
          part("artifact-motion").style.transform = pose(
            0,
            -0.3 * (jump / 2.2),
          );
        const radius = 175 - t.order * 25;
        const phase = reduced() ? Math.floor(p * 4) / 4 : p;
        const angle = 0.55 + Math.PI * (phase - t.hit);
        const point = g.querySelector("[data-playhead]");
        point?.setAttribute("cx", radius * Math.cos(angle));
        point?.setAttribute("cy", radius * Math.sin(angle));
        if (t.damaged && now >= hitAt - 0.22 && now < hitAt && !approachOnce) {
          approachOnce = true;
          void perform("scratch-approach");
          timingEvent("scratch-approaching", {
            scheduledTime: hitAt,
            order: t.order,
          });
        }
        if (atHit && !hitOnce) {
          hitOnce = true;
          scratchHit({
            order: t.order,
            scheduledTime: hitAt,
            visualTime: now,
            disturbanceMs: t.noiseDuration * 1000,
            reduced: reduced(),
          });
        }
      }
      frame = requestAnimationFrame(tick);
    }
    frame = requestAnimationFrame(tick);
  }
  function focusQuestion() {
    if (entrance || disposed || !host.isConnected) return;
    host
      .querySelector(
        ".answer-option:not(:disabled), .completion-panel .primary",
      )
      ?.focus({ preventScroll: true });
  }
  function updateReduction() {
    const g = gram();
    if (g) g.dataset.reducedMotion = String(reduced());
    cancelAllMotion();
    if (reduced())
      g?.querySelector("[data-record]")?.setAttribute("transform", "rotate(0)");
  }
  const visibility = () => {
    if (document.hidden) stop();
  };
  const idle = setInterval(() => {
    if (
      !disposed &&
      !document.hidden &&
      !entrance &&
      !rigPlayback &&
      !currentMotion &&
      host.dataset.playing !== "true" &&
      performance.now() >= nextIdleAt
    ) {
      nextIdleAt = performance.now() + 16000;
      if (!reduced()) void perform("idle-settle");
    }
  }, 2000);
  document.addEventListener("visibilitychange", visibility);
  media.addEventListener("change", updateReduction);
  const api = {
    play,
    stop,
    initialize(sessionId) {
      if (initializedSession === sessionId || !gram()) return;
      stop();
      initializedSession = sessionId;
      restAngle = 0;
      const g = gram();
      refreshLiveParts();
      controllers.set(g, api);
      g.dataset.reducedMotion = String(reduced());
      g.dataset.ready = "true";
      g.dataset.motion = "idle";
      g.dataset.showPivots = "false";
      applyRestPose();
      state("Idle");
      let fresh = false;
      try {
        const key = "gramophone:enter:" + sessionId;
        fresh = sessionStorage.getItem(key) === "1";
        sessionStorage.removeItem(key);
      } catch {}
      if (fresh) void entranceSequence();
      else void perform("listen-lean");
    },
    playEntrance: entranceSequence,
    playMotion(name, options = {}) {
      cancelAllMotion();
      return perform(name, options);
    },
    preview(name) {
      stop();
      if (name === "entrance") void entranceSequence();
      else if (name === "prototype-entrance") {
        prepareRigPreview();
        void sequence(reduced() ? ["wake", "listen-lean"] : entranceStageNames, { rigPreview: true, persist: true });
      }
      else
        void perform(name, {
          cycleIndex: 2,
          duration: name === "enter-drop" ? 2800 : undefined,
          rigPreview: !!rigStageFor(name),
          persist: !!rigStageFor(name),
        });
    },
    refreshParts: refreshLiveParts,
    pause() {
      paused = true;
      for (const animation of animations) animation.pause();
    },
    resume() {
      paused = false;
      for (const animation of animations) animation.play();
    },
    reset() {
      stop();
      restAngle = 0;
      refreshLiveParts();
      applyRestPose();
      const g = gram();
      if (g) {
        g.dataset.ready = "true";
        g.dataset.motion = "idle";
      }
      const record = part("record-root");
      if (record) {
        record.style.visibility = "visible";
        record.style.opacity = "1";
      }
    },
    cancelMotion: cancelAllMotion,
    syncScratchHit: scratchHit,
    listen(repair = false) {
      cancelAllMotion();
      void perform("listen-lean", {
        state: repair ? "Repairing" : "Listening",
      });
    },
    isEnabled: audio.isEnabled,
    reducedPreference: () => reducePreference,
    focusQuestion,
    toggle(topics, options = {}) {
      if (host.dataset.playing === "true") stop();
      else return play(topics, "album", options);
    },
    setEnabled(value) {
      stop();
      audio.setEnabled(value);
    },
    setReducedMotion(value) {
      reducePreference = !!value;
      try {
        localStorage.setItem("hamaahang-reduced-motion", value ? "on" : "off");
      } catch {}
      updateReduction();
    },
    setSlowMotion(value) {
      motionRate = value ? 0.35 : 1;
      for (const animation of animations) animation.playbackRate = motionRate;
    },
    setShowPivots(value) {
      host.dataset.showPivots = String(!!value);
      const g = gram();
      if (g) g.dataset.showPivots = String(!!value);
    },
    diagnostics: () => {
      const binding = refreshLiveParts();
      let currentRig = rigDiagnostics;
      if (rigPlayback) {
        const elapsed = rigPlayback.completed ? rigPlayback.duration
          : Math.max(0, Math.min(rigPlayback.duration, Number(rigPlayback.animation?.currentTime) || 0));
        const sample = sampleEntrancePose(rigPlayback.stage, elapsed / rigPlayback.duration, rigPlayback.geometry);
        currentRig = {
          ...rigDiagnostics,
          currentProgress: elapsed / rigPlayback.duration,
          clamped: sample.diagnostics.clamped,
          wrist: sample.diagnostics.wrist,
          discCenter: sample.diagnostics.discCenter,
          attachmentDistance: sample.diagnostics.attachmentDistance || 0,
          footTargets: sample.diagnostics.feet,
        };
        const machineMatrix = part("machine-root")?.getScreenCTM();
        const renderedPoint = (target, point, reference = machineMatrix) => {
          const matrix = part(target)?.getScreenCTM();
          if (!matrix || !reference) return null;
          const value = new DOMPoint(point.x, point.y).matrixTransform(reference.inverse().multiply(matrix));
          return { x: value.x, y: value.y };
        };
        const wrist = renderedPoint("right-forearm", rigPlayback.geometry.arm.wrist);
        const discCenter = renderedPoint("record-prop", { x: 0, y: 0 });
        currentRig.rendered = {
          wrist, discCenter,
          attachmentDistance: wrist && discCenter ? Math.hypot(wrist.x - discCenter.x, wrist.y - discCenter.y) : null,
          leftFoot: renderedPoint("left-foot", rigPlayback.geometry.left.ankle, part("gramophone-root")?.getScreenCTM()),
          rightFoot: renderedPoint("right-foot", rigPlayback.geometry.right.ankle, part("gramophone-root")?.getScreenCTM()),
        };
      }
      return {
        controllerFound: true,
        hostConnected: host.isConnected,
        ready: gram()?.dataset.ready,
        reducedMotion: reduced(),
        osReduced: media.matches,
        userReduced: reducePreference,
        audioState: audio.status(),
        resolvedParts: binding.count,
        missingParts: binding.missing,
        activeChannels: Object.fromEntries(activeChannels),
        restAngle,
        motionRate,
        motionGeneration,
        targetsConnected: [
          "artifact-motion",
          "machine-root",
          "horn-follow",
          "tonearm-follow",
          "record-root",
        ].every((name) => part(name)?.isConnected),
        motion: currentMotion,
        activeAnimations: animations.size,
        pendingWaits: waits.size,
        entering: entrance,
        rig: currentRig,
        disposed,
      };
    },
    dispose() {
      disposed = true;
      stop();
      clearInterval(idle);
      document.removeEventListener("visibilitychange", visibility);
      media.removeEventListener("change", updateReduction);
      liveControllers.delete(api);
      controllers.delete(host);
      const g = gram();
      if (g) controllers.delete(g);
    },
  };
  controllers.set(host, api);
  liveControllers.add(api);
  return api;
}
