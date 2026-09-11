import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const ROOT = "C:\\SRPG\\collabo-team-project-august-pixelchroma";
const BUILD = path.join(ROOT, ".codex-ppt-build");
const OUT = path.join(ROOT, "outputs");
const SKILL_DIR = "C:\\Users\\정해양\\.codex\\plugins\\cache\\openai-primary-runtime\\presentations\\26.909.12148\\skills\\presentations";
const RUNTIME_PYTHON = "C:\\Users\\정해양\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\python\\python.exe";
const FINAL_PPTX = path.join(OUT, "PixelChroma_추가작업_기술보고_생성형_v2.pptx");
const FONT = "Malgun Gothic";
const MONO = "Consolas";

const C = {
  bg: "#08111F", panel: "#111D2E", panel2: "#17263A", ink: "#F4F8FC",
  muted: "#9CB0C8", cyan: "#3BE0E5", magenta: "#F64D8D", orange: "#FF9C4A",
  violet: "#9A78FF", green: "#58E2A3", red: "#FF5E68", line: "#29405D",
};

const { finalizePresentation } = await import(pathToFileURL(path.join(SKILL_DIR, "container_tools/artifact_tool_utils.mjs")).href);
await fs.mkdir(BUILD, { recursive: true });
await fs.mkdir(OUT, { recursive: true });

const img = async (rel) => new Uint8Array(await fs.readFile(path.join(ROOT, rel)));
const assets = {
  vanguard: await img("Assets/4.Image/Characters/player/Vanguard_Battle.png"),
  tesla: await img("Assets/4.Image/Characters/player/TeslaOperator_Battle.png"),
  grenadier: await img("Assets/4.Image/Characters/player/Grenadier_Battle.png"),
  mage: await img("Assets/4.Image/Characters/player/ArcMage_Battle.png"),
  medic: await img("Assets/4.Image/Characters/player/FieldMedic_Battle.png"),
  bulwark: await img("Assets/4.Image/Characters/Enemy/e_bulwark_Battle.png"),
  jammer: await img("Assets/4.Image/Characters/Enemy/e_jammer_Battle.png"),
  spotter: await img("Assets/4.Image/Characters/Enemy/e_spotter_Battle.png"),
};

const deck = Presentation.create({ slideSize: { width: 1280, height: 720 } });

function shape(slide, geometry, left, top, width, height, fill, line = "none", radius = 0) {
  return slide.shapes.add({
    geometry, position: { left, top, width, height }, fill,
    line: { style: "solid", fill: line, width: line === "none" ? 0 : 1 },
    ...(radius ? { borderRadius: radius } : {}),
  });
}

function text(slide, value, left, top, width, height, size = 24, color = C.ink, bold = false, opts = {}) {
  const box = slide.shapes.add({
    geometry: "textbox", name: opts.name,
    position: { left, top, width, height }, fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  box.text = value;
  box.text.style = {
    typeface: opts.mono ? MONO : FONT, fontSize: size, color, bold,
    autoFit: opts.autoFit ?? "shrinkText",
    ...(opts.align ? { alignment: opts.align } : {}),
  };
  return box;
}

function addImage(slide, bytes, alt, left, top, width, height, fit = "contain", opacity = null) {
  const im = slide.images.add({ blob: bytes, contentType: "image/png", alt, fit, position: { left, top, width, height } });
  if (opacity !== null) im.opacity = opacity;
  return im;
}

function base(slide, section, page, accent = C.cyan) {
  slide.background.fill = C.bg;
  shape(slide, "rect", 0, 0, 14, 720, accent);
  text(slide, `PIXELCHROMA  /  ${section}`, 48, 24, 560, 28, 15, C.muted, true);
  text(slide, String(page).padStart(2, "0"), 1184, 24, 48, 28, 15, accent, true, { align: "right" });
  shape(slide, "line", 48, 60, 1184, 1, "none", C.line);
}

function title(slide, heading, kicker = "") {
  if (kicker) text(slide, kicker.toUpperCase(), 58, 82, 420, 25, 15, C.cyan, true);
  text(slide, heading, 58, 110, 1120, 66, 36, C.ink, true, { name: "title" });
}

function chip(slide, label, left, top, width, color = C.cyan) {
  shape(slide, "roundRect", left, top, width, 34, color, "none", 17);
  text(slide, label, left + 10, top + 5, width - 20, 22, 14, C.bg, true, { align: "center" });
}

function card(slide, left, top, width, height, heading, body, accent = C.cyan, number = "") {
  shape(slide, "roundRect", left, top, width, height, C.panel, C.line, 18);
  shape(slide, "rect", left, top, 6, height, accent);
  if (number) text(slide, number, left + 22, top + 16, 54, 42, 28, accent, true);
  text(slide, heading, left + (number ? 78 : 24), top + 18, width - (number ? 100 : 48), 38, 22, C.ink, true);
  text(slide, body, left + 24, top + 66, width - 48, height - 82, 17, C.muted, false);
}

function codeBox(slide, code, left, top, width, height, accent = C.cyan) {
  shape(slide, "roundRect", left, top, width, height, "#07101B", C.line, 14);
  shape(slide, "rect", left, top, width, 6, accent);
  text(slide, code, left + 20, top + 20, width - 40, height - 34, 13, "#C9D8E8", false, { mono: true, autoFit: "shrinkText" });
}

function sectionSlide(no, label, subtitle, accent, art) {
  const s = deck.slides.add();
  s.background.fill = C.bg;
  shape(s, "rect", 0, 0, 1280, 720, `linear(135deg, ${C.bg} 0%, ${C.panel2} 100%)`);
  shape(s, "rect", 64, 100, 10, 450, accent);
  text(s, `PART ${no}`, 104, 112, 240, 42, 20, accent, true);
  text(s, label, 104, 174, 700, 100, 58, C.ink, true);
  text(s, subtitle, 108, 294, 620, 90, 24, C.muted, false);
  shape(s, "roundRect", 108, 430, 270, 46, C.panel, C.line, 23);
  text(s, "PIXELCHROMA TECH REPORT", 128, 441, 230, 22, 14, accent, true, { align: "center" });
  addImage(s, art, `${label} 대표 캐릭터`, 800, 86, 390, 570, "contain");
  s.speakerNotes.textFrame.setText(`${label} 섹션 구분 슬라이드. 원문 Claude artifact의 내용을 새 레이아웃으로 재구성.`);
  return s;
}

// 01 Cover
{
  const s = deck.slides.add();
  s.background.fill = C.bg;
  shape(s, "rect", 0, 0, 1280, 720, `linear(135deg, ${C.bg} 0%, #14213A 55%, #241332 100%)`);
  shape(s, "rect", 66, 74, 9, 505, C.cyan);
  text(s, "UNITY 6 · GRID TACTICS · TECH REPORT", 106, 76, 650, 30, 16, C.cyan, true);
  text(s, "PixelChroma\n추가 작업 보고", 102, 138, 690, 165, 58, C.ink, true, { name: "title" });
  text(s, "카메라 연출 · 적 이동 시퀀스 · 부대 스프라이트 · 승리/패배 판정", 106, 330, 690, 72, 24, C.muted, false);
  chip(s, "UNITY 6000.3.10f1", 106, 448, 214, C.cyan);
  chip(s, "5 × 6 GRID", 336, 448, 145, C.magenta);
  chip(s, "PORTRAIT / MOBILE", 497, 448, 210, C.orange);
  text(s, "2026.09.11", 106, 526, 240, 28, 17, C.muted, true);
  addImage(s, assets.vanguard, "플레이어 뱅가드", 756, 110, 390, 535, "contain");
  addImage(s, assets.spotter, "적 정찰병", 1002, 400, 220, 180, "contain");
  s.speakerNotes.textFrame.setText("원문 제목: PixelChroma 추가 작업 보고. 기술 환경: Unity 6000.3.10f1, 5×6 Grid, Portrait/Mobile.");
}

// 02 Contents
{
  const s = deck.slides.add(); base(s, "CONTENTS", 2, C.cyan); title(s, "네 가지 작업이 하나의 전투 흐름을 만든다", "overview");
  const items = [
    ["04", "카메라 연출", "포커스 줌 · 건물 페이드", C.cyan],
    ["05", "적 이동 연출", "경로 화살표 · 타일 이동", C.magenta],
    ["06", "부대 & 스프라이트", "SO 기반 유닛 비주얼", C.violet],
    ["07", "승리 · 패배", "배틀 결과 시퀀스", C.orange],
  ];
  items.forEach((it, i) => {
    const x = 58 + (i % 2) * 590, y = 215 + Math.floor(i / 2) * 205;
    card(s, x, y, 550, 164, it[1], it[2], it[3], it[0]);
  });
  s.speakerNotes.textFrame.setText("목차: 04 카메라 연출, 05 적 이동 연출, 06 부대와 스프라이트, 07 승리와 패배.");
}

// 03 Section 04
sectionSlide("04", "카메라 연출", "포커스 줌 · 건물 페이드 · 추적", C.cyan, assets.tesla);

// 04 Focus zoom
{
  const s = deck.slides.add(); base(s, "CAMERA", 4, C.cyan); title(s, "턴 전환마다 유닛을 줌인한다", "CameraController.FocusOn");
  codeBox(s, `public void FocusOn(Vector3 worldPosition)\n{\n  Vector3 offset = cam.transform.position\n    - GetCurrentGroundFocus();\n  focusTargetPos = worldPosition + offset;\n  focusTargetPos.y = cam.transform.position.y;\n  focusStartPos = cam.transform.position;\n  focusStartFOV = cam.fieldOfView;\n  targetFOV = focusZoomFOV; // 38°\n  focusLerp = 0f;\n  isFocusing = true;\n}\n\nfloat t = Mathf.SmoothStep(0f, 1f, focusLerp);\ncam.transform.position = Vector3.Lerp(\n  focusStartPos, focusTargetPos, t);\ncam.fieldOfView = Mathf.Lerp(\n  focusStartFOV, targetFOV, t);`, 58, 195, 636, 438, C.cyan);
  card(s, 728, 195, 480, 120, "FocusOnAndWait", "각 적 행동 전에 코루틴으로 카메라 이동이 끝날 때까지 기다린다.", C.cyan);
  card(s, 728, 337, 480, 120, "ResetFocus", "모든 적 행동이 끝나면 위치와 FOV를 원래 상태로 복원한다.", C.green);
  const metrics = [["38°", "ZOOM FOV"], ["0.35s", "TRANSITION"], ["8f", "FOLLOW DAMPING"]];
  metrics.forEach((m, i) => { const x = 728 + i * 160; shape(s, "roundRect", x, 489, 145, 112, C.panel2, C.line, 16); text(s, m[0], x+12, 504, 121, 38, 28, [C.cyan,C.magenta,C.orange][i], true,{align:"center"}); text(s,m[1],x+10,550,125,23,12,C.muted,true,{align:"center"}); });
  s.speakerNotes.textFrame.setText("SmoothStep 보간으로 카메라 위치와 FOV를 함께 변경한다. 핵심 값은 38도, 0.35초, follow damping 8f.");
}

// 05 Building fade
{
  const s = deck.slides.add(); base(s, "CAMERA", 5, C.cyan); title(s, "카메라에 닿는 건물은 투명해진다", "Building Fade");
  codeBox(s, `int hitCount = Physics.OverlapSphereNonAlloc(\n  transform.position,\n  buildingContactRadius, // 0.75f\n  buildingContactBuffer,\n  buildingContactMask);\n\nfloat targetAlpha = touching\n  ? buildingFadeAlpha : 1f;\npair.Value.MoveTowards(\n  targetAlpha, fadeSpeed * Time.deltaTime);`, 58, 190, 500, 275, C.cyan);
  const steps = [
    ["01", "감지", "OverlapSphereNonAlloc\n버퍼 64 · No-GC", C.cyan],
    ["02", "머티리얼", "Renderer 머티리얼 복제\n_BaseColor 알파 조절", C.magenta],
    ["03", "복원", "alpha=1에서 원본 복구\n복제 인스턴스 Destroy", C.green],
  ];
  steps.forEach((v,i)=>card(s, 590+i*205, 190, 185, 275, v[1], v[2], v[3], v[0]));
  shape(s,"roundRect",58,500,1147,120,C.panel,C.line,18);
  text(s,"URP TRANSPARENT",82,520,210,25,16,C.orange,true);
  text(s,"_Surface=1   ·   SrcAlpha / OneMinusSrcAlpha   ·   _ZWrite=0   ·   RenderQueue=Transparent   ·   Outline OFF",82,552,1080,28,18,C.ink,false);
  text(s,"건물 루트 탐색: building / apartment / skyscraper · city_all / background_에서 중단 · LateUpdate 실행",82,587,1080,23,15,C.muted,false);
  s.speakerNotes.textFrame.setText("2.5D 탑뷰에서 카메라와 건물이 닿아 유닛을 가리는 문제를 충돌 감지와 머티리얼 알파 조절로 해결한다.");
}

// 06 Section 05
sectionSlide("05", "적 이동 연출", "경로 화살표 · Bezier 곡선 · 타일 이동 애니메이션", C.magenta, assets.jammer);

// 07 Path arrow + native table
{
  const s = deck.slides.add(); base(s, "ENEMY MOVE", 7, C.magenta); title(s, "경로를 화살표로 그린다", "PathArrowRenderer");
  codeBox(s, `float dot = Vector3.Dot(dirIn, dirOut);\nif (dot > 0.99f) {\n  result.Add(curr);\n  continue;\n}\nVector3 curveStart = curr - dirIn * radius;\nVector3 curveEnd = curr + dirOut * radius;\nfor (int s = 0; s <= CurveSegments; s++) {\n  float t = (float)s / CurveSegments;\n  Vector3 a = Vector3.Lerp(curveStart, curr, t);\n  Vector3 b = Vector3.Lerp(curr, curveEnd, t);\n  result.Add(Vector3.Lerp(a, b, t));\n}`, 58, 190, 610, 365, C.magenta);
  text(s,"직각 코너를 Quadratic Bezier로 부드럽게 연결",58,574,610,30,18,C.muted,false);
  const tbl = s.tables.add({ rows: 6, columns: 3, left: 710, top: 190, width: 498, height: 310, values: [
    ["상수", "값", "역할"], ["PathWidth", "0.35", "라인 폭"], ["HeadWidth", "0.55", "화살촉 폭"], ["HeadLength", "0.45", "화살촉 길이"], ["CurveSegments", "6", "곡선 분할"], ["CurveRadius", "0.35", "곡률 반경"],
  ]});
  tbl.styleOptions = { headerRow: true, bandedRows: true };
  tbl.borders.assign({ style: "solid", fill: C.line, width: 1 });
  for (let r=0;r<6;r++) for(let c=0;c<3;c++){ const cell=tbl.getCell(r,c); cell.fill = r===0 ? C.magenta : (r%2?C.panel:C.panel2); cell.text.style={typeface:FONT,fontSize:r===0?16:15,bold:r===0,color:r===0?C.bg:C.ink}; }
  shape(s,"roundRect",710,528,498,92,C.panel,C.line,18);
  shape(s,"rect",710,528,6,92,C.magenta);
  text(s,"0.3초 드로잉",734,545,170,28,21,C.ink,true);
  text(s,"ShowPathAnimated()로 진행 방향을 따라 표시",914,548,270,24,15,C.muted,false);
  s.speakerNotes.textFrame.setText("A* 경로를 굵은 붉은 선과 삼각형 화살촉으로 시각화한다. 우회전과 좌회전 코너는 Quadratic Bezier로 완화한다.");
}

// 08 Enemy turn sequence
{
  const s = deck.slides.add(); base(s, "ENEMY MOVE", 8, C.magenta); title(s, "적 턴은 일곱 단계로 연출된다", "ProcessEnemyTurn");
  const seq = [
    ["01","FOCUS","FocusOnAndWait"],["02","PAUSE","0.2s"],["03","PATH","ShowPathAnimated 0.3s"],
    ["04","PAUSE","0.2s"],["05","MOVE","0.4–1.5s"],["06","CLEAR","화살표 제거 · 0.1s"],["07","ATTACK","guard · counter · evasion"],
  ];
  seq.forEach((v,i)=>{
    const x=58+(i%4)*292, y=205+Math.floor(i/4)*166;
    shape(s,"roundRect",x,y,260,126,i===6?"#311B2B":C.panel,C.line,18);
    text(s,v[0],x+18,y+15,44,28,18,C.magenta,true);
    text(s,v[1],x+66,y+15,170,28,18,C.ink,true);
    text(s,v[2],x+20,y+62,220,35,15,C.muted,false);
    if(i<6 && i%4!==3) shape(s,"rightArrow",x+258,y+49,32,26,C.magenta,"none");
  });
  shape(s,"roundRect",934,371,276,126,C.panel2,C.line,18);
  text(s,"RESET",954,388,90,28,18,C.green,true);
  text(s,"ResetFocus → 0.1s\n→ 플레이어 턴 배너",954,430,220,48,16,C.ink,false);
  shape(s,"roundRect",58,548,1152,80,C.panel,C.line,18);
  text(s,"MOVE DURATION",82,568,178,24,14,C.orange,true);
  text(s,"Clamp((tiles - 1) × 0.25, 0.4, 1.5)",270,561,470,36,24,C.ink,true,{mono:true});
  text(s,"첫 타일 RemoveUnit → 타일 간 Lerp → 마지막 타일 PlaceUnit · HeightOffset 반영",760,568,420,35,15,C.muted,false);
  s.speakerNotes.textFrame.setText("적 유닛별로 카메라 포커스, 경로 표시, 이동, 공격, 카메라 복원을 순서대로 실행한다.");
}

// 09 Section 06
sectionSlide("06", "부대 & 스프라이트", "ScriptableObject 기반 유닛 구성 · 2D 스프라이트 비주얼", C.violet, assets.grenadier);

// 10 Squad and sprite
{
  const s = deck.slides.add(); base(s, "SQUAD & SPRITE", 10, C.violet); title(s, "스쿼드 데이터로 적을 구성한다", "EnemySquadData");
  codeBox(s, `[System.Serializable]\npublic class SquadMember {\n  public EnemyUnitData unit;\n  public int count;\n  public Vector2Int[] preferredCells;\n}\n\nUnit.Create(\n  Team.Enemy, pos, enemyPrefab,\n  member.unit, defaultEnemySprite);`,58,190,420,300,C.violet);
  shape(s,"roundRect",510,190,330,300,C.panel,C.line,18);
  text(s,"SQUAD_01_순찰반",534,212,280,32,20,C.violet,true,{align:"center"});
  addImage(s,assets.spotter,"적 정찰병",540,260,125,120,"contain");
  addImage(s,assets.jammer,"적 돌격병",686,252,116,135,"contain");
  text(s,"순찰병 × 2",540,395,125,28,16,C.ink,true,{align:"center"});
  text(s,"돌격병 × 1",680,395,130,28,16,C.ink,true,{align:"center"});
  text(s,"SO가 있으면 구성대로 생성\n없으면 generic 적을 생성",538,443,272,42,15,C.muted,false,{align:"center"});
  shape(s,"roundRect",872,190,338,300,C.panel2,C.line,18);
  text(s,"SPRITE PIPELINE",896,214,290,28,18,C.cyan,true);
  const pipeline=["CharacterData.BattleSprite","unit.battleSprite","fallback Sprite","null / red cube"];
  pipeline.forEach((v,i)=>{shape(s,"roundRect",900,260+i*50,260,36,i===0?"#243B60":C.panel,C.line,14);text(s,v,914,267+i*50,232,20,14,i===3?C.red:C.ink,i===0,{align:"center"});});
  shape(s,"roundRect",58,528,1152,92,C.panel,C.line,18);
  text(s,"EDITOR TOOL",82,548,155,22,14,C.orange,true);
  text(s,"Tools > 적 유닛 스프라이트 강제 연결",248,542,400,31,20,C.ink,true);
  text(s,"이름으로 Sprite와 EnemyUnitData를 매칭 · SetupSpriteVisual이 Quad + 2D Billboard로 변환",678,545,500,42,15,C.muted,false);
  s.speakerNotes.textFrame.setText("EnemySquadData ScriptableObject로 적 구성과 수를 정의한다. 전투 스프라이트는 데이터에서 fallback까지 단계적으로 선택한다.");
}

// 11 Section 07
sectionSlide("07", "승리 · 패배", "배틀 결과 판정 · 배너 연출 · 스테이지 진행", C.orange, assets.mage);

// 12 Battle end
{
  const s = deck.slides.add(); base(s, "BATTLE RESULT", 12, C.orange); title(s, "전투가 끝나는 순간을 판정한다", "CheckBattleEnd");
  shape(s,"roundRect",58,195,1160,86,C.panel,C.line,18);
  text(s,"ATTACK RESOLVED",82,214,220,24,15,C.orange,true);
  text(s,"공격 처리 직후 살아 있는 플레이어와 적 유닛 수를 다시 계산",324,208,838,34,21,C.ink,true);
  shape(s,"downArrow",606,284,64,55,C.orange,"none");
  card(s,58,350,548,205,"VICTORY","적 생존 수 = 0\nBattleResult → 승리 메시지\nUnlockNextStage() → ShowVictory",C.green,"W");
  card(s,670,350,548,205,"DEFEAT","플레이어 생존 수 = 0\nBattleResult → 패배 메시지\nShowDefeat · 스테이지 해금 없음",C.red,"L");
  shape(s,"roundRect",58,585,1160,54,"#2A1F17",C.orange,18);
  text(s,"yield break",82,598,155,25,18,C.orange,true,{mono:true});
  text(s,"결과가 확정되면 남은 코루틴 행동을 즉시 중단한다.",248,598,900,25,17,C.ink,false);
  s.speakerNotes.textFrame.setText("공격 후 CheckBattleEnd가 생존 유닛을 확인한다. 승리 시 다음 스테이지를 해금하고, 패배 시 해금하지 않는다. yield break로 후속 행동을 중단한다.");
}

// 13 Summary + native table
{
  const s = deck.slides.add(); base(s, "SUMMARY", 13, C.cyan); title(s, "네 파트를 관통하는 원칙", "Design Principle");
  shape(s,"roundRect",58,185,1160,92,"#132D3A",C.cyan,20);
  text(s,"“보이지 않던 것을 보이게 만든다”",86,207,1100,46,30,C.ink,true,{align:"center"});
  const tbl=s.tables.add({rows:5,columns:3,left:58,top:310,width:1160,height:260,values:[
    ["파트","이전","변경 후"],["카메라","고정","동적 포커스"],["적 이동","순간 이동","경로 기반 시퀀스"],["부대 & 스프라이트","큐브","SO 기반 비주얼"],["승리 · 패배","수동","자동 판정"],
  ]});
  tbl.styleOptions={headerRow:true,bandedRows:true}; tbl.borders.assign({style:"solid",fill:C.line,width:1});
  for(let r=0;r<5;r++) for(let c=0;c<3;c++){const cell=tbl.getCell(r,c); cell.fill=r===0?C.cyan:(r%2?C.panel:C.panel2); cell.text.style={typeface:FONT,fontSize:r===0?17:16,bold:r===0||c===0,color:r===0?C.bg:(c===2?C.ink:C.muted)};}
  text(s,"KEY FILES",58,598,140,24,14,C.violet,true);
  text(s,"CameraController.cs  ·  PathArrowRenderer.cs  ·  Unit.cs  ·  EnemySpriteLinker.cs  ·  Enemy/Squads/*.asset",210,594,995,31,16,C.muted,false);
  s.speakerNotes.textFrame.setText("핵심 원칙은 전투 상태와 의도를 플레이어가 볼 수 있게 만드는 것이다. 주요 파일: Grid/CameraController.cs, UI/PathArrowRenderer.cs, GamePlay/Unit.cs, Editor/EnemySpriteLinker.cs, SOdata/Enemy/Squads/*.asset.");
}

const stagingDir = path.join(BUILD, ".codex-finalizer");
await fs.mkdir(stagingDir, { recursive: true });
const candidatePath = path.join(stagingDir, "PixelChroma_candidate.pptx");
await (await PresentationFile.exportPptx(deck)).save(candidatePath);

const requirements = {
  explicitTotalSlideCount: 13,
  requiredNativeTableOwnerSlides: [7, 13],
  requiredNativeChartOwnerSlides: [],
  requiredEmbeddedWorkbookChartOwnerSlides: [],
};
const result = await finalizePresentation({
  ...requirements,
  workspaceDir: ROOT,
  candidatePath,
  finalPath: FINAL_PPTX,
  pythonExecutable: RUNTIME_PYTHON,
  integrityValidatorPath: path.join(SKILL_DIR, "container_tools/inspect_presentation_package_integrity.py"),
  layoutValidatorPath: path.join(SKILL_DIR, "container_tools/inspect_presentation_layout_geometry.py"),
  layoutArgs: [
    "--expected-slide-size-emu", "12192000,6858000",
    "--validate-bullet-geometry", "--validate-heading-fit",
    "--require-native-table-slide", "7", "--require-native-table-slide", "13",
  ],
  requiredNativeTableOwnerSlides: [7, 13],
  fontPolicy: { basis: "design", families: [FONT, MONO], scriptFonts: { ea: FONT } },
  verifyArtifactToolImport: true,
  receiptPath: path.join(stagingDir, "PixelChroma_final_v2.validation.json"),
});
console.log(JSON.stringify({ final: FINAL_PPTX, result }, null, 2));
