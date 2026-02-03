import logging
import subprocess


class Game():
    def __init__(self, exe_path: str) -> None:
        self.exe_path = exe_path
        self.process: subprocess.Popen[bytes] | None = None

    def run(self, force: bool = False) -> None:
        """Run the game."""
        if self.process is not None and not force:
            logging.error("Game is already running.")
            return
        self.process = subprocess.Popen(self.exe_path, creationflags=subprocess.CREATE_NO_WINDOW)
        logging.info(f"Game started with PID {self.process.pid}.")

    def kill(self) -> None:
        """Kill the game process."""
        if self.process is None:
            return
        try:
            self.process.terminate()
            try:
                self.process.wait(timeout=5)
                logging.info(f"Game with PID {self.process.pid} terminated.")
            except subprocess.TimeoutExpired:
                self.process.kill()
                logging.warning(f"Game with PID {self.process.pid} killed.")
        except PermissionError:
            logging.warning("No permission to kill the game process.")
        finally:
            self.process = None

    def is_running(self) -> bool:
        """Check if the game is running."""
        if self.process is None:
            return False
        return self.process.poll() is None

    def wait(self) -> None:
        """Wait for the game process to finish."""
        if self.process is not None:
            self.process.wait()

    def __enter__(self) -> "Game":
        return self

    def __exit__(self, _, __, ___) -> None:  # type: ignore
        self.wait()
