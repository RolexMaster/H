from __future__ import annotations

import ctypes
from ctypes import wintypes
import json
import math
import os
import re
import subprocess
import sys
import threading
import time
from collections import deque
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

try:
    from tkinterdnd2 import COPY, DND_FILES, TkinterDnD
except ImportError:
    # The file picker remains available if the optional bundled DnD files are absent.
    COPY = DND_FILES = TkinterDnD = None


APP_DIR = (
    Path(sys.executable).resolve().parent
    if getattr(sys, "frozen", False)
    else Path(__file__).resolve().parent
)
CONFIG_PATH = APP_DIR / "monitor_config.json"
FFMPEG_PATH = APP_DIR / "ffmpeg.exe"
MONITOR_IMAGE_PATH = APP_DIR / "mon.png"
APP_ICON_PATH = APP_DIR / "app_icon.png"


@dataclass(frozen=True)
class MonitorConfig:
    number: int
    x: int
    y: int
    width: int
    height: int

    @property
    def rectangle(self) -> tuple[int, int, int, int]:
        return (self.x, self.y, self.x + self.width, self.y + self.height)


def enable_per_monitor_dpi_awareness() -> None:
    """Keep monitor coordinates in physical pixels on high-DPI Windows setups."""
    if sys.platform != "win32":
        return
    user32 = ctypes.WinDLL("user32", use_last_error=True)
    try:
        # DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        user32.SetProcessDpiAwarenessContext.argtypes = (ctypes.c_void_p,)
        user32.SetProcessDpiAwarenessContext.restype = wintypes.BOOL
        if user32.SetProcessDpiAwarenessContext(ctypes.c_void_p(-4)):
            print("[DPI] Per-monitor DPI awareness V2 enabled.", flush=True)
            return
        print(
            f"[DPI] Per-monitor V2 unavailable (Windows error {ctypes.get_last_error()}); "
            "trying system DPI awareness.",
            flush=True,
        )
    except (AttributeError, OSError):
        print("[DPI] Per-monitor V2 API unavailable; trying system DPI awareness.", flush=True)
    try:
        user32.SetProcessDPIAware.restype = wintypes.BOOL
        if user32.SetProcessDPIAware():
            print("[DPI] System DPI awareness enabled.", flush=True)
        else:
            print(
                f"[DPI] Could not change DPI awareness (Windows error "
                f"{ctypes.get_last_error()}). Coordinate scaling may differ.",
                flush=True,
            )
    except (AttributeError, OSError):
        print("[DPI] DPI awareness API unavailable.", flush=True)


def get_active_monitor_rectangles() -> set[tuple[int, int, int, int]]:
    """Return active Windows display rectangles in virtual-desktop coordinates."""
    if sys.platform != "win32":
        raise OSError("모니터 자동 감지는 Windows에서만 지원됩니다.")

    user32 = ctypes.WinDLL("user32", use_last_error=True)
    monitor_enum_proc = ctypes.WINFUNCTYPE(
        wintypes.BOOL,
        wintypes.HANDLE,
        wintypes.HDC,
        ctypes.POINTER(wintypes.RECT),
        wintypes.LPARAM,
    )
    rectangles: set[tuple[int, int, int, int]] = set()

    @monitor_enum_proc
    def collect_monitor(_monitor, _dc, rect_pointer, _data):
        rect = rect_pointer.contents
        rectangles.add((rect.left, rect.top, rect.right, rect.bottom))
        return True

    user32.EnumDisplayMonitors.argtypes = (
        wintypes.HDC,
        ctypes.POINTER(wintypes.RECT),
        monitor_enum_proc,
        wintypes.LPARAM,
    )
    user32.EnumDisplayMonitors.restype = wintypes.BOOL
    if not user32.EnumDisplayMonitors(None, None, collect_monitor, 0):
        error_code = ctypes.get_last_error()
        raise OSError(error_code, "활성 모니터 정보를 가져오지 못했습니다.")
    return rectangles


def assign_monitor_numbers(
    active_rectangles: set[tuple[int, int, int, int]],
) -> dict[int, MonitorConfig]:
    """Number up to three active displays left-to-right, then top-to-bottom."""
    monitors: dict[int, MonitorConfig] = {}
    for number, (left, top, right, bottom) in enumerate(
        sorted(active_rectangles, key=lambda rect: (rect[0], rect[1]))[:3],
        start=1,
    ):
        monitors[number] = MonitorConfig(
            number=number,
            x=left,
            y=top,
            width=right - left,
            height=bottom - top,
        )
    return monitors


def print_monitor_diagnostics(
    active_rectangles: set[tuple[int, int, int, int]],
    detection_error: OSError | None = None,
) -> None:
    """Show the Windows geometry that will be used directly for recording."""
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    print(f"\n[MONITOR] Scan at {timestamp}", flush=True)
    if detection_error is not None:
        print(
            f"[MONITOR] Windows detection FAILED: {ascii(str(detection_error))}",
            flush=True,
        )
    print(f"[MONITOR] Windows active displays: {len(active_rectangles)}", flush=True)
    for index, (left, top, right, bottom) in enumerate(
        sorted(active_rectangles, key=lambda rect: (rect[0], rect[1])), 1
    ):
        print(
            f"[MONITOR] Detected {index}: x={left}, y={top}, "
            f"width={right - left}, height={bottom - top}",
            flush=True,
        )
    for number, monitor in assign_monitor_numbers(active_rectangles).items():
        print(
            f"[MONITOR] Monitor {number}: x={monitor.x}, y={monitor.y}, "
            f"width={monitor.width}, height={monitor.height} "
            "-> checkbox ENABLED; these values are used for FFmpeg.",
            flush=True,
        )
    if detection_error is None and not active_rectangles:
        print("[MONITOR] No active displays were returned by Windows.", flush=True)
    elif len(active_rectangles) > 3:
        print(
            "[MONITOR] More than 3 displays detected; only the leftmost 3 are available.",
            flush=True,
        )


def load_config(path: Path) -> int:
    """Load the frame rate; monitor geometry is always detected at runtime."""
    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise ValueError(f"설정 파일이 없습니다: {path}") from exc
    except json.JSONDecodeError as exc:
        raise ValueError(f"설정 파일의 JSON 형식이 잘못되었습니다: {exc}") from exc
    except OSError as exc:
        raise ValueError(f"설정 파일을 읽을 수 없습니다: {exc}") from exc

    fps = raw.get("fps", 30)
    if not isinstance(fps, int) or not 1 <= fps <= 240:
        raise ValueError("fps는 1~240 범위의 정수여야 합니다.")

    return fps


class FFmpegRecorder:
    """Build and control one FFmpeg screen-recording process."""

    def __init__(self, ffmpeg_path: Path, fps: int) -> None:
        self.ffmpeg_path = ffmpeg_path
        self.fps = fps
        self.process: subprocess.Popen[str] | None = None
        self._stderr_thread: threading.Thread | None = None
        self.stderr_tail: deque[str] = deque(maxlen=30)
        self.encoder = ""
        self.output_path: Path | None = None
        self.output_seconds = 0.0
        self.launched_at: float | None = None
        self.first_file_at: float | None = None
        self.first_output_at: float | None = None

    @property
    def is_running(self) -> bool:
        return self.process is not None and self.process.poll() is None

    @staticmethod
    def _even(value: int) -> int:
        return value if value % 2 == 0 else value + 1

    def _nvenc_works(self, width: int, height: int) -> bool:
        """Probe NVENC at the requested output size, including driver support."""
        probe = [
            str(self.ffmpeg_path),
            "-hide_banner",
            "-loglevel",
            "error",
            "-f",
            "lavfi",
            "-i",
            f"color=c=black:s={width}x{height}:r=1",
            "-frames:v",
            "1",
            "-c:v",
            "h264_nvenc",
            "-f",
            "null",
            "-",
        ]
        try:
            result = subprocess.run(
                probe,
                stdin=subprocess.DEVNULL,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
                timeout=15,
                check=False,
            )
        except (OSError, subprocess.TimeoutExpired):
            return False
        return result.returncode == 0

    def choose_encoder(self, monitors: list[MonitorConfig]) -> str:
        canvas_height = self._even(max(monitor.height for monitor in monitors))
        output_width = sum(self._even(monitor.width) for monitor in monitors)
        return (
            "h264_nvenc"
            if self._nvenc_works(output_width, canvas_height)
            else "libx264"
        )

    def build_command(
        self,
        monitors: list[MonitorConfig],
        output_path: Path,
        encoder: str,
    ) -> list[str]:
        if not monitors:
            raise ValueError("녹화할 모니터가 선택되지 않았습니다.")

        command = [
            str(self.ffmpeg_path),
            "-hide_banner",
            "-loglevel",
            "info",
            "-nostats",
            "-stats_period",
            "0.25",
            "-progress",
            "pipe:2",
            "-y",
        ]

        for monitor in monitors:
            command.extend(
                [
                    "-thread_queue_size",
                    "8",
                    "-f",
                    "gdigrab",
                    "-framerate",
                    str(self.fps),
                    "-draw_mouse",
                    "1",
                    "-offset_x",
                    str(monitor.x),
                    "-offset_y",
                    str(monitor.y),
                    "-video_size",
                    f"{monitor.width}x{monitor.height}",
                    "-i",
                    "desktop",
                ]
            )

        canvas_height = self._even(max(monitor.height for monitor in monitors))
        filter_parts: list[str] = []
        input_labels: list[str] = []
        for index, monitor in enumerate(monitors):
            padded_width = self._even(monitor.width)
            label = f"v{index}"
            filter_parts.append(
                f"[{index}:v]setpts=PTS-STARTPTS,"
                f"pad={padded_width}:{canvas_height}:0:(oh-ih)/2:color=black,"
                f"setsar=1[{label}]"
            )
            input_labels.append(f"[{label}]")

        if len(monitors) == 1:
            filter_parts.append(f"{input_labels[0]}format=yuv420p[outv]")
        else:
            filter_parts.append(
                "".join(input_labels)
                + f"hstack=inputs={len(monitors)}:shortest=0,"
                + "format=yuv420p[outv]"
            )

        command.extend(
            [
                "-filter_complex",
                ";".join(filter_parts),
                "-map",
                "[outv]",
                "-an",
                # Preserve capture timestamps when the machine cannot encode
                # every requested frame. CFR duplicates frames and can leave
                # the recording shorter than the wall-clock capture interval.
                "-fps_mode",
                "vfr",
                "-c:v",
                encoder,
            ]
        )

        if encoder == "h264_nvenc":
            command.extend(["-preset", "fast", "-rc", "vbr", "-cq", "23", "-b:v", "0"])
        else:
            command.extend(["-preset", "veryfast", "-crf", "23"])

        command.extend(
            [
                "-pix_fmt",
                "yuv420p",
                "-f",
                "matroska",
                str(output_path),
            ]
        )
        return command

    def start(self, monitors: list[MonitorConfig], output_path: Path) -> str:
        if self.is_running:
            raise RuntimeError("이미 녹화 중입니다.")

        self.stderr_tail.clear()
        self.output_seconds = 0.0
        self.first_file_at = None
        self.first_output_at = None
        self.encoder = self.choose_encoder(monitors)
        command = self.build_command(monitors, output_path, self.encoder)

        self.launched_at = time.monotonic()
        self.process = subprocess.Popen(
            command,
            stdin=subprocess.PIPE,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8",
            errors="replace",
            bufsize=1,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
        self.output_path = output_path

        self._stderr_thread = threading.Thread(target=self._drain_stderr, daemon=True)
        self._stderr_thread.start()
        return self.encoder

    def _drain_stderr(self) -> None:
        process = self.process
        if process is None or process.stderr is None:
            return
        for line in process.stderr:
            clean_line = line.rstrip()
            if clean_line.startswith("out_time_us="):
                try:
                    microseconds = int(clean_line.partition("=")[2])
                except ValueError:
                    continue
                if microseconds >= 0:
                    if self.first_output_at is None:
                        self.first_output_at = time.monotonic()
                    self.output_seconds = max(self.output_seconds, microseconds / 1_000_000)
            elif clean_line and not re.match(r"^[a-z][a-z0-9_]*=", clean_line):
                self.stderr_tail.append(clean_line)

    def request_stop(self) -> None:
        """Ask FFmpeg to finalize the MKV normally; never force-kill it."""
        if not self.is_running or self.process is None or self.process.stdin is None:
            return
        try:
            self.process.stdin.write("q\n")
            self.process.stdin.flush()
        except (BrokenPipeError, OSError):
            # FFmpeg may already have exited between is_running and write().
            pass

    def take_finished_process(self) -> tuple[int, Path | None] | None:
        if self.process is None:
            return None
        return_code = self.process.poll()
        if return_code is None:
            return None

        if self._stderr_thread is not None:
            self._stderr_thread.join()
            self._stderr_thread = None
        output_path = self.output_path
        self.process = None
        self.output_path = None
        return return_code, output_path


class FFmpegTrimmer:
    """Run an exact, non-destructive video segment export with FFmpeg."""

    def __init__(self, ffmpeg_path: Path) -> None:
        self.ffmpeg_path = ffmpeg_path
        self.process: subprocess.Popen[str] | None = None
        self.output_path: Path | None = None
        self.work_path: Path | None = None
        self.stderr_tail: deque[str] = deque(maxlen=30)

    def probe_duration(self, input_path: Path) -> float:
        """Read the media duration from FFmpeg without decoding the full file."""
        try:
            result = subprocess.run(
                [
                    str(self.ffmpeg_path),
                    "-hide_banner",
                    "-i",
                    str(input_path),
                ],
                stdin=subprocess.DEVNULL,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.PIPE,
                text=True,
                encoding="utf-8",
                errors="replace",
                creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
                timeout=15,
                check=False,
            )
        except subprocess.TimeoutExpired as exc:
            raise RuntimeError("영상 길이 확인 시간이 초과되었습니다.") from exc
        except OSError as exc:
            raise RuntimeError(f"영상 정보를 읽지 못했습니다: {exc}") from exc

        match = re.search(
            r"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)",
            result.stderr,
        )
        if match is None:
            raise RuntimeError("FFmpeg에서 영상 전체 길이를 확인할 수 없습니다.")
        hours, minutes, seconds = match.groups()
        duration = int(hours) * 3600 + int(minutes) * 60 + float(seconds)
        if duration <= 0:
            raise RuntimeError("영상 길이가 0초이거나 올바르지 않습니다.")
        return duration

    @property
    def is_running(self) -> bool:
        return self.process is not None and self.process.poll() is None

    def start(
        self,
        input_path: Path,
        output_path: Path,
        start_seconds: float,
        duration_seconds: float,
    ) -> None:
        if self.is_running:
            raise RuntimeError("이미 영상 자르기 작업이 진행 중입니다.")

        unique_part = datetime.now().strftime("%Y%m%d%H%M%S%f")
        work_path = output_path.with_name(
            f".{output_path.stem}.trimming-{unique_part}.mkv"
        )
        command = [
            str(self.ffmpeg_path),
            "-hide_banner",
            "-loglevel",
            "info",
            "-y",
            "-ss",
            f"{start_seconds:.3f}",
            "-i",
            str(input_path),
            "-t",
            f"{duration_seconds:.3f}",
            "-map",
            "0:v:0",
            "-map",
            "0:a?",
            "-sn",
            "-dn",
            "-c:v",
            "libx264",
            "-preset",
            "veryfast",
            "-crf",
            "23",
            "-pix_fmt",
            "yuv420p",
            "-c:a",
            "copy",
            "-f",
            "matroska",
            str(work_path),
        ]

        self.stderr_tail.clear()
        self.process = subprocess.Popen(
            command,
            stdin=subprocess.PIPE,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8",
            errors="replace",
            bufsize=1,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
        self.output_path = output_path
        self.work_path = work_path
        threading.Thread(target=self._drain_stderr, daemon=True).start()

    def _drain_stderr(self) -> None:
        process = self.process
        if process is None or process.stderr is None:
            return
        for line in process.stderr:
            clean_line = line.rstrip()
            if clean_line:
                self.stderr_tail.append(clean_line)

    def request_stop(self) -> None:
        if not self.is_running or self.process is None or self.process.stdin is None:
            return
        try:
            self.process.stdin.write("q\n")
            self.process.stdin.flush()
        except (BrokenPipeError, OSError):
            pass

    def take_finished_process(
        self,
    ) -> tuple[int, Path | None, Path | None] | None:
        if self.process is None:
            return None
        return_code = self.process.poll()
        if return_code is None:
            return None

        output_path = self.output_path
        work_path = self.work_path
        self.process = None
        self.output_path = None
        self.work_path = None
        return return_code, output_path, work_path


class ScreenRecorderApp:
    POLL_INTERVAL_MS = 250
    MONITOR_CHECK_INTERVAL_MS = 2000
    ANIMATION_INTERVAL_MS = 100
    MONITOR_IMAGE_SUBSAMPLE = 3
    # Click/overlay polygons in the original 1672x941 mon.png coordinates.
    MONITOR_IMAGE_REGIONS = {
        1: ((40, 120), (500, 75), (500, 475), (40, 560)),
        2: ((552, 75), (1118, 75), (1118, 470), (552, 470)),
        3: ((1152, 75), (1632, 120), (1632, 560), (1152, 475)),
    }
    ACTIVE_PULSE_COLORS = (
        "#26733f",
        "#2f9150",
        "#38b562",
        "#43d979",
        "#54f28d",
        "#43d979",
        "#38b562",
        "#2f9150",
    )

    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("3화면 녹화기")
        self.root.resizable(False, False)
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)
        self.app_icon: tk.PhotoImage | None = None
        try:
            self.app_icon = tk.PhotoImage(file=str(APP_ICON_PATH))
            self.root.iconphoto(True, self.app_icon)
        except tk.TclError:
            # The recorder remains usable if the optional icon asset is missing.
            pass

        self.fps = load_config(CONFIG_PATH)
        self.monitors: dict[int, MonitorConfig] = {}
        self.recorder = FFmpegRecorder(FFMPEG_PATH, self.fps)
        self.trimmer = FFmpegTrimmer(FFMPEG_PATH)
        self.stop_requested = False
        self.trim_stop_requested = False
        self.close_after_stop = False
        self.recording_started_at: float | None = None
        self.recording_wall_seconds: float | None = None
        self.animation_step = 0
        self.monitor_canvas: tk.Canvas | None = None
        self.monitor_image: tk.PhotoImage | None = None
        self.monitor_overlay_items: dict[int, int] = {}
        self.monitor_overlay_text_items: dict[int, int] = {}
        self.monitor_canvas_regions: dict[int, tuple[tuple[int, int], ...]] = {}

        default_output = Path.home() / "Videos"
        if not default_output.is_dir():
            default_output = APP_DIR

        self.monitor_vars = {number: tk.BooleanVar(value=False) for number in (1, 2, 3)}
        self.monitor_label_vars = {
            number: tk.StringVar(value=f"Monitor {number}\n감지 중...")
            for number in (1, 2, 3)
        }
        self.monitor_available = {number: False for number in (1, 2, 3)}
        self._last_monitor_diagnostic: tuple[object, ...] | None = None
        self._last_monitor_layout: tuple[tuple[int, int, int, int], ...] | None = None
        self.output_dir_var = tk.StringVar(value=str(default_output))
        self.status_var = tk.StringVar(value="대기 중")
        self.elapsed_time_var = tk.StringVar(value="00:00:00")
        self.trim_input_var = tk.StringVar()
        self.trim_output_var = tk.StringVar()
        self.trim_start_var = tk.StringVar(value="00:00:00")
        self.trim_end_var = tk.StringVar(value="00:00:00")
        self.trim_status_var = tk.StringVar(value="대기 중")
        self.trim_duration_var = tk.StringVar(value="영상 파일을 선택하세요.")
        self.trim_selected_duration_var = tk.StringVar(value="남길 영상 길이: --")
        self.trim_start_scale_var = tk.DoubleVar(value=0.0)
        self.trim_end_scale_var = tk.DoubleVar(value=0.0)
        self.trim_duration_seconds: float | None = None
        self.trim_probed_input: Path | None = None
        self._build_ui()
        self.trim_start_var.trace_add("write", self._update_trim_range_preview)
        self.trim_end_var.trace_add("write", self._update_trim_range_preview)
        self._update_trim_range_preview()
        try:
            if TkinterDnD is None:
                raise RuntimeError("동봉된 tkinterdnd2 폴더를 찾을 수 없습니다.")
            TkinterDnD.require(self.root)
            self.trim_drop_zone.drop_target_register(DND_FILES)
            self.trim_drop_zone.dnd_bind("<<Drop>>", self._on_trim_drop)
        except (RuntimeError, tk.TclError, OSError) as exc:
            self.trim_drop_zone.configure(
                text="파일 드래그앤드롭 사용 불가 · 파일 선택... 버튼을 이용하세요."
            )
            print(f"[TRIM] File drop unavailable: {exc}", flush=True)
        self._refresh_monitor_availability()
        self.root.after(self.POLL_INTERVAL_MS, self._poll_ffmpeg)
        self.root.after(
            self.MONITOR_CHECK_INTERVAL_MS,
            self._periodic_monitor_check,
        )
        self.root.after(self.ANIMATION_INTERVAL_MS, self._animate_monitor_map)

    def _build_ui(self) -> None:
        self.notebook = ttk.Notebook(self.root)
        self.notebook.grid(row=0, column=0, sticky="nsew")
        recording_tab = ttk.Frame(self.notebook)
        trimming_tab = ttk.Frame(self.notebook)
        self.notebook.add(recording_tab, text="화면 녹화")
        self.notebook.add(trimming_tab, text="영상 구간 자르기")

        main = ttk.Frame(recording_tab, padding=16)
        main.grid(row=0, column=0, sticky="nsew")

        ttk.Label(
            main,
            text=(
                f"기본 설정: 캡처 목표 {self.fps} FPS · MKV · H.264 · yuv420p\n"
                "처리 속도에 따라 실제 프레임률은 낮아질 수 있으며, 파일은 캡처 시각을 "
                "반영해 저장합니다. 여러 화면을 합쳐 "
                "가로 4096픽셀을 초과하는 영상은 Windows Media Player에서 재생 확인이 "
                "필요합니다."
            ),
            justify="left",
            wraplength=700,
        ).grid(
            row=0, column=0, columnspan=3, sticky="w", pady=(0, 8)
        )

        self._build_monitor_map(main)

        self.monitor_checks: dict[int, ttk.Checkbutton] = {}
        for column, number in enumerate((1, 2, 3)):
            check = ttk.Checkbutton(
                main,
                textvariable=self.monitor_label_vars[number],
                variable=self.monitor_vars[number],
                command=self._update_monitor_visuals,
                state="disabled",
            )
            check.grid(row=2, column=column, padx=(0, 18), pady=(10, 0), sticky="w")
            self.monitor_checks[number] = check

        ttk.Label(main, text="저장 경로").grid(
            row=3, column=0, columnspan=3, sticky="w", pady=(18, 5)
        )
        self.path_entry = ttk.Entry(main, textvariable=self.output_dir_var, width=55)
        self.path_entry.grid(row=4, column=0, columnspan=2, sticky="ew", padx=(0, 8))
        path_button_frame = ttk.Frame(main)
        path_button_frame.grid(row=4, column=2, sticky="ew")
        self.browse_button = ttk.Button(
            path_button_frame,
            text="폴더 선택...",
            command=self._browse_output_dir,
        )
        self.browse_button.pack(side="left", padx=(0, 4))
        self.open_folder_button = ttk.Button(
            path_button_frame,
            text="폴더 열기",
            command=self._open_output_dir,
        )
        self.open_folder_button.pack(side="left")

        button_frame = ttk.Frame(main)
        button_frame.grid(row=5, column=0, columnspan=3, sticky="ew", pady=(18, 0))
        self.start_button = ttk.Button(button_frame, text="녹화 시작", command=self._start_recording)
        self.start_button.pack(side="left", expand=True, fill="x", padx=(0, 5))
        self.stop_button = ttk.Button(
            button_frame,
            text="녹화 중지",
            command=self._stop_recording,
            state="disabled",
        )
        self.stop_button.pack(side="left", expand=True, fill="x", padx=(5, 0))

        ttk.Separator(main).grid(row=6, column=0, columnspan=3, sticky="ew", pady=14)
        ttk.Label(main, text="현재 상태:").grid(row=7, column=0, sticky="w")
        ttk.Label(main, textvariable=self.status_var).grid(
            row=7, column=1, columnspan=2, sticky="w"
        )
        ttk.Label(main, text="녹화 경과 시간:").grid(row=8, column=0, sticky="w", pady=(6, 0))
        ttk.Label(
            main,
            textvariable=self.elapsed_time_var,
            font=("Segoe UI", 10, "bold"),
        ).grid(row=8, column=1, columnspan=2, sticky="w", pady=(6, 0))

        self._build_trim_ui(trimming_tab)

    def _build_trim_ui(self, parent: ttk.Frame) -> None:
        main = ttk.Frame(parent, padding=20)
        main.grid(row=0, column=0, sticky="nsew")
        main.columnconfigure(0, weight=1)
        main.columnconfigure(1, weight=1)

        ttk.Label(
            main,
            text=(
                "원본 영상에서 지정한 시작~종료 구간만 새 MKV 파일로 저장합니다.\n"
                "원본 파일은 변경하거나 삭제하지 않습니다."
            ),
            justify="left",
        ).grid(row=0, column=0, columnspan=3, sticky="w", pady=(0, 18))

        ttk.Label(main, text="원본 영상 파일").grid(
            row=1, column=0, columnspan=3, sticky="w", pady=(0, 5)
        )
        self.trim_input_entry = ttk.Entry(
            main,
            textvariable=self.trim_input_var,
            width=70,
            state="readonly",
        )
        self.trim_input_entry.grid(row=2, column=0, columnspan=2, sticky="ew", padx=(0, 8))
        self.trim_input_button = ttk.Button(
            main,
            text="파일 선택...",
            command=self._choose_trim_input,
        )
        self.trim_input_button.grid(row=2, column=2, sticky="ew")

        self.trim_drop_zone = tk.Label(
            main,
            text="영상 파일을 여기에 끌어다 놓으세요 · 또는 파일 선택...",
            relief="groove",
            borderwidth=2,
            bg="#f4f7fb",
            fg="#28547a",
            pady=12,
        )
        self.trim_drop_zone.grid(
            row=3, column=0, columnspan=3, sticky="ew", pady=(10, 0)
        )

        time_frame = ttk.LabelFrame(main, text="남길 구간", padding=12)
        time_frame.grid(row=4, column=0, columnspan=3, sticky="ew", pady=(18, 18))
        time_frame.columnconfigure(1, weight=1)
        ttk.Label(
            time_frame,
            textvariable=self.trim_duration_var,
            font=("Segoe UI", 9, "bold"),
        ).grid(row=0, column=0, columnspan=3, sticky="w", pady=(0, 10))
        ttk.Label(time_frame, text="시작 시간").grid(row=1, column=0, sticky="w")
        self.trim_start_entry = ttk.Entry(
            time_frame,
            textvariable=self.trim_start_var,
            width=14,
            justify="center",
        )
        self.trim_start_entry.grid(row=2, column=0, padx=(0, 18), pady=(4, 0))
        ttk.Label(time_frame, text="→").grid(row=2, column=1)
        ttk.Label(time_frame, text="종료 시간").grid(row=1, column=2, sticky="w")
        self.trim_end_entry = ttk.Entry(
            time_frame,
            textvariable=self.trim_end_var,
            width=14,
            justify="center",
        )
        self.trim_end_entry.grid(row=2, column=2, pady=(4, 0))

        ttk.Label(time_frame, text="시작 위치").grid(
            row=3, column=0, sticky="w", pady=(14, 0)
        )
        self.trim_start_scale = ttk.Scale(
            time_frame,
            from_=0,
            to=1,
            variable=self.trim_start_scale_var,
            command=self._on_trim_start_scale,
            state="disabled",
        )
        self.trim_start_scale.grid(
            row=3, column=1, columnspan=2, sticky="ew", pady=(14, 0)
        )
        ttk.Label(time_frame, text="종료 위치").grid(
            row=4, column=0, sticky="w", pady=(8, 0)
        )
        self.trim_end_scale = ttk.Scale(
            time_frame,
            from_=0,
            to=1,
            variable=self.trim_end_scale_var,
            command=self._on_trim_end_scale,
            state="disabled",
        )
        self.trim_end_scale.grid(
            row=4, column=1, columnspan=2, sticky="ew", pady=(8, 0)
        )
        ttk.Label(
            time_frame,
            text="슬라이더: 1초 단위 · 직접 입력: 초, 분:초 또는 시:분:초",
        ).grid(row=5, column=0, columnspan=3, sticky="w", pady=(10, 0))
        ttk.Label(
            time_frame,
            textvariable=self.trim_selected_duration_var,
            font=("Segoe UI", 10, "bold"),
            foreground="#17613b",
        ).grid(row=6, column=0, columnspan=3, sticky="w", pady=(12, 0))
        self.trim_start_entry.bind("<Return>", self._commit_trim_time_entries)
        self.trim_end_entry.bind("<Return>", self._commit_trim_time_entries)
        for scale in (self.trim_start_scale, self.trim_end_scale):
            scale.bind("<Left>", self._step_trim_scale)
            scale.bind("<Right>", self._step_trim_scale)

        ttk.Label(main, text="결과 파일").grid(
            row=5, column=0, columnspan=3, sticky="w", pady=(0, 5)
        )
        self.trim_output_entry = ttk.Entry(
            main,
            textvariable=self.trim_output_var,
            width=70,
        )
        self.trim_output_entry.grid(row=6, column=0, columnspan=2, sticky="ew", padx=(0, 8))
        self.trim_output_button = ttk.Button(
            main,
            text="저장 위치...",
            command=self._choose_trim_output,
        )
        self.trim_output_button.grid(row=6, column=2, sticky="ew")

        trim_button_frame = ttk.Frame(main)
        trim_button_frame.grid(row=7, column=0, columnspan=3, sticky="ew", pady=(20, 0))
        self.trim_start_button = ttk.Button(
            trim_button_frame,
            text="선택 구간 저장",
            command=self._start_trim,
            state="disabled",
        )
        self.trim_start_button.pack(side="left", expand=True, fill="x", padx=(0, 5))
        self.trim_stop_button = ttk.Button(
            trim_button_frame,
            text="작업 중지",
            command=self._stop_trim,
            state="disabled",
        )
        self.trim_stop_button.pack(side="left", expand=True, fill="x", padx=(5, 0))

        self.trim_progress = ttk.Progressbar(
            main,
            mode="determinate",
            maximum=100,
            value=0,
        )
        self.trim_progress.grid(row=8, column=0, columnspan=3, sticky="ew", pady=(18, 10))
        ttk.Label(main, text="현재 상태:").grid(row=9, column=0, sticky="w")
        ttk.Label(main, textvariable=self.trim_status_var).grid(
            row=9, column=1, columnspan=2, sticky="w"
        )

    def _build_monitor_map(self, parent: ttk.Frame) -> None:
        try:
            source_image = tk.PhotoImage(file=str(MONITOR_IMAGE_PATH))
            self.monitor_image = source_image.subsample(
                self.MONITOR_IMAGE_SUBSAMPLE,
                self.MONITOR_IMAGE_SUBSAMPLE,
            )
        except tk.TclError:
            ttk.Label(
                parent,
                text="mon.png를 불러오지 못했습니다. 체크박스로 모니터를 선택하세요.",
            ).grid(row=1, column=0, columnspan=3, pady=(0, 4))
            return

        self.monitor_canvas = tk.Canvas(
            parent,
            width=self.monitor_image.width(),
            height=self.monitor_image.height(),
            highlightthickness=1,
            highlightbackground="#666666",
            cursor="arrow",
        )
        self.monitor_canvas.grid(row=1, column=0, columnspan=3)
        self.monitor_canvas.create_image(0, 0, anchor="nw", image=self.monitor_image)

        divisor = self.MONITOR_IMAGE_SUBSAMPLE
        for number, original_points in self.MONITOR_IMAGE_REGIONS.items():
            points = tuple((x // divisor, y // divisor) for x, y in original_points)
            self.monitor_canvas_regions[number] = points
            flat_points = [coordinate for point in points for coordinate in point]
            overlay_id = self.monitor_canvas.create_polygon(
                *flat_points,
                fill="",
                outline="#777777",
                width=2,
            )
            center_x = sum(point[0] for point in points) // len(points)
            label_y = max(point[1] for point in points) - 15
            text_id = self.monitor_canvas.create_text(
                center_x,
                label_y,
                text="감지 중...",
                fill="#ffffff",
                font=("Segoe UI", 9, "bold"),
            )
            self.monitor_overlay_items[number] = overlay_id
            self.monitor_overlay_text_items[number] = text_id

        self.monitor_canvas.bind("<Button-1>", self._on_monitor_map_click)
        self.monitor_canvas.bind("<Motion>", self._on_monitor_map_motion)
        self.monitor_canvas.bind("<Leave>", lambda _event: self.monitor_canvas.configure(cursor="arrow"))

    @staticmethod
    def _point_in_polygon(
        x: int,
        y: int,
        polygon: tuple[tuple[int, int], ...],
    ) -> bool:
        inside = False
        previous_x, previous_y = polygon[-1]
        for current_x, current_y in polygon:
            crosses = (current_y > y) != (previous_y > y)
            if crosses:
                boundary_x = (
                    (previous_x - current_x)
                    * (y - current_y)
                    / (previous_y - current_y)
                    + current_x
                )
                if x < boundary_x:
                    inside = not inside
            previous_x, previous_y = current_x, current_y
        return inside

    def _monitor_number_at(self, x: int, y: int) -> int | None:
        for number, polygon in self.monitor_canvas_regions.items():
            if self._point_in_polygon(x, y, polygon):
                return number
        return None

    def _on_monitor_map_click(self, event: tk.Event) -> None:
        number = self._monitor_number_at(event.x, event.y)
        if number is None:
            return
        if self.recorder.is_running or not self.monitor_available.get(number, False):
            self.root.bell()
            return
        self.monitor_vars[number].set(not self.monitor_vars[number].get())
        self._update_monitor_visuals()

    def _on_monitor_map_motion(self, event: tk.Event) -> None:
        if self.monitor_canvas is None:
            return
        number = self._monitor_number_at(event.x, event.y)
        clickable = (
            number is not None
            and not self.recorder.is_running
            and self.monitor_available.get(number, False)
        )
        self.monitor_canvas.configure(cursor="hand2" if clickable else "arrow")

    def _update_monitor_visuals(self) -> None:
        if self.monitor_canvas is None:
            return

        pulse_color = self.ACTIVE_PULSE_COLORS[
            self.animation_step % len(self.ACTIVE_PULSE_COLORS)
        ]
        rec_visible = (self.animation_step // 5) % 2 == 0

        for number in (1, 2, 3):
            overlay_id = self.monitor_overlay_items[number]
            text_id = self.monitor_overlay_text_items[number]
            available = self.monitor_available.get(number, False)
            selected = self.monitor_vars[number].get()

            if not available:
                self.monitor_canvas.itemconfigure(
                    overlay_id,
                    fill="#202020",
                    stipple="gray50",
                    outline="#777777",
                    width=2,
                )
                self.monitor_canvas.itemconfigure(
                    text_id,
                    text="연결 안 됨",
                    fill="#d0d0d0",
                )
            elif self.recorder.is_running and selected:
                self.monitor_canvas.itemconfigure(
                    overlay_id,
                    fill="#6e1010" if rec_visible else "",
                    stipple="gray25" if rec_visible else "",
                    outline="#ff3030" if rec_visible else "#8a1f1f",
                    width=4,
                )
                self.monitor_canvas.itemconfigure(
                    text_id,
                    text="● REC" if rec_visible else "REC",
                    fill="#ff4b4b" if rec_visible else "#a83a3a",
                )
            elif selected:
                self.monitor_canvas.itemconfigure(
                    overlay_id,
                    fill="#075a8a",
                    stipple="gray25",
                    outline="#3db8ff",
                    width=4,
                )
                self.monitor_canvas.itemconfigure(
                    text_id,
                    text="선택됨",
                    fill="#66c9ff",
                )
            else:
                self.monitor_canvas.itemconfigure(
                    overlay_id,
                    fill="",
                    stipple="",
                    outline=pulse_color,
                    width=3,
                )
                self.monitor_canvas.itemconfigure(
                    text_id,
                    text="활성",
                    fill=pulse_color,
                )

    def _animate_monitor_map(self) -> None:
        self.animation_step += 1
        self._update_monitor_visuals()
        self.root.after(self.ANIMATION_INTERVAL_MS, self._animate_monitor_map)

    def _browse_output_dir(self) -> None:
        selected = filedialog.askdirectory(
            title="녹화 파일을 저장할 폴더 선택",
            initialdir=self.output_dir_var.get() or str(APP_DIR),
        )
        if selected:
            self.output_dir_var.set(selected)

    def _open_output_dir(self) -> None:
        output_dir_text = self.output_dir_var.get().strip()
        if not output_dir_text:
            messagebox.showwarning("경로 필요", "먼저 저장 폴더를 선택하세요.")
            return

        output_dir = Path(os.path.expandvars(output_dir_text)).expanduser()
        if not output_dir.is_dir():
            messagebox.showerror(
                "폴더 열기 실패",
                f"저장 폴더가 존재하지 않습니다.\n\n{output_dir}",
            )
            return
        try:
            os.startfile(str(output_dir))
        except OSError as exc:
            messagebox.showerror("폴더 열기 실패", str(exc))

    @staticmethod
    def _next_trim_output_path(input_path: Path) -> Path:
        candidate = input_path.with_name(f"{input_path.stem}_trimmed.mkv")
        if not candidate.exists():
            return candidate
        for suffix in range(1, 1000):
            candidate = input_path.with_name(
                f"{input_path.stem}_trimmed_{suffix}.mkv"
            )
            if not candidate.exists():
                return candidate
        raise RuntimeError("사용 가능한 결과 파일명을 만들 수 없습니다.")

    @staticmethod
    def _format_media_time(seconds: float) -> str:
        total_milliseconds = max(0, int(round(seconds * 1000)))
        total_seconds, milliseconds = divmod(total_milliseconds, 1000)
        hours, remainder = divmod(total_seconds, 3600)
        minutes, whole_seconds = divmod(remainder, 60)
        base = f"{hours:02d}:{minutes:02d}:{whole_seconds:02d}"
        return f"{base}.{milliseconds:03d}" if milliseconds else base

    def _clear_trim_duration(self) -> None:
        self.trim_duration_seconds = None
        self.trim_probed_input = None
        self.trim_duration_var.set("영상 길이를 확인할 수 없습니다.")
        self.trim_start_scale.configure(state="disabled", from_=0, to=1)
        self.trim_end_scale.configure(state="disabled", from_=0, to=1)
        self._update_trim_range_preview()

    def _load_trim_duration(self, input_path: Path) -> bool:
        if not FFMPEG_PATH.is_file():
            messagebox.showerror(
                "FFmpeg 없음",
                f"프로그램 폴더에 ffmpeg.exe가 없습니다.\n\n{FFMPEG_PATH}",
            )
            self._clear_trim_duration()
            return False

        self.trim_status_var.set("영상 전체 길이 확인 중...")
        self.root.update_idletasks()
        try:
            duration = self.trimmer.probe_duration(input_path)
        except RuntimeError as exc:
            self._clear_trim_duration()
            self.trim_status_var.set("영상 길이 확인 실패")
            messagebox.showerror("영상 정보 오류", str(exc))
            return False

        self.trim_duration_seconds = duration
        self.trim_probed_input = input_path.resolve()
        self.trim_duration_var.set(
            f"전체 영상 길이: {self._format_media_time(duration)}  "
            "(이 범위 안에서만 선택 가능)"
        )
        self.trim_start_scale.configure(from_=0, to=duration, state="normal")
        self.trim_end_scale.configure(from_=0, to=duration, state="normal")

        start_seconds = 0.0
        end_seconds = duration
        self.trim_start_scale_var.set(start_seconds)
        self.trim_end_scale_var.set(end_seconds)
        self.trim_start_var.set(self._format_media_time(start_seconds))
        self.trim_end_var.set(self._format_media_time(end_seconds))
        self.trim_status_var.set("영상 분석 완료 - 남길 구간을 선택하세요.")
        self._update_trim_range_preview()
        return True

    def _on_trim_start_scale(self, value: str) -> None:
        duration = self.trim_duration_seconds
        if duration is None:
            return
        end_seconds = self.trim_end_scale_var.get()
        max_start = max(0, math.ceil(end_seconds) - 1)
        start_seconds = min(max_start, max(0, math.floor(float(value) + 0.5)))
        self.trim_start_scale_var.set(start_seconds)
        self.trim_start_var.set(self._format_media_time(start_seconds))

    def _on_trim_end_scale(self, value: str) -> None:
        duration = self.trim_duration_seconds
        if duration is None:
            return
        start_seconds = self.trim_start_scale_var.get()
        min_end = min(duration, math.floor(start_seconds) + 1)
        raw_seconds = float(value)
        if raw_seconds >= duration - 0.001:
            end_seconds = duration
        else:
            end_seconds = max(min_end, math.floor(raw_seconds + 0.5))
            end_seconds = min(duration, end_seconds)
        self.trim_end_scale_var.set(end_seconds)
        self.trim_end_var.set(self._format_media_time(end_seconds))

    def _step_trim_scale(self, event) -> str:
        duration = self.trim_duration_seconds
        if duration is None:
            return "break"
        delta = -1 if event.keysym == "Left" else 1
        if event.widget is self.trim_start_scale:
            self._on_trim_start_scale(str(self.trim_start_scale_var.get() + delta))
        else:
            self._on_trim_end_scale(str(self.trim_end_scale_var.get() + delta))
        return "break"

    @staticmethod
    def _format_human_duration(seconds: float) -> str:
        hours, remainder = divmod(int(seconds), 3600)
        minutes, whole_seconds = divmod(remainder, 60)
        pieces = []
        if hours:
            pieces.append(f"{hours}시간")
        pieces.append(f"{minutes}분")
        fractional = seconds - int(seconds)
        second_text = f"{whole_seconds + fractional:.3f}".rstrip("0").rstrip(".")
        pieces.append(f"{second_text}초")
        return " ".join(pieces)

    def _update_trim_range_preview(self, *_args) -> None:
        try:
            start_seconds, end_seconds = self._validated_trim_range()
        except ValueError:
            self.trim_selected_duration_var.set("남길 영상 길이: -- (유효한 구간을 입력하세요)")
            self.trim_start_button.configure(state="disabled")
            return
        selected_seconds = end_seconds - start_seconds
        self.trim_selected_duration_var.set(
            f"남길 영상 길이: {self._format_human_duration(selected_seconds)} "
            f"({self._format_media_time(selected_seconds)})"
        )
        can_start = not self.recorder.is_running and not self.trimmer.is_running
        self.trim_start_button.configure(state="normal" if can_start else "disabled")

    def _validated_trim_range(self) -> tuple[float, float]:
        duration = self.trim_duration_seconds
        if duration is None:
            raise ValueError("먼저 원본 영상 파일을 선택해 전체 길이를 확인하세요.")
        start_seconds = self._parse_timecode(self.trim_start_var.get())
        end_seconds = self._parse_timecode(self.trim_end_var.get())
        tolerance = 0.01
        if start_seconds >= duration:
            raise ValueError("시작 시간은 영상 전체 길이보다 앞이어야 합니다.")
        if end_seconds > duration + tolerance:
            raise ValueError(
                f"종료 시간은 영상 전체 길이 "
                f"{self._format_media_time(duration)}를 넘을 수 없습니다."
            )
        if end_seconds <= start_seconds:
            raise ValueError("종료 시간은 시작 시간보다 뒤여야 합니다.")
        return start_seconds, min(end_seconds, duration)

    def _commit_trim_time_entries(self, _event=None) -> None:
        try:
            start_seconds, end_seconds = self._validated_trim_range()
        except ValueError as exc:
            messagebox.showerror("시간 입력 오류", str(exc))
            return
        self.trim_start_scale_var.set(start_seconds)
        self.trim_end_scale_var.set(end_seconds)
        self.trim_start_var.set(self._format_media_time(start_seconds))
        self.trim_end_var.set(self._format_media_time(end_seconds))

    def _load_trim_input(self, input_path: Path) -> None:
        if self.trimmer.is_running:
            return
        if not input_path.is_file():
            messagebox.showerror("원본 파일 오류", f"파일이 없습니다.\n\n{input_path}")
            return
        self.trim_input_var.set(str(input_path))
        if not self._load_trim_duration(input_path):
            self.trim_input_var.set("")
            self.trim_output_var.set("")
            return
        try:
            self.trim_output_var.set(str(self._next_trim_output_path(input_path)))
        except RuntimeError as exc:
            self.trim_output_var.set("")
            messagebox.showerror("결과 파일 오류", str(exc))

    def _handle_dropped_trim_files(self, paths: list[Path]) -> None:
        if self.trimmer.is_running:
            return
        if len(paths) != 1:
            messagebox.showwarning("파일 선택", "영상 파일 하나만 놓아주세요.")
            return
        self._load_trim_input(paths[0])

    def _on_trim_drop(self, event) -> str:
        paths = [Path(path) for path in self.root.tk.splitlist(event.data)]
        # Return to Tk's drop handler before the FFmpeg duration probe starts.
        self.root.after_idle(self._handle_dropped_trim_files, paths)
        return COPY

    def _choose_trim_input(self) -> None:
        selected = filedialog.askopenfilename(
            title="자를 원본 영상 선택",
            filetypes=(
                ("영상 파일", "*.mkv *.mp4 *.mov *.avi *.webm"),
                ("모든 파일", "*.*"),
            ),
        )
        if not selected:
            return
        self._load_trim_input(Path(selected))

    def _choose_trim_output(self) -> None:
        current_input = Path(self.trim_input_var.get().strip() or APP_DIR / "video.mkv")
        initial_output = self.trim_output_var.get().strip()
        if initial_output:
            initial_path = Path(initial_output)
        else:
            initial_path = current_input.with_name(f"{current_input.stem}_trimmed.mkv")
        selected = filedialog.asksaveasfilename(
            title="자른 영상 저장 위치",
            initialdir=str(initial_path.parent),
            initialfile=initial_path.name,
            defaultextension=".mkv",
            filetypes=(("MKV 영상", "*.mkv"),),
        )
        if selected:
            self.trim_output_var.set(selected)

    @staticmethod
    def _parse_timecode(value: str) -> float:
        text = value.strip()
        if not text:
            raise ValueError("시간을 입력하세요.")
        parts = text.split(":")
        if len(parts) > 3:
            raise ValueError("시간은 초, 분:초 또는 시:분:초 형식이어야 합니다.")
        try:
            numbers = [float(part) for part in parts]
        except ValueError as exc:
            raise ValueError("시간에는 숫자와 콜론만 사용할 수 있습니다.") from exc
        if any(number < 0 for number in numbers):
            raise ValueError("시간은 음수일 수 없습니다.")
        if len(numbers) >= 2 and numbers[-1] >= 60:
            raise ValueError("초 값은 60보다 작아야 합니다.")
        if len(numbers) == 3 and numbers[-2] >= 60:
            raise ValueError("분 값은 60보다 작아야 합니다.")

        total = 0.0
        for number in numbers:
            total = total * 60 + number
        return total

    def _start_trim(self) -> None:
        if self.recorder.is_running:
            messagebox.showwarning(
                "녹화 중",
                "화면 녹화를 먼저 중지한 후 영상 자르기를 시작하세요.",
            )
            return
        if not FFMPEG_PATH.is_file():
            messagebox.showerror(
                "FFmpeg 없음",
                f"프로그램 폴더에 ffmpeg.exe가 없습니다.\n\n{FFMPEG_PATH}",
            )
            return

        input_text = self.trim_input_var.get().strip()
        output_text = self.trim_output_var.get().strip()
        if not input_text or not output_text:
            messagebox.showwarning(
                "파일 선택 필요",
                "원본 영상과 결과 파일 위치를 모두 지정하세요.",
            )
            return
        input_path = Path(os.path.expandvars(input_text)).expanduser()
        output_path = Path(os.path.expandvars(output_text)).expanduser()
        if not input_path.is_file():
            messagebox.showerror("원본 파일 오류", f"파일이 없습니다.\n\n{input_path}")
            return
        if (
            self.trim_probed_input is None
            or self.trim_probed_input != input_path.resolve()
        ):
            messagebox.showerror(
                "영상 정보 필요",
                "파일 선택 버튼으로 원본 영상을 다시 선택해 전체 길이를 확인하세요.",
            )
            return
        if not output_path.parent.is_dir():
            messagebox.showerror(
                "결과 경로 오류",
                f"저장 폴더가 없습니다.\n\n{output_path.parent}",
            )
            return
        if output_path.suffix.lower() != ".mkv":
            messagebox.showerror(
                "결과 파일 오류",
                "결과 파일의 확장자는 .mkv여야 합니다.",
            )
            return
        if os.path.normcase(str(input_path.resolve())) == os.path.normcase(
            str(output_path.resolve())
        ):
            messagebox.showerror(
                "결과 파일 오류",
                "원본 파일과 다른 결과 파일명을 지정하세요.",
            )
            return

        try:
            start_seconds, end_seconds = self._validated_trim_range()
        except ValueError as exc:
            messagebox.showerror("시간 입력 오류", str(exc))
            return
        self.trim_start_scale_var.set(start_seconds)
        self.trim_end_scale_var.set(end_seconds)
        self.trim_start_var.set(self._format_media_time(start_seconds))
        self.trim_end_var.set(self._format_media_time(end_seconds))
        if output_path.exists() and not messagebox.askyesno(
            "파일 덮어쓰기",
            f"결과 파일이 이미 있습니다. 작업 완료 후 교체할까요?\n\n{output_path}",
        ):
            return

        try:
            self.trimmer.start(
                input_path,
                output_path,
                start_seconds,
                end_seconds - start_seconds,
            )
        except (OSError, RuntimeError) as exc:
            messagebox.showerror("영상 자르기 시작 실패", str(exc))
            return

        self.trim_stop_requested = False
        self.trim_status_var.set(
            f"처리 중 - {self._format_elapsed_time(start_seconds)} → "
            f"{self._format_elapsed_time(end_seconds)}"
        )
        self._set_trim_controls(True)

    def _stop_trim(self) -> None:
        if not self.trimmer.is_running or self.trim_stop_requested:
            return
        self.trim_stop_requested = True
        self.trim_stop_button.configure(state="disabled")
        self.trim_status_var.set("작업 중지 처리 중...")
        self.trimmer.request_stop()

    def _set_trim_controls(self, running: bool) -> None:
        state = "disabled" if running else "normal"
        self.trim_input_entry.configure(state="disabled" if running else "readonly")
        self.trim_input_button.configure(state=state)
        self.trim_start_entry.configure(state=state)
        self.trim_end_entry.configure(state=state)
        scale_state = (
            "normal"
            if not running and self.trim_duration_seconds is not None
            else "disabled"
        )
        self.trim_start_scale.configure(state=scale_state)
        self.trim_end_scale.configure(state=scale_state)
        self.trim_output_entry.configure(state=state)
        self.trim_output_button.configure(state=state)
        self.trim_start_button.configure(state="disabled")
        self.trim_stop_button.configure(state="normal" if running else "disabled")
        self.start_button.configure(state="disabled" if running else "normal")
        if running:
            self.trim_progress.configure(mode="indeterminate")
            self.trim_progress.start(12)
        else:
            self.trim_progress.stop()
            self.trim_progress.configure(mode="determinate", value=0)
            self._update_trim_range_preview()

    @staticmethod
    def _format_elapsed_time(seconds: float) -> str:
        total_seconds = max(0, int(seconds))
        hours, remainder = divmod(total_seconds, 3600)
        minutes, seconds = divmod(remainder, 60)
        return f"{hours:02d}:{minutes:02d}:{seconds:02d}"

    def _update_elapsed_time(self) -> None:
        if self.recording_started_at is None:
            return
        elapsed = time.monotonic() - self.recording_started_at
        self.elapsed_time_var.set(self._format_elapsed_time(elapsed))

    def _selected_monitors(self) -> list[MonitorConfig]:
        return [
            self.monitors[number]
            for number in (1, 2, 3)
            if self.monitor_vars[number].get()
            and self.monitor_available.get(number, False)
        ]

    def _refresh_monitor_availability(self) -> None:
        if self.recorder.is_running:
            return

        detection_error: OSError | None = None
        try:
            active_rectangles = get_active_monitor_rectangles()
        except OSError as exc:
            active_rectangles = set()
            detection_error = exc

        layout = tuple(sorted(active_rectangles, key=lambda rect: (rect[0], rect[1])))
        diagnostic_state = (
            layout,
            str(detection_error) if detection_error is not None else None,
        )
        if diagnostic_state != self._last_monitor_diagnostic:
            print_monitor_diagnostics(active_rectangles, detection_error)
            self._last_monitor_diagnostic = diagnostic_state

        if self._last_monitor_layout is not None and layout != self._last_monitor_layout:
            # A numbered slot may now refer to a different physical display.
            for monitor_var in self.monitor_vars.values():
                monitor_var.set(False)
            self.status_var.set("모니터 배치 변경 - 녹화 화면을 다시 선택하세요.")
        self._last_monitor_layout = layout
        self.monitors = assign_monitor_numbers(active_rectangles)
        detection_failed = detection_error is not None

        for number in (1, 2, 3):
            monitor = self.monitors.get(number)
            available = not detection_failed and monitor is not None
            self.monitor_available[number] = available

            if not available:
                self.monitor_vars[number].set(False)

            if detection_failed:
                state_text = "감지 실패"
            elif available:
                state_text = "활성"
            else:
                state_text = "연결 안 됨"
            label = f"Monitor {number}"
            if monitor is not None:
                label += f"\n{monitor.width}x{monitor.height} · {state_text}"
            else:
                label += f"\n{state_text}"
            self.monitor_label_vars[number].set(label)
            self.monitor_checks[number].configure(
                state="normal" if available else "disabled"
            )

        if detection_failed:
            self.status_var.set("모니터 감지 실패")
        elif self.status_var.get() == "모니터 감지 실패":
            self.status_var.set("대기 중")
        self._update_monitor_visuals()

    def _periodic_monitor_check(self) -> None:
        self._refresh_monitor_availability()
        self.root.after(
            self.MONITOR_CHECK_INTERVAL_MS,
            self._periodic_monitor_check,
        )

    @staticmethod
    def _next_output_path(output_dir: Path) -> Path:
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        base = output_dir / f"ScreenRecord_{timestamp}.mkv"
        if not base.exists():
            return base
        for suffix in range(1, 1000):
            candidate = output_dir / f"ScreenRecord_{timestamp}_{suffix}.mkv"
            if not candidate.exists():
                return candidate
        raise RuntimeError("사용 가능한 출력 파일명을 만들 수 없습니다.")

    def _start_recording(self) -> None:
        if self.trimmer.is_running:
            messagebox.showwarning(
                "영상 처리 중",
                "영상 자르기 작업을 먼저 중지한 후 녹화를 시작하세요.",
            )
            return
        self._refresh_monitor_availability()
        selected = self._selected_monitors()
        if not selected:
            messagebox.showwarning("선택 필요", "녹화할 모니터를 하나 이상 선택하세요.")
            return
        if not FFMPEG_PATH.is_file():
            messagebox.showerror(
                "FFmpeg 없음",
                f"프로그램 폴더에 ffmpeg.exe가 없습니다.\n\n{FFMPEG_PATH}",
            )
            return

        output_dir_text = self.output_dir_var.get().strip()
        if not output_dir_text:
            messagebox.showwarning("경로 필요", "저장 경로를 선택하세요.")
            return
        output_dir = Path(os.path.expandvars(output_dir_text)).expanduser()
        try:
            output_dir.mkdir(parents=True, exist_ok=True)
            if not output_dir.is_dir():
                raise OSError("폴더가 아닙니다.")
            output_path = self._next_output_path(output_dir)
        except (OSError, RuntimeError) as exc:
            messagebox.showerror("저장 경로 오류", str(exc))
            return

        selected_text = "+".join(str(monitor.number) for monitor in selected)
        self.status_var.set("인코더 확인 중...")
        self.root.update_idletasks()
        try:
            encoder = self.recorder.start(selected, output_path)
        except (OSError, RuntimeError, ValueError) as exc:
            self.status_var.set("시작 실패")
            messagebox.showerror("녹화 시작 실패", str(exc))
            return

        self.stop_requested = False
        self.recording_started_at = self.recorder.launched_at or time.monotonic()
        self.recording_wall_seconds = None
        self.elapsed_time_var.set("00:00:00")
        self._set_recording_controls(True)
        self.status_var.set(
            f"녹화 중 - Monitor {selected_text} / {encoder} / {output_path.name}"
        )

    def _stop_recording(self) -> None:
        if not self.recorder.is_running or self.stop_requested:
            return
        self._update_elapsed_time()
        if self.recording_started_at is not None:
            self.recording_wall_seconds = time.monotonic() - self.recording_started_at
        self.recording_started_at = None
        self.stop_requested = True
        self.stop_button.configure(state="disabled")
        self.status_var.set("녹화 종료 처리 중... (MKV 저장 중)")
        self.recorder.request_stop()

    def _set_recording_controls(self, recording: bool) -> None:
        normal_or_disabled = "disabled" if recording else "normal"
        for number, check in self.monitor_checks.items():
            enabled = not recording and self.monitor_available.get(number, False)
            check.configure(state="normal" if enabled else "disabled")
        self.path_entry.configure(state=normal_or_disabled)
        self.browse_button.configure(state=normal_or_disabled)
        self.start_button.configure(state=normal_or_disabled)
        self.stop_button.configure(state="normal" if recording else "disabled")
        self._update_trim_range_preview()
        self._update_monitor_visuals()

    def _poll_ffmpeg(self) -> None:
        self._update_elapsed_time()
        if (
            self.recorder.first_file_at is None
            and self.recorder.output_path is not None
            and self.recorder.output_path.is_file()
        ):
            self.recorder.first_file_at = time.monotonic()
        finished = self.recorder.take_finished_process()
        if finished is not None:
            return_code, output_path = finished
            wall_seconds = self.recording_wall_seconds
            if wall_seconds is None and self.recording_started_at is not None:
                wall_seconds = time.monotonic() - self.recording_started_at
            self.stop_requested = False
            self.recording_started_at = None
            self.recording_wall_seconds = None
            self._set_recording_controls(False)

            if return_code == 0:
                duration = None
                if output_path is not None:
                    try:
                        duration = self.trimmer.probe_duration(output_path)
                    except RuntimeError:
                        pass
                if duration is None:
                    self.status_var.set(
                        f"저장 완료 - {output_path.name if output_path else ''}"
                    )
                else:
                    launched_at = self.recorder.launched_at
                    first_file = self.recorder.first_file_at
                    first_output = self.recorder.first_output_at
                    file_delay = (
                        f"{first_file - launched_at:.3f}s"
                        if launched_at is not None and first_file is not None
                        else "n/a"
                    )
                    output_delay = (
                        f"{first_output - launched_at:.3f}s"
                        if launched_at is not None and first_output is not None
                        else "n/a"
                    )
                    print(
                        f"[RECORD] file={duration:.3f}s "
                        f"wall={wall_seconds if wall_seconds is not None else 0:.3f}s "
                        f"first_file={file_delay} first_output={output_delay} "
                        f"encoder={self.recorder.encoder} fps={self.fps}",
                        flush=True,
                    )
                    if wall_seconds is not None and wall_seconds - duration > 1.0:
                        self.status_var.set(
                            f"저장 완료 - 영상 {self._format_media_time(duration)}, "
                            f"실제 경과 {self._format_media_time(wall_seconds)} "
                            "(처리 지연: FPS를 낮춰 보세요)"
                        )
                    else:
                        self.status_var.set(
                            f"저장 완료 - {output_path.name} "
                            f"({self._format_media_time(duration)})"
                        )
            else:
                self.status_var.set(f"FFmpeg 종료 (오류 코드 {return_code})")
                details = "\n".join(self.recorder.stderr_tail)
                if len(details) > 2500:
                    details = details[-2500:]
                messagebox.showerror(
                    "녹화 오류",
                    "FFmpeg가 오류와 함께 종료되었습니다.\n\n"
                    + (details or "상세 오류 메시지가 없습니다."),
                )

            if self.close_after_stop:
                self._destroy_root()
                return

        if self._poll_trim_job():
            return
        self.root.after(self.POLL_INTERVAL_MS, self._poll_ffmpeg)

    def _poll_trim_job(self) -> bool:
        finished = self.trimmer.take_finished_process()
        if finished is None:
            return False

        return_code, output_path, work_path = finished
        was_stopped = self.trim_stop_requested
        self.trim_stop_requested = False
        self._set_trim_controls(False)

        if was_stopped:
            if work_path is not None:
                try:
                    work_path.unlink(missing_ok=True)
                except OSError:
                    pass
            self.trim_status_var.set("작업이 중지되었습니다.")
        elif return_code == 0 and output_path is not None and work_path is not None:
            try:
                os.replace(work_path, output_path)
            except OSError as exc:
                self.trim_status_var.set("결과 파일 저장 실패")
                messagebox.showerror("결과 파일 저장 실패", str(exc))
            else:
                self.trim_status_var.set(f"저장 완료 - {output_path.name}")
        else:
            if work_path is not None:
                try:
                    work_path.unlink(missing_ok=True)
                except OSError:
                    pass
            self.trim_status_var.set(f"FFmpeg 종료 (오류 코드 {return_code})")
            details = "\n".join(self.trimmer.stderr_tail)
            if len(details) > 2500:
                details = details[-2500:]
            messagebox.showerror(
                "영상 자르기 오류",
                "FFmpeg가 오류와 함께 종료되었습니다.\n\n"
                + (details or "상세 오류 메시지가 없습니다."),
            )

        if self.close_after_stop:
            self._destroy_root()
            return True
        return False

    def _destroy_root(self) -> None:
        self.root.destroy()

    def _on_close(self) -> None:
        if not self.recorder.is_running and not self.trimmer.is_running:
            self._destroy_root()
            return
        if not messagebox.askyesno(
            "작업 종료",
            "진행 중인 작업을 정상 종료하고 프로그램을 닫을까요?",
        ):
            return
        self.close_after_stop = True
        if self.recorder.is_running:
            self._stop_recording()
        if self.trimmer.is_running:
            self._stop_trim()


def main() -> None:
    enable_per_monitor_dpi_awareness()
    if "--diagnose" in sys.argv[1:]:
        try:
            load_config(CONFIG_PATH)
            active_rectangles = get_active_monitor_rectangles()
        except ValueError as exc:
            print(f"[MONITOR] Configuration error: {ascii(str(exc))}", flush=True)
            raise SystemExit(1) from exc
        except OSError as exc:
            print_monitor_diagnostics(set(), exc)
            raise SystemExit(1) from exc
        print_monitor_diagnostics(active_rectangles)
        return
    root = tk.Tk()
    try:
        ScreenRecorderApp(root)
    except ValueError as exc:
        root.withdraw()
        messagebox.showerror("설정 오류", str(exc))
        root.destroy()
        return
    root.mainloop()


if __name__ == "__main__":
    main()
