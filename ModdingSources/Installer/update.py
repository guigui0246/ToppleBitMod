import sys
import os
import time
import subprocess
import shutil


def wait_for_launcher_to_close(launcher_path: str, timeout: float = 30):
    """
    Wait until the launcher exe is no longer locked by Windows.
    """
    start = time.time()
    while True:
        try:
            with open(launcher_path, "rb"):
                return
        except PermissionError:
            if time.time() - start > timeout:
                raise RuntimeError("Timed out waiting for launcher to close")
            time.sleep(0.5)


def main():
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <install_folder>")
        sys.exit(1)

    install_dir = os.path.abspath(sys.argv[1])

    launcher = os.path.join(install_dir, "ToppleBitModdingLauncher.exe")
    new_launcher = os.path.join(install_dir, "ToppleBitModdingLauncher.new.exe")

    if not os.path.exists(new_launcher):
        raise FileNotFoundError("ToppleBitModdingLauncher.new.exe not found")

    # Wait for launcher process to exit and file lock to release
    wait_for_launcher_to_close(launcher)

    # Replace launcher
    backup = launcher + ".bak"
    if os.path.exists(backup):
        os.remove(backup)

    if os.path.exists(launcher):
        os.rename(launcher, backup)

    shutil.move(new_launcher, launcher)

    # Relaunch
    subprocess.Popen(
        [launcher],
        cwd=install_dir,
        close_fds=True,
        shell=False,
    )


if __name__ == "__main__":
    main()
