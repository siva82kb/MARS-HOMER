import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

from docx import Document
from docx.shared import Pt, RGBColor, Inches, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_ALIGN_VERTICAL, WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
import copy

OUTPUT = r"C:\Users\gokul\Downloads\Homer Mars Functional Test.docx"

# ── colour palette ──────────────────────────────────────────────────────────
C_TITLE_BG   = RGBColor(0x1F, 0x49, 0x7D)   # dark navy
C_TITLE_FG   = RGBColor(0xFF, 0xFF, 0xFF)
C_HDR_BG     = RGBColor(0x2E, 0x75, 0xB6)   # medium blue
C_HDR_FG     = RGBColor(0xFF, 0xFF, 0xFF)
C_MOD_BG     = RGBColor(0xBD, 0xD7, 0xEE)   # light blue module header
C_MOD_FG     = RGBColor(0x1F, 0x49, 0x7D)
C_ROW_ALT    = RGBColor(0xF2, 0xF7, 0xFD)   # very light blue alternate row
C_ROW_WHITE  = RGBColor(0xFF, 0xFF, 0xFF)
C_CRIT_BG    = RGBColor(0xFF, 0xE0, 0xE0)   # pale red  → CRITICAL
C_HIGH_BG    = RGBColor(0xFF, 0xF3, 0xCD)   # pale amber → HIGH
C_MED_BG     = RGBColor(0xE2, 0xEF, 0xDA)   # pale green → MEDIUM
C_NEW_FG     = RGBColor(0xC0, 0x00, 0x00)   # dark red for "(new fix)" label
C_BORDER     = "2E75B6"

# ── helpers ─────────────────────────────────────────────────────────────────
def set_cell_bg(cell, rgb: RGBColor):
    tc   = cell._tc
    tcPr = tc.get_or_add_tcPr()
    shd  = OxmlElement('w:shd')
    hex_ = f"{rgb[0]:02X}{rgb[1]:02X}{rgb[2]:02X}"
    shd.set(qn('w:val'),   'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'),  hex_)
    # remove any existing shd
    for old in tcPr.findall(qn('w:shd')):
        tcPr.remove(old)
    tcPr.append(shd)

def set_cell_borders(cell, color=C_BORDER, sz="4"):
    tc   = cell._tc
    tcPr = tc.get_or_add_tcPr()
    borders = OxmlElement('w:tcBorders')
    for side in ('top', 'left', 'bottom', 'right', 'insideH', 'insideV'):
        b = OxmlElement(f'w:{side}')
        b.set(qn('w:val'),   'single')
        b.set(qn('w:sz'),    sz)
        b.set(qn('w:space'), '0')
        b.set(qn('w:color'), color)
        borders.append(b)
    for old in tcPr.findall(qn('w:tcBorders')):
        tcPr.remove(old)
    tcPr.append(borders)

def cell_para(cell, text, bold=False, italic=False,
              fg: RGBColor = None, size=9, align=WD_ALIGN_PARAGRAPH.LEFT,
              new_fix=False):
    """Write text into a cell (clears existing paragraphs first)."""
    for p in cell.paragraphs:
        p._element.getparent().remove(p._element)
    para = cell.add_paragraph()
    para.alignment = align
    para.paragraph_format.space_before = Pt(1)
    para.paragraph_format.space_after  = Pt(1)

    if new_fix and "(new fix)" in text:
        # split on "(new fix)" and colour that part separately
        parts = text.split("(new fix)")
        run1 = para.add_run(parts[0])
        run1.bold, run1.italic = bold, italic
        run1.font.size = Pt(size)
        if fg: run1.font.color.rgb = fg
        run2 = para.add_run("(new fix)")
        run2.bold = True
        run2.font.size = Pt(size)
        run2.font.color.rgb = C_NEW_FG
        if len(parts) > 1 and parts[1]:
            run3 = para.add_run(parts[1])
            run3.bold, run3.italic = bold, italic
            run3.font.size = Pt(size)
            if fg: run3.font.color.rgb = fg
    else:
        run = para.add_run(text)
        run.bold, run.italic = bold, italic
        run.font.size = Pt(size)
        if fg:
            run.font.color.rgb = fg

def set_col_widths(table, widths_cm):
    for row in table.rows:
        for i, cell in enumerate(row.cells):
            if i < len(widths_cm):
                cell.width = Cm(widths_cm[i])

def priority_bg(pri):
    if '[C]' in pri: return C_CRIT_BG
    if '[H]' in pri: return C_HIGH_BG
    if '[M]' in pri: return C_MED_BG
    return C_ROW_WHITE

# ── document setup ───────────────────────────────────────────────────────────
doc = Document()
sec = doc.sections[0]
sec.page_width   = Inches(11.69)   # A4 landscape
sec.page_height  = Inches(8.27)
sec.left_margin  = Inches(0.5)
sec.right_margin = Inches(0.5)
sec.top_margin   = Inches(0.5)
sec.bottom_margin= Inches(0.5)

# default style
style = doc.styles['Normal']
style.font.name = 'Calibri'
style.font.size = Pt(9)

# ── TITLE BLOCK ──────────────────────────────────────────────────────────────
title_tbl = doc.add_table(rows=1, cols=1)
title_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
tc = title_tbl.cell(0, 0)
set_cell_bg(tc, C_TITLE_BG)
cell_para(tc, "MARS-HOMER REHABILITATION ROBOT — FUNCTIONAL TEST CASES",
          bold=True, fg=C_TITLE_FG, size=14, align=WD_ALIGN_PARAGRAPH.CENTER)
tc.add_paragraph()
p2 = tc.paragraphs[-1]
p2.alignment = WD_ALIGN_PARAGRAPH.CENTER
r2 = p2.add_run("Version 2.0  |  Date: 2026-07-30  |  Total: 76 test cases")
r2.font.color.rgb = RGBColor(0xBD, 0xD7, 0xEE)
r2.font.size = Pt(9)

doc.add_paragraph()  # spacer

# ── LEGEND ───────────────────────────────────────────────────────────────────
leg = doc.add_paragraph()
for label, bg in [(" [C] CRITICAL ", C_CRIT_BG),
                  ("  [H] HIGH ", C_HIGH_BG),
                  ("  [M] MEDIUM ", C_MED_BG)]:
    r = leg.add_run(label)
    r.font.size = Pt(8.5)
    r.bold = True
leg.add_run("   Row colour = priority.  ")
r3 = leg.add_run("Red bold = new-fix test case.")
r3.font.color.rgb = C_NEW_FG
r3.font.size = Pt(8.5)

doc.add_paragraph()

# ── COL WIDTHS (cm) for 5-column table on A4 landscape ─────────────────────
# ID | Test Case | Steps | Expected Result | P/F
WIDTHS = [1.5, 5.5, 7.5, 8.5, 1.3]

# ═══════════════════════════════════════════════════════════════════════════
# TEST DATA
# Each module: (module_title, [ (id, name, priority, steps, expected) ])
# ═══════════════════════════════════════════════════════════════════════════
MODULES = [

("1. LOGIN SCENE", [
    ("1.1", "Valid HomerID login", "[C]",
     "Enter a known valid HomerID and submit",
     "System authenticates, loads patient data from S3, navigates to MAIN"),
    ("1.2", "Invalid HomerID", "[C]",
     "Enter a non-existent HomerID and submit",
     "Clear error message shown, no crash, stays on login screen"),
    ("1.3", "Network unavailable", "[C]",
     "Disconnect internet, attempt login",
     "Graceful \"No network\" error shown, app does not freeze or crash"),
    ("1.4", "First-time patient setup — valid total", "[C]",
     "Enter new HomerID, set ML=10 AP=10 MLAP=10 (total=30), set limb & location, confirm",
     "Config saved to configdata.csv, navigates to MAIN scene"),
    ("1.5", "Invalid time total rejected", "[H]",
     "Set ML=15, AP=5, MLAP=5 (total=25), try to confirm",
     "System blocks navigation, displays \"must total 30 minutes\" warning"),
    ("1.6", "Config persists after restart", "[H]",
     "Complete config, close and reopen app, login same HomerID",
     "All values (limb, times, location) restored correctly"),
]),

("2. MAIN (DASHBOARD) SCENE", [
    ("2.1", "Remaining time display", "[C]",
     "Login after partial session",
     "Shows correct remaining minutes for each movement (prescribed minus used)"),
    ("2.2", "7-day pie chart", "[H]",
     "Login as patient with 7 days history",
     "Charts reflect actual per-day data, not blank or zeroed"),
    ("2.3", "Zero remaining time", "[H]",
     "Login as patient who used all minutes today",
     "Dashboard shows 0 min remaining, appropriate day-complete message shown"),
    ("2.4", "ARMBO button press", "[C]",
     "Press ARMBO from dashboard",
     "Navigates to ROBOTCALIB, no duplicate scene transition"),
    ("2.5", "Fresh patient (no history)", "[H]",
     "First-ever login for new patient",
     "Charts show empty/zero state, no crash"),
]),

("3. CALIBRATION SCENE (ROBOTCALIB)", [
    ("3.1", "Robot connected at scene load", "[C]",
     "Device powered on and Bluetooth paired before scene loads",
     "Connection established, sensor stream starts, green connection indicator"),
    ("3.2", "Robot not connected", "[C]",
     "Load ROBOTCALIB with device off or unpaired",
     "Error message shown, red indicator, no freeze or crash"),
    ("3.3", "IMU angles all < 50°", "[C]",
     "Position robot flat (all IMU angles below 50°)",
     "All 4 angle indicators green, ARMBO button enabled"),
    ("3.4", "IMU angle exceeds 50°", "[C]",
     "Tilt robot so one axis exceeds 50°",
     "That angle indicator turns red, ARMBO button disabled"),
    ("3.5", "Calibration trigger", "[C]",
     "Ensure all angles < 50°, press ARMBO",
     "Calibration command sent to robot, confirmation response received"),
    ("3.6", "Frame rate monitoring", "[H]",
     "Let calibration scene run for 10 s, observe frame rate display",
     "Frame rate shown consistently > 85 Hz; warning shown if it drops below 85 Hz"),
    ("3.7", "Bluetooth disconnects mid-calibration", "[C]",
     "Power off device during calibration",
     "Watchdog detects no data for 2 s, marks disconnected, navigates to SUMMARY"),
]),

("4. SET TRAINING PLANE SCENE (CHOOSEPLANE)", [
    ("4.1", "Arm reaches -90°", "[C]",
     "Enter CHOOSEPLANE scene",
     "Robot arm moves to -90°, confirmation message displayed on screen"),
    ("4.2", "Plane angle slider", "[H]",
     "Drag slider to various positions",
     "Displayed angle updates in real time and matches slider position exactly"),
    ("4.3", "Plane angle saved and persists", "[H]",
     "Set angle, confirm, return to scene next session",
     "Previously saved angle loaded from trainingplane.csv correctly"),
]),

("5. MARS CONTROL SCENE (MARSSETUP — ARM ACTIVATION)", [
    ("5.1", "Full activation sequence", "[C]",
     "Attach patient limb correctly, follow on-screen prompts",
     "State progresses: IDLE → ACTIVATE → ATTACHARM → SETANGLE → DONE"),
    ("5.2", "Limb not attached", "[C]",
     "Skip attaching limb, try to proceed",
     "Force < 10 N threshold detected, system blocks progress or warns user"),
    ("5.3", "Arm weight check", "[C]",
     "Attach limb with correct force",
     "Force > ARM_WEIGHT_THRESHOLD (10 N) confirmed, proceeds to next step"),
    ("5.4", "Arm weight out of range", "[H]",
     "Apply excessive or insufficient force",
     "OnArmWeightInOutofRange fires, user warned on screen"),
    ("5.5", "Robot moves to training plane angle", "[C]",
     "Complete limb attachment step",
     "Robot arm automatically moves to the stored training plane angle"),
    ("5.6", "State machine stuck prevention", "[H]",
     "Interrupt mid-sequence (do not press button at required step)",
     "System does not loop infinitely, shows clear instruction prompt, waits safely"),
    ("5.7", "Bluetooth disconnects during setup", "[C]",
     "Power off device during activation",
     "Disconnect detected within 2 s, navigates to SUMMARY, arm drops safely"),
]),

("6. CHOOSE MOVEMENT AND CHOOSE GAME", [
    ("6.1", "Select ML movement", "[C]",
     "Tap ML button",
     "ML selected, AROM status for ML shown"),
    ("6.2", "Select AP movement", "[C]",
     "Tap AP button",
     "AP selected, AROM status for AP shown"),
    ("6.3", "Select MLAP movement", "[C]",
     "Tap MLAP button",
     "MLAP selected, arm weight check triggered automatically"),
    ("6.4", "Missing AROM forces assessment", "[C]",
     "Select movement with no prior AROM data",
     "Navigates to AROM assessment scene for that movement"),
    ("6.5", "Valid AROM skips assessment", "[H]",
     "Select movement with valid up-to-date AROM data",
     "Goes directly to game selection, no assessment triggered"),
    ("6.6", "AROM mismatch forces reassessment", "[H]",
     "Change training plane angle, then select movement with old AROM",
     "System detects plane-angle mismatch, re-triggers AROM assessment"),
    ("6.7", "All 6 games selectable", "[H]",
     "Cycle through SS, PP, DC, TT, TW, MC buttons",
     "Each game selectable, correct icon shown, no UI glitch"),
    ("6.8", "ARMBO starts game", "[C]",
     "Select movement and game, press ARMBO",
     "Navigates to correct game scene"),
    ("6.9", "ARMBO without selection blocked", "[H]",
     "Press ARMBO before choosing movement or game",
     "Navigation blocked, user prompted to make selection first"),
]),

("7. AROM ASSESSMENT (AROMML / AROMAP / AROMMLAP)", [
    ("7.1", "INIT to ASSESSROM transition", "[C]",
     "Enter AROM scene",
     "Robot ready, \"Press ARMBO to start\" instruction shown"),
    ("7.2", "Left boundary detection", "[C]",
     "Move arm to leftmost position, press ARMBO",
     "Left boundary recorded in robot coordinates, shown on screen"),
    ("7.3", "Right boundary detection", "[C]",
     "Move arm to rightmost position",
     "Right boundary recorded, right > left confirmed"),
    ("7.4", "Top and bottom boundaries (AP/MLAP)", "[C]",
     "Move arm to top and bottom limits during AP or MLAP assessment",
     "Top > bottom confirmed, both values stored correctly"),
    ("7.5", "Boundary adjustment L/R/T/B keys", "[H]",
     "Press L key, drag left boundary to a new valid position, release mouse",
     "Boundary updates to new valid position, displayed correctly on screen"),
    ("7.6", "Inverted boundary rejected (new fix)", "[H]",
     "Drag LEFT boundary past RIGHT boundary and release mouse",
     "Adjustment rejected, boundary snaps back to last valid position, warning logged"),
    ("7.7", "Boundary clamped to workspace limits (new fix)", "[H]",
     "Drag boundary marker beyond the screen edge",
     "Boundary value clamped to robot EPMINZ/EPMAXZ or EPMINY/EPMAXY limit"),
    ("7.8", "Save AROM", "[C]",
     "Press ARMBO after ADJUST step",
     "AROM written to file, state moves to DONE, navigates to game selection"),
    ("7.9", "Real-time trajectory display", "[H]",
     "Move arm during assessment",
     "Trajectory line drawn on screen in real time, current position shown"),
    ("7.10", "AROM reused next session", "[C]",
     "Login next day with same training plane angle",
     "Previously saved AROM loaded from file, assessment skipped"),
    ("7.11", "Redo assessment resets state", "[M]",
     "Complete assessment, press REDO button",
     "State resets to INIT, all trajectory lines cleared, fresh assessment starts"),
]),

("8. GAME SCENES (ALL 6 GAMES: SS / PP / DC / TT / TW / MC)", [
    ("8.1", "Arm-to-cursor full screen mapping", "[C]",
     "Move arm through complete AROM range left to right and top to bottom",
     "Game cursor moves from one screen edge to the other proportionally"),
    ("8.2", "Hit detection", "[C]",
     "Move arm to target position during game",
     "Target counted as HIT, hit counter increments on screen"),
    ("8.3", "Miss detection", "[C]",
     "Let a target pass without reaching it",
     "MISS counter increments, no false HIT registered"),
    ("8.4", "Stars awarded on performance", "[H]",
     "Complete trial with > 80% success rate, check star count at trial end",
     "Stars awarded correctly per threshold, shown on summary"),
    ("8.5", "Reach speed increases on high performance", "[H]",
     "Maintain > 90% success rate in a trial",
     "Robot reach speed increases by up to +2.5% in next trial"),
    ("8.6", "Reach speed decreases on low performance", "[H]",
     "Achieve < 80% success rate in a trial",
     "Robot reach speed decreases by up to -2.5% in next trial"),
    ("8.7", "Trial ends at configured duration", "[C]",
     "Start a game, wait for configured game duration",
     "Game ends automatically at correct time, data logging stops cleanly"),
    ("8.8", "Bluetooth disconnects mid-game (new fix)", "[C]",
     "Power off device during active game trial",
     "Within 2 s: trial data saved to disk, session CSV written, app navigates to SUMMARY scene automatically"),
    ("8.9", "Battery warning (30%)", "[H]",
     "Run app with battery below 30% and not charging",
     "Warning panel shown, trial can still continue or exit cleanly"),
    ("8.10", "ML game cursor moves horizontally only", "[C]",
     "Select ML, start game, move arm medially and laterally",
     "Cursor moves left/right only, no unintended vertical drift"),
    ("8.11", "AP game cursor moves vertically only", "[C]",
     "Select AP, start game, move arm anteriorly and posteriorly",
     "Cursor moves up/down only, no unintended horizontal drift"),
    ("8.12", "Quit mid-trial", "[H]",
     "Press exit/quit button during active trial",
     "Trial ends, session row and partial raw data logged, returns to CHOOSEMOVE"),
    ("8.13", "Rapid double-tap ARMBO no duplicate data", "[H]",
     "Double-tap ARMBO fast at game start or end",
     "No duplicate trial row in sessions.csv, no double scene transition"),
    ("8.14", "Idle timeout (60 s)", "[H]",
     "Hold arm still for 60+ seconds in POSITION control mode",
     "Idle warning shown in error panel, session can be resumed or exited"),
    ("8.15", "Frame rate drops below 20 Hz", "[C]",
     "Simulate Bluetooth interference causing very slow data rate",
     "Control mode forced to NONE automatically, warning logged, arm safe"),
]),

("9. DATA LOGGING", [
    ("9.1", "Session CSV row written correctly", "[C]",
     "Complete a full game trial",
     "sessions.csv has new row: SessionNumber, Movement, Game, SuccessRate, Targets, Hits, Misses, Stars, timestamps"),
    ("9.2", "Raw data CSV at ~100 Hz", "[C]",
     "Complete a 3-minute trial, open raw data file",
     "~18,000 rows with all ~40 columns, no missing or duplicate packet numbers"),
    ("9.3", "Periodic flush every ~5 s (new fix)", "[C]",
     "Start trial, force-quit app after 20 s, check raw data folder",
     "Raw data file exists on disk with at least 15 s of data — not empty"),
    ("9.4", "Error log written", "[H]",
     "Trigger a robot error (e.g. angle jump)",
     "errorLog.csv row added: DateTime, Scene, Movement, Error string"),
    ("9.5", "Config data persists", "[H]",
     "Check configdata.csv after oneTimeConfig",
     "File has correct HomerID, dates, movement durations, limb, location"),
    ("9.6", "AROM file saved", "[H]",
     "Complete AROM assessment",
     "AROM raw file created in /rawdata with trajectory data and correct header"),
    ("9.7", "Arm weight file saved", "[H]",
     "Complete arm weight assessment (MLAP)",
     "armweight.csv has 5 position readings with force values"),
    ("9.8", "S3 upload non-blocking (new fix)", "[H]",
     "Complete trial with internet connected, observe app during upload",
     "App remains fully responsive during upload, no freeze or UI stutter"),
    ("9.9", "S3 upload fails gracefully", "[H]",
     "Disable internet, complete trial",
     "Error logged, local file retained, no crash, app continues normally"),
    ("9.10", "Trial count increments", "[H]",
     "Run 3 trials in same session",
     "TrialNumberDay = 1, 2, 3 in sessions.csv"),
    ("9.11", "Disconnect saves data to disk (new fix)", "[C]",
     "Power off device mid-game, check raw data folder",
     "Raw data file on disk contains all data up to disconnect; session CSV row written with actual hit/miss counts"),
]),

("10. SET TIME SCENE", [
    ("10.1", "Valid time reallocation", "[H]",
     "Set ML=10, AP=10, MLAP=10 (total=30), confirm",
     "New times saved, reflected in dashboard remaining time next session"),
    ("10.2", "Invalid total blocked", "[H]",
     "Set ML=20, AP=20, MLAP=20 (total=60), try to confirm",
     "System blocks, \"must total 30 minutes\" message shown"),
    ("10.3", "Disable movement without AROM", "[H]",
     "Try to enable a movement that has no AROM completed",
     "Button disabled until AROM is completed for that movement"),
    ("10.4", "Slider min/max boundaries enforced", "[M]",
     "Drag each slider to extreme positions",
     "Clamps at 10 min (min) and 30 min (max), no value outside this range accepted"),
]),

("11. DIAGNOSTICS SCENE", [
    ("11.1", "Real-time sensor values displayed", "[H]",
     "Enter DIAGNOSTICS with robot connected",
     "angle1-4, imuAngle1-4, force, controlType all update in real time"),
    ("11.2", "Frame rate display", "[H]",
     "Monitor frame rate display for 30 s",
     "Frame rate shown consistently; drops below 85 Hz are flagged visually"),
    ("11.3", "Calibration test", "[M]",
     "Press calibrate button in diagnostics",
     "Calibration command sent, IMU angles reset to near zero"),
]),

("12. END-TO-END SESSION WORKFLOW", [
    ("12.1", "Full ML session (SS game)", "[C]",
     "Login → MAIN → ROBOTCALIB → CHOOSEPLANE → MARSSETUP → Select ML → SS game → complete",
     "All scenes load in order, trial data in sessions.csv and rawdata, remaining time decremented correctly"),
    ("12.2", "Full MLAP session with arm weight + AROM", "[C]",
     "Full flow selecting MLAP, arm weight assessment, AROMMLAP, game trial",
     "Arm weight file saved, AROM file saved, trial data complete in all CSV files"),
    ("12.3", "Day completion", "[H]",
     "Use up all prescribed minutes for all movements",
     "Dashboard shows 0 remaining for all movements, day summary displayed"),
    ("12.4", "Multi-day continuity", "[H]",
     "Complete session Day 1, login on Day 2",
     "SessionNumber = 2, 7-day chart updated, previous data preserved"),
    ("12.5", "Multiple patients on same device", "[C]",
     "Login Patient A, complete trial, logout, login Patient B",
     "Patient B sees only their own data, Patient A data not visible or mixed"),
    ("12.6", "Scene navigation back mid-workflow", "[H]",
     "Press back at various scenes mid-workflow",
     "Returns to correct prior scene, robot state left safe"),
]),

("13. BLUETOOTH DISCONNECT AND SHUTDOWN FLOW", [
    ("13.1", "Disconnect mid-game → SUMMARY → upload → shutdown", "[C]",
     "Power off device while game trial is running",
     "1. Trial stopped, session CSV written\n2. Raw data flushed to disk\n3. App navigates to SUMMARY\n4. SUMMARY shows scores for 5 s\n5. DATAUPLOADING runs, uploads new data\n6. App shuts down cleanly"),
    ("13.2", "Disconnect in setup scene → SUMMARY", "[C]",
     "Power off device during MARSSETUP, ROBOTCALIB, or AROM scene",
     "Disconnect detected within 2 s, navigates directly to SUMMARY scene (no error panel, no hang)"),
    ("13.3", "Watchdog fires when thread dies silently", "[C]",
     "Simulate Bluetooth connection drop (IOException) without explicit disconnect event",
     "Watchdog detects no packet for 2 s, forces isMARS = false, triggers disconnect recovery flow"),
    ("13.4", "No new data → quit without upload", "[H]",
     "Complete session, proceed to DATAUPLOADING with no pending upload",
     "DATAUPLOADING reads uploadStatus.txt, detects no_upload, calls ShutdownSystem immediately without running uploader"),
    ("13.5", "New data → upload → quit", "[H]",
     "Complete session with internet, proceed to DATAUPLOADING",
     "DATAUPLOADING reads upload_needed, runs Python uploader with progress bar, on DONE calls ShutdownSystem"),
    ("13.6", "Upload fails → quit", "[H]",
     "Proceed to DATAUPLOADING with no internet but upload pending",
     "DATAUPLOADING shows ERROR, calls ShutdownSystem so app still exits cleanly, local data preserved"),
    ("13.7", "Arm drops safely on power loss", "[C]",
     "Power off device at any point during session",
     "Firmware drops arm to safe position automatically before any app response; verify arm reaches neutral/low position"),
]),

]  # end MODULES

# ═══════════════════════════════════════════════════════════════════════════
# BUILD TABLES
# ═══════════════════════════════════════════════════════════════════════════
HEADERS = ["ID", "Test Case", "Steps", "Expected Result", "P / F"]

for mod_title, rows in MODULES:
    # ── module heading ──
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(8)
    p.paragraph_format.space_after  = Pt(2)
    r = p.add_run(mod_title)
    r.bold = True
    r.font.size = Pt(11)
    r.font.color.rgb = C_MOD_FG

    # ── table ──
    tbl = doc.add_table(rows=1 + len(rows), cols=5)
    tbl.style = 'Table Grid'
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER

    # header row
    hrow = tbl.rows[0]
    for i, h in enumerate(HEADERS):
        c = hrow.cells[i]
        set_cell_bg(c, C_HDR_BG)
        set_cell_borders(c)
        cell_para(c, h, bold=True, fg=C_HDR_FG, size=9,
                  align=WD_ALIGN_PARAGRAPH.CENTER)
        c.vertical_alignment = WD_ALIGN_VERTICAL.CENTER

    # data rows
    for ri, (tid, tname, pri, steps, expected) in enumerate(rows):
        row   = tbl.rows[ri + 1]
        is_new = "(new fix)" in tname
        bg    = priority_bg(pri)

        vals = [tid, f"{tname}  {pri}", steps, expected, ""]
        for ci, val in enumerate(vals):
            c = row.cells[ci]
            set_cell_bg(c, bg)
            set_cell_borders(c)
            _bold = (ci == 0)
            _fg   = None
            _align = WD_ALIGN_PARAGRAPH.CENTER if ci in (0, 4) else WD_ALIGN_PARAGRAPH.LEFT
            c.vertical_alignment = WD_ALIGN_VERTICAL.TOP

            if ci == 1:
                cell_para(c, val, bold=False, fg=_fg, size=9,
                          align=_align, new_fix=is_new)
            else:
                cell_para(c, val, bold=_bold, fg=_fg, size=9, align=_align)

    set_col_widths(tbl, WIDTHS)
    doc.add_paragraph()   # spacer after each table

# ── PRIORITY SUMMARY TABLE ───────────────────────────────────────────────────
doc.add_paragraph()
ph = doc.add_paragraph()
r = ph.add_run("PRIORITY SUMMARY")
r.bold = True
r.font.size = Pt(11)
r.font.color.rgb = C_TITLE_BG
ph.paragraph_format.space_after = Pt(3)

sum_data = [
    ("[C] CRITICAL — Must pass before any patient use",
     "1.1, 1.2, 1.3, 1.4 | 2.1, 2.4 | 3.1, 3.2, 3.3, 3.4, 3.5, 3.7 | 4.1 | 5.1, 5.2, 5.3, 5.5, 5.7 | "
     "6.1, 6.2, 6.3, 6.4, 6.8 | 7.1, 7.2, 7.3, 7.4, 7.8, 7.10 | "
     "8.1, 8.2, 8.3, 8.7, 8.8, 8.10, 8.11, 8.15 | 9.1, 9.2, 9.3, 9.11 | "
     "12.1, 12.2, 12.5 | 13.1, 13.2, 13.3, 13.7", C_CRIT_BG),
    ("[H] HIGH — Must pass before deployment",
     "1.5, 1.6 | 2.2, 2.3, 2.5 | 3.6 | 4.2, 4.3 | 5.4, 5.6 | 6.5, 6.6, 6.7, 6.9 | "
     "7.5, 7.6, 7.7, 7.9 | 8.4, 8.5, 8.6, 8.9, 8.12, 8.13, 8.14 | "
     "9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10 | 10.1, 10.2, 10.3 | 11.1, 11.2 | "
     "12.3, 12.4, 12.6 | 13.4, 13.5, 13.6", C_HIGH_BG),
    ("[M] MEDIUM — Nice to have",
     "7.11 | 10.4 | 11.3", C_MED_BG),
]

stbl = doc.add_table(rows=3, cols=2)
stbl.style = 'Table Grid'
stbl.alignment = WD_TABLE_ALIGNMENT.CENTER
for ri, (label, ids, bg) in enumerate(sum_data):
    c0 = stbl.rows[ri].cells[0]
    c1 = stbl.rows[ri].cells[1]
    set_cell_bg(c0, bg)
    set_cell_bg(c1, bg)
    set_cell_borders(c0)
    set_cell_borders(c1)
    cell_para(c0, label, bold=True, size=9)
    cell_para(c1, ids, size=8.5)

# set widths
for row in stbl.rows:
    row.cells[0].width = Cm(6)
    row.cells[1].width = Cm(18)

doc.add_paragraph()
fp = doc.add_paragraph()
r = fp.add_run("TOTAL: 76 test cases  |  CRITICAL: 36  |  HIGH: 37  |  MEDIUM: 3")
r.bold = True
r.font.size = Pt(10)
r.font.color.rgb = C_TITLE_BG

doc.save(OUTPUT)
print(f"Saved: {OUTPUT}")
